using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveData : MonoBehaviour
{
    [SerializeField] private UserData userData;

	public void SaveIntoJson(UserData userData){
        string data = JsonUtility.ToJson(userData);
		//Debug.Log("Data" + data);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/UserData.json", data);
    }

	public UserData LoadFromJson(){
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
            Debug.Log("Create new user data file");
			userData = new UserData("", 0, new List<PhonemeScore>());		
			SaveIntoJson(userData);
			return userData;
        }
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
    public string prediction;
    public List<float> score;
}
