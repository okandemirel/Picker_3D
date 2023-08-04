using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using OpenAi.Unity.V1;
using TMPro;
using OpenAI.Integrations.ElevenLabs.Configuration;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAI.Integrations.VoiceRecorder
{
    public class ELSpeaker : MonoBehaviour
    {
        public enum VoiceName
        {
            Rachel,
            Domi,
            Bella,
            Antoni,
            Elli,
            Josh,
            Arnold,
            Adam,
            Sam
        }

        private static readonly Dictionary<VoiceName, string> VoiceIdDictionary = new Dictionary<VoiceName, string>
        {
            { VoiceName.Rachel, "21m00Tcm4TlvDq8ikWAM" },
            { VoiceName.Domi, "AZnzlk1XvdvUeBnXmlld" },
            { VoiceName.Bella, "EXAVITQu4vr4xnSDxMaL" },
            { VoiceName.Antoni, "ErXwobaYiN019PkySvjV" },
            { VoiceName.Elli, "MF3mGyEYCl7XYWbV9V6O" },
            { VoiceName.Josh, "TxGEqnHWrfWFTfGW9XjX" },
            { VoiceName.Arnold, "VR6AewLTigWG4xSOukaG" },
            { VoiceName.Adam, "pNInz6obpgDQGcFmaJgB" },
            { VoiceName.Sam, "yoZ06aMxZJJ28mfd3POQ" }
        };

        [SerializeField]
        public VoiceName selectedVoice = VoiceName.Rachel;
        [SerializeField]
        public string customVoiceID = "";
        [Tooltip(
            "Plays the audio response without downloading (faster), but isn't supported on all platforms."
        )]
        public bool useAudioStreaming = true;
        public bool responsePlaying = false;
        private const string urlTemplate = "https://api.elevenlabs.io/v1/text-to-speech/{{voice_id}}/stream";
        private readonly WaveOutEvent outputDevice = new WaveOutEvent();
        
        [SerializeField,
        Tooltip(
            "Check this file and make sure your API key on it is set."
        )]
        public ELAuthArgsV1 ELAuth;
        private const string jsonBodyTemplate =
            "{\"text\": \"{{text}}\", \"voice_settings\": {\"stability\": {{stability}}, \"similarity_boost\": {{similarity_boost}}}}";

        public TMP_Text textInput;
        [Range(0, 1),
        Tooltip(
                "Boosting voice clarity and target speaker similarity is achieved by high enhancement; however, very high values can produce artifacts, so it's essential to find the optimal setting."
            )]
        public float similarity_boost = 0.4f;
        [Range(0, 1),
        Tooltip(
                "Higher stability ensures consistency but may result in monotony, therefore for longer text, it is recommended to decrease stability."
            )]
        public float stability = 0.1f;

        public void SpeakSentenceFromInput()
        {
            responsePlaying = true;
            Debug.Log("Speaking: " + textInput.text);
            StartCoroutine(SpeakSentence(textInput.text));
        }
        public IEnumerator SpeakSentence(string input)
        {
            string url; 
            if (customVoiceID == ""){
                Debug.Log("custom ID is null");
                url = urlTemplate.Replace("{{voice_id}}", VoiceIdDictionary[selectedVoice]);
                Debug.Log("url changed to " + url);
            } else {
                Debug.Log("custom ID is " + customVoiceID);
                url = urlTemplate.Replace("{{voice_id}}", customVoiceID);
                Debug.Log("url changed to " + url);
            }
            
            Debug.Log(url);
            var APIkey = ELAuth.PrivateApiKey;
            string jsonBody = jsonBodyTemplate
                .Replace("{{text}}", input)
                .Replace("{{stability}}", stability.ToString(CultureInfo.InvariantCulture))
                .Replace(
                    "{{similarity_boost}}",
                    similarity_boost.ToString(CultureInfo.InvariantCulture)
                );
            Debug.Log("json body is: " + jsonBody);
            // Create request content
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            UploadHandlerRaw body = new UploadHandlerRaw(bodyRaw);
            body.contentType = "application/json";

            using (UnityWebRequest www = UnityWebRequest.Post(url, ""))
            {
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", APIkey);

                www.uploadHandler = body;
                www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Audio file download error: {www.error}");
                    yield break;
                }

                if (useAudioStreaming)
                    {
                        Debug.Log("Starting audio stream");
                        // Play an audio stream, which allows you to play the audio without downloading it. Not supported on all platforms. See: https://docs.elevenlabs.io/guides/text-to-speech
                        var audioStream = new MemoryStream(www.downloadHandler.data);
                        var audioReader = new Mp3FileReader(audioStream);
                        outputDevice.Init(audioReader);
                        outputDevice.Play();

                        // Wait for the audio stream to finish playing
                        while (audioReader.CurrentTime < audioReader.TotalTime)
                        {
                            yield return null;
                        }
                        Debug.Log("Audio stream is done playing");
                        responsePlaying = false;
                    }
                    else
                    {
                        Debug.Log("Downloading audio file");
                        // Play the downloaded audio file. This is the easiest way to play audio, but it requires downloading the entire file before playing it.
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                        AudioSource audioSource = GetComponent<AudioSource>();
                        audioSource.clip = audioClip;
                        audioSource.loop = false;
                        audioSource.Play();
                        Debug.Log($"Audio response is {audioSource.clip.length} seconds long");
                        // Check if the audio source is playing and wait until it stops playing
                        while (audioSource.isPlaying)
                        {
                            yield return null;
                        }
                        Debug.Log("Downloaded Audio is done playing");
                        responsePlaying = false;
                    }
                        }
                    }

                    private void OnApplicationQuit()
                    {
                        // Stop the player from exiting the app
                        outputDevice.Stop();
                        outputDevice.Dispose();
                    }
                }
}
