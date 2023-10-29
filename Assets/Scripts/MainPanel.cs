using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System;

public class MainPanel : MonoBehaviour
{       
    [SerializeField] GameObject recordButtonGO;     
    [SerializeField] GameObject inputTransGO;
    [SerializeField] GameObject errorTextGO;     
    [SerializeField] GameObject resultPanelGO;     
    [SerializeField] GameObject replayButtonGO;     
    [SerializeField] GameObject sampleButtonGO; // Replay sample audio
    [SerializeField] GameObject illustrationGO; // Illustration

    [SerializeField] DetailScorePanel detailScorePanel;

    [SerializeField] TMPro.TextMeshProUGUI predictionDebugText;

    [SerializeField] GameObject progressBarGO;

    AudioClip replayClip;
    AudioClip sampleClip;

    private float countdownTime = 2.0f;
    private float currentTime = 2.0f;
    private string transcript;

    void Start()
    {
        // Toggle fullscreen
        Screen.fullScreen = false;
        sampleButtonGO.SetActive(false);
        illustrationGO.SetActive(false);
    }

    void OnEnable() {
        if (!PlayerPrefs.HasKey(Const.PREF_INS_MAIN)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();        
            popUpPanel.OpenPanel(Const.PREF_INS_MAIN);
            popUpPanel.SetText(Const.INSTRUCTION_MAIN);
        }        
    }

    public void OnEditTextFininshed(){
        string finnishText = inputTransGO.GetComponent<TMP_InputField>().text;
        finnishText = TextUtils.SantinizeText(finnishText).ToLower();
        ShowSampleAudio(finnishText);

        // Find the illustration and display it
        Sprite newSprite = Resources.Load<Sprite>(Const.ILLUSTRATIONS_PATH + finnishText);
        if (newSprite) {
            illustrationGO.GetComponent<Image>().sprite = newSprite;
            illustrationGO.SetActive(true);            
        } else {
            // If the illustration is not found, disable the illustration GO
            illustrationGO.SetActive(false);
        }
    }

    public void ShowSampleAudio(string word) {
        GetSampleClip(word);

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
    }
    public void GetSampleClip(string word)     
    {
        // Audio file name is the santinized word
        // with lower case
        string audioFileName = TextUtils.SantinizeText(word).ToLower();
        
        // Load sample clip from the natural folder
        sampleClip = Resources.Load<AudioClip>(Const.AUDIO_NATURAL_PATH + audioFileName);
                
        // If the sample clip is not found in the natural folder
        // try to find it in the AI folder
        if (sampleClip == null) {
            sampleClip = Resources.Load<AudioClip>(Const.AUDIO_AI_PATH + audioFileName);            
        }        
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
        // Hide the error text
        errorTextGO.SetActive(false);
        
        // Clear the prediction text
        predictionDebugText.text = "";

        // Get the text from the input field
        transcript = inputTransGO.GetComponent<TMP_InputField>().text;

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
            AudioManager.GetManager().StartRecording(Const.MAX_REC_TIME);

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
            StartTimer();
        }
    }

    // TODO
    // This code is 95% the same with ExercisePanel
    // maybe consider to merge both code into 1
    public void OnFinnishTimer() {
        // NOTE that in MainPanel the text is get from TMP_InputField, not TMPro.TextMeshProUGUI
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
            // so we RELOAD it back to clip again, which is a waste of processing
            // but at least we got some nice trimmed audioclip        
            yield return StartCoroutine(LoadAudioClip(Const.REPLAY_FILENAME));        

            Button replayButton = replayButtonGO.transform.GetComponent<Button>();                       
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
            resultTextButton.onClick.AddListener(() => detailScorePanel.ShowDetailScorePanel(transcript, null, replayClip));
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
            
            // Need the file:// for GetAudioClip
            // TODO check with iOS version does it need sth similar
            using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
        
                yield return uwr.SendWebRequest();
        
                if (uwr.result==UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {   
                    Debug.LogError("Failed to reload replay audio clip");
                    Debug.LogError(uwr.result);
                    Debug.LogError(path);
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
