using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvancePanel : MonoBehaviour
{
    [SerializeField] GameObject advancePanelGO;
    [SerializeField] GameObject describeButtonAGO;
    [SerializeField] GameObject describeButtonBGO;
    [SerializeField] GameObject describeButtonCGO;
    [SerializeField] GameObject describePanelAGO;
    [SerializeField] GameObject describePanelBGO;
    [SerializeField] GameObject describePanelCGO;

    List<GameObject> listScrollItems = new List<GameObject>();            

    void OnEnable() 
    {
        // Check if the user has correct secret text      
        string secretText = PlayerPrefs.GetString(Const.PREF_SECRET_TEXT);  
        secretText = secretText.Replace("\r", "").Replace("\n", "").Trim();                
        // Remove the last character if the could be a special character
        if (secretText.Length == Secret.SECRET_TEXT.Length + 1) {
           secretText = secretText[..^1]; 
        }

        if (secretText == Secret.SECRET_TEXT) {          
            describeButtonAGO.GetComponent<Button>().onClick.AddListener(OnDescribeAButtonClicked);
            describeButtonBGO.GetComponent<Button>().onClick.AddListener(OnDescribeBButtonClicked);
            describeButtonCGO.GetComponent<Button>().onClick.AddListener(OnDescribeCButtonClicked);

            describeButtonAGO.SetActive(true);
            describeButtonBGO.SetActive(true);
            describeButtonCGO.SetActive(true);

        } else {
            describeButtonAGO.GetComponent<Button>().onClick.RemoveAllListeners();
            describeButtonBGO.GetComponent<Button>().onClick.RemoveAllListeners();
            describeButtonCGO.GetComponent<Button>().onClick.RemoveAllListeners();

            describeButtonAGO.SetActive(false);
            describeButtonBGO.SetActive(false);
            describeButtonCGO.SetActive(false);
        }


        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);

        // Check if the instruction panel has been shown before
        if (!PlayerPrefs.HasKey(Const.PREF_INS_ADVANCE)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
            popUpPanel.OpenPanel(Const.PREF_INS_ADVANCE);
            popUpPanel.SetText(Const.INSTRUCTION_ADVANCE);
        }
    }

    public void OnDescribeAButtonClicked() 
    {
        describePanelAGO.SetActive(true);
    }

    public void OnDescribeBButtonClicked() 
    {
        describePanelBGO.SetActive(true);
    }

    public void OnDescribeCButtonClicked() 
    {
        describePanelCGO.SetActive(true);
    }


    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        foreach (GameObject go in listScrollItems) 
        {
            Destroy(go);
        }
    }
}
