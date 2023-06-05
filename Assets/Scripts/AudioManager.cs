using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioManager : MonoBehaviour
{
    static AudioManager audioManager;
    [SerializeField] AudioClip recordNotif;
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

    public void StartRecording()
    {
        /*
        *   We can skip this block since we nolonger require a notification sound
        */

        //Debug.Log("Play record notif");
        //audioSource.PlayOneShot(recordNotif, 0.5f);
        //The notification sound cause some noise in the recording
        //So we need to delay it a little
        //The new notification sound is just 0.3f long
        //Invoke(nameof(RecordSound), 0.31f);
        RecordSound();
    }

    void RecordSound() 
    {
        // Debug.Log("Name: " + Microphone.devices[0]);        

        // if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        // {
        //     Debug.Log("Microphone found");
        // }
        // else
        // {
        //     Debug.Log("Microphone not found");
        // }
        audioSource.clip = Microphone.Start(Microphone.devices[0], false, Const.MAX_REC_TIME, Const.FREQUENCY);
    }
    public void PlayAudioClip(AudioClip audioClip) {
        // Adding an if statement so that user won't abuse the play audio
        audioSource.Stop();  // stop any audio that might be playing
        audioSource.PlayOneShot(audioClip);
    }
    
    public void GetAudioAndPost(string transcript, GameObject textErrorGO, TMPro.TextMeshProUGUI resultTextTMP, GameObject warningImageGO, 
                                GameObject resultPanelGO, GameObject recordButtonGO, TMPro.TextMeshProUGUI debugText)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(transcript, wavBuffer, textErrorGO, resultTextTMP, warningImageGO, resultPanelGO, recordButtonGO, debugText));
    }

    void ReplayRecordedSample()
    {        
        //Debug.Log("Audio is playing");

        // TODO: trim the audio clip, not just the wavBuffer
        audioSource.Play();
    }
}


