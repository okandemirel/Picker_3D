using OpenAi.Api.V1;
using OpenAi.Unity.V1;

using System.Threading.Tasks;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace OpenAi.Examples
{   
    public class ChatGPT : EditorWindow
    {
        private List<ChatMessageV1> messages = new List<ChatMessageV1>(); // Used as the chat history for context. If it exceeds the amount of tokens allowed, you'll get an error.
        private string _input = "Write your request here. For example:" + Environment.NewLine + "\"Explain what a Scriptable Object is and how to use it\" or" + Environment.NewLine + "\"Write a C# script that rotates a gameObject counter-clockwise over time\".";
        Vector2 scrollPos = Vector2.zero;
        private string _output = "";
        // Prints the words to the UI as soon as they're generated instead of waiting for the full response (faster)
        private bool _wordStreaming = true;
        public event Action WindowClosed;
        public delegate void UIUpdateAction();
        public event UIUpdateAction OnUIUpdate;
        private Task _currentTask;
         // Add a new field for the current chat log file name
        private string _currentChatLogFileName;

        // Keep track of tokens and conversation cost
        private int _totalTokensUsed = 0;
        private float _totalConversationCost = 0.0f;

        private bool _showSettings = false;
        [Range(1, 32768), Tooltip("The maximum number of tokens to generate. Requests can use up to 4000 tokens shared between prompt and completion. (One token is roughly 4 characters for normal English text)")]
        public int max_tokens = 2048;
        [Range(0.0f, 1.0f), Tooltip("Controls randomness: Lowering results in less random completions. As the temperature approaches zero, the model will become deterministic and repetitive.")]
        public float temperature = 0.2f;
        [Range(0.0f, 1.0f), Tooltip("Controls diversity via nucleus sampling: 0.5 means half of all likelihood-weighted options are considered.")]
        public float top_p = 0.8f;
        [Tooltip("Where the API will stop generating further tokens. The returned text will not contain the stop sequence.")]
        public string stop;
        [Range(0.0f, 2.0f), Tooltip("How much to penalize new tokens based on their existing frequency in the text so far. Decreases the model's likelihood to repeat the same line verbatim.")]
        public float frequency_penalty = 0;
        [Range(0.0f, 2.0f), Tooltip("How much to penalize new tokens based on whether they appear in the text so far. Increases the model's likelihood to talk about new topics.")]
        public float presences_penalty = 0;
        [Tooltip("Prompts the AI to only respond with code. Use this if you plan on saving the output to a file.")]
        private bool _outputCodeOnly = false;
        [Tooltip("Prompts the AI to add comments to the code it writes to clarify what it's doing.")]
        private bool _addCommentsToCode = false;
        [Tooltip("The model to use for completion. See https://beta.openai.com/docs/models for a list of available models.")]
        private string model = "gpt-3.5-turbo";
        private string instructions = "I want you to act as a senior Unity game developer. Your task is to assist me in the development of a game using the Unity game engine. You will be responsible for providing assistance with game design, coding, and debugging. You should have experience in developing games with Unity, and be able to create a game with a high level of quality using the best clean coding practices. Here is your first task: ";
        private bool _showInstructionPrompt;
        private bool _saveMessageToMemory = true; // Only adds messages and resposnes to the AI's memory if this is true.

        [MenuItem("Tools/OpenAi/ChatGPT")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ChatGPT));
            var window = GetWindow<ChatGPT>();
            window.Show();

            window.wantsMouseMove = true;
            window.autoRepaintOnSceneChange = true;

            EditorApplication.update += window.EditorUpdate;
            window.WindowClosed += () => EditorApplication.update -= window.EditorUpdate;

        }

        private void EditorUpdate()
        {
            Repaint();
            OnUIUpdate?.Invoke();
        }

        void OnDestroy()
        {
            WindowClosed?.Invoke();
        }

        // Scroll position
            Vector2 scrollPosition = Vector2.zero;
        void OnGUI()
        {
            // Update GUI on main thread
            
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Code:", MonoScript.FromScriptableObject(this), typeof(ScriptableObject), false);
            GUI.enabled = true;

            SOAuthArgsV1 auth = null;

            string assetName = "DefaultAuthArgsV1";
            string[] guids = AssetDatabase.FindAssets($"t:SOAuthArgsV1 {assetName}");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                auth = AssetDatabase.LoadAssetAtPath<SOAuthArgsV1>(assetPath);
            }
            else
            {
                Debug.LogError($"Could not find asset with name '{assetName}' and type 'SOAuthArgsV1'");
            }
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());

            GUIStyle chatGPTStyle = new GUIStyle(EditorStyles.textArea);
            chatGPTStyle.fontSize = 16;
            chatGPTStyle.wordWrap = true;
            chatGPTStyle.normal.textColor = new Color(0.737f, 0.749f, 0.780f, 1f);
            chatGPTStyle.padding = new RectOffset(10, 10, 10, 10);
            chatGPTStyle.margin = new RectOffset(0, 0, 5, 0);
            chatGPTStyle.normal.background = MakeTex(1, 1, new Color(0.267f, 0.271f, 0.325f, 1f));

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
                _output = EditorGUILayout.TextArea(_output.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t"), chatGPTStyle, GUILayout.ExpandHeight(true));
                _output = Regex.Replace(_output, @"`{3}", "");
                EditorGUILayout.EndScrollView();
            
            // Input style
            GUIStyle inputStyle = new GUIStyle(EditorStyles.textArea);
            inputStyle.wordWrap = true;
            inputStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            inputStyle.normal.background = MakeTex(1, 1, new Color(0.204f, 0.208f, 0.255f, 1f));

            

            // Begin ScrollView
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(80), GUILayout.MaxHeight(80));

            // Input field
            string previousInput = _input;
            _input = EditorGUILayout.TextArea(_input, inputStyle, GUILayout.ExpandHeight(true));

            // End ScrollView
            EditorGUILayout.EndScrollView();

            // Check if the input has changed and update the token count if needed
            if (previousInput != _input)
            {
                // Force a repaint of the editor window
                Repaint();
            }

            _saveMessageToMemory = EditorGUILayout.Toggle("Save message to memory", _saveMessageToMemory);

            if (api != null && GUILayout.Button("Send"))
            {
                #pragma warning disable CS4014 // we don't need to await this, so we're just disbaling the warning message
                DoEditorTask(api);
                #pragma warning restore CS4014
            }
            
            // Calculate total tokens in messages array and update the token count
            int totalMessageTokens = CalculateTotalMessageTokens();
            int tokenCount = CountTokens(_input) + totalMessageTokens;
            EditorGUILayout.LabelField($"Estimated token cost: {tokenCount}");

            // Display the total tokens used below the input field

            EditorGUILayout.LabelField($"Estimated total tokens used: {_totalTokensUsed + totalMessageTokens}");

            // Calculate and display the total conversation cost
            _totalConversationCost = CalculateConversationCost(_totalTokensUsed, model);
            EditorGUILayout.LabelField($"Total conversation cost: ${_totalConversationCost:F5}");

            

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Load Chat Log"))
            {
                string chatHistoryFolderPath = "Assets/Chat Logs";
                string filePath = EditorUtility.OpenFilePanel("Load Chat Log", chatHistoryFolderPath, "json");
                if (!string.IsNullOrEmpty(filePath))
                {
                    LoadChatLog(filePath);
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Save as C# file"))
            {
                // Use a regular expression to extract the class name from the output string
                Regex regex = new Regex(@"class\s+(\w+)");
                Match match = regex.Match(_output);
                string className = match.Groups[1].Value;

                // Prompt the user to enter a file name
                string defaultName = className + ".cs";
                string filePath = EditorUtility.SaveFilePanel("Save as C# file", "Assets/Scripts", defaultName, "cs");
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                // Create the Scripts folder if it doesn't exist
                string folderPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Create the C# file
                File.WriteAllText(filePath, _output);

                // Refresh the AssetDatabase to show the new file
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("Settings"))
            {
                _showSettings = !_showSettings;
            }

            if (_showSettings)
            {
                // See https://beta.openai.com/docs/models for a list of available models.
                model = EditorGUILayout.TextField("Model", model);
                // If true, the API will stream words to the client as they become available. This is useful for long-running requests, such as generating a large amount of text. If false, the API will wait until the request is complete before returning a response.
                _wordStreaming = EditorGUILayout.Toggle("Word streaming", _wordStreaming);
                // The maximum number of tokens to return. The API will return fewer tokens if it hits an early termination condition. Token use includes your input + the generated response. If memory is enabled, all previous messages will be included in the token count.
                max_tokens = EditorGUILayout.IntSlider("Max Tokens", max_tokens, 1, 4000);
                // Temperature controls the randomness of the generated text. Lower values will make the text more predictable and more likely to match the input. Higher values will make the text more surprising and more likely to be novel. Try 0.3 for more predictable text and 0.9 for more surprising text.
                temperature = EditorGUILayout.Slider("Temperature", temperature, 0.0f, 1.0f);
                // The probability of sampling a given token. This is a number between 0 and 1. Lower values will make the model more conservative and less likely to generate novel text. Higher values will make the model more creative and more likely to generate novel text. Try 0.1 for more conservative text and 0.9 for more creative text.
                top_p = EditorGUILayout.Slider("Top P", top_p, 0.0f, 1.0f);
                // stop is a string that will be appended to the end of the generated text. This is useful for preventing the model from generating text that goes on forever. The model will stop generating text once it reaches the stop string. If the stop string is not found, the model will stop generating text once it reaches the max_tokens limit.
                stop = EditorGUILayout.TextField("Stop Sequence", stop);
                // Frequency penalty is a number between 0 and 1 that penalizes new tokens based on their existing frequency in the text so far. A value of 0 means no penalty. A value of 1 means the model is only allowed to use each token once. This can be useful for preventing the model from repeating itself.
                frequency_penalty = EditorGUILayout.Slider("Frequency Penalty", frequency_penalty, 0.0f, 2.0f);
                // Presence penalty is a number between 0 and 1 that penalizes new tokens based on whether they appear in the text so far. A value of 0 means no penalty. A value of 1 means the model is only allowed to use each token once. This can be useful for preventing the model from repeating itself.
                presences_penalty = EditorGUILayout.Slider("Presences Penalty", presences_penalty, 0.0f, 2.0f);
                // Output Code Only will only output the code and not the entire class. Recommended when using the Save as C# file option.
                _outputCodeOnly = EditorGUILayout.Toggle("Output Code Only", _outputCodeOnly);
                if (_outputCodeOnly)
                {
                    _addCommentsToCode = EditorGUILayout.Toggle("Add Comments to Code", _addCommentsToCode);
                } else {
                    temperature = 0.2f;
                    top_p = 0.8f;
                }
                if (GUILayout.Button("Instruction Prompt"))
                {
                    // Toggle the visibility of the TextArea
                    _showInstructionPrompt = !_showInstructionPrompt;
                }

                if (_showInstructionPrompt)
                {
                    // Display the TextArea
                    instructions = EditorGUILayout.TextArea(instructions, EditorStyles.textArea, GUILayout.MinHeight(80));
                }
            }
        }

        private async Task DoEditorTask(OpenAiApiV1 api)
        {
            _output = "AI is thinking...";

            if (_outputCodeOnly)
            {
                if (_addCommentsToCode)
                {
                    _input = _input + ". Only respond with Unity code in your response, no additional text or context, Also add comments to the code:";
                }
                else
                {
                    _input = _input + ". Only respond with Unity code in your response, no additional text or context:";
                }
                instructions = "// " + instructions;
                temperature = 0.0f;
                top_p = 1.0f;
            }

            ApiResult<CompletionV1> comp = null;
            ApiResult<ChatCompletionV1> chatComp = null;
            Debug.Log("Sending OpenAI request...");
            // The GPT models use a different format of request than others like Davinci, so if we're using a GPT model we need to call a different type of request
            if (!model.Contains("gpt"))
            {
                // Use this method when making a request to models other than GPT
                comp = await api.Engines.Engine(model).Completions.CreateCompletionAsync(
                    new CompletionRequestV1()
                    {
                        prompt = instructions + _input,
                        max_tokens = max_tokens,
                        temperature = temperature,
                        top_p = top_p,
                        stop = stop,
                        frequency_penalty = frequency_penalty,
                        presence_penalty = presences_penalty

                    }
                );
            } else if (_wordStreaming){

                // Add the user's message to the existing messages list
                messages.Add(new ChatMessageV1() { role = "user", content = _input });

                // Check if the total tokens in the messages array exceed max_tokens
                while (CalculateTotalMessageTokens() > max_tokens)
                {
                    // Remove the first item in the messages array
                    messages.RemoveAt(0);
                }
                
                ChatCompletionRequestV1 request = new ChatCompletionRequestV1()
                {
                    model = model,
                    stream = true,
                    max_tokens = max_tokens,
                    temperature = temperature,
                    top_p = top_p,
                    stop = stop,
                    frequency_penalty = frequency_penalty,
                    presence_penalty = presences_penalty,
                    messages = messages.ToArray() // Pass the chat log to the API request
                };

                await api.ChatCompletions.CreateChatCompletionAsync_EventStream(
                request,
                onRequestStatus: (status) =>
                {
                    if (!status.IsSuccess)
                    {
                        _output = $"ERROR: StatusCode={status.HttpResponse.responseCode} - {status.HttpResponse.error}";
                    }
                },
                onPartialResult: (index, result) =>
                {
                    ChatCompletionV1 chatCompletion = result as ChatCompletionV1;
                    if (chatCompletion != null && !string.IsNullOrEmpty(chatCompletion.content))
                    {
                        if (_output == "AI is thinking...")
                        {
                            _output = "";
                        }
                        string response = chatCompletion.content;
                        _output += response;

                        // Call HandleUIUpdate to update the editor window
                        HandleUIUpdate();
                    }
                    else
                    {
                        Debug.LogError("Invalid result object or missing content");
                    }
                },
                onCompletion: () =>
                {
                    Debug.Log("Response stream complete.");
                    // Add the assistant's response to the messages list only if _saveMessageToMemory is true
                    if (_saveMessageToMemory)
                    {
                        messages.Add(new ChatMessageV1() { role = "assistant", content = _output });
                        // Save the chat log after a new message is sent or received
                        SaveChatLog();
                    }

                    // Remove the users message from the chat log if _saveMessageToMemory is false
                    if (!_saveMessageToMemory)
                    {
                            messages.RemoveAt(messages.Count - 1); // Remove the last item
                    }

                    // Update the total tokens used
                    _totalTokensUsed = CalculateTotalMessageTokens();
                }
                );
            } else {
                chatComp = await SendChatGPTRequest(_input);
            }


            _input = "Enter your next message";
            if ((comp?.IsSuccess ?? false) || (chatComp?.IsSuccess ?? false))
            {
                // Debug logs showing the response from the API
                if (!model.Contains("gpt")){
                Debug.Log($"{comp.Result.choices[0].text}");
                } else if (!_wordStreaming){
                Debug.Log($"{chatComp.Result.choices[0].message.content}");
                }

                if (_outputCodeOnly)
                {
                    if (!model.Contains("gpt")){
                    _output = "\nusing UnityEngine;" + $"{comp.Result.choices[0].text}";
                    } else {
                    _output = $"{chatComp.Result.choices[0].message.content}";
                    }
                }
                else
                {
                    if (!model.Contains("gpt")){
                    _output = $"{comp.Result.choices[0].text}";
                    } else  if (!_wordStreaming){
                    _output = $"{chatComp.Result.choices[0].message.content}";
                    }
                }

                
            }
            else 
            {
                string errorMessage = $"ERROR: StatusCode={chatComp.HttpResponse.responseCode} - {chatComp.HttpResponse.error}";
                if (errorMessage.Contains("Unauthorized"))
                {
                    errorMessage += "\nThis error means your API key is incorrect or improperly setup. Make sure there are no <> symbols in your API key and you've followed the steps in the included ReadMe file.";
                }
                else if (errorMessage.Contains("Too Many Requests"))
                {
                    errorMessage += "\nThis error likely means your OpenAI account has no billing information setup. Verify you've setup billing info and haven't hit any account limits and try again. If that doesn't work, try increasing your 'Max Tokens' setting to a higher value.";
                }

                _output = errorMessage;
            }
        }

        // Create a method to save the chat log to a JSON file
        private void SaveChatLog()
        {
            if (string.IsNullOrEmpty(_currentChatLogFileName))
            {
                _currentChatLogFileName = $"ChatLog_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json";
            }

            string chatHistoryFolderPath = "Assets/Chat Logs";
            if (!Directory.Exists(chatHistoryFolderPath))
            {
                Directory.CreateDirectory(chatHistoryFolderPath);
            }

            string filePath = Path.Combine(chatHistoryFolderPath, _currentChatLogFileName);
            string json = JsonUtility.ToJson(new ChatLog(messages), true); // Set prettyPrint to true
            File.WriteAllText(filePath, json);
            AssetDatabase.Refresh();
        }

        // Create a method to load the chat log from a JSON file
        private void LoadChatLog(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                ChatLog chatLog = JsonUtility.FromJson<ChatLog>(json);
                messages = chatLog.messages;
                _currentChatLogFileName = Path.GetFileName(filePath);
            }
            else
            {
                Debug.LogError($"Chat log file not found: {filePath}");
            }
        }

        // Use this method when making a request to the GPT3.5 or GPT4 models
        public async Task<ApiResult<ChatCompletionV1>> SendChatGPTRequest(string message)
        {
 
            SOAuthArgsV1 auth = null;

            string assetName = "DefaultAuthArgsV1";
            string[] guids = AssetDatabase.FindAssets($"t:SOAuthArgsV1 {assetName}");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                auth = AssetDatabase.LoadAssetAtPath<SOAuthArgsV1>(assetPath);
            }
            else
            {
                Debug.LogError($"Could not find asset with name '{assetName}' and type 'SOAuthArgsV1'");
            }
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());

            // Add the user's message to the existing messages list
            messages.Add(new ChatMessageV1() { role = "user", content = message });

            // Check if the total tokens in the messages array exceed max_tokens
            while (CalculateTotalMessageTokens() > max_tokens)
            {
                // Remove the first item in the messages array
                messages.RemoveAt(0);
            }

            // Send OpenAI API request
            ApiResult<ChatCompletionV1> chatComp = await api.ChatCompletions
                .CreateChatCompletionAsync(
                    new ChatCompletionRequestV1()
                    {
                        model = model,
                        max_tokens = max_tokens,
                        temperature = temperature,
                        top_p = top_p,
                        stop = stop,
                        frequency_penalty = frequency_penalty,
                        presence_penalty = presences_penalty,
                        messages = messages.ToArray() // Pass the chat log to the API request
                    });

            // Add the assistant's response to the messages list only if _saveMessageToMemory is true
            if (_saveMessageToMemory)
            {
                messages.Add(new ChatMessageV1() { role = "assistant", content = chatComp.Result.choices[0].message.content });
            }

            // Remove the users message from the chat log if _saveMessageToMemory is false
            if (!_saveMessageToMemory)
            {
                    messages.RemoveAt(messages.Count - 1); // Remove the last item
            }

            // Update the total tokens used
            _totalTokensUsed = CalculateTotalMessageTokens();

            return chatComp;
        }

        // Create a solid color texture
        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = color;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private int CountTokens(string text)
        {
            // You can use OpenAI's tiktoken Python library to get a precise token count,
            // but for simplicity, we'll use an approximation (1 token ≈ 4 characters)
            return text.Length / 4;
        }

        // Calculates the conversation cost
        private float CalculateConversationCost(int tokens, string model)
        {
            float costPerToken = 0.03f; // Default cost per token in USD

            if (model.Contains("gpt-3"))
            {
                costPerToken = 0.002f;
            }
            else if (model.Contains("gpt-4-32k"))
            {
                costPerToken = 0.06f;
            }

            return (tokens / 1000.0f) * costPerToken;
        }

        private int CalculateTotalMessageTokens()
        {
            // If chat log is empty, add the system instructions message
            if (messages.Count == 0)
            {
                messages.Add(new ChatMessageV1() { role = "system", content = instructions });
            }
            
            int totalTokens = 0;

            foreach (ChatMessageV1 message in messages)
            {
                totalTokens += CountTokens(message.content);
            }

            return totalTokens;
        }

        private void ForceUIUpdate()
        {
            EditorApplication.delayCall += UpdateUIInMainThread;
        }
        private void HandleUIUpdate()
        {
            Repaint();
            OnUIUpdate -= HandleUIUpdate;
        }

        private void UpdateUIInMainThread()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }
            Repaint();
        }

        
    }

    // Create a ChatLog class to store the messages in a JSON format
    [System.Serializable]
    public class ChatLog
    {
        [SerializeField] public List<ChatMessageV1> messages;

        public ChatLog(List<ChatMessageV1> messages)
        {
            this.messages = messages;
        }
    }

}