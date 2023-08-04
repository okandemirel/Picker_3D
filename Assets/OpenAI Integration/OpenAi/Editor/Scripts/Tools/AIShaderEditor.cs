using OpenAi.Api.V1;
using OpenAi.Unity.V1;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace OpenAi.Examples
{
    [CustomEditor(typeof(AIShader))]
    sealed class AIShaderEditor : ScriptedImporterEditor
    {
        #region Private members

        SerializedProperty _prompt;
        SerializedProperty _cached;
        Vector2 _scrollPosition;
        private string model = "gpt-3.5-turbo";

        static string instructions = "Create a shader for Unity. Don't include any note nor explanation in the response. I only need the code body, do not include anything in your response that isn't pure code.";

        async void Regenerate(string promptValue)
        {
            _cached.stringValue = await InvokeChatAsync(promptValue);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region AIShaderEditor overrides

        public override void OnEnable()
        {
            base.OnEnable();
            _prompt = serializedObject.FindProperty("_prompt");
            _cached = serializedObject.FindProperty("_cached");
        }

        public override void OnInspectorGUI()
        {
            // Intro
            serializedObject.Update();

            // Prompt text area
            EditorGUILayout.PropertyField(_prompt);
            model = EditorGUILayout.TextField("Model", model);
            // "Generate" button
            if (GUILayout.Button("Generate"))
            {
                // Extract the string value from the SerializedProperty object
                string promptValue = _prompt.stringValue;
                Regenerate(promptValue);
            }

            // Cached code text area
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_cached);

            // Outro
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        #endregion

        #region Chat request

        public async Task<string> InvokeChatAsync(string message)
        {
            Debug.Log("Sending prompt to AI, please wait...");
            SOAuthArgsV1 auth = AssetDatabase.LoadAssetAtPath<SOAuthArgsV1>("Assets/GPT AI Integration/OpenAi/Runtime/Config/DefaultAuthArgsV1.asset");
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());
            ApiResult<ChatCompletionV1> result = await api.ChatCompletions
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
            if (result.IsSuccess)
            {
                string content = result.Result.choices[0].message.content;

                // Find the positions of the first and second occurrences of ```
                int firstOccurrence = content.IndexOf("```");
                int secondOccurrence = content.LastIndexOf("```");

                if (firstOccurrence != -1 && secondOccurrence != -1 && secondOccurrence > firstOccurrence)
                {
                    // Extract the content between the first and second occurrences of ```
                    content = content.Substring(firstOccurrence + 3, secondOccurrence - firstOccurrence - 3);
                }

                // Replace the escape sequences with their actual characters
                string formattedContent = content
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .Replace("\\t", "\t");

                Debug.Log("Response received: " + formattedContent);
                return formattedContent;
            }
            else
            {
                Debug.Log("OpenAI request failed: " + result.HttpResponse.error);
                return $"ERROR: StatusCode={result.HttpResponse.responseCode} - {result.HttpResponse.error}";
            }
        }

        #endregion
    }
}