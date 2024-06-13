using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SecretPopUpPanel : MonoBehaviour
{
    [SerializeField] GameObject secretPopUpPanelGO;    
    [SerializeField] TMPro.TextMeshProUGUI secretTextTMP;

    // No longer have NO option :D
    public void OnNoButtonClick() {
        secretPopUpPanelGO.SetActive(false);
    }

    public void OnLaterButtonClick() {
        secretPopUpPanelGO.SetActive(false);
    }

    public void OnYesButtonClick() {
        string secretText = secretTextTMP.text;
        secretText = secretText.Replace("\r", "").Replace("\n", "").Trim();
        PlayerPrefs.SetString(Const.PREF_SECRET_TEXT, secretText);
		PlayerPrefs.Save();
        secretPopUpPanelGO.SetActive(false);
    }

    void Update()
    {
        // Handle back button press on phone
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            secretPopUpPanelGO.SetActive(false);
        }
    }
}
