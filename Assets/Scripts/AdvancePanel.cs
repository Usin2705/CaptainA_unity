using System;
using System.Collections.Generic;
using System.Linq;
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

    // "I have read and understood the Privacy Notice." on the consent sheet.
    // Continue stays disabled until this is ticked, so consent cannot be given by
    // reflex-tapping the primary button. Optional: leave unassigned and the sheet
    // behaves as it did before, with Continue always enabled.
    [SerializeField]
    Toggle consentReadToggle;

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

    [SerializeField]
    GameObject loadingPopUpGO;

    [SerializeField]
    GameObject profilePanelGO;

    [SerializeField]
    ASAPanel ASAPanel;

    [SerializeField]
    GameObject asaSecretCodePopUp;

    [SerializeField]
    private TMP_InputField languageOtherField;

    [SerializeField]
    private Toggle languageOther;

    [SerializeField]
    private GameObject languageOtherFieldGO;

    [SerializeField]
    private TMP_InputField motherTongueOptions;

    [SerializeField]
    private GameObject otherLanguageOptionsGO;

    [SerializeField]
    private TextMeshProUGUI errorMessage;

    public ToggleGroup genderOptions;

    public ToggleGroup ageOptions;

    public ToggleGroup movedToFinlandOptions;

    public ToggleGroup learnedFinnishOptions;

    public ToggleGroup selfAssessmentOptions;

    [System.Serializable]
    public class BackgroundFormData
    {
        public (string, string) gender;
        public (string, string) age;
        public (string, string) motherTongue;
        public (string, string) otherLanguages;
        public (string, string) movedToFinland;
        public (string, string) learnedFinnish;
        public (string, string) selfAssessment;
    }

    void OnEnable()
    {
        // Check if the user has correct secret text
        string secretText = PlayerPrefs.GetString(Const.PREF_SECRET_TEXT);
        secretText = secretText.Replace("\r", "").Replace("\n", "").Trim();

        // Remove the last character if there is a special character at the end
        if (secretText.Length == Secret.SECRET_TEXT.Length + 1)
        {
            secretText = secretText[..^1];
        }

        refuseButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        acceptButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();

        numberGameButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => OnNumberGameButtonClicked());
        ASAButtonGO.GetComponent<Button>().onClick.AddListener(() => OnASAButtonClicked());

        acceptButtonGO.GetComponent<Button>().onClick.AddListener(() => AcceptConsent());
        refuseButtonGO.GetComponent<Button>().onClick.AddListener(() => RefuseConsent());

        // Continue follows the checkbox rather than being live from the start. Consent
        // that was given without the notice being acknowledged is not worth much, and
        // the disabled button is what tells the user why nothing happened.
        if (consentReadToggle != null)
        {
            consentReadToggle.onValueChanged.AddListener(isOn =>
                acceptButtonGO.GetComponent<Button>().interactable = isOn
            );
        }

        sendButtonGO.GetComponent<Button>().onClick.AddListener(() => GetUserInput());

        languageOtherFieldGO.SetActive(false);
        languageOther.onValueChanged.AddListener(isOn =>
        {
            languageOtherFieldGO.SetActive(isOn);
        });

        errorMessage.enabled = false;

        // This part is legacy code and these describeButton game objects are not currently active / in use
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

        // Set the proper panels and game objects as inactive at first
        numberGamePanelGO.SetActive(false);
        ASAPanelGO.SetActive(false);
        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);
        feedbackPanelGO.SetActive(false);
        taskPanelGO.SetActive(false);
        loadingPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);
        consentPopUpGO.SetActive(false);
        backgroundPopUpGO.SetActive(false);
        profilePanelGO.SetActive(false);

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
        // Go to number game (not very polished or functional for now)
        numberGamePanelGO.SetActive(true);
        NumberGamePanel numberGamePanel = numberGamePanelGO.GetComponent<NumberGamePanel>();
        if (numberGamePanel != null)
        {
            numberGamePanel.SetTaskType(taskType);
        }
    }

    public void OnASAButtonClicked()
    {
        // Check if secret code has been verified first
        if (!ASASecretCodePopUp.IsASASecretVerified())
        {
            ASASecretCodePopUp secretCodePopUp =
                asaSecretCodePopUp.GetComponent<ASASecretCodePopUp>();
            if (secretCodePopUp != null)
            {
                secretCodePopUp.ShowSecretCodePopUp(() => ProceedToASA());
            }
            return;
        }

        ProceedToASA();
    }

    private void ProceedToASA()
    {
        // Go to ASA task selection if consent has been given and the background form has been filled
        // Open relevant pop-ups otherwise
        if (
            PlayerPrefs.GetInt("ConsentGiven", 0) == 1
            && PlayerPrefs.GetInt("BackgroundFormCompleted", 0) == 1
        )
        {
            Debug.Log(PlayerPrefs.GetString("UserGuid"));
            taskPanelGO.SetActive(true);
        }

        if (
            PlayerPrefs.GetInt("ConsentGiven", 0) == 1
            && PlayerPrefs.GetInt("BackgroundFormCompleted", 0) == 0
        )
        {
            backgroundPopUpGO.SetActive(true);
            dimPanelGO.SetActive(true);
        }

        if (
            PlayerPrefs.GetInt("ConsentGiven", 0) == 0
            && PlayerPrefs.GetInt("BackgroundFormCompleted", 0) == 0
        )
        {
            ResetConsentSheet();
            consentPopUpGO.SetActive(true);
            dimPanelGO.SetActive(true);
        }
    }

    // The sheet is a single reused object, so clear it every time it opens. Without this
    // a user who ticked the box, backed out, and came back would find Continue already
    // enabled and could consent without seeing the notice again.
    private void ResetConsentSheet()
    {
        if (consentReadToggle == null)
        {
            return;
        }

        consentReadToggle.isOn = false;
        acceptButtonGO.GetComponent<Button>().interactable = false;
    }

    public void LinkButtonPressed()
    {
        Application.OpenURL("https://aalto-speech.github.io/privacy-notices/saysuomi-privacy.html");
    }

    public void AcceptConsent()
    {
        // Create new user after consent has been given
        string guid = Guid.NewGuid().ToString();
        PlayerPrefs.SetString("UserGuid", guid);

        PlayerPrefs.SetInt("ConsentGiven", 1);

        PlayerPrefs.SetString("AppVersion", Application.version);

        string timestamp = System.DateTime.UtcNow.ToString("o");
        PlayerPrefs.SetString("ConsentTimestamp", timestamp);
        PlayerPrefs.Save();

        consentPopUpGO.SetActive(false);
        backgroundPopUpGO.SetActive(true);
    }

    public void RefuseConsent()
    {
        consentPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);
    }

    public void GetUserInput()
    {
        // Set all relevant values for the user based on filled background form
        bool isValid = ValidateInformation();
        if (!isValid)
        {
            return;
        }

        var gender = genderOptions
            .ActiveToggles()
            .FirstOrDefault()
            .gameObject.GetComponent<OptionValue>()
            .value;

        var age = ageOptions
            .ActiveToggles()
            .FirstOrDefault()
            .gameObject.GetComponent<OptionValue>()
            .value;

        var motherTongue = new List<string> { motherTongueOptions.text };

        List<string> otherLanguages = new();
        foreach (var toggle in otherLanguageOptionsGO.GetComponentsInChildren<Toggle>())
        {
            if (toggle.isOn)
            {
                otherLanguages.Add(toggle.gameObject.GetComponent<OptionValue>().value);
            }
        }

        var otherLanguagesTextField = languageOtherField.text;
        otherLanguages.Add(otherLanguagesTextField);

        var movedToFinland = movedToFinlandOptions
            .ActiveToggles()
            .FirstOrDefault()
            .gameObject.GetComponent<OptionValue>()
            .value;

        var learnedFinnish = learnedFinnishOptions
            .ActiveToggles()
            .FirstOrDefault()
            .gameObject.GetComponent<OptionValue>()
            .value;

        var selfAssessment = selfAssessmentOptions
            .ActiveToggles()
            .FirstOrDefault()
            .gameObject.GetComponent<OptionValue>()
            .value;

        string timestamp = System.DateTime.UtcNow.ToString("o");
        PlayerPrefs.SetString("BackgroundTimestamp", timestamp);
        PlayerPrefs.Save();

        string motherWrapped = string.Join("\n", motherTongue);
        string otherWrapped = string.Join("\n", otherLanguages);

        // Prepare the form to be sent to the server
        BackgroundFormData backgroundFormData = new()
        {
            gender = ("gender", gender),
            age = ("age_group", age),
            motherTongue = ("native_languages", motherWrapped),
            otherLanguages = ("other_languages", otherWrapped),
            movedToFinland = ("moved_to_finland", movedToFinland),
            learnedFinnish = ("finnish_learning_duration", learnedFinnish),
            selfAssessment = ("finnish_self_assessment", selfAssessment),
        };

        // Send consent and background form to server
        StartCoroutine(
            NetworkManager.GetManager().ServerPost_guid(POSTType.ASA_CONSENT, backgroundFormData)
        );

        PlayerPrefs.SetInt("BackgroundFormCompleted", 1);
        PlayerPrefs.Save();

        backgroundPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);
        taskPanelGO.SetActive(true);
    }

    public bool ValidateInformation()
    {
        // Make sure that background form is filled properly and no fields are empty, error message otherwise
        if (
            genderOptions.AnyTogglesOn()
            && ageOptions.AnyTogglesOn()
            && movedToFinlandOptions.AnyTogglesOn()
            && learnedFinnishOptions.AnyTogglesOn()
            && selfAssessmentOptions.AnyTogglesOn()
            && !string.IsNullOrWhiteSpace(motherTongueOptions.text)
        )
        {
            if (languageOther.isOn && string.IsNullOrWhiteSpace(languageOtherField.text))
            {
                errorMessage.enabled = true;
                return false;
            }

            return true;
        }
        errorMessage.enabled = true;
        return false;
    }

    // These describePanels are again legacy code and not currently in use / active
    // They can't be seen because their respective describeButtons are currently never visible
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
    // Need to destroy all game objects in the list to avoid creating a duplicate scorelist
    {
        numberGamePanelGO.SetActive(false);
        ASAPanelGO.SetActive(false);
        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);
    }
}
