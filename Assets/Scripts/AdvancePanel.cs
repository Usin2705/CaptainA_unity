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
    GameObject loadingPopUpGO;

    [SerializeField]
    GameObject profilePanelGO;

    [SerializeField]
    ASAPanel ASAPanel;

    // Other languages is now one open-ended field, and unlike the rest of the form it is
    // optional - leaving it blank is a valid answer.
    [SerializeField]
    private TMP_InputField languageOtherField;

    [SerializeField]
    private TMP_InputField motherTongueOptions;

    [SerializeField]
    private TextMeshProUGUI errorMessage;

    public ToggleGroup genderOptions;

    public ToggleGroup ageOptions;

    public ToggleGroup selfAssessmentOptions;

    // Deliberately not [System.Serializable]. Unity cannot serialize a ValueTuple, so the
    // attribute did nothing but raise a warning per field. This is a carrier: built in
    // GetUserInput, handed to NetworkManager, never shown in the Inspector or saved.
    public class BackgroundFormData
    {
        public (string, string) gender;
        public (string, string) age;
        public (string, string) motherTongue;
        public (string, string) otherLanguages;
        public (string, string) selfAssessment;
    }

    void OnEnable()
    {
        // The secret text used to be read here to decide whether to wire up the
        // DescribeButtons. Those are gone, and nothing else on this panel looks at it, so
        // the check went with them. CardDeckPanel still uses Secret.SECRET_TEXT.

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

        errorMessage.enabled = false;

        // The picture-description feature has been removed from the Advanced panel: the
        // six DescribeButtons, their OnDescribe*ButtonClicked handlers, and the three
        // describePanel objects are all gone, along with the secret-code gate that used
        // to guard the assessment.

        // Set the proper panels and game objects as inactive at first
        numberGamePanelGO.SetActive(false);
        ASAPanelGO.SetActive(false);
        feedbackPanelGO.SetActive(false);
        taskPanelGO.SetActive(false);
        loadingPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);
        consentPopUpGO.SetActive(false);
        backgroundPopUpGO.SetActive(false);
        profilePanelGO.SetActive(false);

        // Opening the tab is what clears its new-feature dot - the learner has now seen
        // whatever it was pointing at. Recorded here rather than on the tab button so that
        // every route in counts, and so the dot cannot be cleared by a tap that does not
        // actually arrive. NavigationBar reads this back and hides the dot.
        NewFeatureBadge.MarkSeen(Const.PREF_SEEN_ADVANCED, Const.VER_MAX_SHOW_ADVANCED);

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
        // The assessment used to sit behind a secret code while it was being trialled.
        // It is a public feature now, so the button goes straight through - consent and
        // the background form are the only gates that remain.
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

    /// <summary>
    /// Brings the task list back to the front, for a screen that needs to return to it
    /// without holding its own reference. This panel owns every screen under it - OnEnable
    /// is what switches them all off - so the reference stays here rather than being
    /// duplicated into each child.
    ///
    /// Only turns the task list on. The caller switches itself off, because it is the one
    /// that knows whether it has anything to clean up first.
    /// </summary>
    public void ShowTaskPanel()
    {
        taskPanelGO.SetActive(true);
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

        // Open-ended and optional: whatever the user typed, or an empty string. The
        // server accepts other_languages as free text and allows it to be empty.
        var otherLanguages = languageOtherField.text.Trim();

        var selfAssessment = selfAssessmentOptions
            .ActiveToggles()
            .FirstOrDefault()
            .gameObject.GetComponent<OptionValue>()
            .value;

        string timestamp = System.DateTime.UtcNow.ToString("o");
        PlayerPrefs.SetString("BackgroundTimestamp", timestamp);
        PlayerPrefs.Save();

        string motherWrapped = string.Join("\n", motherTongue);

        // Prepare the form to be sent to the server.
        // moved_to_finland and finnish_learning_duration are no longer collected. The
        // server made both nullable in v1.2.0, so omitting them is accepted - see
        // docs/TO_FRONTEND.md item 10.
        BackgroundFormData backgroundFormData = new()
        {
            gender = ("gender", gender),
            age = ("age_group", age),
            motherTongue = ("native_languages", motherWrapped),
            otherLanguages = ("other_languages", otherLanguages),
            selfAssessment = ("finnish_self_assessment", selfAssessment),
        };

        // Send consent and background form to server.
        //
        // Nothing below this point may run until the server confirms the user exists.
        // This used to mark BackgroundFormCompleted and open the task panel immediately,
        // before the request had even been sent - so a failed onboarding still let the
        // user into the assessment with a guid the server had never heard of. Every
        // assessment then answered 404, and because the form was already flagged complete
        // the app never asked again: a permanent dead end that only a reinstall cleared.
        sendButtonGO.GetComponent<Button>().interactable = false;
        errorMessage.enabled = false;

        StartCoroutine(
            NetworkManager
                .GetManager()
                .ServerPost_guid(
                    POSTType.ASA_CONSENT,
                    backgroundFormData,
                    serverOk =>
                    {
                        sendButtonGO.GetComponent<Button>().interactable = true;

                        if (!serverOk)
                        {
                            ShowOnboardingError();
                            return;
                        }

                        // Only now is the user real on the server.
                        PlayerPrefs.SetInt("BackgroundFormCompleted", 1);
                        PlayerPrefs.Save();

                        backgroundPopUpGO.SetActive(false);
                        dimPanelGO.SetActive(false);
                        taskPanelGO.SetActive(true);
                    }
                )
        );
    }

    // Keeps the user on the background form and tells them why they are still there.
    // The type is included because "something went wrong" is useless in a bug report,
    // and for a 422 the user genuinely cannot fix it by trying again.
    private void ShowOnboardingError()
    {
        NetworkManager network = NetworkManager.GetManager();
        string reason = string.IsNullOrEmpty(network.lastError)
            ? "Could not create your account."
            : network.lastError;

        if (!string.IsNullOrEmpty(network.lastErrorType))
        {
            reason += $"\n({network.lastErrorType})";
        }

        errorMessage.text = reason;
        errorMessage.enabled = true;
    }

    public bool ValidateInformation()
    {
        // Every remaining question is mandatory except other languages, which is an
        // open-ended field the user is allowed to leave blank.
        if (
            genderOptions.AnyTogglesOn()
            && ageOptions.AnyTogglesOn()
            && selfAssessmentOptions.AnyTogglesOn()
            && !string.IsNullOrWhiteSpace(motherTongueOptions.text)
        )
        {
            return true;
        }
        errorMessage.enabled = true;
        return false;
    }

    void OnDisable()
    // Need to destroy all game objects in the list to avoid creating a duplicate scorelist
    {
        numberGamePanelGO.SetActive(false);
        ASAPanelGO.SetActive(false);
    }
}
