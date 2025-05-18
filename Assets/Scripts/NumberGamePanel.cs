using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using UnityEngine.Diagnostics;
using System;
using UnityEngine.Networking;

public class NumberGamePanel : MonoBehaviour
{
    [SerializeField] GameObject recordButtonGO;
    [SerializeField] GameObject progressBarGO;    
    [SerializeField] NGTaskType taskType;

    [SerializeField] Toggle easyToggle;
    [SerializeField] Toggle mediumToggle;
    [SerializeField] Toggle hardToggle;
    [SerializeField] Toggle rankToggle;

    [SerializeField] GameObject newButtonGO;

    [SerializeField] GameObject numberLabelGO;

    [SerializeField] GameObject errorTextGO;

    [SerializeField] GameObject replayButtonGO;

    AudioClip replayClip;

    private float recordingTime = Const.MAX_REC_NUMBERGAME_EASY;
    private float currentTime = Const.MAX_REC_NUMBERGAME_EASY;

    private string number = "999";

    
    private void Start()
    {
        // Add listeners
        easyToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetTaskType(NGTaskType.EASY); });
        mediumToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetTaskType(NGTaskType.MEDIUM); });
        hardToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetTaskType(NGTaskType.HARD); });
        rankToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetTaskType(NGTaskType.RANK); });
        
        Debug.Log("NumberGamePanel Start()");
        easyToggle.isOn = true;
        
        // Add listener to record button
        recordButtonGO.GetComponent<Button>().onClick.AddListener(OnRecordButtonClicked);

        // Add listener to new button
        newButtonGO.GetComponent<Button>().onClick.AddListener(OnNewButtonClick);
    }



    void OnEnable()
    {
        Debug.Log("NumberGamePanel OnEnable()");

        // Hide the progress bar
        if (taskType == NGTaskType.EASY)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_EASY;
        }
        else if (taskType == NGTaskType.MEDIUM)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_MEDIUM;
        }
        else if (taskType == NGTaskType.HARD)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_HARD;
        }
        else if (taskType == NGTaskType.RANK)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_RANK;
        }        
    }

    void OnDisable()
    {
        // Reset the toogles to the default state
        easyToggle.isOn = true;
    }

    public void SetTaskType (NGTaskType _taskType) {
        // Disable the replay button
        replayButtonGO.SetActive(false);   

        taskType = _taskType;
        
        // Generate a new number
        int taskNumber = NumberGenerator.GenerateNumber(taskType);
        // Debug.Log("Task Number: " + taskNumber);

        // Get the text TMP component from the GameObject
        // The TMP is a child of the NumberLabelGO
        numberLabelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = taskNumber.ToString();   

        // Set the countdown time based on the task type
        if (taskType == NGTaskType.EASY)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_EASY;
        }
        else if (taskType == NGTaskType.MEDIUM)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_MEDIUM;
        }
        else if (taskType == NGTaskType.HARD)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_HARD;
        }
        else if (taskType == NGTaskType.RANK)
        {
            recordingTime = Const.MAX_REC_NUMBERGAME_RANK;
        }     
    }  

    private void OnNewButtonClick()
    {
        // Disable the replay button
        replayButtonGO.SetActive(false);   

        // Retrigger the current toggle to generate a new number
        if (easyToggle.isOn)
        {
            easyToggle.isOn = false;
            easyToggle.isOn = true;
        }
        else if (mediumToggle.isOn)
        {
            mediumToggle.isOn = false;
            mediumToggle.isOn = true;
        }
        else if (hardToggle.isOn)
        {
            hardToggle.isOn = false;
            hardToggle.isOn = true;
        }
        else if (rankToggle.isOn)
        {
            rankToggle.isOn = false;
            rankToggle.isOn = true;
        }
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
        if (progressBarGO.activeSelf == true)
        {
            UpdateProgressBar();
        }

        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Disable this panel
            gameObject.SetActive(false);
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

    public void OnRecordButtonClicked() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
        progressBarGO.SetActive(true);
        // Start recording
        AudioManager.GetManager().StartRecording((int)recordingTime);

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
        StartTimer();
    }    

    public void OnFinnishTimer() {
        TMPro.TextMeshProUGUI resultTextTMP = numberLabelGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        number = TextUtils.RemoveColorTag(resultTextTMP.text);

        StartCoroutine(DelayPost());
        
        IEnumerator DelayPost()
        {
            Debug.Log("number: " + number);
            AudioManager.GetManager().GetAudioAndNG(number, errorTextGO, resultTextTMP);
            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(LoadAudioClip(Const.NUMBERGAME_FILENAME));        
            recordButtonGO.SetActive(true);

            Button replayButton = replayButtonGO.transform.GetComponent<Button>();                       
            replayButton.onClick.RemoveAllListeners();    
            if(replayClip!=null) {
                replayButton.onClick.AddListener(()=> AudioManager.GetManager().PlayAudioClip(replayClip));            
                replayButtonGO.SetActive(true);
            } else {
                replayButtonGO.SetActive(false);
            }

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