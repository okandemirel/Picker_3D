using OpenAi.Json;
using System;
using System.Collections.Generic;

namespace OpenAi.Api.V1
{
    /// <summary>
    /// Object used when requesting a completion. <see href="https://platform.openai.com/docs/api-reference/audio/create"/>
    /// </summary>
    public class TranscriptionRequestV1 : AModelV1,AUploadFileV1
    {
        /// <summary>
        /// The audio file to translate, in one of these formats: mp3, mp4, mpeg, mpga, m4a, wav, or webm.
        /// </summary>
        public byte[] audioFile;
        
        /// <summary>
        /// ID of the model to use. Only whisper-1 is currently available.
        /// </summary>
        public string model;

        /// <summary>
        /// An optional text to guide the model's style or continue a previous audio segment. The prompt should be in English.
        /// </summary>
        public string prompt;

        /// <summary>
        /// The format of the transcript output, in one of these options: json, text, srt, verbose_json, or vtt.
        /// </summary>
        public string response_format;
        
        /// <summary>
        /// The language of the input audio. Supplying the input language in ISO-639-1 format will improve accuracy and latency.
        /// </summary>
        public string language;

        /// <summary>
        /// The sampling temperature, between 0 and 1.
        /// Higher values like 0.8 will make the output more random,
        /// while lower values like 0.2 will make it more focused and deterministic.
        /// If set to 0, the model will use log probability to automatically increase the temperature
        /// until certain thresholds are hit.
        /// </summary>
        public float temperature;


        /// <inheritdoc />
        public override void FromJson(JsonObject json)
        {
            if (json.Type != EJsonType.Object)
                throw new OpenAiApiException("Deserialization failed, provided json is not an object");

            foreach (JsonObject obj in json.NestedValues)
            {
                switch (obj.Name)
                {
                    case nameof(model):
                        model = obj.StringValue;
                        break;
                    case nameof(prompt):
                        prompt = obj.StringValue;
                        break;
                    case nameof(response_format):
                        response_format = obj.StringValue;
                        break;
                    case nameof(temperature):
                        temperature = float.Parse(obj.StringValue);
                        break;
                    case nameof(language):
                        language = obj.StringValue;
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
            jb.Add(nameof(prompt), prompt);
            jb.Add(nameof(response_format), response_format);
            jb.Add(nameof(temperature), temperature);
            jb.Add(nameof(language), language);
            jb.EndObject();

            return jb.ToString();
        }

        public byte[] GetFileBytes()
        {
            return audioFile;
        }

        public string ToFormDataFields()
        {
            return $@"model={model}&prompt={prompt}&response_format={response_format}&temperature={temperature}&language={language}";
        }
    }
}