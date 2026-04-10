using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ASAProfilePanel : MonoBehaviour
{
    [SerializeField]
    GameObject levelBarGO;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private TextMeshProUGUI performanceText;

    [SerializeField]
    private TextMeshProUGUI rankText;

    [SerializeField]
    private TextMeshProUGUI positionText;

    [SerializeField]
    GameObject profileBackButtonGO;

    [SerializeField]
    GameObject profilePanelGO;

    [SerializeField]
    GameObject settingsButtonGO;

    [SerializeField]
    GameObject settingsBackButtonGO;

    [SerializeField]
    GameObject settingsPopupGO;

    [SerializeField]
    GameObject dimPanelGO;

    [SerializeField]
    public TMP_Text guidTextGO;

    [SerializeField]
    GameObject copyGuidButtonGO;

    [SerializeField]
    GameObject feedbackButtonGO;

    [SerializeField]
    GameObject feedbackPopUpGO;

    [SerializeField]
    GameObject feedbackBackButtonGO;

    [SerializeField]
    GameObject feedbackSendButtonGO;

    [SerializeField]
    GameObject loadingPopUpProfile;

    [SerializeField]
    GameObject performanceInfo;

    [SerializeField]
    GameObject rankInfo;

    [SerializeField]
    GameObject revertLevelButtonGO;

    [SerializeField]
    GameObject advanceLevelButtonGO;

    public ToggleGroup comparisonRatingOptions;

    [SerializeField]
    TMP_InputField comparisonFeedbackTextGO;

    string description = "";

    [SerializeField]
    public TMP_Text descriptionText;

    public string guid;

    void OnEnable()
    {
        profileBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                profilePanelGO.SetActive(false);
                loadingPopUpProfile.SetActive(false);
            });

        settingsPopupGO.SetActive(false);
        dimPanelGO.SetActive(false);

        guid = PlayerPrefs.GetString("UserGuid");
        guidTextGO.text = guid;

        settingsButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                settingsPopupGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        settingsBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                settingsPopupGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        copyGuidButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = guid;
            });

        feedbackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                feedbackPopUpGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        feedbackBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                comparisonRatingOptions.SetAllTogglesOff();
                comparisonFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        feedbackSendButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                var helpful = comparisonRatingOptions.ActiveToggles().FirstOrDefault();
                string comment_helpful = comparisonFeedbackTextGO.text;
                StartCoroutine(
                    NetworkManager
                        .GetManager()
                        .ServerPost_feedback(
                            POSTType.ASA_FEEDBACK,
                            "comparison_ui",
                            helpful.name,
                            comment_helpful
                        )
                );
                comparisonRatingOptions.SetAllTogglesOff();
                comparisonFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });
    }

    public void UpdateLevelBar(Stats user)
    {
        StartCoroutine(AnimateLevelBar(user));
    }

    private IEnumerator AnimateLevelBar(Stats user)
    {
        float filled = 0f;

        while (filled < user.percentile)
        {
            levelBarGO.GetComponent<Image>().fillAmount = filled;
            filled += 0.01f;

            yield return null;
        }
    }

    public void UpdateText(Stats user)
    {
        string performance_text =
            $"You are performing better than {100 * user.percentile}% of {user.cefr_level.Replace("_plus", "+")} users";
        performanceText.text = performance_text;
        levelText.text = user.cefr_level.Replace("_plus", "+");
        string rank_text =
            $"Your rank within other {user.cefr_level.Replace("_plus", "+")} level users";
        rankText.text = rank_text;
        string position_text = $"#{user.rank}";
        positionText.text = position_text;
        var leveDescriptions = new Dictionary<string, string>
        {
            ["A1"] = "Beginner",
            ["A2"] = "Elementary",
            ["B1"] = "Intermediate",
            ["B2"] = "Upper Intermediate",
            ["C1_plus"] = "Advanced/\nFluent",
        };
        if (leveDescriptions.TryGetValue(user.cefr_level, out description))
        {
            descriptionText.text = description;
        }
        ;
    }

    public void CohortTooSmall()
    {
        levelBarGO.SetActive(false);
        revertLevelButtonGO.SetActive(false);
        advanceLevelButtonGO.SetActive(false);
        performanceInfo.SetActive(false);
        rankInfo.SetActive(false);
    }

    [System.Serializable]
    public class Stats
    {
        public float percentile;
        public string cefr_level;
        public int cohort_size;
        public int rank;
    }

    // Stats user = new Stats
    // {
    // percentile = 0.7f,
    // cefr_level = "C1_plus",
    // cohort_size = 50,
    // rank = 17,
    // };
}
