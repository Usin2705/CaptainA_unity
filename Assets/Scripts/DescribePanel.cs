using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class DescribePanel : MonoBehaviour
{
    [SerializeField] GameObject recordButtonGO;
    [SerializeField] GameObject transcriptGO;
    [SerializeField] GameObject progressBarGO;
    [SerializeField] GameObject scoreButtonGO;
    [SerializeField] GameObject PromptGeneratorGO;
    [SerializeField] TaskType taskType;
    [SerializeField] int taskNumber;
    [SerializeField] GPTGradingPanel gptGradingPanel;
    [SerializeField] Image imageComponent;   
    
    // either GOOD, BAD, or NEUTRAL
    public enum TaskType { A, B, C, A2, B2, C2 };    

    public void setTaskType (TaskType _taskType) {
        taskType = _taskType;
    }
    
    private float recordingTime = Const.MAX_REC_TIME_A;
    private float currentTime = Const.MAX_REC_TIME_A;
    private bool isFinnish = true;
    
    void OnEnable()
    {
        // Hide the progress bar
        gptGradingPanel.ClosePanel();

        if (taskType == TaskType.A || taskType == TaskType.A2 || taskType == TaskType.C || taskType == TaskType.C2)
        {
            transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Please describe the room and objects (colours, positions) in the picture above. You have 45 seconds to speak.";
            recordingTime = Const.MAX_REC_TIME_A;
        }
        else if (taskType == TaskType.B)
        {
            transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Situation: You met with your friends at a café yesterday evening. At home, you notice your hoodie is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the hoodie (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Maija/Matti Meikäläinen.";
            recordingTime = Const.MAX_REC_TIME_B;
        }
        else if (taskType == TaskType.B2)
        {
            transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Situation: You met with your friends at a café yesterday evening. At home, you notice your hoodie is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the hoodie (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Jane/John Doe.";
            recordingTime = Const.MAX_REC_TIME_B;
        }
        
        // This part read the image from the persistent data path and replace the image in the scene with it
        // This is to ensure that the image is the same as the one that is generated by the GPT API
        // We no longer need this as we are using the image generated by ownself and not random        
        if (taskType == TaskType.C || taskType == TaskType.C2) 
        {            
            string imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");        

            if (File.Exists(imagePath))
            
            {   
                Debug.Log("Image exists: " + imagePath);
                byte[] fileData = File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData)) // Loads the image into the Texture2D.
                {
                    // Create a sprite and assign it to the image component.
                    Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 1024, 1024), new Vector2(0.5f, 0.5f), 100.0f);
                    imageComponent.sprite = sprite;
                }
            }
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
    /*
    *   This function also attached to GenerateButton OnClick() in Unity
    *   If the task type is task type C, then the image will be generated by the DALL-E API
    *   and the task description will be selected based on the task type (A-A2, B, B2, C) and task number
    *   Otherwise, the image will be selected from the Resources folder, based on the task number
    */
    {
        RandomPromptGenerator promptGenerator = PromptGeneratorGO.GetComponent<RandomPromptGenerator>();
        string prompt = "I NEED to test how the tool works with extremely simple prompts. DO NOT add any detail, just use it AS-IS: " + promptGenerator.GeneratePrompt();        
        //Debug.Log(prompt.Replace("\"", "\\\""));
        //StartCoroutine(NetworkManager.GetManager().GPTImageGenerate(prompt));
        taskNumber += 1;
        if (taskNumber > 1) taskNumber = 0;
        if (taskNumber < 0) taskNumber = 0;

        string taskText = "Situation: You met with your friends at a café yesterday evening. At home, you notice your hoodie is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the hoodie (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Maija/Matti Meikäläinen.";
        
        switch (taskType) {
            // case for TaskType.A or A2
            
            case TaskType.A:
            case TaskType.A2:
                // Select the image to be displayed
                // The image is in the Resources folder
                imageComponent.sprite = Resources.Load<Sprite>("GenAI/" + "taskA_" + taskNumber.ToString());
                break;

            case TaskType.B:
                // Select the image to be displayed
                // The image is in the Resources folder
                imageComponent.sprite = Resources.Load<Sprite>("GenAI/" + "taskB_" + taskNumber.ToString());
                taskText = "Situation: You met with your friends at a café yesterday evening. At home, you notice your hoodie is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the hoodie (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Maija/Matti Meikäläinen.";
                if (taskNumber > 0)
                    taskText = "Situation: You met with your friends at a café yesterday evening. At home, you notice your jacket is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the jacket (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Maija/Matti Meikäläinen.";
                
                transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = taskText;
                break;

            case TaskType.B2:
                // Select the image to be displayed
                // The image is in the Resources folder
                imageComponent.sprite = Resources.Load<Sprite>("GenAI/" + "taskB_" + taskNumber.ToString());
                taskText = "Situation: You met with your friends at a café yesterday evening. At home, you notice your hoodie is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the hoodie (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Jane/John Doe.";
                if (taskNumber > 0)
                    taskText = "Situation: You met with your friends at a café yesterday evening. At home, you notice your jacket is missing. You call the café (max 30sec): Introduce yourself. Politely state your matter. Describe the jacket (see the picture above). <b>Note: Do not disclose your real name or personal matters.</b> You can use the name Jane/John Doe.";
                
                transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = taskText;
                break;

            case TaskType.C:
            case TaskType.C2:
                // Automatic generate new image
                StartCoroutine(NetworkManager.GetManager().GPTImageGenerate(prompt));
                break;            
        }
        Debug.Log("Task Type: " + taskType.ToString() + "Task Number: " + taskNumber.ToString());
    }

    public void OnScoreButtonClicked()
    {
        gptGradingPanel.ShowGPTGradingPanel();
    }

    public void OnRecordButtonClicked() 
    /*
    *   This function also attached to RecordButton OnClick() in Unity
    */
    {
       isFinnish = true;

       // If the task is A2, B2, or C2, then the task is not Finnish
       if (taskType == TaskType.A2 || taskType == TaskType.B2 || taskType == TaskType.C2) {
           isFinnish = false;
       }

       scoreButtonGO.SetActive(false);

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
            AudioManager.GetManager().GetAudioAndASR(transcriptGO, scoreButtonGO, taskType, taskNumber, isFinnish);
            yield return new WaitForSeconds(0.2f);  

            recordButtonGO.SetActive(true);
        }
    }    
}