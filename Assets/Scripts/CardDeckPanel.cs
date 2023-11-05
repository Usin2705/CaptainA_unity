using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDeckPanel : MonoBehaviour
{

    [SerializeField] GameObject deckListPrefab; // Refer to ListCardDeck prefab
    [SerializeField] GameObject content;
    [SerializeField] GameObject cardDeckPanelGO;
    [SerializeField] GameObject superMemoPanelGO;

    List<GameObject> listScrollItems = new List<GameObject>();   

    private bool pointerDown;
    private float pointerDownTimer; 
    
    void OnEnable() 
    {
        superMemoPanelGO.SetActive(false);
        
        LoadFlashCards();  

        if (!PlayerPrefs.HasKey(Const.PREF_INS_VOCABULARY)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
            popUpPanel.OpenPanel(Const.PREF_INS_VOCABULARY);
            popUpPanel.SetText(Const.INSTRUCTION_VOCABULARY);            
        }
    }

    public void LoadFlashCards()
    {
        ClearFlashCards(); // Clear all the card deck in the panel

        // Sort the deck by the last time user use the deck                
        List<FlashCard> flashCards = new List<FlashCard>();
        
        // Create a list of card deck
        foreach(string deckName in Const.FLASH_CARD_FILES)        
        {
            SaveData.UpdateFlashCard(deckName); // Update the flashcard if needed
            FlashCard flashCard = SaveData.LoadFlashCard(deckName);            
            flashCards.Add(flashCard);            
        }         

        // Sort the list of card deck by the last time user use the deck
        // Use try catch to avoid error when the date is not in the correct format
        try {
            flashCards = flashCards.OrderByDescending(flashCard => DateTime.Parse(flashCard.useDateStr)).ToList();
        } catch (Exception e) {
            Debug.Log("Error when sorting flashcards: " + e.Message);
        }          

        // Populate sorted decks into the Panel
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
                    card => card.cardType == (int) CARD_TYPE.REVIEW && DateTime.Parse(card.nextReviewDateStr) <= DateTime.Now);
            
            // Check if the date stored in the deck is at least yesterday or not
            // If yes, reset the flashCard new and review count to max
            
            // In case there some problem with the todayDateStr
            // If we succesfully read the date
            if (DateTime.TryParse(flashCard.todayDateStr, out DateTime flashCardDate)) 
            {
                if (flashCardDate.Date < DateTime.Now.Date) 
                // If the last time user use the deck is yesterday or before
                {
                    flashCard.newCount = flashCard.maxNewCard;
                    flashCard.reviewCount = flashCard.maxReviewCard;
                    flashCard.todayDateStr = DateTime.Now.ToString("O");
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
                flashCard.todayDateStr = DateTime.Now.ToString("O");
            }

            SaveData.SaveIntoJson(flashCard, flashCard.fileName);                

            newCards = Math.Min(newCards, flashCard.maxNewCard);
            dueCards = Math.Min(dueCards, flashCard.maxReviewCard);            
            
            cardDeckGO.transform.Find("DeckPanel").
                    transform.Find("CardInfoText").GetComponent<TMPro.TextMeshProUGUI>().text = TextUtils.FormatCardTypeNumber(
                                                                                                            newCards, learnCards, dueCards);
            
            listScrollItems.Add(cardDeckGO);
            
            // Find the Button to register OnClick Function
            // No longer need this one, we use EventTrigger instead
            // Button deckButton = cardDeckGO.transform.GetComponent<Button>();        

            // Pass phoneme value to EventTrigger function
            // We need EventTrigger to detect long press and short press
            // To be safe, remove all triggers were add to this component
            EventTrigger existingTrigger  = cardDeckGO.gameObject.GetComponent<EventTrigger>();
            if(existingTrigger  != null) Destroy(existingTrigger );

            EventTrigger trigger = cardDeckGO.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { OnPointerDown((PointerEventData)data); });
            trigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { OnPointerUp((PointerEventData)data, flashCard.fileName, newCards, dueCards, learnCards); });
            trigger.triggers.Add(pointerUpEntry);            
        }
    }

    private void ClearFlashCards() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        
        foreach (GameObject go in listScrollItems) 
        {
            Destroy(go);
        }
    }

    private void Update() {
        if(pointerDown) pointerDownTimer += Time.deltaTime;
    }

    public void OnPointerDown(PointerEventData data)
    {
        pointerDown = true;
    }

    public void OnPointerUp(PointerEventData data, string fileName, int newCards, int dueCards, int learnCards)
    {
        if (pointerDownTimer >= Const.REQUIRED_HOLD_TIME) {
            // Long press
            // Add Const.ADDITIONAL_NEW_CARD new cards to the deck
            // Default is 5

            StartStudyDeck(fileName, newCards + Const.ADDITIONAL_NEW_CARD, dueCards);
            ResetTriggerPointer();
        } else {
            // Short press
            // Only StartStudyDeck if there at last 1 new card or 1 due card or 1 learn card
            if (newCards + dueCards + learnCards > 0 ) {
                StartStudyDeck(fileName, newCards, dueCards);
            }
            ResetTriggerPointer();
        }
    }

    private void ResetTriggerPointer()
    {
        pointerDown = false;
        pointerDownTimer = 0;
    }

    public void StartStudyDeck(string deckFileName, int newCards, int dueCards) 
    {
        FlashCard flashCard = SaveData.LoadFlashCard(deckFileName);
        CardQueueManager.GetQueueManager.MakeQueue(flashCard, newCards, dueCards);
        superMemoPanelGO.SetActive(true);
    }

    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        ClearFlashCards();
    }
}
