using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class GPTGradingPanel : MonoBehaviour
{
    [SerializeField] GameObject gptGradingPanel;    
    [SerializeField] TMPro.TextMeshProUGUI transcriptText;    
    [SerializeField] TMPro.TextMeshProUGUI gptGradingText;       
    
    [SerializeField] GameObject ttsGO;

    AudioClip ttsClip;
    
    public void ShowGPTGradingPanel() 
    {
        string transcript = NetworkManager.GetManager().chatGPTTranscript;
        string grading = NetworkManager.GetManager().chatGPTGrading;
        
        transcriptText.text = transcript;
        gptGradingText.text = grading;

        gptGradingPanel.SetActive(true);

        StartCoroutine(LoadTTSClipCoroutine());
        
        IEnumerator LoadTTSClipCoroutine() {
            
            yield return StartCoroutine(LoadAudioClip("speech.mp3"));

            if (ttsClip != null) {
                Button ttsButton = ttsGO.transform.GetComponent<Button>();               
                // Need to remove old OnClick Listeners, otherwise it will keep adding up
                ttsButton.onClick.RemoveAllListeners();       
                ttsButton.onClick.AddListener(() => AudioManager.GetManager().PlayAudioClip(ttsClip));    
            }
        }
    }    

    void Update()
    {
        // Handle back button press on phone
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    /*
    *   This function also attached to BackButton OnClick() in Unity
    */
    {   
        gptGradingPanel.SetActive(false); 
    }

    IEnumerator LoadAudioClip(string filename) 
    /*
    *   This one should be called inside the Panel (not AudioManager)
    *   as it will update the replay audio with current replay audio
    *   Calling it inside AudioManager will lead to a few seconds
    *   of empty audioclip (somehow those wasn't trimmed in AudioManager
    *   but was trimmed from the wav file.
    *   Not very efficiency to reload but at least it work for now
    */
    {
        if(!String.IsNullOrEmpty(filename)) {
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename.EndsWith(".mp3") ? filename : filename + ".mp3");
            
            // Need the file:// for GetAudioClip
            // TODO check with iOS version does it need sth similar
            using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
        
                yield return uwr.SendWebRequest();
        
                if (uwr.result==UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {   
                    Debug.LogError("Failed to reload replay audio clip");
                    Debug.LogError(uwr.result);
                    Debug.LogError(path);
                    yield break;
                }
        
                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
        
                if (dlHandler.isDone)
                {
                    Debug.Log("Replay audio clip is loaded");
                    ttsClip = dlHandler.audioClip;
                }
            }
            
            yield break;
        }
    }    
}

