using System.Text;

using UnityEngine.Networking;

namespace OpenAi.Api.V1
{
    /// <summary>
    /// Entry point for all api calls to the OpenAi Api. Read the docs at <see href="https://beta.openai.com/docs/api-reference"/>.
    /// Api calls are managed using resource objects, which contain various calls that can be performed on that resource. 
    /// For example, <see cref="https://beta.openai.com/docs/api-reference/list-engines"/> is the documentation for the list engines
    /// api call. This is a GET request at https://api.openai.com/v1/engines. To make this call with the <see cref="OpenAiApiV1"/> object,
    /// the syntax is <c>OpenAiApiV1.Engines.List()</c>
    /// </summary>
    public class OpenAiApiV1 : IApiResource
    {
        private SAuthArgsV1 _authArgs;

        /// <inheritdoc />
        public IApiResource ParentResource => null;

        /// <inheritdoc />
        public string Endpoint => "https://api.openai.com/v1";

        /// <inheritdoc />
        public string Url => Endpoint;

        /// <summary>
        /// The Engines resources. <see href="https://beta.openai.com/docs/api-reference/list-engines"/> 
        /// </summary>
        public EnginesResourceV1 Engines { get; private set; }

        /// <summary>
        /// Classifications resource. <see href="https://beta.openai.com/docs/api-reference/classifications"/>
        /// </summary>
        public ClassificationsResourceV1 Classifications { get; set; }

        /// <summary>
        /// Answers resource. <see href="https://beta.openai.com/docs/api-reference/answers"/>
        /// </summary>
        public AnswersResourceV1 Answers { get; set; }

        /// <summary>
        /// The Files resources. <see href="https://beta.openai.com/docs/api-reference/files"/> 
        /// </summary>
        public FilesResourceV1 Files { get; private set; }
        
        /// <summary>
        /// Chat Completions resource. <see href="https://platform.openai.com/docs/api-reference/chat"/>
        /// </summary>
        public ChatCompletionsResourceV1 ChatCompletions { get; private set; }

        /// <summary>
        /// Audio resource. <see href="https://platform.openai.com/docs/api-reference/audio"/>
        /// </summary>
        public AudioResourceV1 Audio{ get; private set; }

        /// <summary>
        /// Images resource. <see href="https://platform.openai.com/docs/api-reference/images"/>
        /// </summary>
        public ImagesResourceV1 Images{ get; private set; }

        /// <summary>
        /// Construct an <see cref="OpenAiApiV1"/> with the provided auth args.
        /// </summary>
        /// <param name="authArgs"></param>
        public OpenAiApiV1(SAuthArgsV1 authArgs)
        {
            _authArgs = authArgs;
            Engines = new EnginesResourceV1(this);
            Files = new FilesResourceV1(this);
            Classifications = new ClassificationsResourceV1(this);
            Answers = new AnswersResourceV1(this);
            ChatCompletions = new ChatCompletionsResourceV1(this);
            Audio = new AudioResourceV1(this);
            Images = new ImagesResourceV1(this);
        }

        /// <inheritdoc />
        public void ConstructEndpoint(StringBuilder sb)
        {
            sb.Append(Endpoint);
        }

        /// <inheritdoc />
        public void PopulateAuthHeaders(UnityWebRequest client)
        {
            client.SetRequestHeader("Authorization", $"Bearer {_authArgs.private_api_key}");
            //if (!string.IsNullOrEmpty(_authArgs.organization)) client.SetRequestHeader("OpenAI-Organization", _authArgs.organization);
        }
    }
}