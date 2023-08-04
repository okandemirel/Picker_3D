using System;
using OpenAi.Json;

namespace OpenAi.Api.V1
{
    public class ChatUsageV1 : AModelV1
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;

        public override void FromJson(JsonObject jsonObj)
        {
            if (jsonObj.Type != EJsonType.Object) throw new Exception("Must be an object");

            foreach (JsonObject jo in jsonObj.NestedValues)
            {
                switch (jo.Name)
                {
                    case nameof(prompt_tokens):
                        prompt_tokens = int.Parse(jo.StringValue);
                        break;
                    case nameof(completion_tokens):
                        completion_tokens = int.Parse(jo.StringValue);
                        break;
                    case nameof(total_tokens):
                        total_tokens = int.Parse(jo.StringValue);
                        break;
                }
            }
        }

        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            jb.StartObject();
            jb.Add(nameof(prompt_tokens), prompt_tokens);
            jb.Add(nameof(completion_tokens), completion_tokens);
            jb.Add(nameof(total_tokens), total_tokens);
            jb.EndObject();

            return jb.ToString();
        }
    }
}