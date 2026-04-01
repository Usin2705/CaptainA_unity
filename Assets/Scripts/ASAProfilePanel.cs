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

    void OnEnable()
    {
        UpdateLevelBar();
        UpdateText();
        profileBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => profilePanelGO.SetActive(false));
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
