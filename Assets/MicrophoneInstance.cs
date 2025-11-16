using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent (typeof (AudioSource))]
public class MicrophoneInstance : MonoBehaviour
{
    AudioSource _audioSource;
    int lastPosition, currentPosition;

    [SerializeField, Range(0.1f, 2f)]
    float chunkLengthSeconds = 0.5f;

    public DeepgramInstance _deepgramInstance;

    readonly List<byte> pendingAudio = new List<byte>(8192);
    int chunkByteThreshold = 4096;

    void Start()
    {
        _audioSource = GetComponent<AudioSource> ();
        if (Microphone.devices.Length > 0)
        {
            _audioSource.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
            var samplesPerChunk = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(chunkLengthSeconds, 0.1f) * AudioSettings.outputSampleRate) * _audioSource.clip.channels);
            chunkByteThreshold = samplesPerChunk * sizeof(short);
        }
        else
        {
            Debug.Log("This will crash!");
        }

        _audioSource.Play();
    }

    void Update()
    {
        if ((currentPosition = Microphone.GetPosition(null)) > 0)
        {
            if (lastPosition > currentPosition)
                lastPosition = 0;

            if (currentPosition - lastPosition > 0)
            {
                float[] samples = new float[(currentPosition - lastPosition) * _audioSource.clip.channels];
                _audioSource.clip.GetData(samples, lastPosition);

                short[] samplesAsShorts = new short[(currentPosition - lastPosition) * _audioSource.clip.channels];
                for (int i = 0; i < samples.Length; i++)
                {
                    samplesAsShorts[i] = f32_to_i16(samples[i]);
                }

                var samplesAsBytes = new byte[samplesAsShorts.Length * sizeof(short)];
                System.Buffer.BlockCopy(samplesAsShorts, 0, samplesAsBytes, 0, samplesAsBytes.Length);
                pendingAudio.AddRange(samplesAsBytes);

                while (pendingAudio.Count >= chunkByteThreshold)
                {
                    var chunk = new byte[chunkByteThreshold];
                    pendingAudio.CopyTo(0, chunk, 0, chunkByteThreshold);
                    pendingAudio.RemoveRange(0, chunkByteThreshold);
                    _deepgramInstance.ProcessAudio(chunk);
                }

                if (!GetComponent<AudioSource>().isPlaying)
                    GetComponent<AudioSource>().Play();
                lastPosition = currentPosition;
            }
        }
    }

    short f32_to_i16(float sample)
    {
        sample = sample * 32768;
        if (sample > 32767)
        {
            return 32767;
        }
        if (sample < -32768)
        {
            return -32768;
        }
        return (short) sample;
    }
}

