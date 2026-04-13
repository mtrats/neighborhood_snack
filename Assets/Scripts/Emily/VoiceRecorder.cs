using UnityEngine;
using System.Collections;

public class VoiceRecorder : MonoBehaviour
{
    private string _device;
    private AudioClip _recording;
    private bool _isRecording;

    // We'll use this to tell other scripts when we're done
    public delegate void RecordingComplete(AudioClip clip);
    public event RecordingComplete OnRecordingComplete;

    void Start()
    {
        // Get the default microphone device
        if (Microphone.devices.Length > 0)
        {
            _device = Microphone.devices[0];
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }
    }

    public void StartRecording(int seconds = 5)
    {
        if (_device == null || _isRecording) return;

        Debug.Log("Recording started...");
        _isRecording = true;
        
        // null uses default device, 16000Hz is standard for AI models
        _recording = Microphone.Start(_device, false, seconds, 16000);
        
        // Start a coroutine to wait for the timer to finish
        StartCoroutine(WaitAndStop(seconds));
    }

    private IEnumerator WaitAndStop(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopRecording();
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        Debug.Log("Recording stopped.");
        Microphone.End(_device);
        _isRecording = false;

        // Fire the event so the Gemini script knows it's time to work
        OnRecordingComplete?.Invoke(_recording);
    }
}