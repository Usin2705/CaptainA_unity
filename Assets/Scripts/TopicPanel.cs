using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TopicPanel : MonoBehaviour
{

    [SerializeField] GameObject topicListPrefab; // Refer to ListItemPanel prefab
    [SerializeField] GameObject content;
    [SerializeField] GameObject topicPanelGO;
    [SerializeField] GameObject exerPanelGO;

    List<GameObject> topicList = new List<GameObject>();        
    

    // Start is called before the first frame update
    void OnEnable() 
    {
        foreach(Topics topic in System.Enum.GetValues(typeof(Topics)))        
        {            
            GameObject topicGO = Instantiate(topicListPrefab, new Vector3(0,0,0), Quaternion.identity);
            topicGO.transform.SetParent(content.transform, false); // Register the big panel (TopicPanel --> Content) as parent 

            // Find the Button to register OnClick Function
            Button topicButton = topicGO.transform.Find("TopicPanel").transform.Find("TopicBG").GetComponent<Button>();                        
            // Pass phoneme value to onClick function
            topicButton.onClick.AddListener(() => StartExerciseWithTopic(topic));

            topicGO.transform.Find("TopicPanel").
                           transform.Find("TopicBG").
                           transform.Find("Topic").GetComponent<TMPro.TextMeshProUGUI>().text = topic.ToString();
            
            topicGO.transform.Find("TopicPanel").
                           transform.Find("NoSamples").GetComponent<TMPro.TextMeshProUGUI>().text = TopicDetail.GetDetail(topic);
            
            topicList.Add(topicGO);
        }
    }

    public void StartExerciseWithTopic(Topics topic) 
    {
        QueueManager.GetQueueManager.MakeQueue(topic, Const.MAX_QUEUE);
        topicPanelGO.SetActive(false);
        exerPanelGO.SetActive(true);
    }

    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        foreach (GameObject go in topicList) 
        {
            Destroy(go);
        }
    }
}
