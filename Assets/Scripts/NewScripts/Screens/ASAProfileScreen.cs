using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SaySuomi.Managers;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class ASAProfileScreen : AppScreen
    {
        private static readonly Guid Table = new Guid("850ceabc4262bb1acb39b8a91b2fcf93");

        [Header("Profile: Level")]
        [Tooltip("Ring filled from the learner's percentile. It shows no figure.")]
        [SerializeField]
        private GameObject levelBarGO;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [Tooltip("The object Level Text is on. Hidden when there is no level.")]
        [SerializeField]
        private GameObject levelTextGO;

        [SerializeField]
        private TextMeshProUGUI levelDescriptionText;

        [Tooltip("The object Level Description Text is on. Hidden when there is no level.")]
        [SerializeField]
        private GameObject levelDescriptionTextGO;

        [Tooltip("Moves down a level. Hidden on the lowest level.")]
        [SerializeField]
        private GameObject revertLevelButtonGO;

        [Tooltip("Moves up a level. Shown only when the learner's percentile is high enough.")]
        [SerializeField]
        private GameObject advanceLevelButtonGO;

        [Header("Profile: Comparison")]
        [Tooltip("Row holding Performance Text. Hidden when no comparison is available.")]
        [SerializeField]
        private GameObject performanceInfo;

        [SerializeField]
        private TextMeshProUGUI performanceText;

        [Tooltip(
            "Row holding Rank Text and Position Text. Hidden when no comparison is available."
        )]
        [SerializeField]
        private GameObject rankInfo;

        [SerializeField]
        private TextMeshProUGUI rankText;

        [SerializeField]
        private TextMeshProUGUI positionText;

        [Tooltip("Shown in place of the two rows above when no comparison is available.")]
        [SerializeField]
        private GameObject insufficientDataNoticeGO;

        [SerializeField]
        private TextMeshProUGUI insufficientDataNotice;

        [Header("Profile: Buttons and Errors")]
        [SerializeField]
        private Button profileBackButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private GameObject feedbackButtonGO;

        [Tooltip("Drop-down error message, shown through ErrorBanner.")]
        [SerializeField]
        private GameObject errorPopupGO;

        [Header("Settings Modal")]
        [SerializeField]
        private GameObject settingsPopupGO;

        [SerializeField]
        private Button settingsBackButton;

        [Tooltip("Shows the learner's user ID.")]
        [SerializeField]
        private TMP_Text guidTextGO;

        [SerializeField]
        private Button guidCopyButton;

        [Tooltip("Opens the delete confirmation modal.")]
        [SerializeField]
        private Button deleteDataButton;

        [Header("Delete Confirmation Modal")]
        [SerializeField]
        private GameObject deleteConfirmPopupGO;

        // Data deletion, run from the app so nobody has to service requests by hand.
        [SerializeField]
        private GameObject deleteConfirmButtonGO;

        [SerializeField]
        private GameObject deleteCancelButtonGO;

        [SerializeField]
        private TextMeshProUGUI deleteStatusText;

        // The cancel button doubles as "OK" once the deletion has run, because by then
        // there is nothing left to cancel.
        [Tooltip("Shown on the cancel button before the deletion runs.")]
        [SerializeField]
        private TextMeshProUGUI deleteCancelLabel;

        [Tooltip("Shown on the cancel button after the deletion runs.")]
        [SerializeField]
        private TextMeshProUGUI deleteOkLabel;

        [Header("Feedback Modal")]
        [SerializeField]
        private GameObject feedbackPopUpGO;

        [SerializeField]
        private Button feedbackBackButton;

        [SerializeField]
        private Button feedbackSendButton;

        [SerializeField]
        private ToggleGroup comparisonRatingOptions;

        [SerializeField]
        private TMP_InputField comparisonFeedbackTextGO;

        private ModalScreen settingsModal;
        private ModalScreen deleteConfirmModal;
        private ModalScreen feedbackModal;

        private LocalizedString deleteWarningText;

        private LocalizedString deleteLoadingText;

        private LocalizedString deleteConfirmedNotice;

        private LocalizedString deleteReceivedNotice;

        private LocalizedTextHelper deleteStatusHelper;

        private LocalizedString levelDescriptionA1;

        private LocalizedString levelDescriptionA2;

        private LocalizedString levelDescriptionB1;

        private LocalizedString levelDescriptionB2;

        private LocalizedString levelDescriptionC1Plus;

        private LocalizedTextHelper levelDescriptionHelper;

        private LocalizedString rankPercentText;

        private LocalizedString rankPositionText;

        private LocalizedString topRankText;

        private LocalizedString rankFirstText;

        private LocalizedString noRankNotice;

        private LocalizedString noPositionNotice;

        private LocalizedString insufficientTasksNotice;

        private LocalizedString cohortSizeNotice;

        private LocalizedString rankUnavailableNotice;

        private LocalizedString rankUnavailableGeneralNotice;

        private LocalizedTextHelper rankPercentHelper;

        private LocalizedTextHelper rankPositionHelper;

        private LocalizedTextHelper rankTopPositionHelper;

        private LocalizedTextHelper insufficientDataHelper;

        private string guid;

        // The level the server last reported, which is what a step is measured from. Set only
        // from a server response - never from a change we assume went through.
        private string currentLevel;

        // The endpoint is still to be built, so ASA_SET_LEVEL_URL is blank in Secret.cs until
        // it exists. A button that visibly does nothing is worse than no button, so both are
        // hidden while it is. Filling the value in is all that is needed to bring them back.
        private static bool LevelChangeConfigured =>
            !string.IsNullOrEmpty(Secret.ASA_SET_LEVEL_URL);

        protected override void Bind()
        {
            deleteWarningText = new LocalizedString(Table, 34375829905408);
            deleteLoadingText = new LocalizedString(Table, 34784963289088);
            deleteConfirmedNotice = new LocalizedString(Table, 35377484226560);
            deleteReceivedNotice = new LocalizedString(Table, 36367994281984);
            levelDescriptionA1 = new LocalizedString(Table, 531886071808);
            levelDescriptionA2 = new LocalizedString(Table, 8894137065472);
            levelDescriptionB1 = new LocalizedString(Table, 13267697229824);
            levelDescriptionB2 = new LocalizedString(Table, 13450052984832);
            levelDescriptionC1Plus = new LocalizedString(Table, 13467077664768);
            rankPercentText = new LocalizedString(Table, 16883006922752);
            rankPositionText = new LocalizedString(Table, 17416568528896);
            topRankText = new LocalizedString(Table, 19380538793984);
            rankFirstText = new LocalizedString(Table, 5902470477864960);
            noRankNotice = new LocalizedString(Table, 19467973255168);
            noPositionNotice = new LocalizedString(Table, 19548101238784);
            insufficientTasksNotice = new LocalizedString(Table, 14536994283520);
            cohortSizeNotice = new LocalizedString(Table, 20111354322944);
            rankUnavailableNotice = new LocalizedString(Table, 20288265871360);
            rankUnavailableGeneralNotice = new LocalizedString(Table, 20790462472192);

            Navigator navigator = Navigator.GetManager();

            ButtonWiring.Replace(profileBackButton, () => navigator.Back());
            ButtonWiring.Replace(settingsButton, () => navigator.Open(settingsModal));
            ButtonWiring.Replace(settingsBackButton, () => navigator.Back());
            ButtonWiring.Replace(guidCopyButton, () => GUIUtility.systemCopyBuffer = guid);
            ButtonWiring.Replace(deleteDataButton, OpenDeleteConfirm);
            ButtonWiring.Replace(
                deleteCancelButtonGO.GetComponent<Button>(),
                () => navigator.Back()
            );
            ButtonWiring.Replace(deleteConfirmButtonGO.GetComponent<Button>(), DeleteData);
            ButtonWiring.Replace(
                feedbackButtonGO.GetComponent<Button>(),
                () => navigator.Open(feedbackModal)
            );
            ButtonWiring.Replace(feedbackBackButton, () => navigator.Back());
            ButtonWiring.Replace(feedbackSendButton, SendFeedback);
            ButtonWiring.Replace(advanceLevelButtonGO.GetComponent<Button>(), () => ChangeLevel(1));
            ButtonWiring.Replace(revertLevelButtonGO.GetComponent<Button>(), () => ChangeLevel(-1));

            settingsModal = ModalScreen.Attach(settingsPopupGO);
            deleteConfirmModal = ModalScreen.Attach(deleteConfirmPopupGO);
            feedbackModal = ModalScreen.Attach(feedbackPopUpGO);

            // Cancelled, this returns to the settings popup underneath. Deleted, there is no
            // account left to show a profile or a guid for, so everything closes back to
            // Advanced rather than returning to numbers that no longer belong to anyone.
            deleteConfirmModal.OnBack = () =>
            {
                if (!string.IsNullOrEmpty(PlayerPrefs.GetString("UserGuid", "")))
                {
                    return false;
                }

                navigator.PopToRoot();
                return true;
            };

            feedbackModal.OnBack = () =>
            {
                ErrorBanner.Hide(errorPopupGO);
                return false;
            };
            feedbackModal.OnDiscard = () =>
            {
                comparisonRatingOptions.SetAllTogglesOff();
                comparisonFeedbackTextGO.text = "";
            };
        }

        private void OnEnable()
        {
            // Stops animated dropdown error message from appearing
            ErrorBanner.Hide(errorPopupGO);

            // Set proper game objects active and establish button behavior
            levelBarGO.SetActive(true);
            revertLevelButtonGO.SetActive(true);
            advanceLevelButtonGO.SetActive(true);
            performanceInfo.SetActive(true);
            rankInfo.SetActive(true);
            feedbackButtonGO.SetActive(true);
            insufficientDataNoticeGO.SetActive(false);

            guid = PlayerPrefs.GetString("UserGuid");
            guidTextGO.text = guid;
        }

        private void OnDisable()
        {
            deleteStatusHelper?.ClearText();
            levelDescriptionHelper?.ClearText();
            rankPercentHelper?.ClearText();
            rankPositionHelper?.ClearText();
            rankTopPositionHelper?.ClearText();
            insufficientDataHelper?.ClearText();
        }

        /// <summary>
        /// The ring around the grade letter, showing roughly where the learner sits.
        ///
        /// Driven by the bucket, not by `percentile`. A ring filled to an exact percentile is
        /// still a display of it - a finer claim than the server is willing to make to a
        /// learner - so it moves in the same steps as the text beside it. Top 5% fills 95%.
        ///
        /// Filled from `percentile`, which is sent on every successful comparison whether or
        /// not a `display` bucket comes with it. That is why the ring keeps working for the
        /// bottom half of a cohort, where both bucket fields are null.
        ///
        /// This is not the thing v1.3.0 asked us to stop showing. What it removed was the
        /// precise figure - "you are in the 13th percentile" - and a ring carries no figure a
        /// learner can read off it. The numbers beside it come from `display` and stay
        /// bucketed.
        ///
        /// The ring is never hidden: it is the frame around the grade letter, and taking it
        /// away leaves the badge looking broken.
        /// </summary>
        public void UpdateLevelBar(Stats user)
        {
            levelBarGO.SetActive(true);

            // Clamped because percentile defaults to -1 for "absent", and a negative fill
            // would silently render as empty rather than as the missing value it is.
            StartCoroutine(AnimateLevelBar(Mathf.Clamp01(user.percentile)));
        }

        /// <summary>
        /// Fills in the profile from a successful comparison.
        ///
        /// The position comes from `display` and nowhere else. This used to print
        /// `100 * percentile` and `#rank` straight from the research fields, which could tell
        /// a learner they were "performing better than 13.4% of A2 users" - a truthful number
        /// and a terrible thing to read. The server now buckets it, and both fields go absent
        /// for the bottom half of a cohort, where the honest thing to show is nothing.
        /// </summary>
        public void UpdateText(Stats user)
        {
            string level = user.cefr_level.Replace("_plus", "+");
            levelText.text = level;
            GiveLevelDescriptions(user.cefr_level);

            Display display = user.display;
            bool hasPercent = display != null && display.HasPercent;
            bool hasRank = display != null && display.HasRank;

            if (hasPercent)
            {
                rankPercentHelper ??= new LocalizedTextHelper(performanceText);

                // Used Const.ASA_RANK_TOP_PERCENT before
                rankPercentText.Arguments = new object[] { display.top_percent, level };
                rankPercentHelper?.SetText(rankPercentText);
            }

            if (hasRank)
            {
                rankPositionHelper ??= new LocalizedTextHelper(rankText);

                // Used Const.ASA_RANK_WITHIN_LEVEL before
                rankPositionText.Arguments = new object[] { level };
                rankPositionHelper?.SetText(rankPositionText);

                // "#1" reads better than "Top 1" for the one case where they mean the same.
                rankTopPositionHelper ??= new LocalizedTextHelper(positionText);

                if (display.top_rank == 1)
                {
                    // Used Const.ASA_RANK_FIRST before
                    rankTopPositionHelper?.SetText(rankFirstText);
                }
                else
                {
                    // Used Const.ASA_RANK_TOP_RANK before
                    topRankText.Arguments = new object[] { display.top_rank };
                    rankTopPositionHelper?.SetText(topRankText);
                }
            }

            if (!hasRank)
            {
                // The box stays where it is; only the position itself is replaced. Hiding it
                // left a hole in the middle of the panel.
                rankPositionHelper ??= new LocalizedTextHelper(rankText);

                // Used Const.ASA_RANK_NO_POSITION_LABEL before
                noPositionNotice.Arguments = new object[] { level };
                rankPositionHelper?.SetText(noPositionNotice);

                rankTopPositionHelper?.ClearText();

                if (positionText == null)
                {
                    positionText.text = "";
                }
            }

            // No position is a valid outcome, not an error - but leaving the panel empty made
            // it look like one, so the performance line stays visible either way.
            //
            // What it says is deliberately about the learner's own result and nothing else.
            // It cannot explain the absence, because both fields go null only for the bottom
            // half of a cohort and any explanation amounts to saying so; and it cannot fall
            // back on cohort_size, because the number of users is not ours to disclose.
            if (!hasPercent)
            {
                rankPercentHelper ??= new LocalizedTextHelper(performanceText);

                // Used Const.ASA_RANK_NO_POSITION before
                rankPercentHelper?.SetText(noRankNotice);
            }

            // Both boxes always stay up. Their contents change when there is no position to
            // show; the panel's shape does not.
            performanceInfo.SetActive(true);
            rankInfo.SetActive(true);
        }

        public void GiveLevelDescriptions(string cefr_level)
        {
            // Gives the CEFR level descriptions in words
            LocalizedString levelDescription = cefr_level switch
            {
                "A1" => levelDescriptionA1,
                "A2" => levelDescriptionA2,
                "B1" => levelDescriptionB1,
                "B2" => levelDescriptionB2,
                "C1_plus" => levelDescriptionC1Plus,
                _ => null,
            };

            if (levelDescription != null)
            {
                levelDescriptionHelper ??= new LocalizedTextHelper(levelDescriptionText);
                levelDescriptionHelper?.SetText(levelDescription);
            }
        }

        /// <summary>
        /// Decides whether Advance and Revert are offered, from the level and percentile the
        /// server just sent.
        ///
        /// Both are set explicitly rather than only switched off. A level change reloads the
        /// profile without re-enabling the panel, so OnEnable does not run again and anything
        /// this method hid would stay hidden - the A1 user who advances to A2 has to get their
        /// Revert button back.
        /// </summary>
        public void ShowButtons(Stats user)
        {
            RememberLevel(user.cefr_level);

            int level = LevelIndex(currentLevel);
            int ceiling = LevelIndex(Const.ASA_ADVANCE_CEILING);

            // A level that is not on the ladder leaves no step to compute in either direction.
            // Should not happen - the server CHECKs the same five values - but guessing a step
            // from an unknown level is how a user ends up somewhere nobody intended.
            bool onLadder = level >= 0;

            // Going down is never gated on anything but having somewhere to go. Whatever
            // reason a learner has for finding their level too hard, making them earn their
            // way out of it is the wrong answer.
            ShowRevertButton();

            // Going up is: not past the ceiling, and not without having earned it - see
            // Const.ASA_ADVANCE_MIN_PERCENTILE for where that bar sits and why.
            advanceLevelButtonGO.SetActive(
                onLadder
                    && level < ceiling
                    && user.percentile >= Const.ASA_ADVANCE_MIN_PERCENTILE
                    && LevelChangeConfigured
            );
        }

        /// <summary>
        /// Fills the screen from a profile response.
        ///
        /// The unavailable responses carry a `status`; a real comparison does not. Branching on
        /// that rather than on percentile == -1, which cannot be told apart from a genuine
        /// bottom-of-cohort result.
        /// </summary>
        public void ShowStats(string json)
        {
            Stats stats = JsonUtility.FromJson<Stats>(json);

            if (!string.IsNullOrEmpty(stats.status) || stats.percentile == -1f)
            {
                NotEnoughTasks(JsonUtility.FromJson<InsufficientStats>(json));
                return;
            }

            UpdateText(stats);
            UpdateLevelBar(stats);
            ShowButtons(stats);
        }

        /// <summary>
        /// Shown when the server could not produce a comparison. There are three reasons and
        /// they are not interchangeable - this used to print the "complete three tasks" line
        /// for all of them, which told a user with 20 assessments to go and do three.
        ///
        /// Branching is on `status`, which only appears on the unavailable responses.
        /// </summary>
        public void NotEnoughTasks(InsufficientStats user)
        {
            insufficientDataHelper ??= new LocalizedTextHelper(insufficientDataNotice);

            switch (user.status)
            {
                case "USER_ASSESSMENT_DATA_INSUFFICIENT":
                    // The one case the user can act on, so it says how far along they are.
                    // The requirement comes from the server (MIN_USER_ASSESSMENTS); 3 is only
                    // the fallback if it is missing, so raising it server-side needs no
                    // client change.
                    int needed = user.required_assessments > 0 ? user.required_assessments : 3;

                    // Used Const.ASA_RANK_NEED_MORE_TASKS before
                    insufficientTasksNotice.Arguments = new object[]
                    {
                        needed,
                        user.current_assessments,
                    };
                    insufficientDataHelper?.SetText(insufficientTasksNotice);
                    break;

                case "COHORT_SIZE_TOO_SMALL":
                    // Used Const.ASA_RANK_COHORT_TOO_SMALL before
                    insufficientDataHelper?.SetText(cohortSizeNotice);
                    break;

                case "RANK_UNAVAILABLE":
                    // Used Const.ASA_RANK_UNAVAILABLE before
                    insufficientDataHelper?.SetText(rankUnavailableNotice);
                    break;

                default:
                    // An unrecognised status: the server's own wording beats anything we could
                    // guess, and this is how a newly added case degrades gracefully.
                    if (string.IsNullOrEmpty(user.message))
                    {
                        // Used Const.ASA_RANK_UNAVAILABLE_GENERIC before
                        insufficientDataHelper?.SetText(rankUnavailableGeneralNotice);
                    }
                    else
                    {
                        insufficientDataHelper?.ClearText();

                        if (insufficientDataNotice != null)
                        {
                            insufficientDataNotice.text = user.message;
                        }
                    }
                    break;
            }

            // RANK_UNAVAILABLE carries no cefr_level, so the level display is skipped rather
            // than showing an empty badge - and the old unconditional .Replace() on it would
            // have thrown here.
            bool hasLevel = !string.IsNullOrEmpty(user.cefr_level);
            if (hasLevel)
            {
                levelText.text = user.cefr_level.Replace("_plus", "+");
                GiveLevelDescriptions(user.cefr_level);
            }

            levelTextGO.SetActive(hasLevel);
            levelDescriptionTextGO.SetActive(hasLevel);

            // The ring is never hidden - it frames the grade letter, and taking it away leaves
            // the badge looking broken on exactly the screens where the learner is told least.
            //
            // Solid, because these three responses carry no percentile at all: there is no
            // comparison behind them, so there is nothing to fill it to. Note this is the one
            // place the ring is not a position - a fill-type Image at 0 renders as nothing,
            // which would be indistinguishable from hiding it.
            levelBarGO.SetActive(true);
            levelBarGO.GetComponent<Image>().fillAmount = 1f;

            RememberLevel(user.cefr_level);

            // Revert survives every one of these three states. There is no ranking to show
            // here, but that is not a reason to trap someone at a level: a learner whose
            // cohort is too small for a comparison still gets to drop back out of it.
            ShowRevertButton();

            // Advance does not. It is earned from a percentile, and there is no percentile on
            // any of these responses.
            advanceLevelButtonGO.SetActive(false);
            performanceInfo.SetActive(false);
            rankInfo.SetActive(false);
            feedbackButtonGO.SetActive(false);
            insufficientDataNoticeGO.SetActive(true);
        }

        private static int LevelIndex(string cefrLevel)
        {
            return Array.IndexOf(Const.ASA_LEVELS, cefrLevel);
        }

        private void SendFeedback()
        {
            var helpful = comparisonRatingOptions.ActiveToggles().FirstOrDefault();
            string comment_helpful = comparisonFeedbackTextGO.text;

            // Show and error popup if user tries to send feeback without choosing any of the emojis
            if (helpful == null)
            {
                ErrorBanner.Show(errorPopupGO);
                return;
            }

            // Send feedback to server
            StartCoroutine(
                NetworkManager
                    .GetManager()
                    .ServerPost_feedback(
                        POSTType.USER_ASA_FEEDBACK,
                        "comparison_ui",
                        helpful.GetComponent<OptionValue>().value,
                        comment_helpful
                    )
            );

            // OnDiscard resets the form.
            Navigator.GetManager().Back();
        }

        // The popup is reused, and the previous run may have left it in its finished state with
        // the confirm button hidden, so reset it every time.
        private void OpenDeleteConfirm()
        {
            deleteStatusHelper ??= new LocalizedTextHelper(deleteStatusText);
            deleteStatusHelper.SetText(deleteWarningText);
            deleteConfirmButtonGO.SetActive(true);
            SetDeleteButtonsInteractable(true);
            deleteCancelLabel.gameObject.SetActive(true);
            deleteOkLabel.gameObject.SetActive(false);
            Navigator.GetManager().Open(deleteConfirmModal);
        }

        /// <summary>
        /// The deletion itself lives in NetworkManager.ServerPost_deleteUser, which clears the
        /// local data whatever the server says, and records whether the server confirmed the
        /// erase. Both outcomes are shown honestly here.
        /// </summary>
        private void DeleteData()
        {
            // Both buttons go dead for the duration, and back with them: a second tap would fire
            // a second deletion for a guid the first one is already removing.
            SetDeleteButtonsInteractable(false);
            deleteConfirmModal.Dismissible = false;
            deleteStatusHelper.SetText(deleteLoadingText);

            StartCoroutine(
                NetworkManager
                    .GetManager()
                    .ServerPost_deleteUser(serverConfirmed =>
                    {
                        NetworkManager network = NetworkManager.GetManager();

                        if (network.DataConfirmedDeleted)
                        {
                            deleteStatusHelper.SetText(deleteConfirmedNotice);
                        }
                        else if (serverConfirmed)
                        {
                            deleteStatusHelper.SetText(deleteReceivedNotice);
                        }
                        else
                        {
                            // Same wording as a server-acknowledged request. The local
                            // data really is gone, and the guid is queued in
                            // PendingDeleteGuid and retried on every launch until the
                            // server takes it - so from the user's side the outcome is
                            // the same and the difference is ours to resolve, not
                            // theirs to worry about. The console still carries the
                            // real failure for us; see DeleteUserOnServer.
                            deleteStatusHelper.SetText(deleteReceivedNotice);
                        }

                        // Nothing left to confirm; the cancel button becomes the way out.
                        deleteConfirmButtonGO.SetActive(false);
                        SetDeleteButtonsInteractable(true);
                        deleteConfirmModal.Dismissible = true;
                        deleteCancelLabel.gameObject.SetActive(false);
                        deleteOkLabel.gameObject.SetActive(true);
                    })
            );
        }

        private void SetDeleteButtonsInteractable(bool interactable)
        {
            deleteConfirmButtonGO.GetComponent<Button>().interactable = interactable;
            deleteCancelButtonGO.GetComponent<Button>().interactable = interactable;
        }

        private IEnumerator AnimateLevelBar(float target)
        {
            float filled = 0f;

            while (filled < target)
            {
                levelBarGO.GetComponent<Image>().fillAmount = filled;
                filled += 0.01f;

                yield return null;
            }

            // The loop overshoots by up to one step and then stops, leaving the ring a hair
            // short of where it was asked to go. Pin it.
            levelBarGO.GetComponent<Image>().fillAmount = target;
        }

        /// <summary>
        /// Offers Revert whenever there is a level below the current one. Called from both the
        /// ranked and the unranked paths - dropping back has no preconditions.
        /// </summary>
        private void ShowRevertButton()
        {
            revertLevelButtonGO.SetActive(LevelIndex(currentLevel) > 0 && LevelChangeConfigured);
        }

        /// <summary>
        /// Records the level a step is measured from, ignoring an empty one.
        ///
        /// RANK_UNAVAILABLE carries no cefr_level. Overwriting with the blank would take the
        /// Revert button with it, stranding whoever is on that screen at a level they may
        /// have opened the panel specifically to leave - so the last level the server did
        /// name is kept instead.
        /// </summary>
        private void RememberLevel(string cefrLevel)
        {
            if (!string.IsNullOrEmpty(cefrLevel))
            {
                currentLevel = cefrLevel;
            }
        }

        /// <summary>
        /// Moves the user one step along Const.ASA_LEVELS and reloads the profile.
        /// </summary>
        /// <param name="step">+1 to advance, -1 to revert.</param>
        private void ChangeLevel(int step)
        {
            int level = LevelIndex(currentLevel);

            if (level < 0)
            {
                Debug.LogWarning(
                    "Level change ignored: no level from the server to step from (got '"
                        + currentLevel
                        + "')."
                );
                return;
            }

            int target = level + step;

            // The buttons are hidden when a step is not allowed, so reaching this is a bug
            // rather than a user action - but the bounds are cheap and a wrong level is not.
            if (target < 0 || target >= Const.ASA_LEVELS.Length)
            {
                return;
            }

            if (step > 0 && target > LevelIndex(Const.ASA_ADVANCE_CEILING))
            {
                return;
            }

            string targetLevel = Const.ASA_LEVELS[target];

            SetLevelButtonsInteractable(false);

            NetworkManager network = NetworkManager.GetManager();
            ASATaskListScreen taskList = Navigator.GetManager().Get<ASATaskListScreen>();

            // With no endpoint configured nothing is sent, so there is no progress to show.
            bool willSend = network.CanSetLevel;
            if (willSend)
            {
                taskList.ShowProfileLoading();
            }

            StartCoroutine(
                network.ServerPost_setLevel(
                    POSTType.ASA_SET_LEVEL,
                    targetLevel,
                    stored =>
                    {
                        SetLevelButtonsInteractable(true);

                        if (!stored)
                        {
                            // The loading popup shows the failure with its own back button, and
                            // the profile underneath is untouched, so the user is still at the
                            // level they started from.
                            if (willSend)
                            {
                                taskList.ShowProfileError();
                            }

                            return;
                        }

                        // Reloaded rather than applied locally. The cohort is a different set of
                        // people now, so the rank and the percentile are different numbers, and
                        // only the server knows them.
                        taskList.LoadProfile();
                    }
                )
            );
        }

        private void SetLevelButtonsInteractable(bool interactable)
        {
            advanceLevelButtonGO.GetComponent<Button>().interactable = interactable;
            revertLevelButtonGO.GetComponent<Button>().interactable = interactable;
        }

        [System.Serializable]
        public class Stats
        {
            // Research values. Neither may be written on the screen as a number: they are
            // finer than anything a learner should be told, and "you are in the 13th
            // percentile" is precisely the claim v1.3.0 set out to remove. The numbers a
            // learner reads come from `display`, already bucketed.
            //
            // percentile has two uses that are not a number on a screen, and both are fine:
            // it fills the level ring, which shows no figure, and it gates whether the Advance
            // button appears.
            public float percentile = -1f;
            public int rank;

            public string cefr_level;
            public int cohort_size;

            // The bucketed position, and the only part of this response that may be shown.
            public Display display;

            // Present only when the comparison could NOT be produced. A successful
            // ComparisonResponse carries no status at all, so an empty string here is the
            // reliable "we got real numbers" signal - more reliable than percentile == -1,
            // which is indistinguishable from a genuine bottom-of-cohort result.
            public string status;
        }

        /// <summary>
        /// The learner-facing position, already rounded into buckets by the server.
        ///
        /// The ladders are deliberately server-side: they get retuned as the cohorts grow, and
        /// that has to reach installed apps without a client release. So nothing here is
        /// recomputed, compared against a threshold, or interpolated - the numbers are
        /// rendered as they arrive or not at all.
        ///
        /// The buckets round away from the learner, never toward: someone genuinely at 3.4%
        /// is shown "top 5%". Every rendered claim is literally true and none of them flatter.
        /// </summary>
        [System.Serializable]
        public class Display
        {
            // Server sends 1, 5, 10, 25, 50, or null. JsonUtility cannot hold a null int, so
            // it lands here as 0 - which is safe, because 0 is not one of the buckets and
            // therefore cannot be confused with a real position.
            public int top_percent;

            // 1, 2, 3, 5, 10, 25, 50, 100, or null (0 here). Null past #100.
            public int top_rank;

            // Both are null for anyone in the bottom half of their cohort. That means show no
            // position at all - not a placeholder, not "unranked".
            public bool HasPercent => top_percent > 0;

            public bool HasRank => top_rank > 0;
        }

        [System.Serializable]
        public class InsufficientStats
        {
            // Which of the three unavailable cases this is. The fields below are populated
            // per case, not all at once - see NotEnoughTasks.
            public string status;

            // The server's own wording. Used as the fallback if it ever adds a status we do
            // not recognise, so a new case degrades to "slightly generic" rather than "wrong".
            public string message;

            // Absent on RANK_UNAVAILABLE.
            public string cefr_level;

            // USER_ASSESSMENT_DATA_INSUFFICIENT only.
            public int current_assessments;
            public int required_assessments;

            // COHORT_SIZE_TOO_SMALL only.
            public int cohort_size;
        }
    }
}
