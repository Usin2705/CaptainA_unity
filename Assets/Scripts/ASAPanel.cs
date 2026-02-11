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

    private float recordingTime = Const.MAX_REC_TIME_A;
    private float currentTime = Const.MAX_REC_TIME_A;

    void OnEnable()
    {
        recordButtonGO.SetActive(true);
        recordButtonGO.GetComponent<Button>().onClick.AddListener(() => OnRecordButtonClicked());
    }

    void StartTimer()
    {
        // Start countdown so the user know how long the recording will be
        currentTime = recordingTime;

        // Hide the record button
        recordButtonGO.SetActive(false);

        // Show the countdown progress bar
        progressBarGO.SetActive(true);
    }

    void Update()
    {
        // Only run this code if the progress bar is active
        if (progressBarGO.activeSelf == true)
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
        transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";

        progressBarGO.SetActive(true);
        // Start recording
        AudioManager.GetManager().StartRecording((int)recordingTime);

        // Start the timer
        // Should not use invoke or delay as it will cause the timer to be inaccurate
        StartTimer();
    }

    public void OnTimerFinished()
    {
        {
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
}
