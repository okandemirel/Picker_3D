using System;
using System.Threading.Tasks;

using UnityEngine;

namespace OpenAi.Api.V1
{

    /// <summary>
    /// Resource providing completions functionality. Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration). <see href="https://beta.openai.com/docs/examples"/>
    /// </summary>
    public class ImagesResourceV1 : AApiResource<OpenAiApiV1>
    {
        /// <inheritdoc/>
        public override string Endpoint => "/images";

        /// <summary>
        /// Transcription resource. <see href="https://platform.openai.com/docs/api-reference/audio/create"/> and onwards.
        /// </summary>
        public GenerationsResourceV1 Generations { get; private set; }

        /// <summary>
        /// Construct with parent
        /// </summary>
        /// <param name="parent"></param>
        public ImagesResourceV1(OpenAiApiV1 parent) : base(parent)
        {
            Generations = new GenerationsResourceV1(this);
        }

    }
}