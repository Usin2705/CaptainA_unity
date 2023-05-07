/*
*   Part of the algorithm is based on the SuperMemo 2 algorithm.
*   The algorithm is described here:
*   https://www.supermemo.com/en/blog/application-of-a-computer-to-improve-the-results-obtained-in-working-with-the-supermemo-method 
*
*   Part of the algorithm is also based on Anki algorithm.
*   The algorithm is described here: https://faqs.ankiweb.net/what-spaced-repetition-algorithm.html#why-doesnt-anki-use-supermemos-latest-algorithm
*   and here: https://github.com/ankitects/anki/blob/main/pylib/anki/scheduler/v3.py
*   and here: https://github.com/ankitects/anki/blob/main/pylib/anki/scheduler/v2.py
*   with License: GNU Affero General Public License v3 or later
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class CardManagerSM2
{

    public FlashCard flashCard;

    public CardManagerSM2()
    {
        // Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.persistentDataPath, "flashcards_omasuomi_1_Finnish1.json");

		if(File.Exists(filePath)) {
            // Since we can't save in Resources folder, we save the edited flashcard file in the persistentDataPath
            // If filePath is exit, load the json file from the persistentDataPath            

            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);    
            // Pass the json to JsonUtility, and tell it to create a UserData object from it            
			flashCard = JsonUtility.FromJson<FlashCard>(dataAsJson);

        } else {
            // If the filePath is not exit, load the json file from Resources folder        
            // Load the json file from Resources folder
            // When load the json file from Resources folder as TextAsset, the file extension should be removed
            TextAsset jsonFile = Resources.Load<TextAsset>("flashcards_omasuomi_1_Finnish1");
            Debug.Log("File loaded from " + jsonFile.text);
            flashCard = JsonUtility.FromJson<FlashCard>(jsonFile.text);
        }

        foreach (Card card in flashCard.cards)
        {
            // If the cardType is not set, set it to NEW
            // Default value for int is 0, so if the cardType is not set, it will be 0
            //if (card.cardType == null) card.cardType = (int)CARD_TYPE.NEW;
            Debug.Log(card.ToString());                        
        }
    }

    public Card GetNextCard()
    {
        try {
            Card nextCard = flashCard.cards.Where(card => DateTime.Parse(card.nextReviewDateStr) <= DateTime.UtcNow)
                .OrderBy(card => DateTime.Parse(card.nextReviewDateStr))
                .FirstOrDefault();
            return nextCard;
        } catch (ArgumentNullException e) {
            Debug.LogError($"Caught ArgumentNullException: {e.Message}");
            return null;
        }        
    }
    
    public float GetCarNewInterval(Card card, int quality) 
    {       
        /*
        *   This calculate the interval for the next review date
        *   based on the quality of the button (Again, Hard, Good, Easy)
        *   The interval is in minutes or days depend on the interval time
        *   The interval time is calculated based on Anki algorithm
        *   When the card enter REVIEW, the interval time is calculated based on SM2 algorithm
        *   We need to calculate the interval in advance to show the next review date to the user
        */        

        // When you select an ease button on a review card, Anki also applies a small amount of random “fuzz” 
        // to prevent cards that were introduced at the same time and given the same ratings from sticking together 
        // and always coming up for review on the same day.                
        float fuzz_factor = UnityEngine.Random.Range(0.0f/24f/60f, 5f/24f/60f); // 0 to 5 minutes
        
        // ########################## NEW CARD ##########################
        // The schedule for new card is 1minute, 5.5 minutes, 10m (next step) and 4 d (REVIEW)
        // https://docs.ankiweb.net/deck-options.html?#learning-steps
        // https://docs.ankiweb.net/studying.html#learningrelearning-cards
        if (card.cardType == (int)CARD_TYPE.NEW) 
        {
            if (quality < Const.CARD_HARD) 
            {
                card.interval = 1f/24f/60f; // 1 minutes
            } else if (quality == Const.CARD_HARD) 
            {
                card.interval = 5.5f/24f/60f; // average of AGAIN and GOOD
            } else if (quality == Const.CARD_GOOD) 
            {            
                card.interval = 10f/24f/60f; // 10m
            } else if (quality == Const.CARD_EASY) 
            {                
                card.interval = 4.0f; // 4 day
            }
        
        } else if (card.cardType == (int) CARD_TYPE.LEARNING) {
        // ########################## LEARNING CARD ##########################
        // The schedule for LEARNING card is 1 minute (first step), repeat, 10 m (next step) and 4 d (REVIEW)        
            if (quality < Const.CARD_AGAIN) 
            {
                card.interval = 1f/24f/60f; // 1 minutes
            } else if (quality == Const.CARD_AGAIN) 
            {
                // Repeat the current delay
                card.interval = card.interval + fuzz_factor/20f;
            } else if (quality == Const.CARD_GOOD) 
            {            
               card.interval = 10f/24f/60f + fuzz_factor/20f; // 10m
            } else if (quality == Const.CARD_EASY) 
            {                
                card.interval = 4.0f + fuzz_factor; // 4 day
            }
        
        } else if (card.cardType == (int) CARD_TYPE.RELEARNING) {
        // ########################## RELEARNING CARD ##########################
        // The schedule for LEARNING card is 1 minute (first step), repeat, 1 d (next step) and 4 d (REVIEW)        
            if (quality < Const.CARD_AGAIN) 
            {
                card.interval = 1f/24f/60f; // 1 minutes
            } else if (quality == Const.CARD_AGAIN) 
            {
                // Repeat the current delay
                card.interval = card.interval + fuzz_factor/20f;
            } else if (quality == Const.CARD_GOOD) 
            {            
                card.interval = 1.0f + fuzz_factor; // 1 day
            } else if (quality == Const.CARD_EASY) 
            {                
                card.interval = 4.0f + fuzz_factor; // 4 day
            }
    
        } else if (card.cardType == (int) CARD_TYPE.REVIEW) {
        // ########################## REVIEW CARD ##########################
        // The schedule for REVIEW card is - 0.2f, (move back) easeFactor, repeat (-0.15f), 0 , and 1.3* Ease + 0.15f
            if (quality < Const.CARD_AGAIN) {
                // The card is placed into relearning mode, the ease is decreased by 20 percentage points 
                // (that is, 20 is subtracted from the ease value, which is in units of percentage points), 
                // and the current interval is multiplied by the value of new interval 
                // (this interval will be used when the card exits relearning mode).

                // When you forget a review card, it is said to have 'lapsed', and the card must be relearnt. 
                // The default behaviour for lapsed reviews is to reset the interval to 1 (i.e. make it due tomorrow), 
                // and put it in the learning queue for a refresher in 10 minutes. 
                // This behaviour can be customized with the options listed below.
                float newEaseFactor = card.easeFactor - 0.2f;
                card.easeFactor = Mathf.Max(newEaseFactor, 1.3f);
                card.interval = 10f/24f/60f; // 10 minutes
            } 
            
            else if (quality == Const.CARD_HARD) {
                // The multiplier used when you use the Hard button. 
                // The percentage is relative to the previous interval: e.g. with a default of 1.20, 
                // a card with a 10-day interval will be given 12 days.
                float hard_multiplier = 1.2f;

                // The card’s ease is decreased by 15 percentage points and the current interval 
                // is multiplied by the value of hard interval (1.2 by default)
                float newEaseFactor = card.easeFactor - 0.15f;
                card.easeFactor = Mathf.Max(newEaseFactor, 1.3f);
                
                // The current interval is multiplied by the value of hard interval (1.2 by default)
                card.interval = card.interval * hard_multiplier + fuzz_factor; 

            } else if (quality == Const.CARD_GOOD) 
            {                            
                //The current interval is multiplied by the current ease. The ease is unchanged.                
                float newEaseFactor = card.easeFactor;
                card.easeFactor = Mathf.Max(newEaseFactor, 1.3f);    
                card.interval = card.interval * card.easeFactor + fuzz_factor;                 
                
            } else if (quality == Const.CARD_EASY) 
            {
                // An extra multiplier applied to the interval when a review card is answered Easy. 
                // With the default value of 1.30, Easy will give an interval that is 1.3 times the Good interval 
                // (e.g. if the Good interval was 10 days, the Easy interval would be around 13 days).
                float easy_multiplier = 1.3f; 

                // The current interval is multiplied by the current ease times the easy bonus 
                // and the ease is increased by 15 percentage points.
                float newEaseFactor = card.easeFactor + 0.15f;       

                card.easeFactor = Mathf.Max(newEaseFactor, 1.3f);
                card.interval = card.interval * card.easeFactor * easy_multiplier + fuzz_factor;                                                
            }
        }
        
        //  Intervals will never be increased beyond the value of maximum interval (36500 by default).
        card.interval = Mathf.Min(card.interval, 36500f);

        Debug.Log("Card interval: " + card.interval);
        return card.interval;
    }

    public void UpdateCardToJson(Card card, int quality, float interval) {
        // ########################## NEW CARD ##########################
        // If the card is new and get HARD or GOOD move the card to LEARNING
        // If the card is new and get EASY move the card to REVIEW
        // https://docs.ankiweb.net/deck-options.html?#learning-steps
        // https://docs.ankiweb.net/studying.html#learningrelearning-cards

        if (card.cardType == (int)CARD_TYPE.NEW) {
            if ((quality == Const.CARD_HARD) || (quality == Const.CARD_GOOD)) {
                card.cardType = (int)CARD_TYPE.LEARNING;
            }             
            
            else if (quality ==  Const.CARD_EASY ) card.cardType = (int)CARD_TYPE.REVIEW;
        }

        else if (card.cardType == (int)CARD_TYPE.LEARNING)         
        {
            // ########################## LEARNING CARD ##########################
            // AGAIN: Moves the card back to the first step setted in Learning/Relearning Steps (DO NOTHING)
            // When you press Good on a card after the 10 minute step, it will be delayed until the next day.
            // (become relearning)
            // Easy always move to REVIEW

            if (quality == Const.CARD_GOOD) {
                card.cardType = (int)CARD_TYPE.RELEARNING;
            } else if (quality == Const.CARD_EASY) {
                card.cardType = (int)CARD_TYPE.REVIEW;
            }
        } 
        
        else if ((card.cardType == (int)CARD_TYPE.RELEARNING)) {
            // ########################## RELEARNING CARD ##########################
            // AGAIN: Moves the card back to the first step setted in Learning/Relearning Steps (LEARNING)
            // When you press Good on the card the next day, it will leave learning 
            // (i.e. it will graduate), and become a review card
            // Easy always move to REVIEW

            if (quality == Const.CARD_AGAIN) {
                card.cardType = (int)CARD_TYPE.LEARNING;
            } 

            else if ((quality == Const.CARD_GOOD) || (quality == Const.CARD_EASY)) {
                card.cardType = (int)CARD_TYPE.REVIEW;
            }
        } 
        
        else if ((card.cardType == (int)CARD_TYPE.REVIEW)) {
            // ########################## REVIEW CARD ##########################
            // AGAIN: Moves the card back to the first step setted in Learning/Relearning Steps (LEARNING)
            // When you press Good on the card the next day, it will leave learning 
            // (i.e. it will graduate), and become a review card
            if (quality == Const.CARD_AGAIN) {
                card.cardType = (int)CARD_TYPE.LEARNING;
            }
        }        
        card.nextReviewDateStr = DateTime.UtcNow.AddDays(interval).ToString("O"); // Use the ISO 8601 format for the string representation
        Debug.Log("Next card.interval: " + card.interval);
        Debug.Log("Next review date: " + card.nextReviewDateStr);

        SaveIntoJson(flashCard, "flashcards_omasuomi_1_Finnish1_TESSSSSSSSSSSSTTTTTTTTT");
    }

    public void SaveIntoJson(FlashCard flashCard, string fileName){
        string jsonString = JsonUtility.ToJson(flashCard, true);
        
        System.IO.File.WriteAllText($"{Application.persistentDataPath}/{fileName}.json", jsonString);
        Debug.Log("File save as " + $"{Application.persistentDataPath}/{fileName}.json");
    }

}


