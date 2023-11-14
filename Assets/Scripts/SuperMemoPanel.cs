using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class SuperMemoPanel : MonoBehaviour
// Default state of the Panel is disabled

{
    [SerializeField] TMPro.TextMeshProUGUI frontCardText; // Front card
    [SerializeField] TMPro.TextMeshProUGUI backCardText; // Back card    

    [SerializeField] TMPro.TextMeshProUGUI etymoText; // Etymology card
    
    [SerializeField] GameObject warningImageFrontGO; // Front card
    [SerializeField] GameObject warningImageBackGO; // Back card    

    [SerializeField] TMPro.TextMeshProUGUI intervalText_again; // Interval text for AGAIN button
    [SerializeField] TMPro.TextMeshProUGUI intervalText_hard; // Interval text for HARD button
    [SerializeField] TMPro.TextMeshProUGUI intervalText_good; // Interval text for GOOD button
    [SerializeField] TMPro.TextMeshProUGUI intervalText_easy; // Interval text for EASY button
    [SerializeField] GameObject showAnswerGO; // Show answer GO
    [SerializeField] GameObject qualityBarGO; // Quality bar GO
    [SerializeField] GameObject recordButtonGO;     
    [SerializeField] GameObject progressBarGO; // Progress bar
    [SerializeField] GameObject errorTextGO;     
    [SerializeField] GameObject sampleButtonGO; // Replay sample audio
    [SerializeField] GameObject replayButtonGO; // Replay recorded audio
    [SerializeField] Toggle autoPlayToggle ; // Auto play audio
    [SerializeField] Toggle hideFinnishToggle; // Hide Finnish text
    [SerializeField] GameObject illustrationGO; // Illustration

    [SerializeField] GameObject superMemoPanel;     
    [SerializeField] GameObject cardDeckPanel;     
    [SerializeField] TMPro.TextMeshProUGUI predictionDebugText;
    [SerializeField] DetailScorePanel detailScorePanel;

    [SerializeField] GameObject surveyPopUpPanelGO;   

    TMPro.TextMeshProUGUI finnishCardText; // The text of the card that is in Finnish
    GameObject finWarningImageGO; // The warningImage of the card that is in Finnish
    private CardManagerSM2 cardManager;
    private Card currentCard;
    private float countdownTime = 2.0f;
    private float currentTime = 2.0f;
    private string transcript;
    AudioClip replayClip;
    AudioClip sampleClip;

    void OnEnable() {      
        // Reset the status of the toggle buttons
        // This must be done because the hideFinnishToggle button is disabled
        // when the front card is not in Finnish
        autoPlayToggle.gameObject.SetActive(true);
        hideFinnishToggle.gameObject.SetActive(true);  

        clearOldCard();
        cardManager = new CardManagerSM2(CardQueueManager.GetQueueManager.GetFlashCardFile());
        autoPlayToggle.isOn = cardManager.GetFlashCardAutoPlay();
        hideFinnishToggle.isOn = cardManager.GetFlashCardHideText();

        ShowNextCard();           
    }

    void OnDisable() {
        // Clear the answer from previous card
        clearOldCard();
        // Clear the cardqueue
        CardQueueManager.GetQueueManager.ClearQueue();      
    }

    public void FinnishFlashCard() {
        // Clear the answer from previous card
        clearOldCard();
        // Clear the cardqueue
        CardQueueManager.GetQueueManager.ClearQueue();      
        
        // toggle the panel to force update the flashcard info        
        CardDeckPanel cardDeckPanel = GameObject.FindAnyObjectByType<CardDeckPanel>();
        cardDeckPanel.LoadFlashCards();
        superMemoPanel.SetActive(false);        
    }
        
    
    public void ShowNextCard()
    {
        // First clear all the onClick events
        frontCardText.GetComponent<Button>().onClick.RemoveAllListeners();
        backCardText.GetComponent<Button>().onClick.RemoveAllListeners();

        if (CardQueueManager.GetQueueManager.GetCount() != 0)
        {
            currentCard = CardQueueManager.GetQueueManager.Peek();

            frontCardText.text = currentCard.frontText;
            // If the front language is Finnish, use the front card text for CAPT
            // Otherwise use the back card text for CAPT
            finnishCardText = currentCard.frontLanguage == "FI"? frontCardText : backCardText;
            finWarningImageGO = currentCard.frontLanguage == "FI"? warningImageFrontGO : warningImageBackGO;
            showAnswerGO.SetActive(true);

            // If the front card is in Finnish, enable the SampleButton to play audio (if any)
            if (currentCard.frontLanguage == "FI") ShowSampleAudio(currentCard.frontText);
            
            // If the front card is not in Finnish, disable the hideFinnishToggle button
            if (currentCard.frontLanguage != "FI") hideFinnishToggle.gameObject.SetActive(false);
            

            if (cardManager.GetFlashCardHideText() & currentCard.frontLanguage == "FI") {
            // Only hide the text if the front card is in Finnish
            // Hide the text by setting the alpha to 0
            // instead of remove the text, because we will need the transcript for recording
            // remember to set the alpha back to 1 when show answer AND clear old card
                Color32 currentColor = frontCardText.faceColor;
                currentColor.a = 0;                            
                frontCardText.faceColor = currentColor;
            }           
            
            // If the card have Auto Play turn on, and front card is Finnish, 
            // auto play the sample audio clip
            // sampleClip is pre-loaded in ShowSampleAudio()
            if (cardManager.GetFlashCardAutoPlay() & currentCard.frontLanguage == "FI") 
            {
                if (sampleClip != null) AudioManager.GetManager().PlayAudioClip(sampleClip);
            }

        }
        else
        {
            // frontCardText.text = "No cards to review.";
            // backCardText.text = "";
            // showAnswerGO.SetActive(false);
            // recordButtonGO.SetActive(false);
            // qualityBarGO.SetActive(false);
            FinnishFlashCard();
        }

    }

    public void displayAnswer()
    /*
    *   This function also attached to ShowAnswer OnClick() in Unity
    */
    {
        if (currentCard.frontLanguage == "FI") {
            Color32 currentColor = frontCardText.faceColor;
            currentColor.a = 255;
            frontCardText.faceColor = currentColor;
        }

        backCardText.text = currentCard.backText;
        // Hide the show answer button
        showAnswerGO.SetActive(false);

        // update the expected interval for 4 quality buttons
        (float newInterval, float newEaseFactor) = cardManager.GetCarNewIntervalEase(currentCard, 0);
        intervalText_again.text = GetIntervalText(newInterval);

        (newInterval, newEaseFactor) = cardManager.GetCarNewIntervalEase(currentCard, 3);
        intervalText_hard.text = GetIntervalText(newInterval);

        (newInterval, newEaseFactor) = cardManager.GetCarNewIntervalEase(currentCard, 4);
        intervalText_good.text = GetIntervalText(newInterval);

        (newInterval, newEaseFactor) = cardManager.GetCarNewIntervalEase(currentCard, 5);
        intervalText_easy.text = GetIntervalText(newInterval);        
        
        // Show the quality bar
        qualityBarGO.SetActive(true);
        
        string finnishText = currentCard.frontLanguage == "FI"? currentCard.frontText : currentCard.backText;
        // Santinize the text
        // Must santiize the text before try to find it in the resources folder
        finnishText = TextUtils.SantinizeText(finnishText).ToLower();

        // If the back card is in Finnish, show the sample audio (if any)
        if (currentCard.backLanguage == "FI") ShowSampleAudio(finnishText);

        // if Auto Play is on, and the backcard is in Finnish, play the sample audio
        if (cardManager.GetFlashCardAutoPlay() & currentCard.backLanguage == "FI") 
        {
            if (sampleClip != null) AudioManager.GetManager().PlayAudioClip(sampleClip);
        }

        // If there is etymology text, display it:
        if (!string.IsNullOrEmpty(currentCard.etymology))
        {
           etymoText.text = TextUtils.FormatEtymology(currentCard.etymology);
        }
        
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

    public string GetIntervalText(float interval) 
    /*
    *   This function will return the interval text
    *   based on the interval value
    *   The interval value is in day
    *   If the interval is more than 1 day, return in day
    *   If the interval is less than 1 day, return in hour
    *   If the interval is less than 1.2 hour, return in minute
    */
    {
        if (interval >= 1.0f) {
            return interval.ToString("0") + " d";
        } else if (interval < 1.0f && interval >= 0.05f) {
            return (interval*24).ToString("0") + " h";
        } else {
            return (interval*24*60).ToString("0") + " m";
        }
        
    }

    /// <summary>
    /// Clears all attributes associated with the previously displayed card.
    /// </summary>
    /// <remarks>
    /// This method ensures that any residual data from a previously displayed card is cleared, ensuring a fresh state 
    /// before a new card is shown. The clearing process involves:
    /// <list type="bullet">
    /// <item><description>Resetting the card's back text to its default state.</description></item>
    /// <item><description>Deactivating warning images associated with the front and back of the card.</description></item>
    /// <item><description>Resetting the prediction debug text.</description></item>
    /// <item><description>Nullifying any sample and recorded audio clips.</description></item>
    /// <item><description>Set the sample and replay buttons to inactive.</description></item>
    /// <item><description>Restoring the black text color of the front card text.</description></item>
    /// <item><description>Hiding any previous card illustrations.</description></item>
    /// </list>
    /// It's crucial to call this method before initializing or displaying a new card to prevent any data overlap or visual inconsistencies.
    /// </remarks>    
    public void clearOldCard()
    {
        // Clear the answer from previous card
        etymoText.text = "";

        backCardText.text = "";
        warningImageFrontGO.SetActive(false);
        warningImageBackGO.SetActive(false);

        // Clear the prediction debug text
        predictionDebugText.text = "";

        sampleClip = null;
        replayClip = null;

        // Only turn on the button if there's a replay samples
        sampleButtonGO.SetActive(sampleClip!=null);     
        replayButtonGO.SetActive(replayClip!=null);
        
        Color32 currentColor = frontCardText.faceColor;
        currentColor.a = 255;
        frontCardText.faceColor = currentColor;     

        // Hide the old illustration
        illustrationGO.SetActive(false);
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

    public void OnSubmitButtonClick(int quality)
    /*
    *   Quality is a number from 0 to 5, where 0 means complete blackout and 5 means perfect recall.
    *   https://www.supermemo.com/english/ol/sm2.htm
    *   We are using Anki algorithm, so quality is a number in [0,3,4,5] where 0 means complete blackout and 5 means perfect recall.
    *   https://apps.ankiweb.net/docs/manual.html#what-spaced-repetition-algorithm-does-anki-use
    *   I'm not sure Anki algorithm use 0 to 3 or 0 to 5, but I think it's 0 to 3. 
    *   I used [0,3,4,5] in CaptainA because the formular is based on SuperMemo 2 algorithm.
    *
    *   The quality is from the OnClick event of the button in the SuperMemoPanel.
    */
    {        
        (float newInterval, float newEaseFactor) = cardManager.GetCarNewIntervalEase(currentCard, quality);     
        cardManager.UpdateCardToJson(currentCard, quality, newInterval, newEaseFactor);        
        
        CardQueueManager.GetQueueManager.Dequeue(); // Need to dequeue to reduce the queue
        clearOldCard();
        ShowNextCard();

        checkSurVey();         
    }
    
	public void checkSurVey() {
		int recordNumber = 1;
		
		// If this is not the first record, get the record number
		if (PlayerPrefs.HasKey(Const.PREF_FLASHCARD_NUMBER)) {
			recordNumber = PlayerPrefs.GetInt(Const.PREF_FLASHCARD_NUMBER) + 1;
		}
		//Debug.Log("Record number: " + recordNumber);
		PlayerPrefs.SetInt(Const.PREF_FLASHCARD_NUMBER, recordNumber);
		PlayerPrefs.Save();

		if (recordNumber % Const.SURVEY_TRIGGER_FLASH_CARD == 0) {
			// Only show survey if user has not has not done survey v1
			// No longer have option to refuse survey
			if (!PlayerPrefs.HasKey(Const.PREF_SURVEY_V1_DONE))  {
				//Debug.Log("Show survey");
				surveyPopUpPanelGO.SetActive(true);
			}
		}
	}

    public void OnRecordButtonClick() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
        transcript = currentCard.frontLanguage == "FI"? currentCard.frontText : currentCard.backText;

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

    public void OnAutoPlayToogle(bool isAutoPlay) 
    /*
    *   This function also attached to AutoPlay OnValueChanged in Unity
    */    
    {
        cardManager.SetFlashCardAutoPlay(isAutoPlay);
    }    

    public void OnHideFinnishToogle(bool isHideText) 
    /*
    *   This function also attached to AutoPlay OnValueChanged in Unity
    */    
    {
        cardManager.SetFlashCardHideText(isHideText);
    }

    void StartTimer()
    /*
    *   This function will start the timer for the recording
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

    // TODO
    // maybe consider to merge both code into 1
    public void OnFinnishTimer() {

        // Maybe this won't cut the recording abruptly
        // by delay the microphone end by 0.5sec            
        StartCoroutine(DelayPost());
        IEnumerator DelayPost()
        {
            //yield return new WaitForSeconds(0.5f);
        
            // Send transcript to server
            // errorTextGO to update if server yield error
            // resultPanelGO to update result (by Enable the AudioClip and display text result)
            predictionDebugText.text = "";
            AudioManager.GetManager().GetAudioAndPost(transcript, errorTextGO, finnishCardText, finWarningImageGO, null, predictionDebugText);

            // TODO Make this part more efficiency
            // The whole block stink
            // The idea is return the audioSource.clip
            // But the clip was trimmed & convert to wav in the above code
            // so we RELOAD it back to clip again, which is a waste of processing
            // but at least we got some nice trimmed audioclip        
            yield return StartCoroutine(LoadAudioClip(Const.REPLAY_FILENAME));        
            
            recordButtonGO.SetActive(true);

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
            Button resultTextButton = finnishCardText.transform.GetComponent<Button>();
            // To be safe, remove all old listeners were add to this component
            resultTextButton.onClick.RemoveAllListeners();
            // Add onclick to text result
            resultTextButton.onClick.AddListener(() => detailScorePanel.ShowDetailScorePanel(transcript, sampleClip, replayClip));
        }

        displayAnswer();
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
