using System;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAi.Api.V1.Api.Commons
{
    public abstract class BaseApiResource<TParent, TRequest, TResponse> : AApiResource<TParent> where TParent : IApiResource
        where TRequest : AModelV1, new()
        where TResponse : AModelV1, new()
    {

        protected BaseApiResource(TParent parent) : base(parent)
        {
        }

         /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>false</c>. To stream, use <see cref="CreateCompletionAsync_EventStream(CompletionRequestV1, Action{ApiResult{CompletionV1}}, Action{int, CompletionV1}, Action)"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public async Task<ApiResult<TResponse>> SendRequestAsync(TRequest request)
        {
            return await PostAsync<TRequest, TResponse>(request);
        }
        
        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>false</c>. To stream, use <see cref="CreateCompletionCoroutine_EventStream(MonoBehaviour, CompletionRequestV1, Action{ApiResult{CompletionV1}}, Action{int, CompletionV1}, Action)"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public Coroutine SendRequestCoroutine(MonoBehaviour mono, TRequest request, Action<ApiResult<TResponse>> onResult)
        {
            return PostCoroutine(mono, request, onResult);
        }

        #region Streaming
        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>true</c>. To stream, use <see cref="CreateCompletionAsync(CompletionRequestV1)"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public async Task CreateRequestAsync_EventStream(TRequest request, Action<ApiResult<TResponse>> onRequestStatus, Action<int, TResponse> onPartialResult, Action onCompletion = null)
        {
            await PostAsync_EventStream(request, onRequestStatus, onPartialResult, onCompletion);
        }

        /// <summary>
        /// This is the main endpoint of the API. Returns the predicted completion for the given prompt, and can also return the probabilities of alternative tokens at each position if requested. <see href="https://beta.openai.com/docs/api-reference/create-completion"/>. Ignores with <c>request.stream</c> parameter and automatically set to <c>true</c>. To stream, use <see cref="CreateCompletionCoroutine(MonoBehaviour, CompletionRequestV1, Action{ApiResult{CompletionV1}})"/> instead
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Asynchronously returns the completion result.  Look in its <see cref="CompletionResult.Choices"/> property for the completions.</returns>
        public Coroutine CreateRequestCoroutine_EventStream(MonoBehaviour mono, TRequest request, Action<ApiResult<TResponse>> onRequestStatus, Action<int, TResponse> onPartialResult, Action onCompletion = null)
        {
            return PostCoroutine_EventStream(mono, request, onRequestStatus, onPartialResult, onCompletion);
        }
        #endregion
    }
}