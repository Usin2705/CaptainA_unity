using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackPanel : MonoBehaviour
{
    NetworkManager networkManager;

    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject backButtonGO;

    [SerializeField]
    GameObject proficiencyButtonGO;

    [SerializeField]
    GameObject pronunciationButtonGO;

    [SerializeField]
    GameObject rangeButtonGO;

    [SerializeField]
    GameObject accuracyButtonGO;

    [SerializeField]
    GameObject fluencyButtonGO;

    [SerializeField]
    GameObject proficiencyPopupGO;

    [SerializeField]
    GameObject pronunciationPopupGO;

    [SerializeField]
    GameObject rangePopupGO;

    [SerializeField]
    GameObject accuracyPopupGO;

    [SerializeField]
    GameObject fluencyPopupGO;

    [SerializeField]
    GameObject dimPanelGO;

    [SerializeField]
    GameObject popupBackButtonGO;

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

    [SerializeField]
    GameObject replayButtonGO;

    [SerializeField]
    GameObject sendButtonGO;

    void OnEnable()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        backButtonGO.GetComponent<Button>().onClick.AddListener(() => ASAPanelGO.SetActive(true));
        backButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                feedbackPanelGO.SetActive(false);
                replayButtonGO.SetActive(false);
                sendButtonGO.SetActive(false);
            });

        proficiencyButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                proficiencyPopupGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        popupBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                proficiencyPopupGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        float proficiencyRating = networkManager.asrResult2.scores.proficiency;
        float pronunciationRating = networkManager.asrResult2.scores.pronunciation;
        float rangeRating = networkManager.asrResult2.scores.range;
        float accuracyRating = networkManager.asrResult2.scores.accuracy;
        float fluencyRating = networkManager.asrResult2.scores.fluency;

        proficiencyScore.SetValue(proficiencyRating, 1);
        pronunciationScore.SetValue(pronunciationRating, 2);
        rangeScore.SetValue(rangeRating, 3);
        accuracyScore.SetValue(accuracyRating, 4);
        fluencyScore.SetValue(fluencyRating, 5);
    }
}
