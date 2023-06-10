using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//######################################## SAVE DATA ########################################
public static class SaveData
{
    public static void SaveIntoJson(object jsonOjb, string fileName){
        /*
        *   This function save a json object into a file
        *   fileName should be without extension
        */

        string jsonString = JsonUtility.ToJson(jsonOjb, true);
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");
        System.IO.File.WriteAllText(filePath, jsonString);
        //Debug.Log("File save as " + filePath);
    }

    public static void UpdateFlashCard(string flashCardFileName) {
        /*
        *   This function update the flashcard file in the persistentDataPath
        *   with the flashcard file in the Resources folder
        *
        *   This function is used to update the flashcard file in the persistentDataPath
        *   when the flashcard file in the Resources folder is updated           
        *   
        */

        // Load the json file from Resources folder
        // When load the json file from Resources folder as TextAsset, the file extension should be removed
        TextAsset jsonFile = Resources.Load<TextAsset>(Const.FLASH_CARDS_PATH + flashCardFileName); // Note: Do not include the file extension
        //Debug.Log("Json data " + jsonFile.text);
        FlashCard flashCardRes = JsonUtility.FromJson<FlashCard>(jsonFile.text);
        //Debug.Log("FlashCard load from Resources folder");

        // Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.persistentDataPath, flashCardFileName + ".json");

        // If the file also exist in the persistentDataPath and the Resources folder
        // It it time to check for update
        if (File.Exists(filePath)) {
            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);    
            //Debug.Log("Json data " + dataAsJson);

            // Pass the json to JsonUtility, and tell it to create a flashcard object from it            
            FlashCard flashCardLocal = JsonUtility.FromJson<FlashCard>(dataAsJson);
            //Debug.Log("File load from " + filePath);

            // If the flashcard in local have lower version than the one in Resources folder:
            if (flashCardLocal.version < flashCardRes.version) {
                
                // Update the flashcard in in Resources folder with the data in the local flashcard
                foreach (Card localCard in flashCardLocal.cards) {                
                    // The updateCardMeta function will find the card with the same id and update it
                    // The updateCardMeta function already handle the case when the card is not found
                    // it only update the meta data (id, cardType, etc.) but not the frontText and backText
                    flashCardRes.updateCardMeta(localCard);
                }

                // update the metadata of the flashcard in Resources folder
                
                // if old flashcard have a useDateStr, use it
                // else use the default value "2000-01-01T00:00:00.0000000Z"                
                flashCardRes.useDateStr = flashCardLocal.useDateStr != ""? flashCardLocal.useDateStr : "2000-01-01T00:00:00.0000000Z";                

                flashCardRes.todayDateStr = flashCardLocal.todayDateStr;
                flashCardRes.newCount = flashCardLocal.newCount;
                flashCardRes.reviewCount = flashCardLocal.reviewCount;

                // Save the updated flashcard into the persistentDataPath
                SaveIntoJson(flashCardRes, flashCardRes.fileName);
            }
        }
    }

    public static FlashCard LoadFlashCard(string flashCardFileName){
        // Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.persistentDataPath, flashCardFileName + ".json");

        FlashCard flashCard;

		if(File.Exists(filePath)) {
            // Since we can't save in Resources folder, we save the edited flashcard file in the persistentDataPath
            // If filePath is exit, load the json file from the persistentDataPath            

            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);    
            //Debug.Log("Json data " + dataAsJson);

            // Pass the json to JsonUtility, and tell it to create a flashcard object from it            
			flashCard = JsonUtility.FromJson<FlashCard>(dataAsJson);
            //Debug.Log("File load from " + filePath);

        } else {
            // If the filePath is not exit, load the json file from Resources folder        
            // Load the json file from Resources folder
            // When load the json file from Resources folder as TextAsset, the file extension should be removed
            TextAsset jsonFile = Resources.Load<TextAsset>(Const.FLASH_CARDS_PATH + flashCardFileName); // Note: Do not include the file extension
            //Debug.Log("Json data " + jsonFile.text);
            flashCard = JsonUtility.FromJson<FlashCard>(jsonFile.text);
            //Debug.Log("FlashCard load from Resources folder");
        }

        foreach (Card card in flashCard.cards)
        {
            // Default value for int is 0, so if the cardType is not set, it will be 0 (NEW)           

            // If the card id is not set, set it to a random string
            // Card ID is needed to keep track of the card in the queue
            if (card.id == ""){
                
                card.id = Guid.NewGuid().ToString();                                
            }
        }

        // If the flashcard is not used before, set the useDateStr to 2000-01-01T00:00:00.0000000Z
        if (flashCard.useDateStr == "") {            
            flashCard.useDateStr = "2000-01-01T00:00:00.0000000Z";
        }

        SaveData.SaveIntoJson(flashCard, flashCardFileName);

        return flashCard;
    }

	public static UserData LoadUserData(){
		// Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.persistentDataPath, "UserData.json");

		if(File.Exists(filePath))
        {
            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);    
            // Pass the json to JsonUtility, and tell it to create a UserData object from it            
			UserData userData = JsonUtility.FromJson<UserData>(dataAsJson);
			return userData;
        }
        else
        {
			// If no UserData file is created, create an empty UserData file
            // with all posible vocab in VOCABS_LIST            
            List<PhonemeScore> tempL = new List<PhonemeScore>();

            foreach (string vocab in Const.VOCABS_LIST)
            {
                List<ScoreWithUTC> tempHistoryScore = new List<ScoreWithUTC>();
                PhonemeScore tempScore = new PhonemeScore(phoneme: vocab, average_score: 1.0f, no_tries: 0, historyScore: tempHistoryScore);
                tempL.Add(tempScore);                
            }
			
            UserData userData = new UserData("", 0, tempL);			
            SaveIntoJson(userData, "UserData");
			return userData;
        }
    }
    
    public static void UpdateUserScores(string transcript, List<float> scoreList) {
        /*
        * Remember to turn transcript to lowercase, as there's different between upper and lowercase
        */

        transcript = transcript.ToLower();        

		// Make sure that stranscript length match with scoreList Length
		if (transcript.Length != scoreList.Count) {	
            Debug.LogError("transcript and score didn't match");
			return;
		}

		UserData userData = LoadUserData();

		for (int i = 0; i < scoreList.Count; i++) 
		{
			string phoneme = transcript[i].ToString();		
			
			int index = userData.IndexOf(phoneme);

            // This record the currernt time in to Seconds Since 1970
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);//January 1, 1970 0 hours, 0 minutes, 0 seconds
            int secondsInUTC =  (int)(DateTime.UtcNow - epochStart).TotalSeconds;            
            ScoreWithUTC scoreWithUTC = new ScoreWithUTC(score: scoreList[i], secondsInUTC: secondsInUTC); 

			// Find phoneme within the list
			// Ideally, Dictionary work better but Dictionary is not Serializable and 
			// therefore can't be Save or Load easily with JSON			
			if (index!=-1)
            
            // If phone is in the list, we can just update the 
            // Maybe using a constructor here would work as well?
			{
				PhonemeScore phonemeScore = userData.phonemeScores[index];
				userData.phonemeScores[index].average_score = (phonemeScore.average_score * phonemeScore.no_tries + scoreList[i])
                                                              /(phonemeScore.no_tries + 1);
				userData.phonemeScores[index].no_tries++;   
                
                if (userData.phonemeScores[index].historyScore == null) {
                // If there weren't a list to store historical score we will create one
                // This function is important for backward compatible (old version don't have the historical score)
                    List<ScoreWithUTC> tempHistoryScore = new List<ScoreWithUTC>();            
                    tempHistoryScore.Add(scoreWithUTC);
                    userData.phonemeScores[index].historyScore = tempHistoryScore;
                    
                } else userData.phonemeScores[index].historyScore.Add(scoreWithUTC);    

                

			} else 			
            // If phoneme is not in the list, create that phoneme with current score            
			{
                List<ScoreWithUTC> tempHistoryScore = new List<ScoreWithUTC>();                
                tempHistoryScore.Add(scoreWithUTC);
                PhonemeScore tempScore = new PhonemeScore(phoneme: phoneme, average_score: scoreList[i], no_tries: 1, 
                                                          historyScore: tempHistoryScore);
				userData.phonemeScores.Add(tempScore);
			}
		}

		SaveIntoJson(userData, "UserData");
    }
}

