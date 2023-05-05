using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SuperMemoPanel : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI frontCardText; // Front card
    [SerializeField] TMPro.TextMeshProUGUI backCardText; // Back card

    private CardManagerSM2 cardManager;
    private Card currentCard;

    // Start is called before the first frame update
    void Start()
    {
        cardManager = new CardManagerSM2();
        ShowNextCard();
    }

    
    public void ShowNextCard()
    {
        currentCard = cardManager.GetNextCard();
        if (currentCard != null)
        {
            frontCardText.text = currentCard.frontText;
            backCardText.text = currentCard.backText;
        }
        else
        {
            frontCardText.text = "No cards to review.";
            backCardText.text = "";
        }
    }

    public void OnSubmitButtonClick(int quality)
    /*
    *   Quality is a number from 0 to 5, where 0 means complete blackout and 5 means perfect recall.
    *   https://www.supermemo.com/english/ol/sm2.htm
    *   We are using Anki algorithm, so quality is a number in [0,3,4,5] where 0 means complete blackout and 5 means perfect recall.
    *   https://apps.ankiweb.net/docs/manual.html#what-spaced-repetition-algorithm-does-anki-use
    *   I'm not sure Anki algorithm use 0 to 3 or 0 to 5, but I think it's 0 to 3. 
    *   I used [0,3,4,5] in CaptainA because the formular is based on SuperMemo 2 algorithm.
    *
    *   The quality is from the OnClick event of the button in the SuperMemoPanel.
    */
    {
        cardManager.UpdateCardReviewDate(currentCard, quality);
        ShowNextCard();
    }
}
