using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueueManager : MonoBehaviour
/*
*   QueueManager don't need to be created everytime
*   Do we on Awake() or not?    
*
*/
{
    static QueueManager queueManager;
    Queue<SOword> wordQueue;

    Topics currentTopic = 0;

	public static QueueManager GetQueueManager {
		get {
			if (queueManager == null) {
				queueManager = new GameObject("QueueManager").AddComponent<QueueManager>();

                // Find always come with huge resources, so best not do it
                // queueManager.transform.SetParent(GameObject.Find("Manager").transform); // Register the Manager as parent 
			}

			return queueManager;
		}
	}

    public void MakeQueue(Topics topic, int maxLength) {
        currentTopic = topic;
		wordQueue = new Queue<SOword>();
        
        // Load all SOword into a list for easier to control
        // Resources LoadAll return array so best to keep it as array
        SOword[] wordArray = Resources.LoadAll<SOword>(Const.WORD_LIST_PATH); 
        List<SOword> selectedWordList = new List<SOword>();      
        
        // It's better to loop through all words array to build a selected list
        // then randomize that list for X sample
        // it's O(n) with no extra at the end
        // Otherwise we need to build the all words array and shuffle that array O(n),
        // then do the loop again to select the first X samples that match,
        // Which might require extra calculation

        foreach (SOword word in wordArray) 
        {   
            if (topic!=Topics.All) {
                foreach (Topics t in word.topicList) 
                {
                    if (t==topic) 
                    {
                        selectedWordList.Add(word);
                        break;
                    }
                }
            } else {
                selectedWordList.Add(word);
            }
        }
        
        // Randomize select from word list until we get it all
        Debug.Log("QueueManager: all possible words: " + selectedWordList.Count);
        for (int count = 0; count < Const.MAX_QUEUE; count++) {            
            int index = Random.Range(0, selectedWordList.Count);
            wordQueue.Enqueue(selectedWordList[index]);
            selectedWordList.RemoveAt(index);
        }
    }

    public void ClearQueue() {        
		wordQueue.Clear();
    }

    public void Dequeue() {        
		wordQueue.Dequeue();         
    }

    public SOword Peek() {
        return wordQueue.Peek();         
    }

    public int GetCount() {
        return wordQueue.Count;
    }

    public Topics GetTopic() {
        return currentTopic;
    }
}
