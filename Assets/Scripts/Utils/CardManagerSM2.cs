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
        string filePath = Path.Combine(Application.persistentDataPath, "flashcards_omasuomi_1_2.json");

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
            TextAsset jsonFile = Resources.Load<TextAsset>("flashcards_omasuomi_1_2");
            Debug.Log("File loaded from " + jsonFile.text);
            flashCard = JsonUtility.FromJson<FlashCard>(jsonFile.text);
        }

        foreach (Card item in flashCard.cards)
        {
            Debug.Log(item.frontText + item.backText + item.interval + item.easeFactor + item.repetitions + item.nextReviewDateStr + item.frontLanguage + item.backLanguage);            
        }
    }

    public Card GetNextCard()
    {
        Card nextCard = flashCard.cards.Where(card => DateTime.Parse(card.nextReviewDateStr) <= DateTime.UtcNow)
            .OrderBy(card => DateTime.Parse(card.nextReviewDateStr))
            .FirstOrDefault();

        return nextCard;
    }
    
    public void UpdateCardReviewDate(Card card, int quality) 
    {       
        // This code resets the repetitions count, interval, and ease factor to their initial values, 
        // effectively treating the card as if it's being seen for the first time. 
        // This will cause the card to be reviewed more frequently, which aligns with the principles 
        // of spaced repetition learning for items with low-quality scores.
        if (quality < 3)
        {           
            card.repetitions = 0;            
            card.interval = 1;
            card.easeFactor = 2.5f; // the default starting ease factor

        }  else {
            // Quality in the SM2 formula have range from 0 to 5.
            // https://www.supermemo.com/en/blog/application-of-a-computer-to-improve-the-results-obtained-in-working-with-the-supermemo-method
            // Quality in the Anki only have 4 scale 0 - 3 (only 0 is counted as failure)
            // https://faqs.ankiweb.net/what-spaced-repetition-algorithm.html
            float newEaseFactor = card.easeFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f));

            // Author noticed that E-Factors should not fall below the value of 1.3. Items having 
            // E-Factors lower than 1.3 were repeated annoyingly often and always seemed to have inherent 
            // flaws in their formulation (usually they did not conform to the minimum information principle)        
            card.easeFactor = Mathf.Max(newEaseFactor, 1.3f);

            card.repetitions++;

            // Calculate the new interval
            if (card.repetitions == 1)
            {
                card.interval = 1;
            }
            else if (card.repetitions == 2)
            {
                card.interval = 6;
            }
            else
            {
                card.interval = card.interval * card.easeFactor;
            }
        }

        card.nextReviewDateStr = DateTime.UtcNow.AddDays(card.interval).ToString("O"); // Use the ISO 8601 format for the string representation
        Debug.Log("Next card.interval: " + card.interval);
        Debug.Log("Next review date: " + card.nextReviewDateStr);

        SaveIntoJson(flashCard, "flashcards_omasuomi_1_2");
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
    public float interval;
    public float easeFactor;
    public int repetitions;
    public string nextReviewDateStr; // Use the ISO 8601 format for the string representation. To convert back use DateTime.Parse(nextReviewDateStr);
    public string frontLanguage;
    public string backLanguage;
}

[System.Serializable]
public class FlashCard
{
    public List<Card> cards;
}
