using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ASAPanel : MonoBehaviour
{
    [SerializeField]
    GameObject ASAButtonGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject recordButtonGO;

    [SerializeField]
    GameObject transcriptGO;

    [SerializeField]
    GameObject progressBarGO;

    [SerializeField]
    GameObject reviewPanelGO;

    [SerializeField]
    GameObject pauseButtonGO;

    [SerializeField]
    GameObject sendButtonGO;

    [SerializeField]
    GameObject replayButtonGO;

    private bool isRecording = false;

    private float recordingTime = Const.MAX_REC_TIME_A;
    private float currentTime = Const.MAX_REC_TIME_A;

    void OnEnable()
    {
        recordButtonGO.GetComponent<Button>().onClick.AddListener(() => OnRecordButtonClicked());

        pauseButtonGO.GetComponent<Button>().onClick.AddListener(OnPauseButtonClicked);

        sendButtonGO.GetComponent<Button>().onClick.AddListener(() => OnSendButtonClicked());
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
        // Start countdown so the user know how long the recording will be
        currentTime = recordingTime;

        // Hide the record button
        recordButtonGO.SetActive(false);
        pauseButtonGO.SetActive(true);
        sendButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);

        // Show the countdown progress bar
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
        progressBarGO.SetActive(false);
        // Stop recording and save the audio
        AudioManager.GetManager().StopRecording();
        // Hide the pause button and show the record button
        AudioManager.GetManager().StopRecording();

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(true);
        sendButtonGO.SetActive(true);
        replayButtonGO.SetActive(true);

        StartCoroutine(
            AudioManager.GetManager().LoadAudioClip(Const.NUMBERGAME_FILENAME, replayButtonGO)
        );
    }

    public void OnTimerFinished()
    {
        isRecording = false;

        progressBarGO.SetActive(false);
        // stop recording and save the audio
        AudioManager.GetManager().StopRecording();

        pauseButtonGO.SetActive(false);
        recordButtonGO.SetActive(false);
        sendButtonGO.SetActive(true);
        replayButtonGO.SetActive(true);

        StartCoroutine(
            AudioManager.GetManager().LoadAudioClip(Const.NUMBERGAME_FILENAME, replayButtonGO)
        );
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
        reviewPanelGO.SetActive(true);
    }
}
