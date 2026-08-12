using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FeedbackPanel : MonoBehaviour
{
    NetworkManager networkManager;

    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject newTaskButtonGO;

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

    // Shown above the score rows when the server's relevance check flagged the recording,
    // for both off_topic and partial. Both fields are optional: left unassigned, the panel
    // behaves as it did before rather than throwing on a result screen.
    //
    // FormerlySerializedAs keeps the existing Inspector wiring after the rename from
    // offTopicWarning*, which happened when partial started using the same notice.
    [SerializeField]
    [FormerlySerializedAs("offTopicWarningGO")]
    GameObject relevanceWarningGO;

    [SerializeField]
    [FormerlySerializedAs("offTopicWarningText")]
    TMPro.TextMeshProUGUI relevanceWarningText;

    // What the recogniser heard, shown at the bottom of the scrollable content. Worth
    // seeing even on a good result: it is the only way a learner can tell a low score
    // apart from a misheard word, and it is what makes an off_topic verdict checkable
    // rather than something they have to take on trust.
    [SerializeField]
    TMPro.TextMeshProUGUI transcriptText;

    // Hidden while a relevance notice is showing. The two occupy the same band, and the
    // notice is the more urgent of the two - InfoText explains how to read the scores,
    // which matters less than being told the scores may not mean what they appear to.
    [SerializeField]
    GameObject infoTextGO;

    // The ScrollRect's Content, and the transcript box inside it. Both are resized at
    // runtime by FitTranscript - see there for why they cannot be fixed heights.
    //
    // The Viewport above them carries an Image with alpha 0, Raycast Target on and Cull
    // Transparent Mesh off. It draws nothing and it is not cruft - do not remove it. A
    // ScrollRect only scrolls when the EventSystem's raycast lands on something, and every
    // graphic inside this viewport that covers real estate - the transcript box, its title
    // and its text, the relevance notice - has Raycast Target off. A drag over any of them
    // therefore hit nothing and raised no event, so a long transcript filled the screen
    // with a region the learner could not scroll away from. Cull Transparent Mesh must
    // stay off too: a culled mesh reports depth -1 and GraphicRaycaster skips those, which
    // would quietly undo the fix.
    [SerializeField]
    RectTransform scrollContent;

    [SerializeField]
    RectTransform transcriptGroup;

    // Geometry of the transcript box, matching how it is built in the prefab.
    const float TRANSCRIPT_SIDE_PADDING = 20f; // left/right inset of the body text
    const float TRANSCRIPT_HEADER = 68f; // title strip above the body text
    const float TRANSCRIPT_BOTTOM_PADDING = 16f;
    const float CONTENT_TAIL = 40f; // breathing room under the box
    const float MIN_CONTENT_HEIGHT = 1620f; // never shrink below the fixed layout above

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

    // Post each emoji on tap rather than waiting for Send. Built once and kept: they
    // remember what the server already has, which is what stops Send re-posting an answer
    // that has not changed since the tap.
    private FeedbackAutoSend accuracyFeedback;
    private FeedbackAutoSend understandingFeedback;

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

    [SerializeField]
    GameObject errorPopupGO;

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
        // Show info popup for chosen category in the feedback panel
        currentPopupOpen = popupOpen;
        PopupAttributes popup = panels[popupOpen];

        titleTextGO.text = popup.TitleText;
        popupTextGO.text = popup.PopupText;
    }

    void ResetTextSizeInTitleAnimation()
    {
        // Resets the animation for the different evaluation category titles in the feedback panel
        proficiencyTitleGO.transform.localScale = Vector3.one;
        pronunciationTitleGO.transform.localScale = Vector3.one;
        rangeTitleGO.transform.localScale = Vector3.one;
        accuracyTitleGO.transform.localScale = Vector3.one;
        fluencyTitleGO.transform.localScale = Vector3.one;
    }

    void StopTitleAnimation()
    {
        // Reset all category title animations
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

    public void StopErrorAnimation()
    {
        // Stop the drop-down error message animation
        Animator anim = errorPopupGO.GetComponent<Animator>();
        anim.enabled = false;
        errorPopupGO.SetActive(false);
    }

    void OnEnable()
    {
        networkManager = NetworkManager.GetManager();

        // Clear last result's notice before anything else runs. The real call is ~200
        // lines below, after the button wiring and the score read, and anything that
        // throws in between would leave the previous warning on screen - so a learner who
        // was warned on one task would be warned again on the next however well they did.
        // Passing null means "no result yet", which resolves to hidden.
        ShowRelevanceWarning(null);

        // Both emoji rows now post the moment one is tapped, so a learner who answers and
        // then closes the popup without pressing Send is still counted. Send keeps working
        // and carries the typed comment - see the feedbackSendButtonGO handler below.
        accuracyFeedback ??= new FeedbackAutoSend(this, "result_accuracy");
        understandingFeedback ??= new FeedbackAutoSend(this, "result_understanding");

        accuracyFeedback.Attach(accuracyRatingOptions, () => accuracyFeedbackTextGO.text);
        understandingFeedback.Attach(
            understandingRatingOptions,
            () => understandingFeedbackTextGO.text
        );

        accuracyFeedback.AttachComment(accuracyFeedbackTextGO, accuracyRatingOptions);
        understandingFeedback.AttachComment(
            understandingFeedbackTextGO,
            understandingRatingOptions
        );

        // Reset all title animations at enable
        ResetTextSizeInTitleAnimation();
        StopTitleAnimation();

        // Set proper game objects active and establish button behavior
        newTaskButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                feedbackPanelGO.SetActive(false);
                replayButtonGO.SetActive(false);
                sendButtonGO.SetActive(false);
                TaskPanelGO.SetActive(true);
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
                StopErrorAnimation();
            });

        feedbackBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                // Reset feedback form and go back
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
                // Get values from feedback form
                var accuracy = accuracyRatingOptions.ActiveToggles().FirstOrDefault();
                var understanding = understandingRatingOptions.ActiveToggles().FirstOrDefault();
                string comment_accuracy = accuracyFeedbackTextGO.text;
                string comment_understanding = understandingFeedbackTextGO.text;

                bool hasAccuracy = accuracy != null;
                bool hasUnderstanding = understanding != null;
                bool hasCommentAccuracy = !string.IsNullOrEmpty(comment_accuracy);
                bool hasCommentUnderstanding = !string.IsNullOrEmpty(comment_understanding);

                // Only proceed if correct fields are filled, error drop-down otherwise
                bool isValid =
                    (hasAccuracy && hasUnderstanding)
                    || (hasAccuracy && !hasCommentUnderstanding)
                    || (hasUnderstanding && !hasCommentAccuracy);

                if (!isValid)
                {
                    Animator anim = errorPopupGO.GetComponent<Animator>();

                    errorPopupGO.SetActive(true);

                    anim.enabled = true;

                    anim.Play("Error Popup Animation");

                    return;
                }

                // Send given feedbacks to server.
                //
                // Usually a no-op for the rating itself, which posted when it was tapped.
                // What this still carries is the comment, typed after the emoji was
                // chosen, and any tap made before the assessment id existed.
                if (accuracy != null)
                {
                    accuracyFeedback.Send(accuracy.name, comment_accuracy);
                }
                if (understanding != null)
                {
                    understandingFeedback.Send(understanding.name, comment_understanding);
                }

                // Reset feedback forms when done
                accuracyRatingOptions.SetAllTogglesOff();
                accuracyFeedbackTextGO.text = "";
                understandingRatingOptions.SetAllTogglesOff();
                understandingFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        if (PlayerPrefs.GetInt("InfoPopupSeen", 0) == 0)
        {
            // Show good to know -pop up if not seen before
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
            .onClick.AddListener(() =>
            {
                ASAPanelGO.SetActive(true);
                infoPopupGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        // Get scores from the server
        NetworkManager.ASRResultASA result = networkManager.asrResultASA;

        float proficiencyRating = result.scores.proficiency;
        float pronunciationRating = result.scores.pronunciation;
        float rangeRating = result.scores.range;
        float accuracyRating = result.scores.accuracy;
        float fluencyRating = result.scores.fluency;

        // The band comes from the server, not from the number. The two used to be worked
        // out here with local thresholds; v1.3.0 moved the boundaries and will move them
        // again, so a client that keeps its own copy of the rule shows a band that
        // disagrees with the learner's stored row and says nothing about it.
        NetworkManager.DimensionLabels labels = result.dimension_labels;

        proficiencyScore.SetValue(proficiencyRating, 1, result.cefr_label_fine);
        pronunciationScore.SetValue(pronunciationRating, 2, LabelOf(labels?.pronunciation));
        rangeScore.SetValue(rangeRating, 3, LabelOf(labels?.range));
        accuracyScore.SetValue(accuracyRating, 4, LabelOf(labels?.accuracy));
        fluencyScore.SetValue(fluencyRating, 5, LabelOf(labels?.fluency));

        ShowRelevanceWarning(networkManager.asrResultASA);

        if (transcriptText != null)
        {
            // Empty rather than left over from the previous recording: this panel is
            // reused, and a stale transcript next to fresh scores is worse than none.
            string heard =
                networkManager.asrResultASA == null ? null : networkManager.asrResultASA.transcript;

            transcriptText.text = string.IsNullOrWhiteSpace(heard)
                ? Const.ASA_TRANSCRIPT_EMPTY
                : heard;

            FitTranscript();
        }

        ScrollToTop();

        // Get the category with the lowest score
        float minRating = Mathf.Min(
            proficiencyRating,
            pronunciationRating,
            rangeRating,
            accuracyRating,
            fluencyRating
        );

        // Play animation for lowest rated category title if min rating is low enough
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

    /// <summary>
    /// Shows or hides the relevance notice above the score rows.
    ///
    /// Two states get a notice, for different reasons:
    ///
    ///   off_topic - the scores really are 0.0, so the notice is what stops a fabricated
    ///               "A1" being read as a verdict on the learner.
    ///   partial   - the scores are genuine; the notice only explains why they may be
    ///               lower than usual.
    ///
    /// The two are mutually exclusive - relevance is a single value from the server - so
    /// off_topic is tested first and there is no case where both could apply.
    ///
    /// Always called, including on a clean result: the panel is a reused object, so a
    /// notice left from a previous recording would otherwise stay on screen.
    /// </summary>
    // Null-safe read of one dimension's band. dimension_labels is absent on an older
    // server and on any response we could not parse, and a null here is the row's signal
    // to fall back to the score rather than draw a blank badge.
    private static string LabelOf(NetworkManager.DimensionLabel dimension)
    {
        return dimension == null ? null : dimension.label_fine;
    }

    private void ShowRelevanceWarning(NetworkManager.ASRResultASA result)
    {
        bool offTopic = result != null && result.IsOffTopic;
        bool partial = result != null && result.IsPartial;
        bool show = offTopic || partial;

        if (relevanceWarningText != null && show)
        {
            relevanceWarningText.text = offTopic ? Const.ASA_OFF_TOPIC : Const.ASA_PARTIAL;
        }

        if (relevanceWarningGO != null)
        {
            relevanceWarningGO.SetActive(show);
        }
        else if (show)
        {
            // On off_topic this notice is the only thing separating "you scored A1" from
            // "you answered a different question", so a missing object is worth saying
            // out loud rather than failing silently.
            Debug.LogWarning(
                "FeedbackPanel: relevance was "
                    + (offTopic ? "off_topic" : "partial")
                    + " but relevanceWarningGO is not assigned - the learner sees the "
                    + "score with no explanation."
            );
        }

        // The notice and InfoText occupy the same band. Swapping them is cleaner than
        // relying on one drawing over the other, and the notice is the more urgent of the
        // two: how to read the scores matters less than being told what the scores mean.
        if (infoTextGO != null)
        {
            infoTextGO.SetActive(!show);
        }
    }

    /// <summary>
    /// Grows the transcript box, and the scroll content under it, to fit the text.
    ///
    /// The transcript is the one element whose height is not knowable in advance - a long
    /// answer runs to several hundred pixels. Everything else on this panel is fixed, so
    /// the box and the ScrollRect's Content are the only things that have to move.
    ///
    /// Without this, a long transcript overflows a fixed-height box and the overflow sits
    /// below the scrollable area: Content stops at its authored height, so scrolling hits
    /// the end while text is still off screen and no amount of dragging reaches it.
    /// </summary>
    /// <summary>
    /// Puts the results back at the top before they are shown.
    ///
    /// This panel is one reused object, and a ScrollRect keeps whatever position it was
    /// left at. Read to the bottom of one result, record the next task, and the new
    /// scores open half-scrolled - past the relevance notice, which is the one thing on
    /// this screen a learner must not miss.
    ///
    /// Found rather than wired: the prefab holds exactly one ScrollRect, so there is
    /// nothing to pick wrong and nothing to forget to assign.
    /// </summary>
    private void ScrollToTop()
    {
        ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);

        if (scroll == null)
        {
            return;
        }

        // Both axes: the panel only scrolls vertically today, but a horizontal offset left
        // behind would be just as invisible from here.
        scroll.verticalNormalizedPosition = 1f;
        scroll.horizontalNormalizedPosition = 0f;
    }

    private void FitTranscript()
    {
        if (transcriptText == null || transcriptGroup == null)
        {
            return;
        }

        // Measured, not laid out: GetPreferredValues answers immediately, where waiting
        // for a layout pass would leave Content at the wrong height for a frame.
        float textWidth = transcriptGroup.sizeDelta.x - TRANSCRIPT_SIDE_PADDING * 2f;
        float textHeight = transcriptText.GetPreferredValues(transcriptText.text, textWidth, 0f).y;

        float groupHeight = TRANSCRIPT_HEADER + textHeight + TRANSCRIPT_BOTTOM_PADDING;
        transcriptGroup.sizeDelta = new Vector2(transcriptGroup.sizeDelta.x, groupHeight);

        if (scrollContent == null)
        {
            return;
        }

        // anchoredPosition.y is negative here - the box is anchored to Content's top edge,
        // so this is how far below that edge it starts.
        float groupTop = -transcriptGroup.anchoredPosition.y;

        scrollContent.sizeDelta = new Vector2(
            scrollContent.sizeDelta.x,
            Mathf.Max(groupTop + groupHeight + CONTENT_TAIL, MIN_CONTENT_HEIGHT)
        );
    }

    void OnDisable()
    {
        // Clear up listeners
        newTaskButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
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
    }
}
