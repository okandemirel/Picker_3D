using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using OpenAI.Integrations.VoiceRecorder;
using NAudio.Wave;
using UnityEngine;

namespace OpenAI.Integrations.VoiceRecorder.Streaming
{
    public class ELStreamingEngine
    {
        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState;
        public volatile bool fullyDownloaded;
        private static HttpClient httpClient;
        private VolumeWaveProvider16 volumeProvider;

        private Stream stream;
        private ReadFullyStream readFullyStream;

        byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame


        private int sampleCounter = 0;
        private bool internalInit;

        /// <summary>
        /// After this time if no data are received, the stream will be finish.
        /// </summary>
        public float maxWaitTime = 5;

        private float timestampWhenNoDataStart = 0;
        private bool noDataDetected;

        public void ChangeVolume(float volume)
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume = volume;
            }
        }


        private IMp3FrameDecompressor decompressor;

        public void InitStream(Stream stream)
        {
            if (internalInit)
            {
                Debug.LogError("ELStreaming not support multiple use. Please create new instance of this class");
            }

            this.stream = stream;
            readFullyStream = new ReadFullyStream(stream);
        }


        public void UpdateStream(AudioSource audioSource)
        {
            if (fullyDownloaded)
            {
                return;
            }

            Mp3Frame frame;
            using (readFullyStream)
            {
                var fallbackPosition = readFullyStream.Position;
                try
                {
                    frame = Mp3Frame.LoadFromStream(readFullyStream);
                }
                catch (EndOfStreamException e)
                {
                    // fullyDownloaded = true;
                    // reached the end of the MP3 file / stream
                    Debug.LogError("EndOfStreamException");
                    Debug.LogException(e);
                    return;
                }
            }

            if (frame == null)
            {
                if (noDataDetected)
                {
                    if (Time.time - timestampWhenNoDataStart > maxWaitTime)
                    {
                        fullyDownloaded = true;
                        Debug.Log("EndOfStream Detected");
                        return;
                    }
                }
                else
                {
                    noDataDetected = true;
                    timestampWhenNoDataStart = Time.time;
                }

                return;
            }

            if (decompressor == null)
            {
                // don't think these details matter too much - just help ACM select the right codec
                // however, the buffered provider doesn't know what sample rate it is working at
                // until we have a frame
                decompressor = CreateFrameDecompressor(frame);
            }

            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
            if (!internalInit)
            {
                internalInit = true;
                InternalInit(audioSource, frame);
            }

            byte[] data = new byte[decompressed];
            Array.Copy(buffer, data, decompressed);

            if (data.Length == 0)
            {
                return;
            }

            var frameDuration = (double) frame.SampleCount / (double) frame.SampleRate;
            // Debug.Log($"frameDuration {frameDuration}");

            var clip = audioSource.clip;
            int sampleIndexEnd = (int) (sampleCounter + frameDuration * clip.frequency);
            // int offsetSamples = clip.samples;
            // Debug.Log($"offsetSamples {sampleCounter}");
            audioSource.clip.SetData(ToFloatArray(data), sampleCounter);
            sampleCounter = sampleIndexEnd;
            audioSource.Play();

            //We have data in the buffer, so we skip no data detection
            noDataDetected = false;
            // Debug.Log("Play Mp3 frame");
        }

        private void InternalInit(AudioSource audioSource, Mp3Frame firstFrame)
        {
            int channels = firstFrame.ChannelMode == ChannelMode.Mono ? 1 : 2;
            int frequency = firstFrame.SampleRate;
            audioSource.clip = AudioClip.Create("stream-voice", firstFrame.SampleCount * channels * 1200, channels,
                frequency, false);
            // Debug.Log($"firstFrame channels: {channels} frequency: {frequency}");
        }

        // Konwersja bajtów na floaty
        private float[] ToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 2];

            for (int i = 0; i < floatArray.Length; i++)
            {
                short shortValue = (short) ((byteArray[i * 2 + 1] << 8) | byteArray[i * 2]);
                floatArray[i] = shortValue / 32768.0f;
            }

            return floatArray;
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate,
                frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);

            // WaveFormat waveFormat = new Mp3WaveFormat(44100,
            //     frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
            //     frame.FrameLength, 128);
            // return new AcmMp3FrameDecompressor(waveFormat);
        }
    }
}