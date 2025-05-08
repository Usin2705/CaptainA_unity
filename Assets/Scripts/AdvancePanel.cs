using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvancePanel : MonoBehaviour
{
    [SerializeField] GameObject advancePanelGO;
    [SerializeField] GameObject numberGameButtonGO;
    [SerializeField] GameObject numberGamePanelGO;
    [SerializeField] GameObject describeButtonAGO;
    [SerializeField] GameObject describeButtonBGO;
    [SerializeField] GameObject describeButtonCGO;
    [SerializeField] GameObject describeButtonA2GO;
    [SerializeField] GameObject describeButtonB2GO;
    [SerializeField] GameObject describeButtonC2GO;
    [SerializeField] GameObject describePanelAGO;
    [SerializeField] GameObject describePanelBGO;
    [SerializeField] GameObject describePanelCGO;
    

    void OnEnable() 
    {
        // Check if the user has correct secret text      
        string secretText = PlayerPrefs.GetString(Const.PREF_SECRET_TEXT);  
        secretText = secretText.Replace("\r", "").Replace("\n", "").Trim();                
        // Remove the last character if there could be a special character
        if (secretText.Length == Secret.SECRET_TEXT.Length + 1) {
           secretText = secretText[..^1]; 
        }        
        numberGameButtonGO.GetComponent<Button>().onClick.AddListener(() => OnNumberGameButtonClicked());

        if (secretText == Secret.SECRET_TEXT) {          
            describeButtonAGO.GetComponent<Button>().onClick.AddListener(() => OnDescribeAButtonClicked());
            describeButtonBGO.GetComponent<Button>().onClick.AddListener(() => OnDescribeBButtonClicked());
            describeButtonCGO.GetComponent<Button>().onClick.AddListener(() => OnDescribeCButtonClicked());

            describeButtonAGO.SetActive(true);
            describeButtonBGO.SetActive(true);
            describeButtonCGO.SetActive(true);

            // English
            describeButtonA2GO.GetComponent<Button>().onClick.AddListener(() => OnDescribeAButtonClicked(DescribePanel.TaskType.A2));
            describeButtonB2GO.GetComponent<Button>().onClick.AddListener(() => OnDescribeBButtonClicked(DescribePanel.TaskType.B2));
            describeButtonC2GO.GetComponent<Button>().onClick.AddListener(() => OnDescribeCButtonClicked(DescribePanel.TaskType.C2));

            describeButtonA2GO.SetActive(true);
            describeButtonB2GO.SetActive(true);
            describeButtonC2GO.SetActive(true);

        } else {
            describeButtonAGO.GetComponent<Button>().onClick.RemoveAllListeners();
            describeButtonBGO.GetComponent<Button>().onClick.RemoveAllListeners();
            describeButtonCGO.GetComponent<Button>().onClick.RemoveAllListeners();

            describeButtonAGO.SetActive(false);
            describeButtonBGO.SetActive(false);
            describeButtonCGO.SetActive(false);

            // English
            describeButtonA2GO.GetComponent<Button>().onClick.RemoveAllListeners();
            describeButtonB2GO.GetComponent<Button>().onClick.RemoveAllListeners();
            describeButtonC2GO.GetComponent<Button>().onClick.RemoveAllListeners();

            describeButtonA2GO.SetActive(false);
            describeButtonB2GO.SetActive(false);
            describeButtonC2GO.SetActive(false);
        }

        numberGamePanelGO.SetActive(false);
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

    public void OnNumberGameButtonClicked(NGTaskType taskType = NGTaskType.EASY) 
    {
        numberGamePanelGO.SetActive(true);
        NumberGamePanel numberGamePanel = numberGamePanelGO.GetComponent<NumberGamePanel>();
        if (numberGamePanel != null)
        {
            numberGamePanel.SetTaskType(taskType);
        }
    }

    public void OnDescribeAButtonClicked(DescribePanel.TaskType taskType = DescribePanel.TaskType.A) 
    {
        describePanelAGO.SetActive(true);
        DescribePanel describePanel = describePanelAGO.GetComponent<DescribePanel>();
        if (describePanel != null)
        {
            describePanel.setTaskType(taskType);
        }
    }

    public void OnDescribeBButtonClicked(DescribePanel.TaskType taskType = DescribePanel.TaskType.B) 
    {
        Debug.Log("OnDescribeBButtonClicked");
        describePanelBGO.SetActive(true);
        DescribePanel describePanel = describePanelBGO.GetComponent<DescribePanel>();
        if (describePanel != null)
        {
            describePanel.setTaskType(taskType);
        }
    }

    public void OnDescribeCButtonClicked(DescribePanel.TaskType taskType = DescribePanel.TaskType.C) 
    {
        describePanelCGO.SetActive(true);
        DescribePanel describePanel = describePanelCGO.GetComponent<DescribePanel>();
        if (describePanel != null)
        {
            describePanel.setTaskType(taskType);
        }
    }


    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        numberGamePanelGO.SetActive(false);
        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);
    }
}
