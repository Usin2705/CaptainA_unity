using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sends one rating group's answer as soon as the learner taps an emoji, and again
/// whenever they change their mind.
///
/// Before this, a rating only reached the server if the learner went on to press the
/// button at the bottom of the screen - Results on the loading popup, Send on the feedback
/// popup. Anyone who rated the result and then closed the app, or backed out to redo the
/// task, was never counted at all. Those are the sessions most worth hearing about, and
/// they were the ones being dropped.
///
/// Those buttons still send, through this same object, so a rating followed by a typed
/// comment arrives as a revision of the same answer rather than being lost.
///
/// **This depends on the server treating a repeat as an update.** One learner answering
/// one question about one recording must end up as one row, whether they tapped once or
/// changed their mind four times. Until that lands, every change of mind is a new row -
/// see the request in docs/TO_BACKEND.md.
/// </summary>
public class FeedbackAutoSend
{
    private readonly MonoBehaviour host;
    private readonly string feedbackType;

    // What the server has already been told, and which recording it was about. The
    // assessment id belongs in the key: the same emoji on the next recording is a new
    // answer, not a repeat of this one.
    private int sentAssessmentId = -1;
    private string sentGrade;
    private string sentComment;

    // One request at a time. A learner trying three emoji in quick succession would
    // otherwise have three posts racing, and the row would keep whichever reply landed
    // last rather than whichever emoji they settled on.
    private bool inFlight;
    private bool hasQueued;
    private string queuedGrade;
    private string queuedComment;

    public FeedbackAutoSend(MonoBehaviour host, string feedbackType)
    {
        this.host = host;
        this.feedbackType = feedbackType;
    }

    /// <summary>
    /// Hooks every toggle in <paramref name="group"/> so choosing one sends it.
    /// </summary>
    /// <param name="readComment">
    /// Reads the comment box beside the group at the moment of the tap. A function rather
    /// than a value because the comment is usually typed after the emoji is chosen.
    /// </param>
    public void Attach(ToggleGroup group, Func<string> readComment)
    {
        if (group == null)
        {
            return;
        }

        foreach (Toggle toggle in group.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.group != group)
            {
                continue;
            }

            Toggle chosen = toggle;

            // Panels wire in OnEnable, which runs on every reopen, so clear first. Same
            // reason as ButtonUtils.Rewire.
            chosen.onValueChanged.RemoveAllListeners();
            chosen.onValueChanged.AddListener(isOn =>
            {
                // A group fires twice per change: false for the emoji being dropped, then
                // true for the one being chosen. Only the second one is an answer.
                if (isOn)
                {
                    Send(chosen.name, readComment == null ? "" : readComment());
                }
            });
        }
    }

    /// <summary>
    /// Hooks the comment box so what was typed goes up when the learner leaves the field,
    /// rather than only when they press the button at the bottom.
    ///
    /// onEndEdit, not onValueChanged: the latter fires per keystroke and would post a
    /// request per letter. This fires once, when the field is deselected or submitted -
    /// which includes tapping the screen's own button, so the comment is already on its
    /// way by the time that handler runs.
    /// </summary>
    /// <param name="group">
    /// Read for the emoji currently chosen. Every feedback row carries a 1-5 value, so a
    /// comment cannot travel on its own - see the guard below.
    /// </param>
    public void AttachComment(TMP_InputField field, ToggleGroup group)
    {
        if (field == null || group == null)
        {
            return;
        }

        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(comment =>
        {
            // No emoji chosen yet. The server requires reaction_value on every row, so
            // there is nothing valid to send - this comment goes up the moment they pick
            // one, because choosing an emoji reads the box as it stands.
            Toggle chosen = group.ActiveToggles().FirstOrDefault();

            if (chosen != null)
            {
                Send(chosen.name, comment);
            }
        });
    }

    /// <summary>
    /// Sends unless the server already has exactly this answer. Also called by the
    /// screen's own button, which is therefore free of charge when nothing has changed
    /// since the tap.
    /// </summary>
    /// <param name="grade">The toggle's name - its last character is the 1-5 value.</param>
    public void Send(string grade, string comment)
    {
        if (string.IsNullOrEmpty(grade))
        {
            return;
        }

        comment ??= "";

        // No result has come back yet, so there is no recording to attach this to and the
        // server would answer 422. Rare - the emoji sit on the screen that appears once
        // the scores arrive - but reachable if the user is quick. The screen's own button
        // sends it later, by which time the id exists.
        int assessmentId = NetworkManager.CurrentAssessmentId;
        if (assessmentId < 0)
        {
            return;
        }

        bool alreadySent =
            assessmentId == sentAssessmentId && grade == sentGrade && comment == sentComment;

        if (alreadySent)
        {
            return;
        }

        if (inFlight)
        {
            // Keep only the latest: intermediate emoji on the way to the one they settle
            // on are not answers anybody needs.
            queuedGrade = grade;
            queuedComment = comment;
            hasQueued = true;
            return;
        }

        Post(assessmentId, grade, comment);
    }

    private void Post(int assessmentId, string grade, string comment)
    {
        inFlight = true;

        host.StartCoroutine(
            NetworkManager
                .GetManager()
                .ServerPost_feedback(
                    POSTType.USER_ASA_FEEDBACK,
                    feedbackType,
                    grade,
                    comment,
                    stored =>
                    {
                        inFlight = false;

                        // Recorded only on success. A failed post must stay un-remembered,
                        // or the dedup above would swallow the retry that the screen's
                        // button would otherwise have made.
                        if (stored)
                        {
                            sentAssessmentId = assessmentId;
                            sentGrade = grade;
                            sentComment = comment;
                        }

                        if (hasQueued)
                        {
                            hasQueued = false;
                            Send(queuedGrade, queuedComment);
                        }
                    }
                )
        );
    }
}
