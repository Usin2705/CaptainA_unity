using System;
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
    GameObject copyGuidButtonGO;

    // Data deletion, run from the app so nobody has to service requests by hand.
    [SerializeField]
    GameObject deleteDataButtonGO;

    [SerializeField]
    GameObject deleteConfirmPopupGO;

    [SerializeField]
    GameObject deleteConfirmButtonGO;

    [SerializeField]
    GameObject deleteCancelButtonGO;

    [SerializeField]
    TMP_Text deleteStatusText;

    // The cancel button doubles as "close" once the deletion has run, because by then
    // there is nothing left to cancel.
    [SerializeField]
    TMP_Text deleteCancelLabel;

    // Closed along with the profile after a deletion: the task list is where the guid was
    // reachable from, and there is no account behind it any more.
    [SerializeField]
    GameObject taskPanelGO;

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

    [SerializeField]
    GameObject insufficientDataNoticeGO;

    [SerializeField]
    GameObject levelTextGO;

    [SerializeField]
    GameObject levelDescriptionTextGO;

    [SerializeField]
    GameObject errorPopupGO;

    [SerializeField]
    TMP_InputField comparisonFeedbackTextGO;

    [SerializeField]
    TMP_Text insufficientDataNotice;

    [SerializeField]
    private FeedbackPanel FeedbackPanel;

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

    public TMP_Text guidTextGO;

    public TMP_Text descriptionText;

    public ToggleGroup comparisonRatingOptions;

    public string guid;

    string description = "";

    void OnEnable()
    {
        // Stops animated dropdown error message from appearing
        StopErrorAnimation();

        // Set proper game objects active and establish button behavior
        levelBarGO.SetActive(true);
        revertLevelButtonGO.SetActive(true);
        advanceLevelButtonGO.SetActive(true);
        performanceInfo.SetActive(true);
        rankInfo.SetActive(true);
        feedbackButtonGO.SetActive(true);
        insufficientDataNoticeGO.SetActive(false);

        ButtonUtils.Rewire(
            profileBackButtonGO,
            () =>
            {
                profilePanelGO.SetActive(false);
                loadingPopUpProfile.SetActive(false);
            }
        );

        settingsPopupGO.SetActive(false);
        dimPanelGO.SetActive(false);

        guid = PlayerPrefs.GetString("UserGuid");
        guidTextGO.text = guid;

        ButtonUtils.Rewire(
            settingsButtonGO,
            () =>
            {
                settingsPopupGO.SetActive(true);
                dimPanelGO.SetActive(true);
            }
        );

        ButtonUtils.Rewire(
            settingsBackButtonGO,
            () =>
            {
                settingsPopupGO.SetActive(false);
                dimPanelGO.SetActive(false);
            }
        );

        ButtonUtils.Rewire(copyGuidButtonGO, () => GUIUtility.systemCopyBuffer = guid);

        SetUpDeleteData();
        SetUpLevelChange();

        ButtonUtils.Rewire(
            feedbackButtonGO,
            () =>
            {
                feedbackPopUpGO.SetActive(true);
                dimPanelGO.SetActive(true);
            }
        );

        ButtonUtils.Rewire(
            feedbackBackButtonGO,
            () =>
            {
                comparisonRatingOptions.SetAllTogglesOff();
                comparisonFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
                errorPopupGO.SetActive(false);
            }
        );

        ButtonUtils.Rewire(
            feedbackSendButtonGO,
            () =>
            {
                var helpful = comparisonRatingOptions.ActiveToggles().FirstOrDefault();
                string comment_helpful = comparisonFeedbackTextGO.text;

                // Show and error popup if user tries to send feeback without choosing any of the emojis
                if (helpful == null)
                {
                    Animator anim = errorPopupGO.GetComponent<Animator>();

                    errorPopupGO.SetActive(true);

                    anim.enabled = true;

                    anim.Play("Error Popup Animation");

                    return;
                }
                // Send feedback to server
                StartCoroutine(
                    NetworkManager
                        .GetManager()
                        .ServerPost_feedback(
                            POSTType.USER_ASA_FEEDBACK,
                            "comparison_ui",
                            helpful.name,
                            comment_helpful
                        )
                );
                // Reset feedback form when done
                comparisonRatingOptions.SetAllTogglesOff();
                comparisonFeedbackTextGO.text = "";
                feedbackPopUpGO.SetActive(false);
                dimPanelGO.SetActive(false);
                errorPopupGO.SetActive(false);
            }
        );
    }

    /// <summary>
    /// Wires the delete-my-data flow: red button in settings, then a confirmation the user
    /// has to pass before anything is sent.
    ///
    /// The deletion itself lives in NetworkManager.ServerPost_deleteUser, which clears the
    /// local data whatever the server says, and records whether the server confirmed the
    /// erase. Both outcomes are shown honestly here.
    /// </summary>
    private void SetUpDeleteData()
    {
        if (deleteDataButtonGO == null || deleteConfirmPopupGO == null)
        {
            return;
        }

        deleteConfirmPopupGO.SetActive(false);

        ButtonUtils.Rewire(
            deleteDataButtonGO,
            () =>
            {
                // Reset every time: the popup is reused, and the previous run may have
                // left it in its finished state with the confirm button hidden.
                deleteStatusText.text = Const.ASA_DELETE_WARNING;
                deleteConfirmButtonGO.SetActive(true);
                SetDeleteButtonsInteractable(true);

                if (deleteCancelLabel != null)
                {
                    deleteCancelLabel.text = Const.ASA_DELETE_CANCEL_LABEL;
                }

                deleteConfirmPopupGO.SetActive(true);
                dimPanelGO.SetActive(true);
            }
        );

        ButtonUtils.Rewire(
            deleteCancelButtonGO,
            () =>
            {
                deleteConfirmPopupGO.SetActive(false);

                bool accountGone = string.IsNullOrEmpty(PlayerPrefs.GetString("UserGuid", ""));

                if (!accountGone)
                {
                    // Cancelled. This returns to the settings popup, which is still open
                    // underneath - so the dim stays on. Turning it off here left the popup
                    // sitting on an undimmed screen.
                    return;
                }

                // Deleted. There is no account left to show a profile or a guid for, so
                // everything closes back to the task list rather than returning to numbers
                // that no longer belong to anyone.
                settingsPopupGO.SetActive(false);
                profilePanelGO.SetActive(false);

                if (taskPanelGO != null)
                {
                    taskPanelGO.SetActive(false);
                }

                dimPanelGO.SetActive(false);
            }
        );

        ButtonUtils.Rewire(
            deleteConfirmButtonGO,
            () =>
            {
                // Both buttons go dead for the duration: a second tap would fire a second
                // deletion for a guid the first one is already removing.
                SetDeleteButtonsInteractable(false);
                deleteStatusText.text = Const.ASA_DELETE_IN_PROGRESS;

                StartCoroutine(
                    NetworkManager
                        .GetManager()
                        .ServerPost_deleteUser(serverConfirmed =>
                        {
                            NetworkManager network = NetworkManager.GetManager();

                            if (network.DataConfirmedDeleted)
                            {
                                deleteStatusText.text = Const.ASA_DELETE_CONFIRMED;
                            }
                            else if (serverConfirmed)
                            {
                                deleteStatusText.text = Const.ASA_DELETE_RECEIVED;
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
                                deleteStatusText.text = Const.ASA_DELETE_RECEIVED;
                            }

                            // Nothing left to confirm; the cancel button becomes the way out.
                            deleteConfirmButtonGO.SetActive(false);
                            SetDeleteButtonsInteractable(true);

                            if (deleteCancelLabel != null)
                            {
                                deleteCancelLabel.text = Const.ASA_DELETE_CLOSE_LABEL;
                            }
                        })
                );
            }
        );
    }

    private void SetDeleteButtonsInteractable(bool interactable)
    {
        if (deleteConfirmButtonGO != null)
        {
            deleteConfirmButtonGO.GetComponent<Button>().interactable = interactable;
        }

        if (deleteCancelButtonGO != null)
        {
            deleteCancelButtonGO.GetComponent<Button>().interactable = interactable;
        }
    }

    public void StopErrorAnimation()
    {
        // Stops the drop-down error message animation
        Animator anim = errorPopupGO.GetComponent<Animator>();
        anim.enabled = false;
        errorPopupGO.SetActive(false);
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
            performanceText.text = string.Format(
                Const.ASA_RANK_TOP_PERCENT,
                display.top_percent,
                level
            );
        }

        if (hasRank)
        {
            rankText.text = string.Format(Const.ASA_RANK_WITHIN_LEVEL, level);

            // "#1" reads better than "Top 1" for the one case where they mean the same.
            positionText.text =
                display.top_rank == 1
                    ? Const.ASA_RANK_FIRST
                    : string.Format(Const.ASA_RANK_TOP_RANK, display.top_rank);
        }
        else
        {
            // The box stays where it is; only the position itself is replaced. Hiding it
            // left a hole in the middle of the panel.
            rankText.text = string.Format(Const.ASA_RANK_NO_POSITION_LABEL, level);
            positionText.text = "";
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
            performanceText.text = Const.ASA_RANK_NO_POSITION;
        }

        // Both boxes always stay up. Their contents change when there is no position to
        // show; the panel's shape does not.
        performanceInfo.SetActive(true);
        rankInfo.SetActive(true);
    }

    public void GiveLevelDescriptions(String cefr_level)
    {
        // Gives the CEFR level descriptions in words
        var levelDescriptions = new Dictionary<string, string>
        {
            ["A1"] = "Beginner",
            ["A2"] = "Elementary",
            ["B1"] = "Intermediate",
            ["B2"] = "Upper Intermediate",
            ["C1_plus"] = "Advanced/\nFluent",
        };
        if (levelDescriptions.TryGetValue(cefr_level, out description))
        {
            descriptionText.text = description;
        }
        ;
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

    // The level the server last reported, which is what a step is measured from. Set only
    // from a server response - never from a change we assume went through.
    private string currentLevel;

    // The endpoint is still to be built, so ASA_SET_LEVEL_URL is blank in Secret.cs until
    // it exists. A button that visibly does nothing is worse than no button, so both are
    // hidden while it is. Filling the value in is all that is needed to bring them back.
    private static bool LevelChangeConfigured => !string.IsNullOrEmpty(Secret.ASA_SET_LEVEL_URL);

    private static int LevelIndex(string cefrLevel)
    {
        return Array.IndexOf(Const.ASA_LEVELS, cefrLevel);
    }

    /// <summary>
    /// Wires Advance and Revert. Neither had a handler before - the Button components
    /// carry an empty persistent call from the Inspector and nothing else - so tapping
    /// them has never done anything.
    /// </summary>
    private void SetUpLevelChange()
    {
        ButtonUtils.Rewire(advanceLevelButtonGO, () => ChangeLevel(1));
        ButtonUtils.Rewire(revertLevelButtonGO, () => ChangeLevel(-1));
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

        StartCoroutine(
            NetworkManager
                .GetManager()
                .ServerPost_setLevel(
                    POSTType.ASA_SET_LEVEL,
                    targetLevel,
                    stored =>
                    {
                        SetLevelButtonsInteractable(true);

                        if (!stored)
                        {
                            // The loading popup is showing the failure with its own back
                            // button, and the profile underneath is untouched, so the user
                            // is still at the level they started from.
                            return;
                        }

                        // Reloaded rather than applied locally. The cohort is a different
                        // set of people now, so the rank and the percentile are different
                        // numbers, and only the server knows them.
                        StartCoroutine(
                            NetworkManager.GetManager().ServerPost_profile(POSTType.ASA_PROFILE)
                        );
                    }
                )
        );
    }

    private void SetLevelButtonsInteractable(bool interactable)
    {
        advanceLevelButtonGO.GetComponent<Button>().interactable = interactable;
        revertLevelButtonGO.GetComponent<Button>().interactable = interactable;
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
        switch (user.status)
        {
            case "USER_ASSESSMENT_DATA_INSUFFICIENT":
                // The one case the user can act on, so it says how far along they are.
                // The requirement comes from the server (MIN_USER_ASSESSMENTS); 3 is only
                // the fallback if it is missing, so raising it server-side needs no
                // client change.
                int needed = user.required_assessments > 0 ? user.required_assessments : 3;
                insufficientDataNotice.text = string.Format(
                    Const.ASA_RANK_NEED_MORE_TASKS,
                    needed,
                    user.current_assessments
                );
                break;

            case "COHORT_SIZE_TOO_SMALL":
                insufficientDataNotice.text = Const.ASA_RANK_COHORT_TOO_SMALL;
                break;

            case "RANK_UNAVAILABLE":
                insufficientDataNotice.text = Const.ASA_RANK_UNAVAILABLE;
                break;

            default:
                // An unrecognised status: the server's own wording beats anything we could
                // guess, and this is how a newly added case degrades gracefully.
                insufficientDataNotice.text = string.IsNullOrEmpty(user.message)
                    ? Const.ASA_RANK_UNAVAILABLE_GENERIC
                    : user.message;
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
}
