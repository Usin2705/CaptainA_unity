using System.Collections;
using System.Numerics;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ASAPanel : MonoBehaviour
{
    NetworkManager networkManager;

    [SerializeField]
    AudioManager audioManager;

    [SerializeField]
    GameObject ASAButtonGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject recordButtonGO;

    [SerializeField]
    GameObject transcriptGO;

    [SerializeField]
    GameObject replayBarGO;

    [SerializeField]
    GameObject progressBarBackgroundGO;

    [SerializeField]
    GameObject progressBarGO;

    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject pauseButtonGO;

    [SerializeField]
    GameObject sendButtonGO;

    [SerializeField]
    GameObject replayButtonGO;

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

    public ToggleGroup ratingOptions;

    [SerializeField]
    AdvancePanel AdvancePanel;

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
    private bool isLoading = false;

    private float currentTime = 0;

    public int currentTaskSelected = -1;

    private AudioClip recording;

    public void DisplayTask(int taskSelected)
    {
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

        recordButtonGO.GetComponent<Button>().onClick.AddListener(() => OnRecordButtonClicked());
        pauseButtonGO.GetComponent<Button>().onClick.AddListener(() => OnPauseButtonClicked());
        sendButtonGO.GetComponent<Button>().onClick.AddListener(() => OnSendButtonClicked());
        replayButtonGO.GetComponent<Button>().onClick.AddListener(() => OnReplayButtonClicked());
        resultsButtonGO.GetComponent<Button>().onClick.AddListener(() => OnResultsButtonClicked());

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

        // Hide the record button
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
        // Only run this code if the progress bar is active
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
            animateLoading();
        }
    }

    void UpdateProgressBar()
    {
        /*
        *   This function will update the progress bar
        */
        currentTime -= Time.deltaTime;
        progressBarGO.GetComponent<Image>().fillAmount =
            currentTime / tasks[currentTaskSelected].recordingTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            progressBarGO.SetActive(false);
            OnTimerFinished();
        }
    }

    void UpdateReplayBar()
    {
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
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
        // Clear the transcript text
        //transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";

        // Start recording
        AudioManager.GetManager().StartRecording((int)tasks[currentTaskSelected].recordingTime);

        // Start the timer
        // Should not use invoke or delay as it will cause the timer to be inaccurate
        StartTimer();
    }

    public void OnPauseButtonClicked()
    {
        isRecording = false;
        // Stop the progress bar
        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);
        // Stop recording and save the audio
        AudioManager.GetManager().StopRecording();
        // Hide the pause button and show the record button

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(true);
        replayButtonGO.SetActive(true);

        StartCoroutine(AudioManager.GetManager().LoadAudioClip(Const.ASA_FILENAME, replayButtonGO));
    }

    public void OnTimerFinished()
    {
        isRecording = false;

        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);
        // stop recording and save the audio
        AudioManager.GetManager().StopRecording();

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(true);
        replayButtonGO.SetActive(true);

        StartCoroutine(AudioManager.GetManager().LoadAudioClip(Const.ASA_FILENAME, replayButtonGO));
    }

    public void OnReplayButtonClicked()
    {
        recording = audioManager.GetReplayClip();
        currentTime = recording.length; // to counter the small lag you can manually add "+ (float)0.4" or so at the end
        replayBarGO.SetActive(true);
        isReplaying = true;
    }

    public void OnSendButtonClicked()
    {
        // send the audio to the server when clicked
        AudioManager
            .GetManager()
            .GetAudioAndPost_ASA(
                POSTType.ASA_TASK, // POST type = OTHER
                null // transcript text
            );

        loadingPopUpGO.SetActive(true);
        ratingOptions.SetAllTogglesOff();
        AdvancePanel.self_rating = null;
        feedbackTextGO.text = "";
        resultsButtonGO.SetActive(false);
        errorTextGO.SetActive(false);
        loadingIconGO.SetActive(true);
        isLoading = true;
        dimPanelASAGO.SetActive(true);
    }

    public void animateLoading()
    {
        loadingIconGO.transform.Rotate(0, 0, -6.0f, Space.Self);
    }

    public void OnResultsButtonClicked()
    {
        loadingPopUpGO.SetActive(false);

        dimPanelASAGO.SetActive(false);
        feedbackPanelGO.SetActive(true);
        if (AdvancePanel.self_rating != null)
        {
            Debug.Log("Test");
            StartCoroutine(
                NetworkManager
                    .GetManager()
                    .ServerPost_feedback(POSTType.ASA_FEEDBACK, "self_assessment")
            );
        }
        else
        {
            return;
        }
    }
}
