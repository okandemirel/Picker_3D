using OpenAi.Json;
using UnityEngine;
using System;
using System.Text;

namespace OpenAi.Api.V1
{
    /// <summary>
    /// The response to completion request
    /// </summary>
    public class ChatCompletionV1 : AModelV1
    {
        /// <summary>
        /// the id of the competion
        /// </summary>
        public string id;
        public string content;
        /// <summary>
        /// The object type (text_completion)
        /// </summary>
        public string obj;

        /// <summary>
        /// The created time as Unix epoch
        /// </summary>
        public int created;

        /// <summary>
        /// The model used to create the completion
        /// </summary>
        public string model;

        
        /// <summary>
        /// The usage returned by the completion
        /// </summary>
        public ChatUsageV1 usage;
        
        /// <summary>
        /// The choices returned by the completion
        /// </summary>
        public ChatChoiceV1[] choices;

        /// <inheritdoc />
        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            if (content != null)
            {
                return "\"" + content + "\"";
            }
            else
            {
                jb.StartObject();
                jb.Add(nameof(id), id);
                jb.Add("object", obj);
                jb.Add(nameof(created), created);
                jb.Add(nameof(model), model);
                jb.AddSimpleObject(nameof(usage), usage);
                jb.AddArray(nameof(choices), choices);
                jb.EndObject();
            }

            return jb.ToString();
        }

        /// <inheritdoc />
        public override void FromJson(JsonObject jsonObj)
        {
            if (jsonObj.Type != EJsonType.Object && jsonObj.Type != EJsonType.Value)
                throw new Exception("Must be an object or a value");

            if (jsonObj.Type == EJsonType.Value)
            {
                content = jsonObj.StringValue;
            }
            else
            {
                foreach (JsonObject jo in jsonObj.NestedValues)
                {
                    switch (jo.Name)
                    {
                        case nameof(id):
                            id = jo.StringValue;
                            break;
                        case "object":
                            obj = jo.StringValue;
                            break;
                        case nameof(created):
                            created = int.Parse(jo.StringValue);
                            break;
                        case nameof(model):
                            model = jo.StringValue;
                            break;
                        case nameof(choices):
                            ChatChoiceV1[] choiceArray = new ChatChoiceV1[jo.NestedValues.Count];
                            for (int i = 0; i < choiceArray.Length; i++)
                            {
                                ChatChoiceV1 n = new ChatChoiceV1();
                                n.FromJson(jo.NestedValues[i]);
                                choiceArray[i] = n;
                            }
                            choices = choiceArray;
                            break;
                    }
                }
            }
        }
}}
