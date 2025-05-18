using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{

    [SerializeField] GameObject infoPanel;
    [SerializeField] GameObject secretPanel;

    public void ShowInfoPanel(){
        infoPanel.SetActive(true);
    }

    void Update()
    {
        // Handle back button press on new Input System
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    /*
    *   This function also attached to BackButton OnClick() in Unity
    */
    {
        infoPanel.SetActive(false);
    }

    public void SecretButton()
    /*
    *   This function also attached to SecretButton OnClick() in Unity
    */
    {
        secretPanel.SetActive(true);
    }
}
