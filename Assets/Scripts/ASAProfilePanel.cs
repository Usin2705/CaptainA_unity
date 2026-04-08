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

    public ToggleGroup comparisonRatingOptions;

    [SerializeField]
    TMP_InputField comparisonFeedbackTextGO;

    public string guid;

    void OnEnable()
    {
        UpdateLevelBar();
        UpdateText();
        profileBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => profilePanelGO.SetActive(false));

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

    public void UpdateLevelBar()
    {
        levelBarGO.GetComponent<Image>().fillAmount = user.percentage;
    }

    public void UpdateText()
    {
        string performance_text =
            $"You are performing better than {100 * user.percentage}% of {user.level} users";
        performanceText.text = performance_text;
        levelText.text = user.level;
        string rank_text = $"Your rank within other {user.level} level users";
        rankText.text = rank_text;
        string position_text = $"#{user.global_rank}";
        positionText.text = position_text;
    }

    [System.Serializable]
    public class Stats
    {
        public float percentage;
        public string level;
        public int cohort_size;
        public int global_rank;
    }

    Stats user = new Stats
    {
        percentage = 0.7f,
        level = "A2",
        cohort_size = 50,
        global_rank = 17,
    };
}
