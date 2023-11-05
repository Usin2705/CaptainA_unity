using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DescribePanel : MonoBehaviour
{
    [SerializeField] GameObject recordButtonGO;
    [SerializeField] GameObject transcriptGO;
    [SerializeField] GameObject progressBarGO;
    [SerializeField] GameObject scoreButtonGO;
    [SerializeField] GameObject PromptGeneratorGO;

    [SerializeField] GPTGradingPanel gptGradingPanel;
    private float recordingTime = 45.0f;    
    private float currentTime = 45.0f;
    private bool isFinnish = true;
    
    void OnEnable()
    {
        // Hide the progress bar
        gptGradingPanel.ClosePanel();
    }

    void StartTimer()
    /*
    *   
    */
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
        if (progressBarGO.activeSelf == true) {
            UpdateProgressBar();            
        } 
    }

    void UpdateProgressBar() {
        /*
        *   This function will update the progress bar
        */
        currentTime -= Time.deltaTime;        
        progressBarGO.GetComponent<Image>().fillAmount = currentTime / recordingTime;

        if (currentTime <= 0)
        {
            
            currentTime = 0;
            progressBarGO.SetActive(false);
            OnFinnishTimer();
        }
    }
    
    public void OnGenerateButtonClicked()
    {
        RandomPromptGenerator promptGenerator = PromptGeneratorGO.GetComponent<RandomPromptGenerator>();
        string prompt = promptGenerator.GeneratePrompt();        
        Debug.Log(prompt);
        // StartCoroutine(NetworkManager.GetManager().GPTImageGenerate(prompt));
    }

    public void OnScoreButtonClicked()
    {
        // RandomPromptGenerator promptGenerator = PromptGeneratorGO.GetComponent<RandomPromptGenerator>();
        // string prompt = promptGenerator.GeneratePrompt();        
        // StartCoroutine(NetworkManager.GetManager().GPTImageGenerate(prompt));
        gptGradingPanel.ShowGPTGradingPanel();
    }

    public void OnRecordButtonClicked() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
       isFinnish = true;

        // Clear the transcript text
        transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";        

        progressBarGO.SetActive(true);
        // Start recording
        AudioManager.GetManager().StartRecording((int)recordingTime);

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
        StartTimer();
    }

    public void OnRecordButtonGlobalClicked() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
       isFinnish = false;

        // Clear the transcript text
        transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";        

        progressBarGO.SetActive(true);
        // Start recording
        AudioManager.GetManager().StartRecording((int)recordingTime);

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
        StartTimer();
    }    

    public void OnFinnishTimer() {
        StartCoroutine(DelayPost());
        
        IEnumerator DelayPost()
        {
            AudioManager.GetManager().GetAudioAndASR(transcriptGO, scoreButtonGO, isFinnish);
            yield return new WaitForSeconds(0.2f);  

            recordButtonGO.SetActive(true);
        }
    }    
}