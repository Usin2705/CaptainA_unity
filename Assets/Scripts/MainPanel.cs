using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using System;

public class MainPanel : MonoBehaviour
{
    [SerializeField] GameObject buttonRecordGO;     
    [SerializeField] GameObject inputTransGO;
    [SerializeField] GameObject errorTextGO;     
    [SerializeField] GameObject resultPanelGO;     
    [SerializeField] GameObject replayButtonGO;     

    AudioClip replayClip;

    public void OnButtonPointerDown() {
        // Attached to ButtonRecord GameObject        
        //Debug.Log("Pointer Pressed");

        errorTextGO.SetActive(false);

        inputTransGO.GetComponent<TMP_InputField>().text = SantinizeText(inputTransGO.GetComponent<TMP_InputField>().text);
        if (inputTransGO.GetComponent<TMP_InputField>().text!="") 
        {
            AudioManager.GetManager().StartRecording();
        }
        
    }

    public void OnButtonPointerUp() {
        // Attached to ButtonRecord GameObject
        //Debug.Log("Pointer Up");   
        if (inputTransGO.GetComponent<TMP_InputField>().text!="") 
        {
            buttonRecordGO.SetActive(false);
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
                AudioManager.GetManager().GetAudioAndPost(inputTransGO.GetComponent<TMP_InputField>().text, errorTextGO, resultPanelGO);
                
                // TODO Make this part more efficiency
                // The whole block stink
                // The idea is return the audioSource.clip
                // But the clip was trimmed & convert to wav in the above code
                // so we RELOAD it back to clip again, which is a waste of processing
                // but at least we got some nice trimmed audioclip        
                StartCoroutine(LoadAudioClip("speech_sample"));        

                Button replayButton = replayButtonGO.transform.GetComponent<Button>();                       
                replayButton.onClick.AddListener(() => ReplaySample()); 
                replayButtonGO.SetActive(true);
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

    void ReplaySample() {
        if (replayClip!=null) AudioManager.GetManager().PlayAudioClip(replayClip);
    }

    string SantinizeText(string text) 
    {
        text = text.Trim(); // Remove trailing white space
        text = Regex.Replace(text, "[zZ]", "ts"); //Replace z with ts
        text = Regex.Replace(text, "[0-9]", ""); //Remove numbers
        text = Regex.Replace(text, "[-!$%^&*()_+|~=`{}\\[\\]:\";'<>?,.\\/@]", ""); //Remove symbols

        return text;
    }
}
