using UnityEngine;
using UnityEngine.UI;

public class FeedbackPanel : MonoBehaviour
{
    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject backButtonGO;

    [SerializeField]
    FeedbackRow proficiencyScore;

    [SerializeField]
    FeedbackRow pronunciationScore;

    [SerializeField]
    FeedbackRow rangeScore;

    [SerializeField]
    FeedbackRow accuracyScore;

    [SerializeField]
    FeedbackRow fluencyScore;

    void OnEnable()
    {
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => ASAPanelGO.SetActive(true));
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => feedbackPanelGO.SetActive(false));

        float proficiencyRating = 2.9f;
        float pronunciationRating = 3.1f;
        float rangeRating = 2.5f;
        float accuracyRating = 1.3f;
        float fluencyRating = 1.0f;

        proficiencyScore.SetValue(proficiencyRating, 1);
        pronunciationScore.SetValue(pronunciationRating, 2);
        rangeScore.SetValue(rangeRating, 3);
        accuracyScore.SetValue(accuracyRating, 4);
        fluencyScore.SetValue(fluencyRating, 5);
    }
}
