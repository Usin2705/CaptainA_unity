using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdvancePanel : MonoBehaviour
{
    [SerializeField]
    GameObject advancePanelGO;

    [SerializeField]
    GameObject taskPanelGO;

    [SerializeField]
    GameObject numberGameButtonGO;

    [SerializeField]
    GameObject numberGamePanelGO;

    [SerializeField]
    GameObject ASAButtonGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject consentPopUpGO;

    [SerializeField]
    GameObject acceptButtonGO;

    [SerializeField]
    GameObject refuseButtonGO;

    [SerializeField]
    GameObject backgroundPopUpGO;

    [SerializeField]
    GameObject sendButtonGO;

    [SerializeField]
    GameObject dimPanelGO;

    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject describeButtonAGO;

    [SerializeField]
    GameObject describeButtonBGO;

    [SerializeField]
    GameObject describeButtonCGO;

    [SerializeField]
    GameObject describeButtonA2GO;

    [SerializeField]
    GameObject describeButtonB2GO;

    [SerializeField]
    GameObject describeButtonC2GO;

    [SerializeField]
    GameObject describePanelAGO;

    [SerializeField]
    GameObject describePanelBGO;

    [SerializeField]
    GameObject describePanelCGO;

    void OnEnable()
    {
        // This is for testing purposes
        PlayerPrefs.SetInt("InfoPopupSeen", 0);
        PlayerPrefs.SetInt("ConsentGiven", 0);
        PlayerPrefs.Save();

        refuseButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        acceptButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();

        // Check if the user has correct secret text
        string secretText = PlayerPrefs.GetString(Const.PREF_SECRET_TEXT);
        secretText = secretText.Replace("\r", "").Replace("\n", "").Trim();
        // Remove the last character if there could be a special character
        if (secretText.Length == Secret.SECRET_TEXT.Length + 1)
        {
            secretText = secretText[..^1];
        }
        numberGameButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => OnNumberGameButtonClicked());
        ASAButtonGO.GetComponent<Button>().onClick.AddListener(() => OnASAButtonClicked());

        acceptButtonGO.GetComponent<Button>().onClick.AddListener(() => AcceptConsent());
        refuseButtonGO.GetComponent<Button>().onClick.AddListener(() => RefuseConsent());

        sendButtonGO.GetComponent<Button>().onClick.AddListener(() => ValidateInformation());

        if (secretText == Secret.SECRET_TEXT)
        {
            describeButtonAGO
                .GetComponent<Button>()
                .onClick.AddListener(() => OnDescribeAButtonClicked());
            describeButtonBGO
                .GetComponent<Button>()
                .onClick.AddListener(() => OnDescribeBButtonClicked());
            describeButtonCGO
                .GetComponent<Button>()
                .onClick.AddListener(() => OnDescribeCButtonClicked());

            describeButtonAGO.SetActive(false);
            describeButtonBGO.SetActive(false);
            describeButtonCGO.SetActive(false);

            // English
            describeButtonA2GO
                .GetComponent<Button>()
                .onClick.AddListener(() => OnDescribeAButtonClicked(DescribePanel.TaskType.A2));
            describeButtonB2GO
                .GetComponent<Button>()
                .onClick.AddListener(() => OnDescribeBButtonClicked(DescribePanel.TaskType.B2));
            describeButtonC2GO
                .GetComponent<Button>()
                .onClick.AddListener(() => OnDescribeCButtonClicked(DescribePanel.TaskType.C2));

            describeButtonA2GO.SetActive(false);
            describeButtonB2GO.SetActive(false);
            describeButtonC2GO.SetActive(false);
        }
        else
        {
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
        ASAPanelGO.SetActive(false);
        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);
        feedbackPanelGO.SetActive(false);
        taskPanelGO.SetActive(false);

        // Check if the instruction panel has been shown before
        if (!PlayerPrefs.HasKey(Const.PREF_INS_ADVANCE))
        {
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

    public void OnASAButtonClicked()
    {
        if (PlayerPrefs.GetInt("ConsentGiven", 0) == 1)
        {
            Debug.Log(PlayerPrefs.GetString("user_guid"));
            taskPanelGO.SetActive(true);
        }
        else
        {
            consentPopUpGO.SetActive(true);
            dimPanelGO.SetActive(true);
        }
    }

    public void AcceptConsent()
    {
        string guid = Guid.NewGuid().ToString();
        PlayerPrefs.SetString("user_guid", guid);

        PlayerPrefs.SetInt("ConsentGiven", 1);
        PlayerPrefs.Save();

        consentPopUpGO.SetActive(false);
        backgroundPopUpGO.SetActive(true);

        StartCoroutine(NetworkManager.GetManager().ServerPost_guid(POSTType.ASA_CONSENT, null));
    }

    public void RefuseConsent()
    {
        consentPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);
    }

    public void ValidateInformation()
    {
        backgroundPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);

        taskPanelGO.SetActive(true);
    }

    /* */
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
        ASAPanelGO.SetActive(false);
        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);
    }
}
