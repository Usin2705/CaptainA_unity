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
    GameObject loadingPopUpGO;
    private bool isLoading = false;

    public ToggleGroup overallRatingOptions;

    [SerializeField]
    GameObject overallFeedbackPopUpGO;

    [SerializeField]
    GameObject feedbackSendButtonGO;

    [SerializeField]
    public TMP_InputField overallFeedbackTextGO;

    [SerializeField]
    GameObject feedbackBackButtonGO;

    void OnEnable()
    {
        isLoading = false;
        Debug.Log(PlayerPrefs.GetInt("TasksSent"));
        // PlayerPrefs.SetInt("TasksSent", 5);
        OverallFeedback();

        backButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => advancePanelGO.SetActive(true));
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => taskPanelGO.SetActive(false));

        task1ButtonGO.GetComponent<Button>().onClick.AddListener(() => ASAPanelGO.SetActive(true));
        task1ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => taskPanelGO.SetActive(false));

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
                isLoading = true;
                StartCoroutine(
                    NetworkManager.GetManager().ServerPost_profile(POSTType.ASA_PROFILE)
                );
            });

        feedbackSendButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                var overallRating = overallRatingOptions.ActiveToggles().FirstOrDefault();
                string comment_overall = overallFeedbackTextGO.text;
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
                overallRatingOptions.SetAllTogglesOff();
                overallFeedbackTextGO.text = "";
                overallFeedbackPopUpGO.SetActive(false);
            });

        feedbackBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                overallRatingOptions.SetAllTogglesOff();
                overallFeedbackTextGO.text = "";
                overallFeedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });
    }

    public void OverallFeedback()
    {
        if (PlayerPrefs.GetInt("TasksSent", 0) % 5 == 0 && PlayerPrefs.GetInt("TasksSent", 0) > 0)
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
        if (isLoading)
        {
            AnimateLoadingProfile();
        }
    }

    public void AnimateLoadingProfile()
    {
        loadingIconGO.transform.Rotate(0, 0, -6.0f, Space.Self);
    }
}