[System.Serializable]
public class Card
{
    public string frontText;
    public string backText;
    public int cardType; // 0: New, 1: Learning, 2: Review, 3: Relearning
    public float interval;
    public float easeFactor;
    public int repetitions;
    public string nextReviewDateStr; // Use the ISO 8601 format for the string representation. To convert back use DateTime.Parse(nextReviewDateStr);
    public string frontLanguage;
    public string backLanguage;

    public override string ToString()
    {
        return $"Front Text: {frontText}\nBack Text: {backText}\nCard Type: {cardType}\nInterval: {interval}\nEase Factor: {easeFactor}\nRepetitions: {repetitions}\nNext Review Date Str: {nextReviewDateStr}\nFront Language: {frontLanguage}\nBack Language: {backLanguage}";
    }
}

[System.Serializable]
public class FlashCard
{
    public List<Card> cards;
}

public enum CARD_TYPE
{
    /*
    *   In Anki, there are 4 types of card:
    *   1. New
    *   2. Learning
    *   3. Review
    *   4. Relearning
    *   https://faqs.ankiweb.net/what-spaced-repetition-algorithm.html#what-are-the-different-card-types
    *   Each card type has different behavior in the algorithm   
    *
    */
    NEW,
    LEARNING,
    RELEARNING,
    REVIEW,    
}
