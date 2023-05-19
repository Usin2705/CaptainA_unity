using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Networking;

public class ExercisePanel : MonoBehaviour
{
    [SerializeField] GameObject topicPanelGO; 
    [SerializeField] GameObject exerPanelGO;     
    [SerializeField] GameObject recordButtonGO;     
    [SerializeField] GameObject topTextGO;
    [SerializeField] GameObject sampleButtonGO; // Replay sample audio
    [SerializeField] GameObject sampleTextGO;
    [SerializeField] GameObject dictTextGO;
    [SerializeField] GameObject creditTextGO;
    [SerializeField] GameObject resultPanelGO; 
    [SerializeField] GameObject replayButtonGO; // Replay recorded audio
    [SerializeField] GameObject errorTextGO; 
    [SerializeField] GameObject bottomPanelGO;    
    [SerializeField] GameObject skipButtonGO;    
    [SerializeField] GameObject nextButtonGO;    

    [SerializeField] DetailScorePanel detailScorePanel;

    [SerializeField] TMPro.TextMeshProUGUI predictionDebugText;

    [SerializeField] GameObject progressBarGO;

    AudioClip sampleClip;
    AudioClip replayClip;

    private float countdownTime = 2.0f;
    private float currentTime = 2.0f;
    private string transcript;

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

    void OnEnable() 
    {
        ClearExercise();
        
        // Just in case the queue have 0 samples
        // We called the queue in the TopicPanel so it 
        // should have at least 1 sample anyway    
        int count = 0;
        try {
            count = QueueManager.GetQueueManager.GetCount();
        } catch (NullReferenceException ex) {
            Debug.LogError("No Queue was set: "+ ex.ToString());
        }

        if (count == 0)
            FinishExercise();

        topTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = QueueManager.GetQueueManager.GetTopic().ToString();
        UpdateExercise();
    }

    void UpdateExercise() 
    {
        SOword word = QueueManager.GetQueueManager.Peek();
        QueueManager.GetQueueManager.Dequeue(); // Need to dequeue to reduce the queue

        if (QueueManager.GetQueueManager.GetCount() == 0) {
            nextButtonGO.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "FINISH";
        } else {
            nextButtonGO.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "NEXT";
        }
        
        // Set up sample play button
        sampleClip = word.sampleAudio;
        if (sampleClip != null) {
            //Debug.Log("Sample audio update");
            sampleButtonGO.SetActive(true);     

            Button sampleButton = sampleButtonGO.transform.GetComponent<Button>();               
            // Need to remove old OnClick Listeners, otherwise it will keep adding up
            sampleButton.onClick.RemoveAllListeners();       
            sampleButton.onClick.AddListener(() => AudioManager.GetManager().PlayAudioClip(sampleClip));            
        } else{
            sampleButtonGO.SetActive(false);
        }

        // Set up sample text
        sampleTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = word.word;

        // Set up dictionary text        
        dictTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = word.definition;

        // Set up credit text        
        creditTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = string.IsNullOrEmpty(word.credit)?"":$"Credit: {word.credit}";

        // Only turn on the replay clip button if there's a replay sample
        replayButtonGO.SetActive(replayClip!=null);        
    }

    void ClearExercise() 
    /*
    *   This function will clear the exercise panel
    */
    {
        sampleClip = null;
        replayClip = null;
        resultPanelGO.SetActive(false);
        predictionDebugText.text = "";
    }

    public void onSkipClick()
    {
        if (QueueManager.GetQueueManager.GetCount() == 0) 
            FinishExercise();
        else
            QueueManager.GetQueueManager.Dequeue();
    }

    public void onNextClick()
    {       
        if (QueueManager.GetQueueManager.GetCount() == 0) 
            FinishExercise();
        else            
            ClearExercise();
            UpdateExercise();
    }

    void FinishExercise() 
    {
        topicPanelGO.SetActive(true);
        exerPanelGO.SetActive(false);
        QueueManager.GetQueueManager.ClearQueue();

    }

