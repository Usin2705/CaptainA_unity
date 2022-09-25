using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;


public class AudioManager : MonoBehaviour
{
    static AudioManager audioManager;
    [SerializeField] AudioClip recordNotif;

    int frequency = 16000;
    int maxRecTime = 5;

    AudioSource audioSource;

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

        audioSource.clip = Microphone.Start(Microphone.devices[0], false, maxRecTime, frequency);
    }

    public void ReplayAndPost(string transcript, GameObject textErrorGO)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save("speech_sample", audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(transcript, wavBuffer, textErrorGO));
        Invoke(nameof(ReplayRecordedSample), 0.5f);
    }

    void ReplayRecordedSample()
    {        
        Debug.Log("Audio is playing");

        // TODO: trim the audio clip, not just the wavBuffer
        audioSource.Play();
    }
}


