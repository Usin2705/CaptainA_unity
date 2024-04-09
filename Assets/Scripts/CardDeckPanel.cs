using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// This class is used to manage the Card Deck Panel
/// The Card Deck Panel is the panel that contains all the card decks
/// The card deck is the list of cards that the user want to study
/// <para>
/// Remember to update the variable FLASH_CARD_DICT in the Const.cs file when you add a new card deck
/// </para>
/// </summary>
public class CardDeckPanel : MonoBehaviour
{

    [SerializeField] GameObject deckListPrefab; // Refer to ListCardDeck prefab
    [SerializeField] GameObject content;
    [SerializeField] GameObject cardDeckPanelGO;
    [SerializeField] GameObject superMemoPanelGO;

    //Create a list of GameObject to store all the card deck in the panel
    //You need to assign this list manually in the Unity Editor
    [SerializeField] List<GameObject> flashCardsGO = new List<GameObject>();       

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

    public void LoadFlashCards() {

        // Populate the decks into the Panel
        foreach(GameObject flashCardGO in flashCardsGO) 
        {   
            // Get the name of the component
            string cardGOName = flashCardGO.name;            
            string deckName = Const.FLASH_CARD_DICT[cardGOName];
            SaveData.UpdateFlashCard(deckName); // Update the flashcard if needed
            FlashCard flashCard = SaveData.LoadFlashCard(deckName);            
            

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

            // Update the progress value. The progress value is the number of cards that the user in CARD_TYPE.REVIEW
            // round down instead of round up (int) is enough for rounding down
            int reviewCards = flashCard.cards.Count(card => card.cardType == (int) CARD_TYPE.REVIEW);
            flashCard.progress = (int) ((double) reviewCards / flashCard.cards.Count * 100);

            SaveData.SaveIntoJson(flashCard, flashCard.fileName);                

            newCards = Math.Min(newCards, flashCard.maxNewCard);
            dueCards = Math.Min(dueCards, flashCard.maxReviewCard);            

            flashCardGO.transform.Find("ProgressPanel").
                    transform.Find("ProgressBar").GetComponent<Image>().fillAmount = flashCard.progress / 100f;
            flashCardGO.transform.Find("ProgressPanel").
                    transform.Find("ProgressText").GetComponent<TMPro.TextMeshProUGUI>().text = flashCard.progress + "%";

            
            flashCardGO.transform.Find("DeckPanel").
                    transform.Find("CardInfoText").GetComponent<TMPro.TextMeshProUGUI>().text = TextUtils.FormatCardTypeNumber(
                                                                                                            newCards, learnCards, dueCards);
            
            // Pass phoneme value to EventTrigger function
            // We need EventTrigger to detect long press and short press
            // To be safe, remove all triggers were add to this component
            EventTrigger existingTrigger  = flashCardGO.gameObject.GetComponent<EventTrigger>();
            if(existingTrigger  != null) Destroy(existingTrigger );

            EventTrigger trigger = flashCardGO.gameObject.AddComponent<EventTrigger>();
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
        Debug.Log("Start study deck: " + deckFileName);
        FlashCard flashCard = SaveData.LoadFlashCard(deckFileName);
        CardQueueManager.GetQueueManager.MakeQueue(flashCard, newCards, dueCards);
        superMemoPanelGO.SetActive(true);
    }
}