    void StartTimer()
    /*
    *   
    */
    {        
        // The length of the audio clip depend on the number of characters
        // of the text to be recorded + EXTRA_TIME
        countdownTime = transcript.Length*Const.SEC_PER_CHAR + Const.EXTRA_TIME;

        // Make sure the countdown time is not more than MAX_REC_TIME
        if (countdownTime > Const.MAX_REC_TIME) countdownTime = Const.MAX_REC_TIME;        

        // Start countdown so the user know how long the recording will be
        currentTime = countdownTime;

        // Hide the record button
        recordButtonGO.SetActive(false);

        // Show the countdown progress bar
        progressBarGO.SetActive(true);
    }

    public void OnRecordButtonClick() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
        // Hide the error text
        errorTextGO.SetActive(false);

        // Clear the prediction text
        predictionDebugText.text = "";

        // Get the text from the input field
        transcript = sampleTextGO.GetComponent<TMPro.TextMeshProUGUI>().text;

        // Santinize the text
        transcript = TextUtils.SantinizeText(transcript);

        // Check if the text is empty or not
        if (transcript=="") 
        {
            // Show the error text
            errorTextGO.SetActive(true);
            errorTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Please enter a word or phrase";
        }
        else 
        {   
            progressBarGO.SetActive(true);
            // Start recording
            AudioManager.GetManager().StartRecording();

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
            StartTimer();
        }
    }    

    public void OnFinnishTimer() {
        resultPanelGO.SetActive(false);

        // Maybe this won't cut the recording abruptly
        // by delay the microphone end by 0.5sec            
        StartCoroutine(DelayPost());
        IEnumerator DelayPost()
        {
            //yield return new WaitForSeconds(0.5f);
        
            // Send transcript to server
            // errorTextGO to update if server yield error
            // resultPanelGO to update result (by Enable the AudioClip and display text result)
            TMPro.TextMeshProUGUI resultTextTMP = resultPanelGO.transform.Find("ResultText").GetComponent<TMPro.TextMeshProUGUI>();
            GameObject warningImageGO = resultPanelGO.transform.Find("WarningImage").gameObject;

            AudioManager.GetManager().GetAudioAndPost(transcript, errorTextGO, resultTextTMP, warningImageGO, resultPanelGO, recordButtonGO, predictionDebugText);
            
            // TODO Make this part more efficiency
            // The whole block stink
            // The idea is return the audioSource.clip
            // But the clip was trimmed & convert to wav in the above code
            // so we RELOAD it back to audioclip again, which is a waste of processing
            // but at least we got some nice trimmed audioclip        
            yield return StartCoroutine(LoadAudioClip(Const.REPLAY_FILENAME));        

            Button replayButton = replayButtonGO.transform.GetComponent<Button>();     
            // To be safe, remove all old listeners were add to this component
            replayButton.onClick.RemoveAllListeners();    
            if(replayClip!=null) {
                replayButton.onClick.AddListener(()=> AudioManager.GetManager().PlayAudioClip(replayClip));            
                replayButtonGO.SetActive(true);
            } else {
                replayButtonGO.SetActive(false);
            }

            // Look for the ResultTextButton (which will pop up the DetailScorePanel onclick)
            Button resultTextButton = resultPanelGO.transform.Find("ResultText").transform.GetComponent<Button>();
            // To be safe, remove all old listeners were add to this component
            resultTextButton.onClick.RemoveAllListeners();
            // Add onclick to text result
            resultTextButton.onClick.AddListener(() => detailScorePanel.ShowDetailScorePanel(transcript, sampleClip, replayClip));
        }
        
    }

    IEnumerator LoadAudioClip(string filename) 
    /*
    *   This one should be called inside the Panel (not AudioManager)
    *   as it will update the replay audio with current replay audio
    *   Calling it inside AudioManager will lead to a few seconds
    *   of empty audioclip (somehow those wasn't trimmed in AudioManager
    *   but was trimmed from the wav file.
    *   Not very efficiency to reload but at least it work for now
    */
    {
        if(!String.IsNullOrEmpty(filename)) {
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename.EndsWith(".wav") ? filename : filename + ".wav");
            
            using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
        
                yield return uwr.SendWebRequest();
        
                if (uwr.result==UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {   
                    Debug.LogError("Failed to reload replay audio clip");
                    yield break;
                }
        
                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
        
                if (dlHandler.isDone)
                {
                    Debug.Log("Replay audio clip is loaded");
                    replayClip = dlHandler.audioClip;
                }
            }
            
            yield break;
        }
    }
}
