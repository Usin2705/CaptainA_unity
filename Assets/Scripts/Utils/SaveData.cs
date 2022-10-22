using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class SaveData
{
    public static void SaveIntoJson(UserData userData){
        string data = JsonUtility.ToJson(userData);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/UserData.json", data);
        Debug.Log("File save as " + Application.persistentDataPath + "/UserData.json");
    }

	public static UserData LoadFromJson(){
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
                PhonemeScore tempScore = new PhonemeScore(vocab, 1.0f, 0);
                tempL.Add(tempScore);                
            }
			
            UserData userData = new UserData("", 0, tempL);			
            SaveIntoJson(userData);
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

                

		UserData userData = LoadFromJson();

		for (int i = 0; i < scoreList.Count; i++) 
		{
			string phoneme = transcript[i].ToString();		
			
			int index = userData.IndexOf(phoneme);
			// Find phoneme within the list
			// Ideally, Dictionary work better but Dictionary is not Serializable and 
			// therefore can't be Save or Load easily with JSON			
			if (index!=-1)
			{
				PhonemeScore phonemeScore = userData.phonemeScores[index];
				userData.phonemeScores[index].average_score = phonemeScore.average_score*(1-Const.SCORE_WEIGHT) + 
                                                              Const.SCORE_WEIGHT*scoreList[i];
				userData.phonemeScores[index].no_tries++;
			} else 			
            // If phone is not in the list, create that phoneme with current score            
			{
                PhonemeScore tempScore = new PhonemeScore(phoneme, scoreList[i], 1);
				userData.phonemeScores.Add(tempScore);
			}
		}

		SaveIntoJson(userData);
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

    public PhonemeScore(string phoneme, float average_score, int no_tries) 
    {        
        this.phoneme = phoneme;
        this.average_score = average_score;
        this.no_tries = no_tries;
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
public class ASRResult{
    /*
    *  
    *
    */

    // TODO Add special cases 
    // (double vowel, double consonant)
    
    
    // TODO Add "ng" and "nk" as they have different IPA

    public string prediction;
    public List<float> score;
}
