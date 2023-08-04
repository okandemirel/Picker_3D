using System;
using OpenAi.Json;

namespace OpenAi.Api.V1
{
    [Serializable]
    public class ChatMessageV1 : AModelV1
    {
        public string role;
        public string content;
        
        public override void FromJson(JsonObject json)
        {
            foreach (JsonObject jo in json.NestedValues)
            {
                switch (jo.Name)
                {
                    case nameof(role):
                        role = jo.StringValue;
                        break;
                    case nameof(content):
                        content = jo.StringValue;
                        break;
                }
            }
        }

        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            jb.StartObject();
            jb.Add(nameof(role), role);
            jb.Add(nameof(content), content);
            jb.EndObject();

            return jb.ToString();
        }
    }
}