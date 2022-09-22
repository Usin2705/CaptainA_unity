using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonRecord : MonoBehaviour
{

    //TODO: ButtonGO should not handle other GO (input)
    // Sannitize input should be done in inputGO script
    // And a game view should be the one that handle all func that require 
    // interconnection between different variable in different GOs
    
    [SerializeField] GameObject buttonGO;     
    [SerializeField] GameObject inputGO;     

    public void OnButtonPointerEnter() {
        // Attached to ButtonRecord GameObject        
        Debug.Log("Pointer Pressed");
        AudioManager.GetManager().RecordSound();
    }


    public void OnButtonPointerUp() {
        // Attached to ButtonRecord GameObject
        Debug.Log("Pointer Up");    


        AudioManager.GetManager().ReplayAndPost(inputGO.GetComponent<TMP_InputField>().text);
        buttonGO.SetActive(false);
    }


    void SetButtonState(bool state) {        
        // TODO Change from disable to a loading animated sprite 
        buttonGO.SetActive(state);
    }
}
