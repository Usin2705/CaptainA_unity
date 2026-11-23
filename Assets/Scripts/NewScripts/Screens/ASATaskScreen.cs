using System.Collections;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using SaySuomi.Managers;
using SaySuomi.Modals;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class ASATaskScreen : AppScreen
    {
        [Header("Task")]
        // In the Inspector because it holds sprites, and assets cannot be found by path.
        [Tooltip("In task order. An entry's position plus one is the task ID sent to the server.")]
        [SerializeField]
        private TaskAttributes[] tasks;

        [SerializeField]
        private TextMeshProUGUI taskTextGO;

        [SerializeField]
        private Image taskImageGO;

        [Tooltip("Pos Y of the task text when the task has no picture.")]
        [SerializeField]
        private float taskTextPosYWithoutImage = 0f;

        [Tooltip("Also the template for the back button on screens that have none of their own.")]
        [SerializeField]
        private Button taskBackButton;

        [Header("Recording")]
        [SerializeField]
        private GameObject recordButtonGO;

        [Tooltip("Stops the recording.")]
        [SerializeField]
        private GameObject pauseButtonGO;

        [Tooltip("Fill that counts down the recording time.")]
        [SerializeField]
        private GameObject progressBarGO;

        [SerializeField]
        private GameObject progressBarBackgroundGO;

        [Header("Replay and Send")]
        [SerializeField]
        private GameObject replayButtonGO;

        [Tooltip("Fill that shows replay progress.")]
        [SerializeField]
        private GameObject replayBarGO;

        [SerializeField]
        private GameObject sendButtonGO;

        [Header("Sending Modal")]
        [SerializeField]
        private TextMeshProUGUI loadingTitleText;

        [SerializeField]
        private GameObject loadingIconGO;

        [SerializeField]
        private GameObject errorTextGO;

        [Tooltip("Drop-down error message, shown through ErrorBanner.")]
        [SerializeField]
        private GameObject errorPopupGO;

        [SerializeField]
        private ToggleGroup ratingOptions;

        [SerializeField]
        private TMP_InputField feedbackTextGO;

        [Tooltip("Shown once the server has scored the recording.")]
        [SerializeField]
        private GameObject resultsButtonGO;

        [Tooltip("Shown only after a failed send.")]
        [SerializeField]
        private GameObject backButtonGO;

        [System.NonSerialized]
        public int currentTaskSelected = -1;

        private TaskSendingModal loadingModal;

        // Posts the self-assessment emoji on tap rather than waiting for Results. Built once
        // and kept: it remembers what the server already has, and that memory is what stops
        // the Results button re-sending an answer that has not changed.
        private FeedbackAutoSend selfAssessmentFeedback;

        private LocalizedTextHelper taskTextHelper;

        private LocalizedTextHelper loadingTitleHelper;

        private LocalizedTextHelper loadingErrorHelper;

        // The scene's own position for the text, read on the first DisplayTask so tasks with a
        // picture are put back exactly where the scene has them.
        private float? taskTextPosYWithImage;

        private bool isRecording = false;

        private bool isReplaying = false;

        private float currentTime = 0;

        private AudioClip recording;

        /// <summary>
        /// The id the DTA server knows this task by. Its task ids start at 1
        /// (inference/assets/task_id_map.json: 1 = the friend borrowing money, 2 = cannot come
        /// to work, ... 5, and 6 = the experimental "An Important Place", DTA task 04_a), while
        /// currentTaskSelected is an index into the tasks array above and starts at 0.
        ///
        /// Sending the index unshifted made task 1 fail with 400 "Unknown task_id 0" and -
        /// far worse - scored tasks 2 to 5 against the PREVIOUS task's prompt, which returns
        /// a plausible number and no error at all. Convert here and nowhere else, so the value
        /// we send and the value we check the response against cannot drift apart.
        /// </summary>
        public int CurrentServerTaskId => currentTaskSelected + 1;

        /// <summary>The button ScreenBackButton copies, so all back buttons look the same.</summary>
        internal GameObject BackButtonTemplate => taskBackButton.gameObject;

        protected override void Bind()
        {
            loadingModal = Navigator.GetManager().Get<TaskSendingModal>();

            Navigator navigator = Navigator.GetManager();

            ButtonWiring.Replace(recordButtonGO.GetComponent<Button>(), OnRecordButtonClicked);
            ButtonWiring.Replace(pauseButtonGO.GetComponent<Button>(), StopRecord);
            ButtonWiring.Replace(sendButtonGO.GetComponent<Button>(), OnSendButtonClicked);
            ButtonWiring.Replace(replayButtonGO.GetComponent<Button>(), OnReplayButtonClicked);
            ButtonWiring.Replace(taskBackButton, () => navigator.Back());
            ButtonWiring.Replace(resultsButtonGO.GetComponent<Button>(), OnResultsButtonClicked);
            ButtonWiring.Replace(backButtonGO.GetComponent<Button>(), () => navigator.Back());

            // Go back to redo current task
            loadingModal.OnBack = () =>
            {
                ErrorBanner.Hide(errorPopupGO);
                return false;
            };
            loadingModal.OnDiscard = () =>
            {
                ratingOptions.SetAllTogglesOff();
                feedbackTextGO.text = "";
            };

            // Each emoji now posts the moment it is tapped, so a learner who rates the result
            // and then quits, or backs out to redo the task, is still counted. Results keeps
            // sending as well - see OnResultsButtonClicked.
            //
            // Hosted on NetworkManager, because this screen hides behind Results and a post
            // running on it would die there.
            selfAssessmentFeedback = new FeedbackAutoSend(
                NetworkManager.GetManager(),
                "self_assessment"
            );
            selfAssessmentFeedback.Attach(ratingOptions, () => feedbackTextGO.text);
            selfAssessmentFeedback.AttachComment(feedbackTextGO, ratingOptions);
        }

        private void OnEnable()
        {
            replayBarGO.SetActive(false);
            progressBarBackgroundGO.SetActive(false);
            progressBarGO.SetActive(false);

            pauseButtonGO.SetActive(false);
            recordButtonGO.SetActive(true);
            sendButtonGO.SetActive(false);
            replayButtonGO.SetActive(false);

            // The panel is reused, so it must open in a known state rather than whatever the
            // last visit left behind. OnDisable already does this, but not every way out of a
            // screen runs it - the app being backgrounded mid-recording does not - and the
            // cost of being wrong here is a Stop button that never works again.
            CancelRecording();
        }

        private void Update()
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
        }

        private void OnDisable()
        {
            taskTextHelper?.ClearText();

            // Already destroyed when this runs because the app is quitting.
            AudioManager audioManager = AudioManager.GetManager();
            if (audioManager != null)
            {
                audioManager.StopReplaying();
            }

            CancelRecording();
        }

        private void OnDestroy()
        {
            loadingTitleHelper?.ClearText();
            loadingErrorHelper?.ClearText();
        }

        /// <summary>
        /// The microphone takes priority over leaving, because leaving would throw the recording
        /// away. What the screen shows then is Stop, so back does that: the first press stops
        /// the recording, and the next one leaves.
        /// </summary>
        public override bool HandleBack()
        {
            if (!isRecording)
            {
                return false;
            }

            StopRecord();
            return true;
        }

        public void DisplayTask(int taskSelected)
        {
            // Determines graphic and text for selected task if applicable
            currentTaskSelected = taskSelected;
            TaskAttributes task = tasks[taskSelected];

            taskTextHelper ??= new LocalizedTextHelper(taskTextGO);
            taskTextHelper?.SetText(task.taskText);

            if (task.taskImage != null)
            {
                taskImageGO.gameObject.SetActive(true);
                taskImageGO.sprite = task.taskImage;
            }
            else
            {
                taskImageGO.gameObject.SetActive(false);
            }

            // The text sits below the picture. Without one that gap is wasted, and a long prompt
            // runs into the record button. Only task 6 has no picture today.
            RectTransform textRect = taskTextGO.rectTransform;
            taskTextPosYWithImage ??= textRect.anchoredPosition.y;
            textRect.anchoredPosition = new Vector2(
                textRect.anchoredPosition.x,
                task.taskImage != null ? taskTextPosYWithImage.Value : taskTextPosYWithoutImage
            );
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
            StartCoroutine(
                AudioManager.GetManager().LoadAudioClip(Const.ASA_FILENAME, replayButtonGO)
            );
        }

        public void OnReplayButtonClicked()
        {
            // Retrieve audio and set the relevant variables for playing the animation
            recording = AudioManager.GetManager().GetReplayClip();
            currentTime = recording.length;
            replayBarGO.SetActive(true);
            isReplaying = true;
        }

        public void OnSendButtonClicked()
        {
            AudioManager.GetManager().StopReplaying();
            isReplaying = false;
            replayButtonGO.SetActive(false);
            replayBarGO.SetActive(false);

            // The popup is reused for every recording, so put the title back to its sending
            // state here. Otherwise the second recording spins underneath whatever the first
            // one finished with.
            SetLoadingTitle(ServerNotices.Loading());

            // Send the audio to the server when clicked
            AudioManager
                .GetManager()
                .GetAudioAndPost_ASA(
                    POSTType.ASA_TASK, // POST type = OTHER
                    CurrentServerTaskId,
                    OnASAPostDone
                );

            // The popup only offers Back once the upload has failed. While the request is in
            // flight there is nothing to go back to, and once the result has arrived the way on
            // is Results - backing out there would throw away a score the learner cannot get
            // again without recording the task a second time.
            loadingModal.Dismissible = false;
            Navigator.GetManager().Open(loadingModal);

            ErrorBanner.Hide(errorPopupGO);
            ratingOptions.SetAllTogglesOff();
            feedbackTextGO.text = "";
            resultsButtonGO.SetActive(false);
            errorTextGO.SetActive(false);
            loadingIconGO.SetActive(true);
            backButtonGO.SetActive(false);
        }

        public void OnResultsButtonClicked()
        {
            var self_rating = ratingOptions.ActiveToggles().FirstOrDefault();
            string comment_self_rating = feedbackTextGO.text;

            // Send feedback if given
            if (self_rating == null && comment_self_rating != "")
            {
                ErrorBanner.Show(errorPopupGO);
                return;
            }

            // Reset feedback forms, which OnDiscard does, and go to feedback panel
            Navigator navigator = Navigator.GetManager();
            loadingModal.Dismissible = true;
            navigator.Back();
            navigator.Push<ASAResultsScreen>();

            if (self_rating != null)
            {
                // Usually a no-op by now: the emoji posted itself when it was tapped. It still
                // matters when a comment was typed afterwards, and when the tap happened
                // before the assessment id existed.
                selfAssessmentFeedback.Send(
                    self_rating.GetComponent<OptionValue>().value,
                    comment_self_rating
                );
            }
        }

        private void StartTimer()
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

        private void UpdateProgressBar()
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

        private void UpdateReplayBar()
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

        private void OnASAPostDone(bool serverOk)
        {
            loadingIconGO.SetActive(false);

            if (serverOk)
            {
                resultsButtonGO.SetActive(true);
                SetLoadingTitle(ServerNotices.LoadingCompleted());
                return;
            }

            loadingModal.Dismissible = true;
            errorTextGO.SetActive(true);
            backButtonGO.SetActive(true);
            SetLoadingTitle(ServerNotices.LoadingFailed());

            loadingErrorHelper ??= new LocalizedTextHelper(
                errorTextGO.GetComponent<TextMeshProUGUI>()
            );
            ServerNotices.Show(loadingErrorHelper, NetworkManager.GetManager().lastServerError);
        }

        private void SetLoadingTitle(LocalizedString message)
        {
            loadingTitleHelper ??= new LocalizedTextHelper(loadingTitleText);
            loadingTitleHelper.SetText(message);
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

        [System.Serializable]
        public class TaskAttributes
        {
            public LocalizedString taskText;
            public Sprite taskImage;

            [Tooltip("The longest the learner can record for, in seconds.")]
            public float recordingTime = 0;
        }
    }
}
