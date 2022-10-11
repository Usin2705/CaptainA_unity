using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class MainPanel : MonoBehaviour
{
    [SerializeField] GameObject buttonRecordGO;     
    [SerializeField] GameObject inputTransGO;
    [SerializeField] GameObject textErrorGO;     
    [SerializeField] GameObject textResultGO;     

    public void OnButtonPointerDown() {
        // Attached to ButtonRecord GameObject        
        //Debug.Log("Pointer Pressed");

        textErrorGO.SetActive(false);

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

            // Maybe this won't cut the recording abruptly
            // by delay the microphone end by 0.5sec            
            StartCoroutine(DelayReplayAndPost());
            IEnumerator DelayReplayAndPost()
            {
                yield return new WaitForSeconds(0.5f);
            
                AudioManager.GetManager().ReplayAndPost(inputTransGO.GetComponent<TMP_InputField>().text, textErrorGO, textResultGO);
            }
        }
    }

    string SantinizeText(string text) 
    {
        text = text.Trim(); // Remove trailing white space
        text = Regex.Replace(text, "[zZ]", "ts"); //Replace z with ts
        text = Regex.Replace(text, "[0-9]", ""); //Remove numbers
        text = Regex.Replace(text, "[-!$%^&*()_+|~=`{}\\[\\]:\";'<>?,.\\/@]", ""); //Remove symbols

        return text;
    }

    void SetButtonState(bool state) {        
        // TODO Change from disable to a loading animated sprite 
        buttonRecordGO.SetActive(state);
    }
}
