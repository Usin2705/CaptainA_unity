using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExercisePanel : MonoBehaviour
{
    [SerializeField] GameObject topicPanelGO; 
    [SerializeField] GameObject exerPanelGO;     
    [SerializeField] GameObject buttonRecordGO;     
    [SerializeField] GameObject sampleButtonGO;
    [SerializeField] GameObject sampleTextGO;
    [SerializeField] GameObject dictTextGO;
    [SerializeField] GameObject replayButtonGO;
    [SerializeField] GameObject resultTextGO;   
    [SerializeField] GameObject errorTextGO; 
    [SerializeField] GameObject bottomPanelGO;    
    [SerializeField] GameObject skipButtonGO;    
    [SerializeField] GameObject nextButtonGO;    

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
    }

    void FinishExercise() 
    {
        topicPanelGO.SetActive(true);
        exerPanelGO.SetActive(false);
    }

}
