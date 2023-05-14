using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardDeckPanel : MonoBehaviour
{

    [SerializeField] GameObject deckListPrefab; // Refer to ListCardDeck prefab
    [SerializeField] GameObject content;
    [SerializeField] GameObject cardDeckPanelGO;
    [SerializeField] GameObject superMemoPanelGO;

    List<GameObject> listScrollItems = new List<GameObject>();        
    
    void OnEnable() 
    {
        // Sort the deck by the last time user use the deck                
        List<FlashCard> flashCards = new List<FlashCard>();
        
        // Create a list of card deck
        foreach(string deckName in Const.FLASK_CARD_FILES)        
        {
            FlashCard flashCard = SaveData.LoadFlashCard(deckName);
            flashCards.Add(flashCard);            
        }         

        // Don't sort flashcard based on the last time user use the deck
        // It could be confusing for user when card move up and down too much
        //List<FlashCard> sortedFlashCards = flashCards.OrderByDescending(flashCard => DateTime.Parse(flashCard.todayDateStr)).ToList();

        // Populate sorted deck into the Panel
        foreach(FlashCard flashCard in flashCards)        
        {            
            GameObject cardDeckGO = Instantiate(deckListPrefab, new Vector3(0,0,0), Quaternion.identity);
            cardDeckGO.transform.SetParent(content.transform, false); // Register the big panel (CardDeckPanel --> Content) as parent 

            cardDeckGO.transform.Find("DeckPanel").
                    transform.Find("CardNameText").GetComponent<TMPro.TextMeshProUGUI>().text = flashCard.name;

            // Update flashcard info, including:
            // 1. Number of new cards to learn (new card)
            // 2. Number of learn cards (learn card)
            // 3. Number of due cards (review card due today)
            // 4. Check the date of the last time user use the deck
            // 5. Reset the FlaskCard new card count and review count if the last time user use the deck is yesterday
            // 6. Update the new and review cards counts (Anki does not limit learn cards)
            // 7. Update the date of the last time user use the deck

            int newCards = flashCard.cards.Count(card => card.cardType == (int) CARD_TYPE.NEW);
            int learnCards = flashCard.cards.Count(card => card.cardType == (int) CARD_TYPE.LEARNING || card.cardType == (int) CARD_TYPE.RELEARNING);
            int dueCards = flashCard.cards.Count(
                    card => card.cardType == (int) CARD_TYPE.REVIEW && DateTime.Parse(card.nextReviewDateStr) <= DateTime.UtcNow);
            
            // Check if the date stored in the deck is at least yesterday or not
            // If yes, reset the flashCard new and review count to max
            
            // In case there some problem with the todayDateStr
            // If we succesfully read the date
            if (DateTime.TryParse(flashCard.todayDateStr, out DateTime flashCardDate)) 
            {
                if (flashCardDate.Date < DateTime.UtcNow.Date) 
                // If the last time user use the deck is yesterday or before
                {
                    flashCard.newCount = flashCard.maxNewCard;
                    flashCard.reviewCount = flashCard.maxReviewCard;
                    flashCard.todayDateStr = DateTime.UtcNow.ToString();
                } else {
                    // If not, update the new and review cards counts
                    // The new and review cards counts are the minimum of the possible and
                    // the current new and review cards counts
                    newCards = Math.Min(newCards, flashCard.newCount);
                    dueCards = Math.Min(dueCards, flashCard.reviewCount);
                }
                
            } else {
                // If failed to read the date, reset the flashCard new and review count to max
                flashCard.newCount = flashCard.maxNewCard;
                flashCard.reviewCount = flashCard.maxReviewCard;
                flashCard.todayDateStr = DateTime.UtcNow.ToString();
            }

            SaveData.SaveIntoJson(flashCard, flashCard.fileName);                

            newCards = Math.Min(newCards, flashCard.maxNewCard);
            dueCards = Math.Min(dueCards, flashCard.maxReviewCard);            
            
            cardDeckGO.transform.Find("DeckPanel").
                    transform.Find("CardInfoText").GetComponent<TMPro.TextMeshProUGUI>().text = TextUtils.FormatCardTypeNumber(
                                                                                                            newCards, learnCards, dueCards);
            
            listScrollItems.Add(cardDeckGO);
            
            // Find the Button to register OnClick Function
            Button deckButton = cardDeckGO.transform.GetComponent<Button>();                        
            // Pass phoneme value to onClick function
            // To be safe, remove all old listeners were add to this component
            deckButton.onClick.RemoveAllListeners();    

            // Only add the onClick function if there at last 1 new card or 1 due card or 1 learn card
            if (newCards + dueCards + learnCards > 0 ) {
                deckButton.onClick.AddListener(() => StartStudyDeck(flashCard.fileName, newCards, dueCards));
            }
            
        }
    }

    public void StartStudyDeck(string deckFileName, int newCards, int dueCards) 
    {
        FlashCard flashCard = SaveData.LoadFlashCard(deckFileName);
        CardQueueManager.GetQueueManager.MakeQueue(flashCard, newCards, dueCards);        
        cardDeckPanelGO.SetActive(false);
        superMemoPanelGO.SetActive(true);
    }

    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        foreach (GameObject go in listScrollItems) 
        {
            Destroy(go);
        }
    }
}
