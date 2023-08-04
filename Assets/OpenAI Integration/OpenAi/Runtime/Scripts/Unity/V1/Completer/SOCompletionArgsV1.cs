using OpenAi.Api.V1;

using UnityEngine;

namespace OpenAi.Unity.V1
{
    /// <summary>
    /// Arguments used to create a completion with the <see cref="OpenAiCompleterV1"/>
    /// </summary>
    [CreateAssetMenu(fileName = "CompletionArgs", menuName = "OpenAi/Unity/V1/CompletionArgs")]
    public class SOCompletionArgsV1 : ScriptableObject
    {
        [Range(1, 4000), Tooltip("The maximum number of tokens to generate. Requests can use up to 4000 tokens shared between prompt and completion. (One token is roughly 4 characters for normal English text)")]
        public int max_tokens = 2048;
        [Range(0.0f, 1.0f), Tooltip("Controls randomness: Lowering results in less random completions. As the temperature approaches zero, the model will become deterministic and repetitive.")]
        public float temperature = 0.7f;
        [Range(0.0f, 1.0f), Tooltip("Controls diversity via nucleus sampling: 0.5 means half of all likelihood-weighted options are considered.")]
        public float top_p = 1;
        [Tooltip("Where the API will stop generating further tokens. The returned text will not contain the stop sequence.")]
        public string stop;
        [Range(0.0f, 2.0f), Tooltip("How much to penalize new tokens based on their existing frequency in the text so far. Decreases the model's likelihood to repeat the same line verbatim.")]
        public float frequency_penalty = 0;
        [Range(0.0f, 2.0f), Tooltip("How much to penalize new tokens based on whether they appear in the text so far. Increases the model's likelihood to talk about new topics.")]
        public float presence_penalty = 0;

        public CompletionRequestV1 AsCompletionRequest()
        {
            return new CompletionRequestV1()
            {
                max_tokens = max_tokens,
                temperature = temperature,
                top_p = top_p,
                stop = "\n",
                frequency_penalty = frequency_penalty,
                presence_penalty = presence_penalty
            };
        }
    }
}