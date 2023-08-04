using OpenAi.Json;

namespace OpenAi.Api.V1
{
    public class GenerationsRequestV1 : AModelV1
    {
        /// <summary>
        /// A text description of the desired image(s). The maximum length is 1000 characters.
        /// </summary>
        public string prompt;

        /// <summary>
        /// The number of images to generate. Must be between 1 and 10.
        /// </summary>
        public int n;
        
        /// <summary>
        /// The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024
        /// </summary>
        public string size;
        
        /// <summary>
        /// The format in which the generated images are returned. Must be one of url or b64_json
        /// </summary>
        public string response_format;
        
        /// <summary>
        /// A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse
        /// </summary>
        public string user;
        

        public override void FromJson(JsonObject json)
        {
            //TODO not need as it's only used to send
        }

        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            jb.StartObject();
            jb.Add(nameof(prompt), prompt);
            jb.Add(nameof(n), n);
            jb.Add(nameof(size), size);
            jb.Add(nameof(response_format), response_format);
            jb.Add(nameof(user), user);
            jb.EndObject();

            return jb.ToString();
        }
    }
}