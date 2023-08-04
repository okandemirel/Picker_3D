using OpenAi.Api.V1;
using OpenAi.Unity.V1;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace OpenAi.Examples
{
    public class PromptGenerator : EditorWindow
    {
        private string _input = "Senior Unity Developer";
        Vector2 scrollPos = Vector2.zero;
        private string _output = "";
        private Task _currentTask;
        public event Action WindowClosed;
        public delegate void UIUpdateAction();
        public event UIUpdateAction OnUIUpdate;

        public int max_tokens = 1024;
        public float temperature = 0.7f;
        public float top_p = 1.0f;
        public string stop;
        public float frequency_penalty = 0;
        public float presences_penalty = 0;
        public string model = "gpt-3.5-turbo";
        private string instructions = "I want you to act as a prompt generator. Firstly, I will give you a title like this: “Act as a Senior Unity Game Developer”. The output I expect from you should look like what I put here in quotes: “I want you to act as a senior Unity game developer. Your task is to assist me in the development of a game using the Unity game engine. You will be responsible for providing assistance with game design, coding, and debugging. You should have experience in developing games with Unity, and be able to create a game with a high level of quality using the best clean coding practices. Here is your first task: ” (You should adapt the sample prompt according to the title I give. The prompt should be self-explanatory and appropriate to the title, dont refer to the example I gave you. in the quotes, only make a response structred in a similar way, based off of the title I give you). ";
        private bool _showInstructionPrompt;

        [MenuItem("Tools/OpenAi/Prompt Generator")]

        public static void ShowWindow()
        {
            var window = GetWindow<PromptGenerator>();
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

        void OnGUI()
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

            // Always display the output field
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            _output = EditorGUILayout.TextArea(_output.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t"), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Act as a... ");
            _input = EditorGUILayout.TextField(_input, EditorStyles.textField);
            EditorStyles.textField.wordWrap = true;

            if (api != null && GUILayout.Button("Generate Instruction Prompt"))
            {
                if (_currentTask == null || _currentTask.IsCompleted)
                {
                    _currentTask = DoEditorTask(api);
                }
            }
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

        private async Task DoEditorTask(OpenAiApiV1 api)
        {
            _output = "Generating your prompt...";

            ChatCompletionRequestV1 request = new ChatCompletionRequestV1()
            {
                model = "gpt-3.5-turbo",
                stream = true,
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
                        content = _input
                    }
                }
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
                // Assuming result is of type ChatCompletionV1
                ChatCompletionV1 chatCompletion = result as ChatCompletionV1;
                if (chatCompletion != null && !string.IsNullOrEmpty(chatCompletion.content))
                {
                    if (_output == "Generating your prompt...")
                    {
                        _output = "";
                    }
                    string content = chatCompletion.content;
                    _output += content;

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
                _output += " Respond only as if you were this character.";
                Debug.Log("onCompletion");
            }
        );
        }}}
    