using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopicPanel : MonoBehaviour
{

    [SerializeField] GameObject topicListPrefab; // Refer to ListItemPanel prefab
    [SerializeField] GameObject content;
    [SerializeField] TopicPanel topicPanel;
    [SerializeField] InfoPanel infoPanel;

    List<GameObject> topicList = new List<GameObject>();        

    // Start is called before the first frame update
    void OnEnable() 
    {
        foreach(Topics topic in System.Enum.GetValues(typeof(Topics)))
        {
            Debug.Log(topic);
        }
    }

    public void OnTopicButtonClick()
    {
        infoPanel.ShowInfoPanel();
    }

    void OnDisable() 
    {

    }
}
