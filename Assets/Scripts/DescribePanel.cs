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
    private float countdownTime = 30.0f;    
    private float currentTime = 30.0f;
    public void OnGenerateButtonClick() 
    {
        
    }

    void StartTimer()
    /*
    *   
    */
    {        
        // Start countdown so the user know how long the recording will be
        currentTime = countdownTime;

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
        progressBarGO.GetComponent<Image>().fillAmount = currentTime / countdownTime;

        if (currentTime <= 0)
        {
            
            currentTime = 0;
            progressBarGO.SetActive(false);
            OnFinnishTimer();
        }
    }

        public void OnRecordButtonClick() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
       
        // Clear the transcript text
        transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";        

        progressBarGO.SetActive(true);
        // Start recording
        AudioManager.GetManager().StartRecording(30);

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
        StartTimer();
    }

    public void OnFinnishTimer() {
        StartCoroutine(DelayPost());
        
        IEnumerator DelayPost()
        {
            AudioManager.GetManager().GetAudioAndASR(transcriptGO, scoreButtonGO);
            yield return new WaitForSeconds(0.5f);;        
        }
    }    
}