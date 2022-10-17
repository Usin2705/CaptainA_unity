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

	public static QueueManager GetQueueManager {
		get {
			if (queueManager == null) {
				queueManager = new GameObject("QueueManager").AddComponent<QueueManager>();
			}

			return queueManager;
		}
	}

    public void MakeQueue(Topics topic, int maxLength) {
		wordQueue = new Queue<SOword>();
        
        //Load all SOword into a list for easier to control
        SOword[] wordList = Resources.LoadAll<SOword>(Const.WORD_LIST_PATH);

        int count = 0;
        foreach (SOword word in wordList) 
        {
            foreach (Topics t in word.topicList) 
            {
                if (t==topic) 
                {
                    count++;
                    wordQueue.Enqueue(word);
                    break;
                }
            }

            if (count>=maxLength)  {break;}
        }

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
}
