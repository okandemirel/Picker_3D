// This script listens to the microphone and when the input crosses the set threshold, it begins recording. If the volume of the input falls below that threshold for the set duration of time, it stops recording and sends it to the Whisper API for transcription. After it's transcribed, the text is sent to the OpenAI API (via OpenAIDemo.cs) for a completion which is then sent to the ElevenLabs API (via ELSpeaker.cs) for the generation of the voice and played.
#if !UNITY_WEBGL
using System;
using System.IO;
using System.Threading.Tasks;
using OpenAi.Api.V1;
using OpenAi.Unity.V1;
using OpenAI.Integrations.VoiceRecorder;
using TMPro;
using UnityEngine;
using OpenAI.DemoScript;

namespace OpenAI.Integrations.ElevenLabs
{
    [RequireComponent(typeof(AudioSource))]
    public class VoiceRecorder : MonoBehaviour
    {
        public TMP_InputField input; // Displays the transcription text response of the audio sent to the Whisper API
        public OpenAiCompleterV1 completer; // Holds the OpenAI API key settings
        private AudioSource audioSource; // The first audio source is responsible for playback
        private AudioSource micMonitor; // The second audio source is responsible for monitoring the mic

        public int recordingLength = 10; // Length of recording in seconds
        public float recordingThreshold = 0.8f; // Mic volume threshold for triggering recording
        public float recordingTimeThreshold = 2.0f; // Time threshold for stopping recording after mic volume drops below threshold
        public float sensitivity = 100.0f; // Used to adjust the sensitivity of the mic volume
        public float loudness = 0.0f; // Current volume level of the microphone
        private bool isRecording = false; // Used to make sure we don't start recording multiple times
        private bool processingResponse = false; // Used to make sure we don't start processing multiple responses
        private float timeBelowThreshold = 0.0f; // Used to keep track of how long the mic volume has been below the threshold

        void Start()
        {
            MonitorMic();
        }

        void Update()
        {
            // Get the current volume level
            ELSpeaker ELSpeaker = GetComponent<ELSpeaker>();
            if (!ELSpeaker.responsePlaying)
            {
                loudness = GetAveragedVolume() * sensitivity;
            }

            // Check if mic volume is above threshold and start recording if it is
            if (!isRecording && !processingResponse && loudness > recordingThreshold)
            {
                isRecording = true;
                StartRecording();
            }
            // Check if mic volume is below threshold and stop recording if it is for a certain amount of time
            else if (isRecording && loudness < recordingThreshold)
            {
                timeBelowThreshold += Time.deltaTime;
                //Debug.Log("Time below threshold: " + timeBelowThreshold + " Loudness: " + loudness + " is less than " + recordingThreshold);
                if (timeBelowThreshold >= recordingTimeThreshold)
                {
                    Debug.Log("Mic recording has stopped.");
                    StopRecording();
                    isRecording = false;
                    processingResponse = true;
                    timeBelowThreshold = 0.0f;
                    loudness = 0.0f;
                }
            }
            // Reset time below threshold if mic volume goes back above threshold
            else if (isRecording && loudness > recordingThreshold)
            {
                timeBelowThreshold = 0.0f;
            }
        }

        //Listen to the microphone
        void MonitorMic()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.volume = 0; // Mute the first audio source to prevent mic monitoring
            micMonitor = GetComponents<AudioSource>()[1];
            micMonitor.volume = 1; // Set the volume of the second audio source to 1 to monitor the mic
            GetComponent<AudioSource>().clip = Microphone.Start(null, true, recordingLength, 16000);
            GetComponent<AudioSource>().loop = true;
            while (!(Microphone.GetPosition(null) > 0)) { }
            GetComponent<AudioSource>().Play();
        }

        // Gets the current volume level
        float GetAveragedVolume()
        {
            float[] data = new float[1024];
            int micPosition = Microphone.GetPosition(null) - (1024 + 1); // Get the position 1024 samples ago
            if (micPosition < 0)
                return 0; // Return 0 if the position is negative
            audioSource.clip.GetData(data, micPosition);
            float a = 0;
            foreach (float s in data)
            {
                a += Mathf.Abs(s);
            }
            return a / 1024;
        }

        public void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone found to record audio clip sample with.");
                return;
            }
            string mic = Microphone.devices[0];
            Debug.Log("Mic audio crossed threshold, starting recording...");
            audioSource.clip = Microphone.Start(mic, false, recordingLength, 16000);
            //GetComponent<AudioSource>().loop = true;
            while (!(Microphone.GetPosition(null) > 0)) { }
            GetComponent<AudioSource>().Play();
        }

        public async void StopRecording()
        {
            //int recordingLength = Microphone.GetPosition(null) / 16000; // Calculate the actual length of the recording in seconds
            Microphone.End(null);
            string filepath;
            WavUtility.FromAudioClip(audioSource.clip, out filepath);
            Debug.Log($"Voice recording saved to: {filepath}");
            GetComponent<AudioSource>().Stop();
            // SavWav.Save(fileName, audioSource.clip);
            await TranscriptRecording();
            processingResponse = false;
            Debug.Log("Response processing complete, listening for new mic input...");
            MonitorMic();
        }

        public void PlayRecording()
        {
            audioSource.Play();
        }

        public async Task TranscriptRecording()
        {
            var transcript = await SendTranscriptRequest(
                audioSource.clip, // The audio file to be transcribed
                "Hello, welcome to my lecture." // For details on Whisper API prompting, see: https://platform.openai.com/docs/guides/speech-to-text/prompting
            );
            if (transcript.IsSuccess)
            {
                input.text = transcript.Result.text;
                OpenAIDemo openAIDemo = GetComponent<OpenAIDemo>();
                await openAIDemo.SendOpenAIRequest();
                ELSpeaker ELSpeaker = GetComponent<ELSpeaker>();
                ELSpeaker.SpeakSentenceFromInput();
                Debug.Log("Playing response audio...");
                GetComponent<AudioSource>().volume = 1;
                // Check if the ELSpeaker is playing and wait until it stops playing
                while (ELSpeaker.responsePlaying)
                {
                    await Task.Yield();
                }
            }
            else
            {
                input.text =
                    $"ERROR: StatusCode: {transcript.HttpResponse.responseCode} - {transcript.HttpResponse.error}";
            }
        }

        public async Task<ApiResult<TranscriptionV1>> SendTranscriptRequest(
            AudioClip clip,
            string prompt
        )
        {
            SOAuthArgsV1 auth = completer.Auth;
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());
            Debug.Log("Sending audio recording to the Whisper API...");
            //Convert voice from clip to wav
            string filepath;
            byte[] audioFile = WavUtility.FromAudioClip(audioSource.clip, out filepath, false);

            ApiResult<TranscriptionV1> comp =
                await api.Audio.Transcriptions.CreateTranscriptionAsync(
                    new TranscriptionRequestV1()
                    {
                        model = "whisper-1",
                        prompt = prompt,
                        response_format = "json",
                        audioFile = audioFile
                    }
                );

            return comp;
        }
    }
}
#endif