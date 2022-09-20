using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonRecord : MonoBehaviour
{
    [SerializeField] GameObject buttonGO;     

    public void OnButtonPointerEnter() {
        // Attached to ButtonRecord GameObject        
        Debug.Log("Pointer Pressed");
        AudioManager.GetManager().RecordSound();
    }


    public void OnButtonPointerUp() {
        // Attached to ButtonRecord GameObject
        Debug.Log("Pointer Up");    
        AudioManager.GetManager().StopAndReplay();
        SetButtonState(false);
    }


    void SetButtonState(bool state) {        
        // TODO Change from disable to a loading animated sprite 
        buttonGO.SetActive(state);
    }
}
