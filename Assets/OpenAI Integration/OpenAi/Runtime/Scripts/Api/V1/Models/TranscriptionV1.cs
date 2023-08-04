using OpenAi.Json;

namespace OpenAi.Api.V1
{
    public class TranscriptionV1 : AModelV1
    {
        public string text;
        
        public override void FromJson(JsonObject json)
        {
            foreach (JsonObject jo in json.NestedValues)
            {
                switch (jo.Name)
                {
                    case nameof(text):
                        text = jo.StringValue;
                        break;
                }
            }
        }

        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            jb.StartObject();
            jb.Add(nameof(text), text);
            jb.EndObject();

            return jb.ToString();
        }
    }
}