using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CardQueueManager : MonoBehaviour
/*
*   QueueManager don't need to be created everytime
*   Do we on Awake() or not?    
*
*/
{
    static CardQueueManager queueManager;
    Queue<Card> cardQueue;

    FlashCard flashCard;

    string flashCardFile;

	public static CardQueueManager GetQueueManager {
		get {
			if (queueManager == null) {
				queueManager = new GameObject("QueueManager").AddComponent<CardQueueManager>();

                // Find always come with huge resources, so best not do it
                // queueManager.transform.SetParent(GameObject.Find("Manager").transform); // Register the Manager as parent 
			}

			return queueManager;
		}
	}

    public void MakeQueue(FlashCard _flashCard, int newCount, int reviewCount) {
        flashCard = _flashCard;
        flashCardFile = flashCard.fileName;
		cardQueue = new Queue<Card>();

        // Where return the IEnumerable in the order of the original list
        // For InterleaveCardLists see extra snippet from Chat GPT 4.0

        // Get all learn/relearning cards
        IEnumerable<Card> learnCards = flashCard.cards.Where(
            card => card.cardType == (int) CARD_TYPE.LEARNING || card.cardType == (int) CARD_TYPE.RELEARNING);
        
        // Find new cards with maximum is newCount
        IEnumerable<Card> newCards = flashCard.cards.Where(card => card.cardType == (int) CARD_TYPE.NEW).Take(newCount);

        // Find due cards with maximum is reviewCount  (nextReviewDate is earlier than DateTime.UtcNow)
        IEnumerable<Card> reviewCards = flashCard.cards
            .Where(card => card.cardType == (int) CARD_TYPE.REVIEW && DateTime.Parse(card.nextReviewDateStr) <= DateTime.UtcNow)
            .Take(reviewCount);
        
        // Concatenate the card lists
        List<Card> combinedCards = learnCards.Concat(newCards).Concat(reviewCards).ToList();
        
        // Sort list by order of nextReviewDate
        List<Card> sortedCards = combinedCards.OrderBy(card => DateTime.Parse(card.nextReviewDateStr)).ToList();

        foreach (Card card in combinedCards) {
            cardQueue.Enqueue(card);
        }
    }

    public void ClearQueue() {        
		cardQueue.Clear();
    }

    public void Dequeue() {        
		cardQueue.Dequeue();         
    }

    public Card Peek() {
        return cardQueue.Peek();         
    }

    public int GetCount() {
        return cardQueue.Count;
    }

    public string GetFlashCardFile() {
        return flashCardFile;
    }
}