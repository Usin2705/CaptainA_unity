using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
    [SerializeField]
    GameObject advancePanelGO;

    [SerializeField]
    GameObject taskPanelGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject backButtonGO;

    [SerializeField]
    GameObject task1ButtonGO;

    [SerializeField]
    GameObject task2ButtonGO;

    [SerializeField]
    GameObject task3ButtonGO;

    [SerializeField]
    GameObject task4ButtonGO;

    [SerializeField]
    GameObject task5ButtonGO;

    [SerializeField]
    ASAPanel ASAPanel;

    [SerializeField]
    GameObject dimPanelGO;

    [SerializeField]
    GameObject profileButtonGO;

    [SerializeField]
    GameObject profilePanelGO;

    [SerializeField]
    GameObject loadingIconGO;

    [SerializeField]
    GameObject profileLoadingBackButtonGO;

    [SerializeField]
    GameObject profileLoadingErrorTextGO;

    [SerializeField]
    GameObject loadingPopUpGO;

    [SerializeField]
    GameObject errorPopupGO;

    [SerializeField]
    GameObject overallFeedbackPopUpGO;

    [SerializeField]
    GameObject feedbackSendButtonGO;

    [SerializeField]
    public TMP_InputField overallFeedbackTextGO;

    [SerializeField]
    GameObject feedbackBackButtonGO;

    public bool isLoading = false;

    public ToggleGroup overallRatingOptions;

    public void StopErrorAnimation()
    {
        Animator anim = errorPopupGO.GetComponent<Animator>();
        anim.enabled = false;
        errorPopupGO.SetActive(false);
    }

    void OnEnable()
    {
        // Stops animated dropdown error message from appearing
        StopErrorAnimation();
        // Reset listeners so that they won't get duplicated later
        profileButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        feedbackSendButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        feedbackBackButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();

        isLoading = false;
        Debug.Log(PlayerPrefs.GetInt("TasksSent"));
        // Ask general feedback if number of completed tasks so far is a multiple of 5
        OverallFeedback();

        // Set proper game objects active and establish button behavior
        backButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => advancePanelGO.SetActive(true));
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => taskPanelGO.SetActive(false));

        task1ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(0);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task2ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(1);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task3ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(2);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task4ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(3);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task5ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(4);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });

        profileButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                loadingPopUpGO.SetActive(true);
                dimPanelGO.SetActive(true);
                profileLoadingErrorTextGO.SetActive(false);

                isLoading = true;
                // Get profile data from server
                StartCoroutine(
                    NetworkManager.GetManager().ServerPost_profile(POSTType.ASA_PROFILE)
                );
            });

        profileLoadingBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => OnProfileLoadingBackButtonClicked());

        feedbackSendButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                var overallRating = overallRatingOptions.ActiveToggles().FirstOrDefault();
                string comment_overall = overallFeedbackTextGO.text;
                // Show drop-down error animation if attempting to send feedback without selecting an emoji
                if (overallRating == null)
                {
                    Animator anim = errorPopupGO.GetComponent<Animator>();

                    errorPopupGO.SetActive(true);

                    anim.enabled = true;

                    anim.Play("Error Popup Animation");

                    return;
                }
                // Send feedback to server
                StartCoroutine(
                    NetworkManager
                        .GetManager()
                        .ServerPost_feedback(
                            POSTType.ASA_FEEDBACK,
                            "overall_experience",
                            overallRating.name,
                            comment_overall
                        )
                );
                // Reset feedback form when done
                PlayerPrefs.SetInt("OverallFeedbackSent", 1);
                overallRatingOptions.SetAllTogglesOff();
                overallFeedbackTextGO.text = "";
                overallFeedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
                errorPopupGO.SetActive(false);
            });

        feedbackBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                overallRatingOptions.SetAllTogglesOff();
                overallFeedbackTextGO.text = "";
                overallFeedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
                errorPopupGO.SetActive(false);
            });
    }

    public void OnProfileLoadingBackButtonClicked()
    {
        loadingPopUpGO.SetActive(false);
        dimPanelGO.SetActive(false);
    }

    public void OverallFeedback()
    {
        // Ask overall feedback after every 5 completed tasks
        if (
            PlayerPrefs.GetInt("TasksSent", 0) % 5 == 0
            && PlayerPrefs.GetInt("TasksSent", 0) > 0
            && PlayerPrefs.GetInt("OverallFeedbackSent", 0) == 0
        )
        {
            overallFeedbackPopUpGO.SetActive(true);
            dimPanelGO.SetActive(true);
        }
    }

    public void ReturnToTaskPanel()
    {
        isLoading = false;
        loadingPopUpGO.SetActive(true);
    }

    void Update()
    {
        // For updating different animations depending on state
        if (isLoading)
        {
            AnimateLoadingProfile();
        }
    }

    public void AnimateLoadingProfile()
    {
        // Change value -6.0f to change rotation speed
        loadingIconGO.transform.Rotate(0, 0, -6.0f, Space.Self);
    }
}
