using System.Linq;
using SaySuomi.Managers;
using SaySuomi.Modals;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class ASATaskListScreen : AppScreen
    {
        [Header("Task List")]
        [Tooltip("In task order: each opens the matching entry in ASATaskScreen's Tasks.")]
        [SerializeField]
        private Button[] taskButtons;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button profileButton;

        [Tooltip("Drop-down error message, shown through ErrorBanner.")]
        [SerializeField]
        private GameObject errorPopupGO;

        [Header("Profile Loading Modal")]
        [SerializeField]
        private LoadingModal profileLoadingModal;

        [SerializeField]
        private GameObject loadingIconGO;

        [Tooltip("Shown only after the request fails.")]
        [SerializeField]
        private GameObject profileLoadingBackButtonGO;

        [SerializeField]
        private GameObject profileLoadingErrorTextGO;

        [Header("Feedback Modal")]
        [SerializeField]
        private GameObject overallFeedbackPopUpGO;

        [SerializeField]
        private Button feedbackBackButton;

        [SerializeField]
        private Button feedbackSendButton;

        [SerializeField]
        private ToggleGroup overallRatingOptions;

        [SerializeField]
        private TMP_InputField overallFeedbackTextGO;

        private ModalScreen overallFeedbackModal;

        private LocalizedTextHelper profileErrorHelper;

        protected override void Bind()
        {
            Navigator navigator = Navigator.GetManager();

            ButtonWiring.Replace(backButton, () => navigator.Back());
            for (int i = 0; i < taskButtons.Length; i++)
            {
                int task = i;
                ButtonWiring.Replace(taskButtons[i], () => OpenTask(task));
            }
            ButtonWiring.Replace(profileButton, LoadProfile);
            ButtonWiring.Replace(
                profileLoadingBackButtonGO.GetComponent<Button>(),
                () => navigator.Back()
            );
            ButtonWiring.Replace(feedbackSendButton, SendOverallFeedback);
            ButtonWiring.Replace(feedbackBackButton, () => navigator.Back());

            profileLoadingModal.Dismissible = false;

            overallFeedbackModal = ModalScreen.Attach(overallFeedbackPopUpGO);

            // Declining restarts the count, exactly as answering does. It used to only
            // hide the window and write nothing, so every return to the task list
            // reopened it and the only ways out were sending feedback or completing
            // another task.
            overallFeedbackModal.OnDiscard = () =>
            {
                MarkOverallFeedbackAsked();
                overallRatingOptions.SetAllTogglesOff();
                overallFeedbackTextGO.text = "";
            };
            overallFeedbackModal.OnBack = () =>
            {
                ErrorBanner.Hide(errorPopupGO);
                return false;
            };
        }

        private void OnEnable()
        {
            // Stops animated dropdown error message from appearing
            ErrorBanner.Hide(errorPopupGO);

            Debug.Log(PlayerPrefs.GetInt("TasksSent"));
            // Ask general feedback if number of completed tasks so far is a multiple of 5
            OverallFeedback();
        }

        private void OnDestroy()
        {
            profileErrorHelper?.ClearText();
        }

        /// <summary>
        /// Fetches the profile behind the loading popup, then opens it. Also used by
        /// ASAProfileScreen to reload after a level change, when it is already open.
        ///
        /// Runs on NetworkManager, because this screen is hidden during that reload and a
        /// disabled object cannot run a coroutine. The modal stops the learner leaving while
        /// it waits, so nothing can be closed underneath the response.
        /// </summary>
        public void LoadProfile()
        {
            ShowProfileLoading();

            NetworkManager network = NetworkManager.GetManager();

            network.StartCoroutine(
                network.ServerPost_profile(
                    POSTType.ASA_PROFILE,
                    json =>
                    {
                        if (json == null)
                        {
                            ShowProfileError();
                            return;
                        }

                        Navigator navigator = Navigator.GetManager();

                        profileLoadingModal.Dismissible = true;
                        navigator.Back();

                        ASAProfileScreen profile = navigator.Get<ASAProfileScreen>();
                        if (!navigator.IsOpen(profile))
                        {
                            navigator.Push<ASAProfileScreen>();
                        }

                        profile.ShowStats(json);
                    }
                )
            );
        }

        public void ShowProfileLoading()
        {
            profileLoadingModal.Dismissible = false;
            loadingIconGO.SetActive(true);
            profileLoadingErrorTextGO.SetActive(false);
            profileLoadingBackButtonGO.SetActive(false);
            Navigator.GetManager().Open(profileLoadingModal);
        }

        /// <summary>Shows the last failed request on the loading popup, with its back button.</summary>
        public void ShowProfileError()
        {
            profileLoadingModal.Dismissible = true;
            loadingIconGO.SetActive(false);
            profileLoadingErrorTextGO.SetActive(true);
            profileLoadingBackButtonGO.SetActive(true);

            profileErrorHelper ??= new LocalizedTextHelper(
                profileLoadingErrorTextGO.GetComponent<TextMeshProUGUI>()
            );
            ServerNotices.Show(profileErrorHelper, NetworkManager.GetManager().lastServerError);
        }

        private void SendOverallFeedback()
        {
            var overallRating = overallRatingOptions.ActiveToggles().FirstOrDefault();
            string comment_overall = overallFeedbackTextGO.text;

            // Show drop-down error animation if attempting to send feedback without selecting an emoji
            if (overallRating == null)
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
                        "overall_experience",
                        overallRating.GetComponent<OptionValue>().value,
                        comment_overall
                    )
            );

            // OnDiscard resets the form and records that the learner was asked.
            Navigator.GetManager().Back();
        }

        /// <summary>
        /// Restarts the countdown to the next overall-feedback ask.
        ///
        /// Called for both Send and Cancel: what is recorded is that the learner was asked at
        /// this point, not what they said. That is enough to pace the question, and it means
        /// declining is respected for a full interval rather than until the next task.
        ///
        /// Saved immediately - a decision lost to the app being killed is a learner who gets
        /// asked again anyway, which is the behaviour this exists to stop.
        /// </summary>
        private void MarkOverallFeedbackAsked()
        {
            PlayerPrefs.SetInt(
                Const.PREF_OVERALL_FEEDBACK_ASKED_AT,
                PlayerPrefs.GetInt("TasksSent", 0)
            );
            PlayerPrefs.Save();
        }

        // The task buttons differ only by index.
        private void OpenTask(int taskIndex)
        {
            Navigator.GetManager().Push<ASATaskScreen>().DisplayTask(taskIndex);
        }

        private void OverallFeedback()
        {
            // Ask for overall feedback once Const.ASA_OVERALL_FEEDBACK_EVERY tasks have been
            // completed since the last time we asked.
            //
            // Deliberately >= rather than landing on a multiple. The check only runs when the
            // task list opens, so an exact test silently skipped anyone who finished their
            // tenth task and went straight on to an eleventh - the one moment they were owed
            // the question was the one moment nobody was looking. A backlog cannot be missed:
            // it just waits until they next open the list.
            int tasksSent = PlayerPrefs.GetInt("TasksSent", 0);
            int askedAt = PlayerPrefs.GetInt(Const.PREF_OVERALL_FEEDBACK_ASKED_AT, 0);

            if (tasksSent - askedAt >= Const.ASA_OVERALL_FEEDBACK_EVERY)
            {
                Navigator.GetManager().Open(overallFeedbackModal);
            }
        }
    }
}
