using System.Linq;
using TMPro;
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
    GameObject popupPanelGO;

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

    [SerializeField]
    private TMPro.TextMeshProUGUI titleTextGO;

    [SerializeField]
    private TMPro.TextMeshProUGUI popupTextGO;

    [SerializeField]
    GameObject infoButtonGO;

    [SerializeField]
    GameObject infoPopupGO;

    [SerializeField]
    GameObject infoBackButtonGO;

    [SerializeField]
    GameObject feedbackButtonGO;

    [SerializeField]
    GameObject feedbackPopUpGO;

    [SerializeField]
    GameObject feedbackBackButtonGO;

    [SerializeField]
    GameObject feedbackSendButtonGO;

    public ToggleGroup accuracyRatingOptions;

    public ToggleGroup understandingRatingOptions;

    [SerializeField]
    TMP_InputField accuracyFeedbackTextGO;

    [SerializeField]
    TMP_InputField understandingFeedbackTextGO;

    [SerializeField]
    GameObject proficiencyTitleGO;

    [SerializeField]
    GameObject pronunciationTitleGO;

    [SerializeField]
    GameObject rangeTitleGO;

    [SerializeField]
    GameObject accuracyTitleGO;

    [SerializeField]
    GameObject fluencyTitleGO;

    [SerializeField]
    GameObject TaskPanelGO;

    [System.Serializable]
    public class PopupAttributes
    {
        public string TitleText;
        public string PopupText;
    }

    [SerializeField]
    private PopupAttributes[] panels;
    private int currentPopupOpen = -1;

    public void DisplayPopup(int popupOpen)
    {
        currentPopupOpen = popupOpen;
        PopupAttributes popup = panels[popupOpen];

        titleTextGO.text = popup.TitleText;
        popupTextGO.text = popup.PopupText;
    }

    void OnEnable()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        backButtonGO.GetComponent<Button>().onClick.AddListener(() => TaskPanelGO.SetActive(true));
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
                DisplayPopup(0);
                popupPanelGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        pronunciationButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                DisplayPopup(1);
                popupPanelGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        rangeButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                DisplayPopup(2);
                popupPanelGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        accuracyButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                DisplayPopup(3);
                popupPanelGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        fluencyButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                DisplayPopup(4);
                popupPanelGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        popupBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                popupPanelGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        infoButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                infoPopupGO.SetActive(true);
                dimPanelGO.SetActive(true);
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
                accuracyRatingOptions.SetAllTogglesOff();
                accuracyFeedbackTextGO.text = "";
                understandingRatingOptions.SetAllTogglesOff();
                understandingFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        feedbackSendButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                var accuracy = accuracyRatingOptions.ActiveToggles().FirstOrDefault();
                var understanding = understandingRatingOptions.ActiveToggles().FirstOrDefault();
                string comment_accuracy = accuracyFeedbackTextGO.text;
                string comment_understanding = understandingFeedbackTextGO.text;
                if (accuracy != null)
                {
                    StartCoroutine(
                        NetworkManager
                            .GetManager()
                            .ServerPost_feedback(
                                POSTType.ASA_FEEDBACK,
                                "result_accuracy",
                                accuracy.name,
                                comment_accuracy
                            )
                    );
                }
                if (understanding != null)
                {
                    StartCoroutine(
                        NetworkManager
                            .GetManager()
                            .ServerPost_feedback(
                                POSTType.ASA_FEEDBACK,
                                "result_understanding",
                                understanding.name,
                                comment_understanding
                            )
                    );
                }
                accuracyRatingOptions.SetAllTogglesOff();
                accuracyFeedbackTextGO.text = "";
                understandingRatingOptions.SetAllTogglesOff();
                understandingFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        if (PlayerPrefs.GetInt("InfoPopupSeen", 0) == 0)
        {
            infoPopupGO.SetActive(true);
            dimPanelGO.SetActive(true);
            PlayerPrefs.SetInt("InfoPopupSeen", 1);
            PlayerPrefs.Save();
        }
        else
        {
            infoPopupGO.SetActive(false);
            dimPanelGO.SetActive(false);
        }

        infoBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => ASAPanelGO.SetActive(true));
        infoBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                infoPopupGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        float proficiencyRating = networkManager.asrResultASA.scores.proficiency;
        float pronunciationRating = networkManager.asrResultASA.scores.pronunciation;
        float rangeRating = networkManager.asrResultASA.scores.range;
        float accuracyRating = networkManager.asrResultASA.scores.accuracy;
        float fluencyRating = networkManager.asrResultASA.scores.fluency;

        proficiencyScore.SetValue(proficiencyRating, 1);
        pronunciationScore.SetValue(pronunciationRating, 2);
        rangeScore.SetValue(rangeRating, 3);
        accuracyScore.SetValue(accuracyRating, 4);
        fluencyScore.SetValue(fluencyRating, 5);

        float minRating = Mathf.Min(
            proficiencyRating,
            pronunciationRating,
            rangeRating,
            accuracyRating,
            fluencyRating
        );

        if (minRating < 3)
        {
            if (proficiencyRating == minRating)
            {
                Animator anim = proficiencyTitleGO.GetComponent<Animator>();

                anim.enabled = true;

                anim.Play("Proficiency Title Animation");
            }
            else if (pronunciationRating == minRating)
            {
                Animator anim = pronunciationTitleGO.GetComponent<Animator>();

                anim.enabled = true;

                anim.Play("Pronounciation Title Animation");
            }
            else if (rangeRating == minRating)
            {
                Animator anim = rangeTitleGO.GetComponent<Animator>();

                anim.enabled = true;

                anim.Play("Range Title Animation");
            }
            else if (accuracyRating == minRating)
            {
                Animator anim = accuracyTitleGO.GetComponent<Animator>();

                anim.enabled = true;

                anim.Play("Accuracy Title Animation");
            }
            else if (fluencyRating == minRating)
            {
                Animator anim = fluencyTitleGO.GetComponent<Animator>();

                anim.enabled = true;

                anim.Play("Fluency Title Animation");
            }
        }
    }

    void OnDisable()
    {
        backButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        proficiencyButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        pronunciationButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        rangeButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        accuracyButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        fluencyButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        popupBackButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        infoButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        infoBackButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        feedbackButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        feedbackBackButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        feedbackSendButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();

        Animator anim;

        anim = proficiencyTitleGO.GetComponent<Animator>();
        anim.enabled = false;

        anim = pronunciationTitleGO.GetComponent<Animator>();
        anim.enabled = false;

        anim = rangeTitleGO.GetComponent<Animator>();
        anim.enabled = false;

        anim = accuracyTitleGO.GetComponent<Animator>();
        anim.enabled = false;

        anim = fluencyTitleGO.GetComponent<Animator>();
        anim.enabled = false;
    }
}
