using OpenAi.Api.V1.Api.Commons;

namespace OpenAi.Api.V1
{

    /// <summary>
    /// Resource providing completions functionality. Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration). <see href="https://beta.openai.com/docs/examples"/>
    /// </summary>
    public class GenerationsResourceV1 : BaseApiResource<ImagesResourceV1,GenerationsRequestV1, GenerationsV1>
    {
        /// <inheritdoc/>
        public override string Endpoint => "/generations";


        public GenerationsResourceV1(ImagesResourceV1 parent) : base(parent)
        {
        }
    }
}