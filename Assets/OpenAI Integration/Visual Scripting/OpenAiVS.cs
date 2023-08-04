using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.VisualScripting;
using OpenAi.Api.V1;
using OpenAi.Unity.V1;
using System.IO;
using System.Linq;

namespace OpenAI.VisualScripting
{
    public class OpenAiVS : MonoBehaviour
    {
        [SerializeField]
        public OpenAiCompleterV1 completer;
  
        [
            Range(1, 4000),
            Tooltip(
                "The maximum number of tokens to generate. Requests can use up to 4000 tokens shared between prompt and completion. (One token is roughly 4 characters for normal English text)"
            )
        ]
        public int max_tokens = 256;
        [
            Range(0.0f, 1.0f),
            Tooltip(
                "Controls randomness: Lowering results in less random completions. As the temperature approaches zero, the model will become deterministic and repetitive."
            )
        ]
        public float temperature = 0.7f;
        [
            Range(0.0f, 1.0f),
            Tooltip(
                "Controls diversity via nucleus sampling: 0.5 means half of all likelihood-weighted options are considered."
            )
        ]
        public float top_p = 1;
        [Tooltip(
            "Where the API will stop generating further tokens. The returned text will not contain the stop sequence."
        )]
        public string stop;
        [
            Range(0.0f, 2.0f),
            Tooltip(
                "How much to penalize new tokens based on their existing frequency in the text so far. Decreases the model's likelihood to repeat the same line verbatim."
            )
        ]
        public float frequency_penalty = 0;
        [
            Range(0.0f, 2.0f),
            Tooltip(
                "How much to penalize new tokens based on whether they appear in the text so far. Increases the model's likelihood to talk about new topics."
            )
        ]
        public float presence_penalty = 0;
        public bool _wordStreaming = true;
        public List<ChatMessageV1> messages = new List<ChatMessageV1>(); // Used as the chat history for context. If it exceeds the amount of tokens allowed, you'll get an error.

        public async void SendOpenAIRequest(string model, string prompt, string SuccessEventName,string FailEventName, string CompleteEventName)
        {
            if (_wordStreaming)
            {
                await SendOpenAIRequestWithStreaming(model, prompt, SuccessEventName, FailEventName, CompleteEventName);
            }
            else
            {
            
            ApiResult<CompletionV1> comp = null;
            ApiResult<ChatCompletionV1> chatComp = null;

            if (model.Contains("gpt"))
            {
                chatComp = await SendChatGPTRequest(prompt, model);

                if (chatComp.IsSuccess)
                {
                    CustomEvent.Trigger(
                        gameObject,
                        SuccessEventName,
                        chatComp.Result.choices[0].message.content
                    );
                }
                else
                {
                    CustomEvent.Trigger(
                        gameObject,
                        FailEventName,
                        $"ERROR: StatusCode: {chatComp.HttpResponse.responseCode} - {chatComp.HttpResponse.error}"
                    );
                }
            }
            else
            {
                //Use this method when calling a model other than gpt
                comp = await completer._gateway.Api.Engines
                    .Engine(model)
                    .Completions.CreateCompletionAsync(
                        new CompletionRequestV1()
                        {
                            prompt = prompt,
                            max_tokens = completer.Args.max_tokens,
                            temperature = completer.Args.temperature,
                            top_p = completer.Args.top_p,
                            stop = completer.Args.stop,
                            frequency_penalty = completer.Args.frequency_penalty,
                            presence_penalty = completer.Args.presence_penalty
                        }
                    );

                if (comp.IsSuccess)
                {
                    CustomEvent.Trigger(gameObject, SuccessEventName, comp.Result.choices[0].text);
                }
                else
                {
                    CustomEvent.Trigger(
                        gameObject,
                        FailEventName,
                        $"ERROR: StatusCode: {comp.HttpResponse.responseCode} - {comp.HttpResponse.error}"
                    );
                };
            }
            }
        }

        public async Task SendOpenAIRequestWithStreaming(string model, string prompt, string SuccessEventName, string FailEventName, string CompleteEventName)
        {
            SOAuthArgsV1 auth = completer.Auth;
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());

            ChatCompletionRequestV1 request = new ChatCompletionRequestV1()
            {
                model = model,
                stream = true,
                messages = messages.ToArray()
            };

            await api.ChatCompletions.CreateChatCompletionAsync_EventStream(
                request,
                onRequestStatus: (status) =>
                {
                    if (!status.IsSuccess)
                    {
                        CustomEvent.Trigger(
                            gameObject,
                            FailEventName,
                            $"ERROR: StatusCode: {status.HttpResponse.responseCode} - {status.HttpResponse.error}"
                        );
                    }
                },
                onPartialResult: (index, result) =>
                {
                    ChatCompletionV1 chatCompletion = result as ChatCompletionV1;
                    if (chatCompletion != null && !string.IsNullOrEmpty(chatCompletion.content))
                    {
                        CustomEvent.Trigger(
                            gameObject,
                            SuccessEventName,
                            chatCompletion.content
                        );
                    }
                    else
                    {
                        Debug.LogError("Invalid result object or missing content");
                    }
                },
                onCompletion: () =>
                {
                    CustomEvent.Trigger(
                            gameObject,
                            CompleteEventName
                        );

                }
            );
        }

        // Use this method when calling a GPT model
        public async Task<ApiResult<ChatCompletionV1>> SendChatGPTRequest(string message, string GPTmodel)
        {
            SOAuthArgsV1 auth = completer.Auth;
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());

            ApiResult<ChatCompletionV1> chatComp =
                await api.ChatCompletions.CreateChatCompletionAsync(
                    new ChatCompletionRequestV1()
                    {
                        model = GPTmodel,
                        //max_tokens = completer.Args.max_tokens,
                        //temperature = completer.Args.temperature,
                        //top_p = completer.Args.top_p,
                        //stop = completer.Args.stop,
                        //frequency_penalty = completer.Args.frequency_penalty,
                        //presence_penalty = completer.Args.presence_penalty,
                        messages = messages.ToArray()
                    }
                );

            return chatComp;
        }
    }
}
