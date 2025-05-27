using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;


public class AudioManager : MonoBehaviour
{
    static AudioManager audioManager;
    [SerializeField] AudioClip recordNotif;
    AudioSource audioSource;

    private AudioClip replayClip;

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

    public void StartRecording(int lengthSec)
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
        RecordSound(lengthSec);
    }

    void RecordSound(int lengthSec) 
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
        audioSource.clip = Microphone.Start(Microphone.devices[0], false, lengthSec, Const.FREQUENCY);
    }
    public void PlayAudioClip(AudioClip audioClip) {
        // Adding an if statement so that user won't abuse the play audio
        audioSource.Stop();  // stop any audio that might be playing
        audioSource.PlayOneShot(audioClip, 1.0f);
    }

    public AudioClip GetReplayClip() {
        return replayClip;
    }   
    
    public void GetAudioAndPost(POSTType postType,string transcript, GameObject textErrorGO, GameObject resultTextGO, 
                                       GameObject resultPanelGO, GameObject debugTextGO, System.Action OnServerDone = null)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(postType, 
                                                              transcript, 
                                                              wavBuffer, 
                                                              textErrorGO, 
                                                              resultTextGO, 
                                                              resultPanelGO, 
                                                              debugTextGO, 
                                                              OnServerDone));
    }    




    public void GetAudioAndPost(string transcript, GameObject textErrorGO, TMPro.TextMeshProUGUI resultTextTMP, GameObject warningImageGO, 
                                GameObject resultPanelGO, TMPro.TextMeshProUGUI debugText)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().ServerPost(transcript, wavBuffer, textErrorGO, resultTextTMP, warningImageGO, resultPanelGO, debugText));
    }

    public void GetAudioAndASR(GameObject transcriptGO, GameObject scoreButtonGO, DescribePanel.TaskType taskType, int taskNumber, bool isFinnish=true)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.DESCRIBE_FILENAME, audioSource.clip, trim:true); // for debug purpose

        //StartCoroutine(NetworkManager.GetManager().GPTTranscribe(wavBuffer, transcriptGO, scoreButtonGO, taskType, taskNumber, isFinnish));
        StartCoroutine(NetworkManager.GetManager().GPTTranscribeWhisper(wavBuffer, transcriptGO, scoreButtonGO, taskType, taskNumber, isFinnish));
    }

    public void GetAudioAndNG(string number, GameObject textErrorGO, TMPro.TextMeshProUGUI resultTextTMP)
    {
        Microphone.End("");        
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim:true);
        SavWav.Save(Const.NUMBERGAME_FILENAME, audioSource.clip, trim:true); // for debug purpose

        StartCoroutine(NetworkManager.GetManager().NumberGamePost(number, wavBuffer, textErrorGO, resultTextTMP));
    }

    public IEnumerator LoadAudioClip(string filename, GameObject replayButtonGO)
    {
        if(!string.IsNullOrEmpty(filename)) {
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename.EndsWith(".wav") ? filename : filename + ".wav");
            
            // Need the file:// for GetAudioClip            
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))            
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
        
                yield return uwr.SendWebRequest();
        
                if (uwr.result==UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {   
                    Debug.LogError("Failed to reload replay audio clip");
                    Debug.LogError(uwr.result);
                    Debug.LogError(path);
                    replayClip = null;
                    if (replayButtonGO != null)
                    {
                        replayButtonGO.SetActive(false);
                    }
                    yield break;
                }
        
                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
        
                if (dlHandler.isDone)
                {
                    Debug.Log("Replay audio clip is loaded");
                    replayClip = dlHandler.audioClip;
                    if (replayButtonGO != null)
                    {
                        replayButtonGO.transform.GetComponent<Button>().onClick.AddListener(()=> GetManager().PlayAudioClip(replayClip));            
                        replayButtonGO.SetActive(true);
                    }   
                }
            }
            
            yield break;
        }
    }

}


