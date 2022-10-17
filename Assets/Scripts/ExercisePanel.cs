using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ExercisePanel : MonoBehaviour
{
    [SerializeField] GameObject topicPanelGO; 
    [SerializeField] GameObject exerPanelGO;     
    [SerializeField] GameObject buttonRecordGO;     
    [SerializeField] GameObject topTextGO;
    [SerializeField] GameObject sampleButtonGO;
    [SerializeField] GameObject sampleTextGO;
    [SerializeField] GameObject dictTextGO;
    [SerializeField] GameObject replayButtonGO;
    [SerializeField] GameObject resultTextGO;   
    [SerializeField] GameObject errorTextGO; 
    [SerializeField] GameObject bottomPanelGO;    
    [SerializeField] GameObject skipButtonGO;    
    [SerializeField] GameObject nextButtonGO;    
    
    void OnEnable() 
    {
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
        
        // Set up sample play button
        if (word.sampleAudio != null) {
            sampleButtonGO.SetActive(true);
            Button sampleButton = sampleButtonGO.transform.GetComponent<Button>();       
            sampleButton.onClick.AddListener(() => AudioManager.GetManager().PlayAudioClip(word.sampleAudio));            
        } else{
            sampleButtonGO.SetActive(false);
        }

        // Set up sample text
        sampleTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = word.word;


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
            QueueManager.GetQueueManager.Dequeue();
            UpdateExercise();
    }

    void FinishExercise() 
    {
        topicPanelGO.SetActive(true);
        exerPanelGO.SetActive(false);
        QueueManager.GetQueueManager.ClearQueue();

    }

}
