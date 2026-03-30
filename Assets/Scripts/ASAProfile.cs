using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ASAPRofilePanel : MonoBehaviour
{
    [SerializeField]
    GameObject levelBarGO;

    [SerializeField]
    private TextMeshProUGUI levelTextGO;

    [SerializeField]
    private TextMeshProUGUI performanceTextGO;

    [SerializeField]
    private TextMeshProUGUI rankTextGO;

    [SerializeField]
    private TextMeshProUGUI positionTextGO;

    void OnEnable()
    {
        UpdateLevelBar();
        UpdateText();
    }

    public void UpdateLevelBar()
    {
        levelBarGO.GetComponent<Image>().fillAmount = user.percentage;
    }

    public void UpdateText()
    {
        string performance_text =
            $"You are performing better than {100 * user.percentage}% of {user.level} learners";
        performanceTextGO.text = performance_text;
        levelTextGO.text = user.level;
        string rank_text = $"Global rank ({user.level})";
        rankTextGO.text = rank_text;
        string position_text = $"{user.global_rank}#";
        positionTextGO.text = position_text;
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
