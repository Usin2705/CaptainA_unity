using System.Collections;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ASAPanel : MonoBehaviour
{
    [SerializeField]
    private AudioManager audioManager;

    [SerializeField]
    private AdvancePanel AdvancePanel;

    [SerializeField]
    GameObject recordButtonGO;

    [SerializeField]
    GameObject pauseButtonGO;

    [SerializeField]
    GameObject sendButtonGO;

    [SerializeField]
    GameObject replayButtonGO;

    [SerializeField]
    GameObject replayBarGO;

    [SerializeField]
    GameObject progressBarBackgroundGO;

    [SerializeField]
    GameObject progressBarGO;

    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject loadingPopUpGO;

    [SerializeField]
    GameObject dimPanelASAGO;

    [SerializeField]
    GameObject loadingIconGO;

    [SerializeField]
    GameObject resultsButtonGO;

    [SerializeField]
    GameObject errorTextGO;

    [SerializeField]
    GameObject backButtonGO;

    public ToggleGroup ratingOptions;

    // Posts the self-assessment emoji on tap rather than waiting for Results. Built once
    // and kept: it remembers what the server already has, and that memory is what stops
    // the Results button re-sending an answer that has not changed.
    private FeedbackAutoSend selfAssessmentFeedback;

    [SerializeField]
    TMP_InputField feedbackTextGO;

    [System.Serializable]
    public class TaskAttributes
    {
        public string taskText;
        public Sprite taskImage;
        public float recordingTime = 0;
    }

    [SerializeField]
    private TaskAttributes[] tasks;

    [SerializeField]
    private TMPro.TextMeshProUGUI taskTextGO;

    [SerializeField]
    private Image taskImageGO;

    private bool isRecording = false;

    private bool isReplaying = false;

    public bool isLoading = false;

    private float currentTime = 0;

    public int currentTaskSelected = -1;

    /// <summary>
    /// The id the DTA server knows this task by. Its task ids start at 1
    /// (inference/assets/task_id_map.json: 1 = the friend borrowing money, 2 = cannot come
    /// to work, ... 5), while currentTaskSelected is an index into the tasks array above
    /// and starts at 0.
    ///
    /// Sending the index unshifted made task 1 fail with 400 "Unknown task_id 0" and -
    /// far worse - scored tasks 2 to 5 against the PREVIOUS task's prompt, which returns
    /// a plausible number and no error at all. Convert here and nowhere else, so the value
    /// we send and the value we check the response against cannot drift apart.
    /// </summary>
    public int CurrentServerTaskId => currentTaskSelected + 1;

    private AudioClip recording;

    [SerializeField]
    GameObject errorPopupGO;

    public void DisplayTask(int taskSelected)
    {
        // Determines graphic and text for selected task if applicable
        currentTaskSelected = taskSelected;
        TaskAttributes task = tasks[taskSelected];

        taskTextGO.text = task.taskText;

        if (task.taskImage != null)
        {
            taskImageGO.gameObject.SetActive(true);
            taskImageGO.sprite = task.taskImage;
        }
        else
        {
            taskImageGO.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        recordButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        pauseButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        sendButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        replayButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        resultsButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        backButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();

        recordButtonGO.GetComponent<Button>().onClick.AddListener(() => OnRecordButtonClicked());
        pauseButtonGO.GetComponent<Button>().onClick.AddListener(() => StopRecord());
        sendButtonGO.GetComponent<Button>().onClick.AddListener(() => OnSendButtonClicked());
        replayButtonGO.GetComponent<Button>().onClick.AddListener(() => OnReplayButtonClicked());
        resultsButtonGO.GetComponent<Button>().onClick.AddListener(() => OnResultsButtonClicked());
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => OnBackButtonClicked());

        // Each emoji now posts the moment it is tapped, so a learner who rates the result
        // and then quits, or backs out to redo the task, is still counted. Results keeps
        // sending as well - see OnResultsButtonClicked.
        selfAssessmentFeedback ??= new FeedbackAutoSend(this, "self_assessment");
        selfAssessmentFeedback.Attach(ratingOptions, () => feedbackTextGO.text);
        selfAssessmentFeedback.AttachComment(feedbackTextGO, ratingOptions);

        replayBarGO.SetActive(false);
        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);
        resultsButtonGO.SetActive(false);
        dimPanelASAGO.SetActive(false);

        // The panel is reused, so it must open in a known state rather than whatever the
        // last visit left behind. OnDisable already does this, but not every way out of a
        // screen runs it - the app being backgrounded mid-recording does not - and the
        // cost of being wrong here is a Stop button that never works again.
        CancelRecording();
    }

    void StartTimer()
    {
        // Start countdown so the user knows how long the recording will be
        currentTime = tasks[currentTaskSelected].recordingTime;

        // Hide the record button and replace it with the pause button
        recordButtonGO.SetActive(false);
        pauseButtonGO.SetActive(true);
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);
        replayBarGO.SetActive(false);

        // Show the countdown progress bar
        progressBarBackgroundGO.SetActive(true);
        progressBarGO.SetActive(true);

        isRecording = true;
    }

    void Update()
    {
        // Show relevant animations depending on state
        if (isRecording)
        {
            UpdateProgressBar();
        }
        if (isReplaying)
        {
            UpdateReplayBar();
        }
        if (isLoading)
        {
            AnimateLoading();
        }

        // Handle back button press on new Input System
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnPhoneBackPressed();
        }
    }

    /// <summary>
    /// Android's back button and back gesture, which Unity reports as Escape.
    ///
    /// Until now this screen had no way out at all: it has no back button of its own, so a
    /// learner who opened a task was stuck there until they finished it. Back now leaves
    /// the task the same way the New task button does.
    ///
    /// The rule is that back does whatever the visible back affordance does, and nothing
    /// when there is none - so it cannot reach a state the buttons on screen would not.
    /// Both screens that can sit in front of this one are siblings rather than children,
    /// which means switching this panel off would leave them on screen; each has to be
    /// ruled out before falling through to leaving the task.
    /// </summary>
    private void OnPhoneBackPressed()
    {
        // Not while the microphone is open. Leaving would throw the recording away, and
        // there is no back affordance on screen to say so - what is showing is Stop. The
        // learner is not stuck either: stop first, then back.
        if (isRecording)
        {
            return;
        }

        // The results screen opens on top of this one without switching it off, so this
        // Update carries on running behind it. It owns the key while it is up.
        if (feedbackPanelGO.activeSelf)
        {
            return;
        }

        if (loadingPopUpGO.activeSelf)
        {
            // The popup only shows its own Back button when the upload failed. While the
            // request is in flight there is nothing to go back to, and once the result has
            // arrived the way on is Results - backing out there would throw away a score
            // the learner cannot get again without recording the task a second time.
            if (backButtonGO.activeSelf)
            {
                OnBackButtonClicked();
            }

            return;
        }

        ReturnToTaskPanel();
    }

    /// <summary>
    /// Leaves the task for the task list - the exact reverse of TaskPanel.OpenTask.
    ///
    /// Only reached with the microphone closed, so nothing is being thrown away mid-flight.
    /// A finished recording that was never sent does go, which is the intended reading of
    /// backing out: an answer the learner chose not to submit.
    /// </summary>
    private void ReturnToTaskPanel()
    {
        AdvancePanel.ShowTaskPanel();
        gameObject.SetActive(false);
    }

    void UpdateProgressBar()
    {
        // Start decreasing currentTime with deltaTime and compare the ratio to given recordingTime
        currentTime -= Time.deltaTime;
        progressBarGO.GetComponent<Image>().fillAmount =
            currentTime / tasks[currentTaskSelected].recordingTime;

        // When currentTime reaches zero, the time has run out and recording is stopped
        if (currentTime <= 0)
        {
            currentTime = 0;
            progressBarGO.SetActive(false);
            StopRecord();
        }
    }

    void UpdateReplayBar()
    {
        // Same principle as with UpdateProgressBar(), but this time comparing with the length of the recording
        currentTime -= Time.deltaTime;
        replayBarGO.GetComponent<Image>().fillAmount = currentTime / recording.length;
        if (currentTime <= 0)
        {
            currentTime = 0;
            replayBarGO.SetActive(false);
            isReplaying = false;
        }
    }

    public void OnRecordButtonClicked()
    {
        // Stop stays dead until the microphone is genuinely open. It used to be re-enabled
        // by a 0.3s timer running alongside AudioManager's own 0.3s startup delay, so the
        // two finished together and a quick tap could land Microphone.End() before the
        // Microphone.Start() it was meant to cancel: the device then opened *after* the
        // stop and recorded on unattended, while the file saved was whatever the clip held
        // from before. Waiting for the callback removes the race rather than tuning it.
        var stopButton = pauseButtonGO.GetComponent<Button>();
        stopButton.interactable = false;

        StartTimer();

        AudioManager
            .GetManager()
            .StartRecording(
                (int)tasks[currentTaskSelected].recordingTime,
                started =>
                {
                    if (started)
                    {
                        stopButton.interactable = true;
                        return;
                    }

                    // No microphone - permission refused, or another app holds the device.
                    // Put the panel back rather than counting down against a recording
                    // that is not happening and then offering the silence for replay.
                    Debug.LogError("Recording did not start; returning to the record button.");
                    AbandonRecording();
                }
            );
    }

    /// <summary>
    /// Returns the panel to its ready-to-record state without producing a result.
    /// Used when a recording never started, and when it captured nothing.
    /// </summary>
    private void AbandonRecording()
    {
        isRecording = false;
        currentTime = 0f;

        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);

        pauseButtonGO.SetActive(false);
        pauseButtonGO.GetComponent<Button>().interactable = true;
        recordButtonGO.SetActive(true);

        // Deliberately not offered: there is no new recording behind them, and the file on
        // disk is still the previous answer.
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);
    }

    public void StopRecord()
    {
        isRecording = false;

        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);

        // Stop recording and save the audio
        bool saved = AudioManager.GetManager().StopRecording();

        if (!saved)
        {
            // Stopped before the device captured anything - a tap the instant Stop became
            // live, or a microphone that never opened. There is no answer here to send or
            // replay, and offering the silence as one is how a learner ends up submitting
            // 30 seconds of nothing.
            AbandonRecording();
            return;
        }

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(true);
        replayButtonGO.SetActive(true);

        // Retrieve audio for replaying
        StartCoroutine(AudioManager.GetManager().LoadAudioClip(Const.ASA_FILENAME, replayButtonGO));
    }

    public void OnReplayButtonClicked()
    {
        // Retrieve audio and set the relevant variables for playing the animation
        recording = audioManager.GetReplayClip();
        currentTime = recording.length;
        replayBarGO.SetActive(true);
        isReplaying = true;
    }

    public void OnSendButtonClicked()
    {
        audioManager.StopReplaying();
        isReplaying = false;
        replayButtonGO.SetActive(false);
        replayBarGO.SetActive(false);

        // Send the audio to the server when clicked
        AudioManager
            .GetManager()
            .GetAudioAndPost_ASA(
                POSTType.ASA_TASK, // POST type = OTHER
                null // transcript text
            );

        loadingPopUpGO.SetActive(true);
        StopErrorAnimation();
        ratingOptions.SetAllTogglesOff();
        feedbackTextGO.text = "";
        resultsButtonGO.SetActive(false);
        errorTextGO.SetActive(false);
        loadingIconGO.SetActive(true);
        isLoading = true;
        dimPanelASAGO.SetActive(true);
        backButtonGO.SetActive(false);
    }

    public void AnimateLoading()
    {
        // Change value -6.0f to change rotation speed
        loadingIconGO.transform.Rotate(0, 0, -6.0f, Space.Self);
    }

    public void StopErrorAnimation()
    {
        Animator anim = errorPopupGO.GetComponent<Animator>();
        anim.enabled = false;
        errorPopupGO.SetActive(false);
    }

    public void OnResultsButtonClicked()
    {
        var self_rating = ratingOptions.ActiveToggles().FirstOrDefault();
        string comment_self_rating = feedbackTextGO.text;

        // Send feedback if given
        if (self_rating == null && comment_self_rating != "")
        {
            Animator anim = errorPopupGO.GetComponent<Animator>();

            errorPopupGO.SetActive(true);

            anim.enabled = true;

            anim.Play("Error Popup Animation");

            return;
        }

        // Reset feedback forms and go to feedback panel
        ratingOptions.SetAllTogglesOff();
        loadingPopUpGO.SetActive(false);
        dimPanelASAGO.SetActive(false);
        feedbackPanelGO.SetActive(true);

        if (self_rating != null)
        {
            // Usually a no-op by now: the emoji posted itself when it was tapped. It still
            // matters when a comment was typed afterwards, and when the tap happened
            // before the assessment id existed.
            selfAssessmentFeedback.Send(self_rating.name, comment_self_rating);
        }
    }

    public void OnBackButtonClicked()
    {
        // Go back to redo current task
        errorPopupGO.SetActive(false);
        ratingOptions.SetAllTogglesOff();
        feedbackTextGO.text = "";
        loadingPopUpGO.SetActive(false);
        dimPanelASAGO.SetActive(false);
    }

    void OnDisable()
    {
        audioManager.StopReplaying();
        CancelRecording();
    }

    /// <summary>
    /// Abandons a recording that is still running when the panel closes.
    ///
    /// Leaving mid-recording used to walk away from three things at once, and the third
    /// one left the screen permanently unusable:
    ///
    ///   - The microphone stayed open. Nothing stopped it, so it kept recording behind
    ///     the flashcards or the profile until its length ran out.
    ///   - isRecording stayed true. Update() does not run on a disabled object, so the
    ///     countdown froze; on returning it picked up from the stale currentTime and could
    ///     fire StopRecord() on its own, part way through a screen the learner had just
    ///     opened fresh.
    ///   - The pause button stayed dead. OnRecordButtonClicked turns it off and schedules
    ///     EnableAfterDelay to turn it back on 0.3s later - and disabling a GameObject
    ///     kills its coroutines. Leave inside that window and the button is never
    ///     re-enabled, so on the next recording Stop does nothing at all. That is the
    ///     freeze: the recording runs to its full length with no way to end it early.
    ///
    /// The audio is deliberately discarded rather than kept. A half-finished answer the
    /// learner walked away from is not one they meant to send.
    /// </summary>
    private void CancelRecording()
    {
        if (isRecording)
        {
            AudioManager.GetManager().StopRecording();
        }

        isRecording = false;
        isReplaying = false;
        currentTime = 0f;

        // Undoes the interactable=false set by OnRecordButtonClicked, whose coroutine is
        // about to be killed. Cheap to do unconditionally, and the one line that keeps the
        // Stop button alive.
        if (pauseButtonGO != null)
        {
            pauseButtonGO.GetComponent<Button>().interactable = true;
        }
    }
}
