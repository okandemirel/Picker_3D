using UnityEngine;

namespace OpenAI.Integrations.ElevenLabs.Configuration
{
    /// <summary>
    /// The Authentication arguments required to authenticate an ElevenLabs Api request.
    /// </summary>
    /// <remarks>
    /// Projects pushed to public reposities should not use the String authentication type, as the private key will be exposed to the public. 
    /// </remarks>
    [CreateAssetMenu(fileName = "ELAuthArgs", menuName = "ElevenLabs/Configuration/ElevenLabs Auth configuration")]
    public class ELAuthArgsV1 : ScriptableObject
    {
        /// <summary>
        /// The private key provided by Elevenlabs
        /// </summary>
        public string PrivateApiKey;
    }
}