[System.Serializable]
public class UserData{
    public string placeHolderString;
    public int placeHolderInt;
    public List<PhonemeScore> phonemeScores; // Ideally we want a Dictionary here, but Dictionary is a pain when save and load in Unity with Json (not Seriablizable)

    public UserData (string placeHolderString, int placeHolderInt,  List<PhonemeScore> phonemeScores) {
        this.placeHolderString = placeHolderString;
        this.placeHolderInt = placeHolderInt;
        this.phonemeScores = phonemeScores;
    }

    public int IndexOf(string phoneme) {
        /*
            Find the Index of a phoneme within the PhonemeScores list
            and return its index. If phoneme is not within the list 
            return -1.
        */
        for (int i = 0; i < phonemeScores.Count; i++) 
        {
            if(phonemeScores[i].phoneme==phoneme) {
                return i;
            }
        }

        return -1;
    }
}


[System.Serializable]
public class PhonemeScore: IComparable<PhonemeScore>{
    public string phoneme;
    public float average_score;
    public int no_tries;
    public List<ScoreWithUTC> historyScore;

    public PhonemeScore(string phoneme, float average_score, int no_tries, List<ScoreWithUTC> historyScore) 
    {        
        this.phoneme = phoneme;
        this.average_score = average_score;
        this.no_tries = no_tries;        
        this.historyScore = historyScore;
    }

    public int CompareTo(PhonemeScore other)
    {
        if(other == null)
        {
            return 1; //greater than
        }

        //Return the rank in average score
        //1 mean greater than other
        //-1 mean less than other
        if (other.average_score > average_score) return -1;
        else if (other.average_score == average_score) return 0;
        else return 1;
    }
}

[System.Serializable]
public class ScoreWithUTC {
    public float score;
    public int secondsInUTC;

    public ScoreWithUTC(float score, int secondsInUTC) 
    {        
        this.score = score;
        this.secondsInUTC = secondsInUTC;        
    }
}

//#########################################################################################


//##################################### GET RESTFUL RESULT #####################################
[System.Serializable]
public class ASRResult{
    /*
    *  
    *
    */
    // TODO Add special cases 
    // (double vowel, double consonant)        
    // TODO Add "ng" and "nk" as they have different IPA

    public List<OPS> levenshtein;
    public string prediction;
    public List<float> score;
    public List<WARNINGS> warning;
}

//##################################### LEVENSHTEIN #######################################
[System.Serializable]
public class OPS 
{   
    /*
    *
    *   There's 3 code in ops: replace, insert, delete
    *   there's "equal" which is not used in the python code so it won't show up here
    *
    */

    public string ops;
    public int tran_index;
    public int pred_index;
}
//#########################################################################################


//######################################## WARNING ########################################
public enum WARNINGS
{
    AAA,
    NP,
    NGK,
    MENEP,
}