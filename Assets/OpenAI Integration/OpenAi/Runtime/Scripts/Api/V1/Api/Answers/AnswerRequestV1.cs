﻿using OpenAi.Json;

using System;
using System.Collections.Generic;

namespace OpenAi.Api.V1
{
    /// <summary>
    /// Object used when requesting an answer. <see href="https://beta.openai.com/docs/api-reference/answers"/>
    /// </summary>
    public class AnswerRequestV1 : AModelV1
    {
        /// <summary>
        /// [Required] ID of the engine to use for completion.
        /// </summary>
        public string model;

        /// <summary>
        /// [Required] Question to get answered.
        /// </summary>
        public string question;

        /// <summary>
        /// [Required] List of (question, answer) pairs that will help steer the model towards the tone and answer format you'd like. We recommend adding 2 to 3 examples.
        /// </summary>
        public QuestionAnswerPairV1[] examples;

        /// <summary>
        /// [Required] A text snippet containing the contextual information used to generate the answers for the examples you provide.
        /// </summary>
        public string examples_context;

        /// <summary>
        /// List of documents from which the answer for the input question should be derived. If this is an empty list, the question will be answered based on the question-answer examples. You should specify either documents or a file, but not both.
        /// </summary>
        public string[] documents;

        /// <summary>
        /// The ID of an uploaded file that contains documents to search over. See upload file for how to upload a file of the desired format and purpose. You should specify either documents or a file, but not both.
        /// </summary>
        public string file;

        /// <summary>
        /// ID of the engine to use for Search.
        /// </summary>
        public string search_model;

        /// <summary>
        /// The maximum number of documents to be ranked by Search when using file. Setting it to a higher value leads to improved accuracy but with increased latency and cost.
        /// </summary>
        public int? max_rerank;

        /// <summary>
        /// What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer. We generally recommend altering this or top_p but not both.
        /// </summary>
        public float? temperature;

        /// <summary>
        /// Include the log probabilities on the logprobs most likely tokens, as well the chosen tokens. For example, if logprobs is 10, the API will return a list of the 10 most likely tokens. the API will always return the logprob of the sampled token, so there may be up to logprobs+1 elements in the response.
        /// </summary>
        public int? logprobs;

        /// <summary>
        /// The maximum number of tokens allowed for the generated answer
        /// </summary>
        public int? max_tokens;

        /// <summary>
        /// Up to 4 sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence.
        /// </summary>
        public StringOrArray stop;

        /// <summary>
        /// How many answers to generate for each question.
        /// </summary>
        public int? n;

        /// <summary>
        /// Modify the likelihood of specified tokens appearing in the completion. Accepts a json object that maps tokens(specified by their token ID in the GPT tokenizer) to an associated bias value from -100 to 100. You can use this tokenizer tool (which works for both GPT-2 and GPT-3) to convert text to token IDs. Mathematically, the bias is added to the logits generated by the model prior to sampling. The exact effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of selection; values like -100 or 100 should result in a ban or exclusive selection of the relevant token. As an example, you can pass <c>{"50256": -100}</c> to prevent the <|endoftext|> token from being generated.
        /// </summary>
        public Dictionary<string, int> logit_bias;

        /// <summary>
        /// If set to true, the returned JSON will include a "prompt" field containing the final prompt that was used to request a completion. This is mainly useful for debugging purposes.
        /// </summary>
        public bool return_prompt;

        /// <summary>
        /// A special boolean flag for showing metadata. If set to true, each document entry in the returned JSON will contain a "metadata" field. This flag only takes effect when file is set.
        /// </summary>
        public bool return_metadata;

        /// <summary>
        /// If an object name is in the list, we provide the full information of the object; otherwise, we only provide the object ID. Currently we support completion and file objects for expansion.
        /// </summary>
        public string[] expand;

        /// <inheritdoc />
        public override void FromJson(JsonObject json)
        {
            if (json.Type != EJsonType.Object) throw new OpenAiApiException("Deserialization failed, provided json is not an object");

            foreach(JsonObject obj in json.NestedValues)
            {
                switch (obj.Name) 
                {
                    case nameof(model):
                        model = obj.StringValue;
                        break;
                    case nameof(question):
                        question = obj.StringValue;
                        break;
                    case nameof(examples):
                        examples = ArrayFromJson<QuestionAnswerPairV1>(obj);
                        break;
                    case nameof(examples_context):
                        examples_context = obj.StringValue;
                        break;
                    case nameof(documents):
                        documents = obj.AsStringArray();
                        break;
                    case nameof(file):
                        file = obj.StringValue;
                        break;
                    case nameof(search_model):
                        search_model = obj.StringValue;
                        break;
                    case nameof(max_rerank):
                        max_rerank = int.Parse(obj.StringValue);
                        break;
                    case nameof(temperature):
                        temperature = float.Parse(obj.StringValue);
                        break;
                    case nameof(logprobs):
                        logprobs = int.Parse(obj.StringValue);
                        break;
                    case nameof(max_tokens):
                        max_tokens = int.Parse(obj.StringValue);
                        break;
                    case nameof(stop):
                        stop = new StringOrArray();
                        stop.FromJson(obj);
                        break;
                    case nameof(n):
                        n = int.Parse(obj.StringValue);
                        break;
                    case nameof(logit_bias):
                        logit_bias = new Dictionary<string, int>();
                        foreach(JsonObject child in obj.NestedValues)
                        {
                            logit_bias.Add(child.Name, int.Parse(child.StringValue));
                        }
                        break;
                    case nameof(return_metadata):
                        return_metadata = bool.Parse(obj.StringValue);
                        break;
                    case nameof(return_prompt):
                        return_prompt = bool.Parse(obj.StringValue);
                        break;
                    case nameof(expand):
                        expand = obj.AsStringArray();
                        break;
                }
            }
        }

        /// <inheritdoc />
        public override string ToJson()
        {
            JsonBuilder jb = new JsonBuilder();

            jb.StartObject();
            jb.Add(nameof(model), model);
            jb.Add(nameof(question), question);
            jb.AddArray(nameof(examples), examples);
            jb.Add(nameof(examples_context), examples_context);
            jb.AddArray(nameof(documents), documents);
            jb.Add(nameof(file), file);
            jb.Add(nameof(search_model), search_model);
            jb.Add(nameof(max_rerank), max_rerank);
            jb.Add(nameof(temperature), temperature);
            jb.Add(nameof(logprobs), logprobs);
            jb.Add(nameof(max_tokens), max_tokens);
            jb.Add(nameof(stop), stop);
            jb.Add(nameof(n), n);
            jb.Add(nameof(logit_bias), logit_bias);
            jb.Add(nameof(return_metadata), return_metadata);
            jb.Add(nameof(return_prompt), return_prompt);
            jb.Add(nameof(expand), expand);

            jb.EndObject();

            return jb.ToString();
        }
    }
}
