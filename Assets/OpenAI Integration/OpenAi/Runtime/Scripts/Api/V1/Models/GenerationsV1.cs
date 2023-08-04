using System;
using System.Collections.Generic;
using OpenAi.Json;

namespace OpenAi.Api.V1
{
    public class GenerationsDataV1 : AModelV1
    {
        /// <summary>
        /// URL to image
        /// </summary>
        public string url;

        /// <summary>
        /// Image in base 64 format
        /// </summary>
        public string b64_json;

        public override void FromJson(JsonObject json)
        {
            if (json.Type != EJsonType.Object) throw new Exception("Must be an object");

            foreach (JsonObject jo in json.NestedValues)
            {
                switch (jo.Name)
                {
                    case nameof(url):
                        url = jo.StringValue;
                        break;
                    case nameof(b64_json):
                        b64_json = jo.StringValue;
                        break;
                }
            }
        }

        public override string ToJson()
        {
            //TODO not need as it's only used to receive from api
            return "{}";
        }
    }

    public class GenerationsV1 : AModelV1
    {
        /// <summary>
        /// URL to image
        /// </summary>
        public int created;

        /// <summary>
        /// Image in base 64 format
        /// </summary>
        public GenerationsDataV1[] data;


        public override void FromJson(JsonObject json)
        {
            if (json.Type != EJsonType.Object) throw new Exception("Must be an object");

            foreach (JsonObject jo in json.NestedValues)
            {
                switch (jo.Name)
                {
                    case nameof(created):
                        created = int.Parse(jo.StringValue);
                        break;
                    case nameof(data):
                        GenerationsDataV1[] dataArray = new GenerationsDataV1[jo.NestedValues.Count];
                        for(int i = 0; i<dataArray.Length; i++)
                        {
                            GenerationsDataV1 n = new GenerationsDataV1();
                            n.FromJson(jo.NestedValues[i]);
                            dataArray[i] = n;
                        }
                        data = dataArray;
                        break;
                }
            }
        }

        public override string ToJson()
        {
            //TODO not need as it's only used to receive from api
            return "{}";
        }
    }
}