using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using System;

public class MainPanel : MonoBehaviour
{
    [SerializeField] GameObject topPanelGO;
    [SerializeField] GameObject dropShadowLGO;
    [SerializeField] GameObject dropShadowBGO;
    [SerializeField] GameObject dropShadowRGO;
    [SerializeField] GameObject textInputPanelGO;
    [SerializeField] GameObject debugTextGO;


    [SerializeField] GameObject startButtonGO;
    [SerializeField] GameObject againButtonGO;
    [SerializeField] TMPro.TMP_InputField inputText;


    [SerializeField] GameObject stopButtonPanelGO;
    [SerializeField] GameObject stopButtonGO;
    [SerializeField] GameObject waitIconGO;


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


    /// <summary>
    /// Awake is called when the script instance is being loaded. This is used to initialize any variables or game state 
    /// before the game starts. Awake is called only once during the lifetime of the script instance.
    /// 
    /// In this method, we set up listeners for the input field's onSelect and onDeselect events. These listeners will 
    /// trigger the OnInputTextFocus and OnInputTextUnfocus methods respectively when the input field gains or loses focus.
    /// 
    /// Awake is called before any Start methods, making it a good place to set up references and initialize variables 
    /// that other scripts might depend on during their Start method.
    /// </summary>
    private void Awake() 
    {
        SetUpStartUI();
        inputText.onSelect.AddListener(delegate { OnInputTextFocus(inputText); });
        // inputText.onDeselect.AddListener(delegate { OnInputTextUnfocus(inputText); });
        inputText.onValueChanged.AddListener(delegate { OnInputTextChange(inputText); });
        
        // This is not working, as it trigger even when the text is unfocused
        // inputText.onEndEdit.AddListener(delegate { OnInputTextFinish(inputText); });
        startButtonGO.GetComponent<Button>().onClick.AddListener(delegate { OnStartButtonClick(); });
        recordButtonGO.GetComponent<Button>().onClick.AddListener(delegate { OnRecordButtonClick(); });
        againButtonGO.GetComponent<Button>().onClick.AddListener(delegate { OnAgainButtonClick(); });
    }







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
        transcript = inputTransGO.GetComponent<TMPro.TMP_InputField>().text;

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

            AudioManager.GetManager().GetAudioAndPost(transcript, errorTextGO, resultTextTMP, warningImageGO, resultPanelGO, predictionDebugText);

            // TODO Make this part more efficiency
            // The whole block stink
            // The idea is return the audioSource.clip
            // But the clip was trimmed & convert to wav in the above code
            // so we RELOAD it back to clip again, which is a waste of processing
            // but at least we got some nice trimmed audioclip        
            yield return StartCoroutine(LoadAudioClip(Const.REPLAY_FILENAME));        

            recordButtonGO.SetActive(true);

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






    private void OnInputTextFocus(TMPro.TMP_InputField inputField)
    {
        Debug.Log(inputField.name + " is focused.");
        SetupFocusTextUI();
    }    

    /// <summary>
    /// Handles the event when the text in the input field changes.
    /// </summary>
    /// <param name="inputField">The input field whose text has changed.</param>
    /// <remarks>
    /// This method lenables or disables the Start button based on whether the input field contains any text.
    /// If the input field's text length is greater than 0, the Start button is made interactable.
    /// Otherwise, the Start button is made non-interactable.
    /// </remarks>
    private void OnInputTextChange(TMPro.TMP_InputField inputField)
    {
        // If the input field contains any text, enable the Start button
        if (inputField.text.Length > 0)
        {
            startButtonGO.GetComponent<Button>().interactable = true;
        }        
        else
        {
            startButtonGO.GetComponent<Button>().interactable = false;
        }

        // If the input field is longer than max char, do not allow to enter more
        if (inputField.text.Length > Const.MAX_CHAR_ALLOWED)
        {
            inputField.text = inputField.text.Substring(0, Const.MAX_CHAR_ALLOWED);
            // Debug.Log(inputField.name + " restricted to : " + inputField.text);
        }
    }

    /// <summary>
    /// Handles two type of event depend on the scenario: 
    /// 1. The user focus on the EditText when the EditText is empty -> change to EditText focus layout
    /// 2. The input field's text is finished and the SayIt button is clicked. -> change to new layout
    /// </summary>
    private void OnStartButtonClick()
    {
        Debug.Log("Start button clicked to focus on EditText.");
        if (inputText.text.Length > 0)
        {
            Debug.Log(inputText.name + " text finished: " + inputText.text);
            OnStartButtonClickDoneText();
        } else {
            inputText.Select();
            Debug.Log(inputText.name + " text finished but there is nothing: " + inputText.text);
        }
    }

    private void OnStartButtonClickDoneText()
    {
        Debug.Log("Start button clicked to finish EditText.");
        SetUpReadAloudUI();
    }    

