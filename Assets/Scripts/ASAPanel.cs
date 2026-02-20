using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ASAPanel : MonoBehaviour
{
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
    GameObject replayBarBackgroundGO;

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

    private bool isRecording = false;
    private bool isReplaying = false;

    private float recordingTime = Const.MAX_REC_TIME_A;
    private float currentTime = Const.MAX_REC_TIME_A;

    private AudioClip recording;

    void OnEnable()
    {
        recordButtonGO.GetComponent<Button>().onClick.AddListener(() => OnRecordButtonClicked());
        pauseButtonGO.GetComponent<Button>().onClick.AddListener(OnPauseButtonClicked);
        sendButtonGO.GetComponent<Button>().onClick.AddListener(() => OnSendButtonClicked());
        replayButtonGO.GetComponent<Button>().onClick.AddListener(() => OnReplayButtonClicked());
        replayBarBackgroundGO.SetActive(false);
        replayBarGO.SetActive(false);
        progressBarBackgroundGO.SetActive(false);
        progressBarGO.SetActive(false);
    }

    void Start()
    {
        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);
    }

    void StartTimer()
    {
        // Start countdown so the user knows how long the recording will be
        currentTime = recordingTime;

        // Hide the record button
        recordButtonGO.SetActive(false);
        pauseButtonGO.SetActive(true);
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);

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
    }

    void UpdateProgressBar()
    {
        /*
        *   This function will update the progress bar
        */
        currentTime -= Time.deltaTime;
        progressBarGO.GetComponent<Image>().fillAmount = currentTime / recordingTime;

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
            replayBarBackgroundGO.SetActive(false);
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
        AudioManager.GetManager().StartRecording((int)recordingTime);

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
        replayBarBackgroundGO.SetActive(true);
        replayBarGO.SetActive(true);
        isReplaying = true;
    }

    public void OnSendButtonClicked()
    {
        // send the audio to the server when clicked
        AudioManager
            .GetManager()
            .GetAudioAndPost(
                POSTType.OTHER, // POST type = OTHER
                transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text, // transcript text
                null, // textErrorGO
                null, // resultTextGO
                null, // resultPanelGO
                null // debugTextGO
            );

        ASAPanelGO.SetActive(false);
        feedbackPanelGO.SetActive(true);
    }
}
