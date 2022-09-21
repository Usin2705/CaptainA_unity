using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class AudioManager : MonoBehaviour
{
    static AudioManager audioManager;
    [SerializeField] AudioClip recordNotif;

    int frequency = 16000;
    int maxRecTime = 5;

    AudioSource audioSource;
    AudioClip recording;

    void Awake()
    {
        if (audioManager != null)
        {
            Debug.LogError("Multiple AudioManagers");
            return;
        }
        audioSource = GetComponent<AudioSource>();
        audioManager = this;
    }

    public static AudioManager GetManager()
    {
        return audioManager;
    }

    public void RecordSound()
    {
        audioSource.PlayOneShot(recordNotif, 0.5f);

        // Debug.Log("Name: " + Microphone.devices[0]);        

        // if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        // {
        //     Debug.Log("Microphone found");
        // }
        // else
        // {
        //     Debug.Log("Microphone not found");
        // }
        recording = Microphone.Start(Microphone.devices[0], false, maxRecTime, frequency);
    }

    public void StopAndReplay()
    {
        Microphone.End("");
        audioSource.clip = recording;
        float[] audioBufferFloat = new float[audioSource.clip.samples * audioSource.clip.channels];
        audioSource.clip.GetData(audioBufferFloat, 0);

        // create a byte array and copy the floats into it...
        var byteArray = new byte[audioBufferFloat.Length * 4];
        Buffer.BlockCopy(audioBufferFloat, 0, byteArray, 0, byteArray.Length);

        // create a second float array and copy the bytes into it...
        //var floatArray2 = new float[byteArray.Length / 4];
        //Buffer.BlockCopy(byteArray, 0, floatArray2, 0, byteArray.Length);
        // do we have the same sequence of floats that we started with?
        //Debug.Log(audioBufferFloat.SequenceEqual(floatArray2));    // True

        StartCoroutine(NetworkManager.GetManager().ServerPost("test", byteArray));
        Invoke(nameof(ReplayRecordedSample), 0.5f);
    }

    void ReplayRecordedSample()
    {        
        Debug.Log("Audio is playing");
        audioSource.Play();
    }
}


