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
        //Debug.Log("Play record notif");
        audioSource.PlayOneShot(recordNotif, 0.8f);
        //The notification sound cause some noise in the recording
        //So we need to delay it a little
        //No need to wait for the sound to finish playing
        //Invoke(nameof(RecordSound), Const.BUFFER_TIME);
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
        if (audioSource.isPlaying == false) audioSource.PlayOneShot(audioClip);
    }
    
    public void GetAudioAndPost(string transcript, GameObject textErrorGO, GameObject resultPanelGO, GameObject recordButtonGO, TMPro.TextMeshProUGUI debugText)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(transcript, wavBuffer, textErrorGO, resultPanelGO, recordButtonGO, debugText));
    }

    public void GetAudioAndPost(string transcript, GameObject textErrorGO, TMPro.TextMeshProUGUI flashCardText, GameObject recordButtonGO, TMPro.TextMeshProUGUI debugText)
    {
    /*
    *   Different from the above function, this function will not update the result panel
    *   Instead, it will update the flashCardText
    *   This function is used in the SuperMemo Panel
    */
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(transcript, wavBuffer, textErrorGO, flashCardText, recordButtonGO, debugText));
    }

    void ReplayRecordedSample()
    {        
        //Debug.Log("Audio is playing");

        // TODO: trim the audio clip, not just the wavBuffer
        audioSource.Play();
    }
}


