using System;
using System.Threading.Tasks;

using UnityEngine;

namespace OpenAi.Api.V1
{

    /// <summary>
    /// Resource providing completions functionality. Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration). <see href="https://beta.openai.com/docs/examples"/>
    /// </summary>
    public class TranscriptionsResourceV1 : AApiResource<AudioResourceV1>
    {
        /// <inheritdoc/>
        public override string Endpoint => "/transcriptions";

        /// <summary>
        /// Construct with parent
        /// </summary>
        /// <param name="parent"></param>
        public TranscriptionsResourceV1(AudioResourceV1 parent) : base(parent) { }

        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>false</c>. To stream, use <see cref="CreateCompletionAsync_EventStream(CompletionRequestV1, Action{ApiResult{CompletionV1}}, Action{int, CompletionV1}, Action)"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public async Task<ApiResult<TranscriptionV1>> CreateTranscriptionAsync(TranscriptionRequestV1 request)
        {
            return await PostAsync<TranscriptionRequestV1, TranscriptionV1>(request);
        }
        
        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>false</c>. To stream, use <see cref="CreateCompletionCoroutine_EventStream(MonoBehaviour, CompletionRequestV1, Action{ApiResult{CompletionV1}}, Action{int, CompletionV1}, Action)"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public Coroutine CreateTranscriptionCoroutine(MonoBehaviour mono, TranscriptionRequestV1 request, Action<ApiResult<TranscriptionV1>> onResult)
        {
            return PostCoroutine(mono, request, onResult);
        }

        #region Streaming
        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>true</c>. To stream, use <see cref="CreateCompletionAsync(CompletionRequestV1)"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public async Task CreateTranscriptionAsync_EventStream(TranscriptionRequestV1 request, Action<ApiResult<TranscriptionV1>> onRequestStatus, Action<int, TranscriptionV1> onPartialResult, Action onCompletion = null)
        {
            await PostAsync_EventStream(request, onRequestStatus, onPartialResult, onCompletion);
        }

        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>true</c>. To stream, use <see cref="CreateCompletionCoroutine(MonoBehaviour, CompletionRequestV1, Action{ApiResult{CompletionV1}})"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public Coroutine CreateTranscriptionsCoroutine_EventStream(MonoBehaviour mono, TranscriptionRequestV1 request, Action<ApiResult<TranscriptionV1>> onRequestStatus, Action<int, TranscriptionV1> onPartialResult, Action onCompletion = null)
        {
            return PostCoroutine_EventStream(mono, request, onRequestStatus, onPartialResult, onCompletion);
        }
        #endregion
    }
}