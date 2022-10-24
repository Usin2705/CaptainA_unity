using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System;

public class MainPanel : MonoBehaviour
{
    [SerializeField] GameObject recordButtonGO;     
    [SerializeField] GameObject inputTransGO;
    [SerializeField] GameObject errorTextGO;     
    [SerializeField] GameObject resultPanelGO;     
    [SerializeField] GameObject replayButtonGO;     

    [SerializeField] DetailScorePanel detailScorePanel;

    [SerializeField] TMPro.TextMeshProUGUI predictionDebugText;

    AudioClip replayClip;

    public void OnButtonPointerDown() {
        // Attached to ButtonRecord GameObject        
        //Debug.Log("Pointer Pressed");

        errorTextGO.SetActive(false);

        inputTransGO.GetComponent<TMP_InputField>().text = TextUtils.SantinizeText(inputTransGO.GetComponent<TMP_InputField>().text);
        if (inputTransGO.GetComponent<TMP_InputField>().text!="") 
        {
            AudioManager.GetManager().StartRecording();
        }
        
    }

    // TODO
    // This code is 95% the same with ExercisePanel
    // maybe consider to merge both code into 1
    public void OnButtonPointerUp() {
        // Attached to ButtonRecord GameObject
        // Debug.Log("Pointer Up");   
        // We already done a santinize text before, but we did it again to keep the 
        // code consistent with Exercise Panel
        // NOTE that in MainPanel the text is get from TMP_InputField, not TMPro.TextMeshProUGUI
        string transcript = TextUtils.SantinizeText(inputTransGO.GetComponent<TMP_InputField>().text);
        if (transcript!="") 
        {
            recordButtonGO.SetActive(false);
            resultPanelGO.SetActive(false);

            // Maybe this won't cut the recording abruptly
            // by delay the microphone end by 0.5sec            
            StartCoroutine(DelayPost());
            IEnumerator DelayPost()
            {
                yield return new WaitForSeconds(0.5f);
            
                // Send transcript to server
                // errorTextGO to update if server yield error
                // resultPanelGO to update result (by Enable the AudioClip and display text result)
                predictionDebugText.text = "";
                AudioManager.GetManager().GetAudioAndPost(transcript, errorTextGO, resultPanelGO, recordButtonGO, predictionDebugText);

                // TODO Make this part more efficiency
                // The whole block stink
                // The idea is return the audioSource.clip
                // But the clip was trimmed & convert to wav in the above code
                // so we RELOAD it back to clip again, which is a waste of processing
                // but at least we got some nice trimmed audioclip        
                yield return StartCoroutine(LoadAudioClip(Const.REPLAY_FILENAME));        

                Button replayButton = replayButtonGO.transform.GetComponent<Button>();                       
                replayButton.onClick.RemoveAllListeners();    
                if(replayClip!=null) {
                    replayButton.onClick.AddListener(()=> AudioManager.GetManager().PlayAudioClip(replayClip));            
                    replayButtonGO.SetActive(true);
                } else {
                    replayButtonGO.SetActive(false);
                }

                // Look for the ResultTextButton (which will pop up the DetailScorePanel onclick)
                Button resultTextButton = resultPanelGO.transform.Find("ResultText").transform.GetComponent<Button>();
                // To be safe, remove all old listeners were add to this component
                resultTextButton.onClick.RemoveAllListeners();
                // Add onclick to text result
                resultTextButton.onClick.AddListener(() => detailScorePanel.ShowDetailScorePanel(transcript, null, replayClip));
            }
        }
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
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename.EndsWith(".wav") ? filename : filename + ".wav");
            
            using (var uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
        
                yield return uwr.SendWebRequest();
        
                if (uwr.result==UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {   
                    Debug.LogError("Failed to reload replay audio clip");
                    yield break;
                }
        
                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
        
                if (dlHandler.isDone)
                {
                    Debug.Log("Replay audio clip is loaded");
                    replayClip = dlHandler.audioClip;
                }
            }
            
            yield break;
        }
    }
}
