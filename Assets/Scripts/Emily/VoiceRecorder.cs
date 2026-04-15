using UnityEngine;
using System.Collections;

public class VoiceRecorder : MonoBehaviour
{
    private string _device;
    private AudioClip _recording;
    private bool _isRecording;
    private Coroutine _recordingCoroutine;

    public delegate void RecordingComplete(AudioClip clip);
    public event RecordingComplete OnRecordingComplete;

    void Start()
    {
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
        _recording = Microphone.Start(_device, false, seconds, 16000);
        _recordingCoroutine = StartCoroutine(WaitAndStop(seconds));
    }

    private IEnumerator WaitAndStop(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopRecording();
    }

    public void StopRecording()
    {
        if (!_isRecording) return;
        if (_recordingCoroutine != null) StopCoroutine(_recordingCoroutine);
        Debug.Log("Recording stopped.");
        Microphone.End(_device);
        _isRecording = false;
        OnRecordingComplete?.Invoke(_recording);
    }
}