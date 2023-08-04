using OpenAi.Api.V1;
using OpenAi.Unity.V1;

using System.Threading.Tasks;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace OpenAi.Examples
{
    public class ScriptEditor : EditorWindow
    {
        private string _input = "Explain the issues you're having with this script.";
        Vector2 scrollPos = Vector2.zero;
        private string _output;
        private string recommendations;

        public int max_tokens = 2048;
        public float temperature = 0.2f;
        public float top_p = 0.8f;
        public string stop;
        public float frequency_penalty = 0;
        public float presences_penalty = 0;
        // By default, we're using the GPT3.5 model
        public string model = "gpt-3.5-turbo";
        private string instructions = "I want you to act as a senior Unity game developer. Your task is to assist me in the development of a game using the Unity game engine. You will be responsible for providing assistance with game design, coding, and debugging. You should have experience in developing games with Unity, and be able to create a game with a high level of quality using the best clean coding practices. Your first task is to identify any possible bugs and provide solutions in the script I provide you. If you have any recommendations, include the code for them in your response. My request is: ";
        private string scriptFilePath;
        private string scriptText;
        private MonoScript selectedObject;
        private bool showButton;

        [MenuItem("Tools/OpenAi/Script Editor")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ScriptEditor));
        }

        async void OnGUI()
        {
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

            if (!string.IsNullOrEmpty(_output))
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
                _output = EditorGUILayout.TextArea(_output.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t"), chatGPTStyle, GUILayout.ExpandHeight(true));
                _output = Regex.Replace(_output, @"`{3}", "");
                EditorGUILayout.EndScrollView();                
            }
            
            selectedObject = (MonoScript)EditorGUILayout.ObjectField("File", selectedObject, typeof(MonoScript), false);

            if (selectedObject != null)
            {
                scriptFilePath = AssetDatabase.GetAssetPath(selectedObject);
            }

            EditorGUILayout.LabelField("Bug description: ");
            _input = EditorGUILayout.TextArea(_input, EditorStyles.textField, GUILayout.MinHeight(80));
            EditorStyles.textField.wordWrap = true;

            if (api != null && GUILayout.Button("Identify issues"))
            {
                await DoEditorTask(api);
            }

            if (showButton)
            {
                if (GUILayout.Button("Apply recommendations"))
                {
                    // Send another request with the recommendatiosn and instruct it to rewrite the script with the recommendations in only code
                    recommendations = _output;
                    await UpdateEditorFile(api);
                }
            }

        }

        private async Task DoEditorTask(OpenAiApiV1 api)
        {
            _output = "Analyzing script...";
            TextAsset scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptFilePath);
            scriptText = scriptAsset.text;
            ApiResult<ChatCompletionV1> comp = null;
            comp = await SendChatGPTRequest(instructions + _input + "Here is the script: \n" + scriptText);
            if (comp.IsSuccess)
            {
                _output = $"{comp.Result.choices[0].message.content}";
                showButton = true;
            }
            else
            {
                _output = $"ERROR: StatusCode={comp.HttpResponse.responseCode} - {comp.HttpResponse.error}";
            }
        }

        private async Task UpdateEditorFile(OpenAiApiV1 api)
        {
            //_output = "Applying changes...";
            TextAsset scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptFilePath);
            scriptText = scriptAsset.text;
            ApiResult<ChatCompletionV1> comp = null;
            comp = await SendChatGPTRequest("Original script:\n" + scriptText + "\n" + "Instructions: Rewrite this entire script based on the following recommendations: '" + recommendations + ".' Only respond with Unity code in your response, no additional text or context:\n");
            _input = "Explain the issues you're having with this script.";
            if (comp.IsSuccess)
            {
                _output = $"{comp.Result.choices[0].message.content}";
                showButton = false;
                _output = _output.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t");
                Debug.Log("Rewriting file with the following code: " + _output);
                File.WriteAllText(scriptFilePath, _output);
                _output = "Rewritten code:\n" + _output;
                AssetDatabase.Refresh();
            }
            else
            {
                _output = $"ERROR: StatusCode={comp.HttpResponse.responseCode} - {comp.HttpResponse.error}";
            }
        }

        public async Task<ApiResult<ChatCompletionV1>> SendChatGPTRequest(string message)
        {
            SOAuthArgsV1 auth = AssetDatabase.LoadAssetAtPath<SOAuthArgsV1>("Assets/GPT AI Integration/OpenAi/Runtime/Config/DefaultAuthArgsV1.asset");
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());
            ApiResult<ChatCompletionV1> comp = await api.ChatCompletions
                .CreateChatCompletionAsync(
                    new ChatCompletionRequestV1()
                    {
                        model = model,
                        messages = new[]
                        {
                            new ChatMessageV1()
                            {
                                role = "system",
                                content = instructions
                            },
                            new ChatMessageV1()
                            {
                                role = "user",
                                content = message
                            }
                        }
                    });

            return comp;
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
    }
}