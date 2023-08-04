using System;

namespace OpenAi.Api
{
    public enum EEngineName
    {
        ada,
        babbage,
        content_filter_alpha,
        curie,
        davinci,
        gpt3_5,
        gpt4,
        gpt4_32k
    }

    public static class UTEEngineName
    {
        public static string GetEngineName(EEngineName name)
        {
            switch (name)
            {
                case EEngineName.ada:
                    return UTEngineNames.ada;
                case EEngineName.babbage:
                    return UTEngineNames.babbage;
                case EEngineName.content_filter_alpha:
                    return UTEngineNames.content_filter_alpha;
                case EEngineName.curie:
                    return UTEngineNames.curie;
                case EEngineName.davinci:
                    return UTEngineNames.text_davinci_003;
                case EEngineName.gpt3_5:
                    return UTEngineNames.gpt_3_5_turbo;
                case EEngineName.gpt4:
                    return UTEngineNames.gpt4;
                case EEngineName.gpt4_32k:
                    return UTEngineNames.gpt4_32k;   
            }

            throw new ArgumentException($"Invalid enum value provided when getting engine name. Value provided: {name}");
        }
    }
}