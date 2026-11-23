using System.Linq;
using SaySuomi.Managers;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class ASAResultsScreen : AppScreen
    {
        // Geometry of the transcript box, matching how it is built in the prefab.
        private const float TRANSCRIPT_SIDE_PADDING = 20f; // left/right inset of the body text
        private const float TRANSCRIPT_HEADER = 68f; // title strip above the body text
        private const float TRANSCRIPT_BOTTOM_PADDING = 16f;
        private const float CONTENT_TAIL = 40f; // breathing room under the box
        private const float MIN_CONTENT_HEIGHT = 1620f; // never shrink below the fixed layout above

        // The "ASAPanel Labels"-style table holding this screen's text.
        private static readonly System.Guid Table = new System.Guid(
            "db69d3d55aef5df5fa3b2c531d1f5502"
        );

        [Header("Results: Scores")]
        [SerializeField]
        private FeedbackRow proficiencyScore;

        [SerializeField]
        private FeedbackRow pronunciationScore;

        [SerializeField]
        private FeedbackRow rangeScore;

        [SerializeField]
        private FeedbackRow accuracyScore;

        [SerializeField]
        private FeedbackRow fluencyScore;

        [Tooltip("In order: proficiency, pronunciation, range, accuracy, fluency.")]
        [SerializeField]
        private Button[] scoreInfoButtons;

        [Header("Results: Score Titles")]
        [SerializeField]
        private GameObject proficiencyTitleGO;

        [SerializeField]
        private GameObject pronunciationTitleGO;

        [SerializeField]
        private GameObject rangeTitleGO;

        [SerializeField]
        private GameObject accuracyTitleGO;

        [SerializeField]
        private GameObject fluencyTitleGO;

        [Header("Results: Notices")]
        // Shown above the score rows when the server's relevance check flagged the recording,
        // for both off_topic and partial.
        [SerializeField]
        private GameObject relevanceWarningGO;

        [SerializeField]
        private TextMeshProUGUI relevanceWarningText;

        // Hidden while a relevance notice is showing. The two occupy the same band, and the
        // notice is the more urgent of the two - InfoText explains how to read the scores,
        // which matters less than being told the scores may not mean what they appear to.
        [Tooltip("Hidden while the relevance warning is showing.")]
        [SerializeField]
        private GameObject infoTextGO;

        [Header("Results: Transcript")]
        // What the recogniser heard, shown at the bottom of the scrollable content. Worth
        // seeing even on a good result: it is the only way a learner can tell a low score
        // apart from a misheard word, and it is what makes an off_topic verdict checkable
        // rather than something they have to take on trust.
        [SerializeField]
        private TextMeshProUGUI transcriptText;

        [Tooltip("Resized at runtime to fit the transcript.")]
        [SerializeField]
        private RectTransform transcriptGroup;

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
        [Tooltip("The ScrollRect's Content, resized at runtime with the transcript box.")]
        [SerializeField]
        private RectTransform scrollContent;

        [Header("Results: Buttons")]
        [SerializeField]
        private Button infoButton;

        [SerializeField]
        private Button newTaskButton;

        [SerializeField]
        private Button feedbackButton;

        [Header("Score Info Modal")]
        [SerializeField]
        private GameObject popupPanelGO;

        [SerializeField]
        private Button popupBackButton;

        [SerializeField]
        private TextMeshProUGUI titleTextGO;

        [SerializeField]
        private TextMeshProUGUI popupTextGO;

        [Header("Info Modal")]
        [SerializeField]
        private GameObject infoPopupGO;

        [SerializeField]
        private Button infoBackButton;

        [Header("Feedback Modal")]
        [SerializeField]
        private GameObject feedbackPopUpGO;

        [SerializeField]
        private Button feedbackBackButton;

        [SerializeField]
        private Button feedbackSendButton;

        [SerializeField]
        private ToggleGroup accuracyRatingOptions;

        [SerializeField]
        private TMP_InputField accuracyFeedbackTextGO;

        [SerializeField]
        private ToggleGroup understandingRatingOptions;

        [SerializeField]
        private TMP_InputField understandingFeedbackTextGO;

        [Tooltip("Drop-down error message, shown through ErrorBanner.")]
        [SerializeField]
        private GameObject errorPopupGO;

        private NetworkManager networkManager;

        private ModalScreen popupModal;
        private ModalScreen infoModal;
        private ModalScreen feedbackModal;

        private LocalizedString answerOffTopicNotice;

        private LocalizedString answerPartialNotice;

        private LocalizedTextHelper relevanceWarningHelper;

        private LocalizedString emptyTranscriptNotice;

        private LocalizedTextHelper transcriptNoticeHelper;

        // Post each emoji on tap rather than waiting for Send. Built once and kept: they
        // remember what the server already has, which is what stops Send re-posting an answer
        // that has not changed since the tap.
        private FeedbackAutoSend accuracyFeedback;
        private FeedbackAutoSend understandingFeedback;

        private PopupAttributes[] panels;

        private LocalizedTextHelper popupTitleTextHelper;

        private LocalizedTextHelper popupTextHelper;

        private int currentPopupOpen = -1;

        protected override void Bind()
        {
            networkManager = NetworkManager.GetManager();

            answerOffTopicNotice = new LocalizedString(Table, 4093949075456);
            answerPartialNotice = new LocalizedString(Table, 4361424035840);

            // Unassigned in the scene when this moved to code, so a blank transcript shows
            // nothing. An entry needs adding to the table to give it wording.
            emptyTranscriptNotice = new LocalizedString();

            panels = new[]
            {
                Popup(669878673408, 6343601774592),
                Popup(2876388761600, 6757755740160),
                Popup(2907997036544, 6846674984960),
                Popup(2925197877248, 6874936205312),
                Popup(2951772987392, 6939780145152),
            };

            Navigator navigator = Navigator.GetManager();

            ButtonWiring.Replace(newTaskButton, () => navigator.PopTo<ASATaskListScreen>());
            for (int i = 0; i < scoreInfoButtons.Length; i++)
            {
                int popup = i;
                ButtonWiring.Replace(scoreInfoButtons[i], () => OpenPopup(popup));
            }
            ButtonWiring.Replace(popupBackButton, () => navigator.Back());
            ButtonWiring.Replace(infoButton, () => navigator.Open(infoModal));
            ButtonWiring.Replace(infoBackButton, () => navigator.Back());
            ButtonWiring.Replace(feedbackButton, OpenFeedback);
            ButtonWiring.Replace(feedbackBackButton, () => navigator.Back());
            ButtonWiring.Replace(feedbackSendButton, SendFeedback);

            popupModal = ModalScreen.Attach(popupPanelGO);
            infoModal = ModalScreen.Attach(infoPopupGO);
            feedbackModal = ModalScreen.Attach(feedbackPopUpGO);

            feedbackModal.OnBack = () =>
            {
                ErrorBanner.Hide(errorPopupGO);
                return false;
            };
            feedbackModal.OnDiscard = ResetFeedbackForm;

            // Both emoji rows now post the moment one is tapped, so a learner who answers and
            // then closes the popup without pressing Send is still counted. Send keeps working
            // and carries the typed comment - see SendFeedback.
            accuracyFeedback = new FeedbackAutoSend(networkManager, "result_accuracy");
            understandingFeedback = new FeedbackAutoSend(networkManager, "result_understanding");

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
        }

        private void OnEnable()
        {
            // Clear last result's notice before anything else runs. The real call is ~200
            // lines below, after the button wiring and the score read, and anything that
            // throws in between would leave the previous warning on screen - so a learner who
            // was warned on one task would be warned again on the next however well they did.
            // Passing null means "no result yet", which resolves to hidden.
            ShowRelevanceWarning(null);

            // A tap on the dim can keep a comment, which belongs to the result it was typed for.
            ResetFeedbackForm();

            // Reset all title animations at enable
            ResetTextSizeInTitleAnimation();
            StopTitleAnimation();

            if (PlayerPrefs.GetInt("InfoPopupSeen", 0) == 0)
            {
                // Show good to know -pop up if not seen before
                Navigator.GetManager().Open(infoModal);
                PlayerPrefs.SetInt("InfoPopupSeen", 1);
                PlayerPrefs.Save();
            }

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
                    networkManager.asrResultASA == null
                        ? null
                        : networkManager.asrResultASA.transcript;

                if (string.IsNullOrWhiteSpace(heard))
                {
                    transcriptNoticeHelper ??= new LocalizedTextHelper(transcriptText);
                    transcriptNoticeHelper?.SetText(emptyTranscriptNotice);
                }
                else
                {
                    transcriptNoticeHelper?.ClearText();
                    transcriptText.text = heard;
                }

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

        private void OnDisable()
        {
            // Clear up listeners
            popupTitleTextHelper?.ClearText();
            popupTextHelper?.ClearText();
            relevanceWarningHelper?.ClearText();
            transcriptNoticeHelper?.ClearText();
        }

        /// <summary>Back leaves the results for the task list, the same as New Task.</summary>
        public override bool HandleBack()
        {
            Navigator.GetManager().PopTo<ASATaskListScreen>();
            return true;
        }

        public void DisplayPopup(int popupOpen)
        {
            // Show info popup for chosen category in the feedback panel
            currentPopupOpen = popupOpen;
            PopupAttributes popup = panels[popupOpen];

            popupTitleTextHelper ??= new LocalizedTextHelper(titleTextGO);
            popupTitleTextHelper?.SetText(popup.titleText);

            popupTextHelper ??= new LocalizedTextHelper(popupTextGO);
            popupTextHelper?.SetText(popup.popupText);
        }

        private static PopupAttributes Popup(long titleKey, long textKey)
        {
            return new PopupAttributes
            {
                titleText = new LocalizedString(Table, titleKey),
                popupText = new LocalizedString(Table, textKey),
            };
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

        private void OpenPopup(int popupOpen)
        {
            DisplayPopup(popupOpen);
            Navigator.GetManager().Open(popupModal);
        }

        private void OpenFeedback()
        {
            Navigator.GetManager().Open(feedbackModal);
            ErrorBanner.Hide(errorPopupGO);
        }

        private void SendFeedback()
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
                ErrorBanner.Show(errorPopupGO);
                return;
            }

            // Send given feedbacks to server.
            //
            // Usually a no-op for the rating itself, which posted when it was tapped.
            // What this still carries is the comment, typed after the emoji was
            // chosen, and any tap made before the assessment id existed.
            if (accuracy != null)
            {
                accuracyFeedback.Send(accuracy.GetComponent<OptionValue>().value, comment_accuracy);
            }
            if (understanding != null)
            {
                understandingFeedback.Send(
                    understanding.GetComponent<OptionValue>().value,
                    comment_understanding
                );
            }

            // OnDiscard resets the form.
            Navigator.GetManager().Back();
        }

        private void ResetFeedbackForm()
        {
            accuracyRatingOptions.SetAllTogglesOff();
            accuracyFeedbackTextGO.text = "";
            understandingRatingOptions.SetAllTogglesOff();
            understandingFeedbackTextGO.text = "";
        }

        private void ResetTextSizeInTitleAnimation()
        {
            // Resets the animation for the different evaluation category titles in the feedback panel
            proficiencyTitleGO.transform.localScale = Vector3.one;
            pronunciationTitleGO.transform.localScale = Vector3.one;
            rangeTitleGO.transform.localScale = Vector3.one;
            accuracyTitleGO.transform.localScale = Vector3.one;
            fluencyTitleGO.transform.localScale = Vector3.one;
        }

        private void StopTitleAnimation()
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

        private void ShowRelevanceWarning(NetworkManager.ASRResultASA result)
        {
            bool offTopic = result != null && result.IsOffTopic;
            bool partial = result != null && result.IsPartial;
            bool show = offTopic || partial;

            if (relevanceWarningText != null && show)
            {
                relevanceWarningHelper ??= new LocalizedTextHelper(relevanceWarningText);
                relevanceWarningHelper?.SetText(
                    offTopic ? answerOffTopicNotice : answerPartialNotice
                );
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
            float textHeight = transcriptText
                .GetPreferredValues(transcriptText.text, textWidth, 0f)
                .y;

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

        public class PopupAttributes
        {
            public LocalizedString titleText;
            public LocalizedString popupText;
        }
    }
}
