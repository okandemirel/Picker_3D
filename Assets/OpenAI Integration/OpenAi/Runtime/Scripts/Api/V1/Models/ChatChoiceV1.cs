using OpenAi.Json;

using System;

namespace OpenAi.Api.V1
{
    /// <summary>
    /// A single choice returned by the OpenAi Api completion endpoint
    /// </summary>
    public class ChatChoiceV1 : AModelV1
    {
        /// <summary>
        /// The returned text
        /// </summary>
        public ChatMessageV1 message;

        /// <summary>
        /// the index of the choice
        /// </summary>
        public int index;

        /// <summary>
        /// The reason the engine ended the completion
        /// </summary>
        public string finish_reason;

        /// <inheritdoc />
        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            jb.StartObject();
            jb.AddSimpleObject(nameof(message), message);
            jb.Add(nameof(index), index);
            jb.Add(nameof(finish_reason), finish_reason);
            jb.EndObject();

            return jb.ToString();
        }

        /// <inheritdoc />
        public override void FromJson(JsonObject jsonObj)
        {
            if (jsonObj.Type != EJsonType.Object) throw new Exception("Must be an object");

            foreach (JsonObject jo in jsonObj.NestedValues)
            {
                switch (jo.Name)
                {
                    case nameof(message):
                        message = new ChatMessageV1();
                        message.FromJson(jo);
                        break;
                    case nameof(index):
                        index = int.Parse(jo.StringValue);
                        break;
                    case nameof(finish_reason):
                        finish_reason = jo.StringValue;
                        break;
                }
            }
        }
    }
}