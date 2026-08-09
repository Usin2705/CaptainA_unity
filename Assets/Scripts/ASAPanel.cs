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

        replayBarGO.SetActive(false);
        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);
        resultsButtonGO.SetActive(false);
        dimPanelASAGO.SetActive(false);
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
        // Start recording
        AudioManager.GetManager().StartRecording((int)tasks[currentTaskSelected].recordingTime);
        StartTimer();

        var btn = pauseButtonGO.GetComponent<Button>();
        btn.interactable = false;

        StartCoroutine(EnableAfterDelay(btn, 0.3f));
    }

    IEnumerator EnableAfterDelay(Button btn, float delay)
    {
        yield return new WaitForSeconds(delay);
        btn.interactable = true;
    }

    public void StopRecord()
    {
        isRecording = false;

        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);

        // Stop recording and save the audio
        AudioManager.GetManager().StopRecording();

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
            StartCoroutine(
                NetworkManager
                    .GetManager()
                    .ServerPost_feedback(
                        POSTType.USER_ASA_FEEDBACK,
                        "self_assessment",
                        self_rating.name,
                        comment_self_rating
                    )
            );
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
    }
}
