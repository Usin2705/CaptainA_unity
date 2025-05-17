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
    [SerializeField] GameObject sampleButtonGO; // Play sample audio
    [SerializeField] GameObject detailButtonGO; // Display detail feedback

    
    [SerializeField] GameObject illustrationGO; // Illustration

    [SerializeField] DetailScorePanel detailScorePanel;

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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set the start UI to be active
        SetUpStartUI();

    }

    void OnEnable() {

        // Check if the instruction for MainPanel is already shown        
        if (!PlayerPrefs.HasKey(Const.PREF_INS_MAIN))
        {
            PopUpManager popUpPanel = FindAnyObjectByType<PopUpManager>();
            popUpPanel.OpenPanel(Const.PREF_INS_MAIN);
            popUpPanel.SetText(Const.INSTRUCTION_MAIN);
        }        
    }    

    // Update is called once per frame
    void Update()
    {
        // Only run this code if the stopButtonGO  is active        
        if (stopButtonPanelGO.activeSelf == true) {
            UpdateProgressCircle();            
        }        

        // Rotate the wait icon if it is active every frame        
        // only less half 1 degree per 2 frames
        if (waitIconGO.activeSelf == true) {
            if (Time.frameCount % 2 == 0) {
            waitIconGO.transform.Rotate(0, 0, -0.5f);
            }
        }
    }

    /// <summary>
    /// Updates the progress circle based on the remaining countdown time.
    /// </summary>
    /// <remarks>
    /// This method calculates the current progress of a countdown timer and updates the source image
    /// of the stop button to reflect the progress. The progress is divided into four stages, each represented
    /// by a different image. When the progress reaches zero, the timer is stopped and the <c>StopTimer</c>
    /// method is called.
    /// 
    /// Since this is run within the Update method, the progress circle is updated every frame.
    /// The varoable <c>currentTime</c> is a global variable and need to be set on <c>StartTimer</c> method.
    /// </remarks>
    private void UpdateProgressCircle()
    {
        // Calculate the current time
        currentTime -= Time.deltaTime;

        // Calculate the current progress
        float currentProgress = currentTime / countdownTime;

        // Change the StopButton source image to show the progress circle
        
        // If the progress is less than or equal to 0, stop the timer and call the StopTimer method
        // Remember to reset the currentTime to 0 and set the stopButtonGO source image to default
        // Most important, disable the stopButtonGO (it will disable the progress circle call in Update())
        if (currentProgress <= 0) 
        {
            StopTimer();
        }
        else if (currentProgress <= 0.05)
        {
            stopButtonPanelGO.GetComponent<Image>().sprite = Resources.Load<Sprite>("app_icons/ic_timer_4");                                    
        } else if (currentProgress < 0.25)
        {
            stopButtonPanelGO.GetComponent<Image>().sprite = Resources.Load<Sprite>("app_icons/ic_timer_3");                        
        } else if (currentProgress < 0.5)
        {
            stopButtonPanelGO.GetComponent<Image>().sprite = Resources.Load<Sprite>("app_icons/ic_timer_2");            
        } else if (currentProgress < 0.75)
        {
            stopButtonPanelGO.GetComponent<Image>().sprite = Resources.Load<Sprite>("app_icons/ic_timer_1");
        } 
    }

    /// <summary>
    /// Initializes and starts the recording timer based on the length of the transcript.
    /// </summary>
    /// <remarks>
    /// The length of the audio clip depends on the number of characters in the text to be recorded plus an extra time.
    /// The countdown time is capped at a maximum recording time.
    /// The method hides the recordButton, shows the countdown progress circle and the stopButton, and registers the OnClick event for the stop button.
    /// </remarks>
    private void StartTimer() 
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

        // Show the countdown progress circle and the stop button
        stopButtonPanelGO.SetActive(true);
        // Register the OnClick event for the stop button
        stopButtonGO.GetComponent<Button>().onClick.AddListener(delegate { OnStopRecordButtonClick(); });
    }       

    private void OnStopRecordButtonClick()
    {
        StopTimer();
    }    

    /// <summary>
    /// Stops the recording timer and resets the UI elements associated with the timer.
    /// </summary>
    /// <remarks>
    /// This method stops the countdown timer by setting the <c>currentTime</c> to 0 and disabling the <c>stopButtonGO</c>.
    /// It also removes all listeners from the stop button and resets its source image to the default.
    /// After stopping the timer, it calls the <c>StopRecordingAndProcessAudio</c> coroutine to handle the recording and processing of the audio.
    /// </remarks>
    private void StopTimer()
    {
        // Stop the timer
        currentTime = 0;
        
        ResetStopButtonAnimation();

        // Once timer finish, call the external method to stop the recording
        // and wait for the audio to be processed (coroutine) and then enable the replay button
        StartCoroutine(StopRecordingAndProcessAudio());
    }

    private void OnInputTextFocus(TMPro.TMP_InputField inputField)
    {
        // Debug.Log(inputField.name + " is focused.");
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

    private void OnInputTextUnfocus(TMPro.TMP_InputField inputField)
    {
        Debug.Log(inputField.name + " is unfocused.");
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
            // Debug.Log(inputText.name + " text finished: " + inputText.text);
            OnStartButtonClickDoneText();
        } else {
            inputText.Select();
            // Debug.Log(inputText.name + " text finished but there is nothing: " + inputText.text);
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


    private void OnRecordButtonClick() 
    {
        Debug.Log("Record button clicked.");
        
        // Get the text from the ReadAloudText and store it in the transcript variable
        // Technically, this should be done in the OnStartButtonClickDoneText method, but it is done here 
        // to ensure that the text is captured before the recording starts and to be consistent with the
        // original code.
        // Use GetParsedText(): This returns the plain text content with all rich text formatting removed.
        // Since we are format result with richtext, this is the best method to use. (in case user record after the result is shown)
        transcript = textInputPanelGO.transform.Find("ReadAloudText").GetComponent<TMPro.TextMeshProUGUI>().GetParsedText();
        // Debug.Log("Transcript: " + transcript);

        // Santinize the text
        // We already did this in the OnStartButtonClickDoneText method, but it is done here
        // to ensure that the text is properly captured before the recording starts.        
        transcript = TextUtils.SantinizeText(transcript).ToLower();
        Debug.Log("Sanitized Transcript: " + transcript);

        // Start recording
        AudioManager.GetManager().StartRecording(Const.MAX_REC_TIME);

        StartTimer();
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
        detailButtonGO.SetActive(false);

        // Disable the illustration
        illustrationGO.SetActive(false);

        // Disable the sample button
        sampleButtonGO.SetActive(false);

        ResetStopButtonAnimation();

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
        detailButtonGO.SetActive(false);

        // Disable the illustration
        illustrationGO.SetActive(false);

        // Disable the sample button
        sampleButtonGO.SetActive(false);

        ResetStopButtonAnimation();

        ResetWaitIcon();
    }

    private void ResetStopButtonAnimation()
    {
        // Disable the stop button
        stopButtonPanelGO.SetActive(false);
        // Remove all listeners from the stop button
        stopButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
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

    private IEnumerator StopRecordingAndProcessAudio()
    {
        GameObject textErrorGO = textInputPanelGO.transform.Find("PromptText").gameObject;
        GameObject resultTextGO = textInputPanelGO.transform.Find("ReadAloudText").gameObject;            
        
        // Toggle the loading 
        waitIconGO.SetActive(true);

        // Turn off the replay button
        // It will be a mess if the user click the replay button while the server is still processing
        againButtonGO.SetActive(false);    

        AudioManager.GetManager().GetAudioAndPost(POSTType.MDD_TASK, transcript, textErrorGO, resultTextGO, null, null, OnServerDone);

        replayButtonGO.transform.GetComponent<Button>().onClick.RemoveAllListeners();
        yield return AudioManager.GetManager().LoadAudioClip(Const.REPLAY_FILENAME, replayButtonGO);
    }

    private void OnServerDone()
    {
        ResetWaitIcon();
        recordButtonGO.SetActive(true);
        againButtonGO.SetActive(true);

        // Enable the detail button
        // TODO: add a check to see if the detail results are available        
        AudioClip replayClip = AudioManager.GetManager().GetReplayClip();
        if (replayClip != null) {
            detailButtonGO.SetActive(true);
        // To be safe, remove all old listeners were add to this component
        detailButtonGO.GetComponent<Button>().onClick.RemoveAllListeners();
        // Add onclick to text result
        detailButtonGO.GetComponent<Button>().onClick.AddListener(
            () => detailScorePanel.ShowDetailScorePanel(transcript, null, AudioManager.GetManager().GetReplayClip()));
        }
    }

}