    private void OnAgainButtonClick()
    {
        Debug.Log("Again button clicked.");
        SetupFocusTextUI();

        // Refocus on the input text
        inputText.Select();
    }


    public void SetUpStartUI()
    {
        // Set up the default image for the top panel. This image is a sprite that is loaded from the Resources folder.
        topPanelGO.GetComponent<Image>().sprite = Resources.Load<Sprite>("app_icons/top_bar_BIG");

        // Move the top panel to the top of the screen
        topPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        // Set the default size for the top panel
        topPanelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 560);

        // Change the text prompt of PromptText
        textInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Did you find a new Finnish word or sentence?";

        // Clean the debug text
        debugTextGO.SetActive(false);
        debugTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";

        textInputPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -904);

        dropShadowLGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-384, -904);
        dropShadowBGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1188);
        dropShadowRGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(384, -904);
        
        // Enable the startButtonGO
        startButtonGO.SetActive(true);
        // Enable the start button button, not GO        
        startButtonGO.GetComponent<Button>().interactable = true;
        
        // Disable the ReadAloudTextGO
        textInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(false);

        // Disable the recording button
        recordButtonGO.SetActive(false);
        againButtonGO.SetActive(false);
        replayButtonGO.SetActive(false);

        ResetStopButton();

        ResetWaitIcon();
    }    

    public void SetupFocusTextUI()
    {
        // Set up the smaller image for the top panel. This image is a sprite that is loaded from the Resources folder.
        topPanelGO.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("app_icons/top_bar_SMALL");

        // Move the top panel to the top of the screen
        topPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        // Set the default size for the top panel
        topPanelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 250);

        // Change the text prompt of PromptText
        textInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Type in Finnish, and \nlet's practice!";

        // Clean the debug text, if any
        debugTextGO.SetActive(false);
        debugTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";

        textInputPanelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -692);

        dropShadowLGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-384, -692);
        dropShadowBGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -976);
        dropShadowRGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(384, -692);

        // Enable the inputText
        inputText.gameObject.SetActive(true);

        // Disable the text prompt of PromptText
        textInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(false);

        // Disable the start button button, not GO
        // Disable the button will change its color to gray and make it unclickable (set in the Editor)

        // Only disable the start button if there is no text in the input field
        if (inputText.text.Length == 0)
            startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = false;
        else {
            startButtonGO.GetComponent<UnityEngine.UI.Button>().interactable = true;
        }

        // Enable the startButtonGO
        startButtonGO.SetActive(true);

        // Disable the recording button        
        recordButtonGO.SetActive(false);
        againButtonGO.SetActive(false);

        // Disable the illustration
        illustrationGO.SetActive(false);

        // Disable the sample button
        sampleButtonGO.SetActive(false);

        ResetStopButton();

        ResetWaitIcon();
    }

    private void ResetStopButton()
    {
        // Disable the stop button
        stopButtonPanelGO.SetActive(false);
        // Remove all listeners from the stop button
        stopButtonGO.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
        // Reset the stopButtonGO source image to the default
        stopButtonPanelGO.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>("app_icons/ic_timer_0");
    }

    private void ResetWaitIcon()
    {
        // Disable the wait icon
        waitIconGO.SetActive(false);
        // Reset the wait icon rotation
        // Quaternion.identity is the default rotation (0, 0, 0)
        waitIconGO.transform.rotation = Quaternion.identity; 
    }    


    public void SetUpReadAloudUI()
    {
        // Change the text prompt of PromptText
        textInputPanelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Tap the record \nbutton and speak.";
        
        // Disable the inputText
        inputText.gameObject.SetActive(false);

        // Santinize the text
        string finnishText = inputText.text;
        finnishText = TextUtils.SantinizeText(finnishText).ToLower();

        textInputPanelGO.transform.Find("ReadAloudText").gameObject.SetActive(true);
        textInputPanelGO.transform.Find("ReadAloudText").GetComponent<TMPro.TextMeshProUGUI>().text = finnishText;
        
        // Set ReadAloudText to normal following design guideline
        // ReadAloudText is the text that will be read aloud
        // It will be set to bold when the result is shown
		textInputPanelGO.transform.Find("ReadAloudText").GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Normal;

        // Try to find the illustration and display it
        ShowSampleIllustration(finnishText);
        
        // Try to find if there is audio sample for the text
        ShowSampleAudio(finnishText);
        
        // Clean the input text
        inputText.text = "";

        // Clean the debug text, if any
        debugTextGO.SetActive(false);
        debugTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = "";

        // Disable the startButtonGO
        startButtonGO.SetActive(false);

        // Enable the recording button
        recordButtonGO.SetActive(true);
        againButtonGO.SetActive(true);
    }


    public void ShowSampleIllustration(string word){
        // Find the illustration and display it
        Sprite newSprite = Resources.Load<Sprite>(Const.ILLUSTRATIONS_PATH + word);
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

}
