using UnityEngine;

namespace OpenAI.DemoScript{
public static class MicInput
{
    public static float MicLoudness;
    private static string _device;

    private static AudioClip _clipRecord;
    private static bool _isRecording;

    public static void Start()
    {
        if (_isRecording) return;
        _device = Microphone.devices[0];
        _clipRecord = Microphone.Start(_device, true, 999, 44100);
        _isRecording = true;
    }

    public static void Stop()
    {
        if (!_isRecording) return;
        Microphone.End(_device);
        _isRecording = false;
    }

    public static float LevelMax()
    {
        var levelMax = 0f;
        var waveData = new float[1024];
        var micPosition = Microphone.GetPosition(null) - (1024 + 1);
        if (micPosition < 0) return -1;
        _clipRecord.GetData(waveData, micPosition);
        for (var i = 0; i < 1024; i++)
        {
            var wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        MicLoudness = Mathf.Sqrt(Mathf.Sqrt(levelMax));
        return MicLoudness;
    }

}
}
