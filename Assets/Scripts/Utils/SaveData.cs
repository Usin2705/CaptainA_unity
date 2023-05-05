using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//######################################## SAVE DATA ########################################
public static class SaveData
{
    public static void SaveIntoJson(UserData userData){
        string jsonString = JsonUtility.ToJson(userData);
        System.IO.File.WriteAllText($"{Application.persistentDataPath}/UserData.json", jsonString);
        Debug.Log("File save as " + $"{Application.persistentDataPath}/UserData.json");
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
                List<ScoreWithUTC> tempHistoryScore = new List<ScoreWithUTC>();
                PhonemeScore tempScore = new PhonemeScore(phoneme: vocab, average_score: 1.0f, no_tries: 0, historyScore: tempHistoryScore);
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

public static class WarningDetail 
{
    public static string GetWarning(List<WARNINGS> warningList)
    /*
    *
    *
    *
    *
    */
    {
        string warn_text = "";
        int count = 1;
        //TODO try to sort from small to large to be safe
        foreach (WARNINGS warn in warningList)
        {
            if (warn == WARNINGS.AAA)
            {
                warn_text += count + ". ";
                count++;
                warn_text += "The AI model is not designed for very short word like ää or syy. Try using longer word by combining with consonants. For example, instead of using <b>ää</b>, use s<b>ää</b>t<b>ää</b>";                
            }
            
            if (warn == WARNINGS.NP)
            {                
                if (!warn_text.EndsWith("\n") && warn_text !="") warn_text += "\n";
                warn_text += count + ". ";
                count++;
                warn_text += "In Finnish, <b>np</b> is pronounced as <b>mp</b>. The AI model still mark you correctly if you use either \"np\" or \"mp\"";
            }
            
            if (warn == WARNINGS.NGK)
            {
                if (!warn_text.EndsWith("\n") && warn_text !="") warn_text += "\n";
                warn_text += count + ". ";
                count++;
                warn_text += "The <b>ng</b> is pronounced as /<b>ŋː</b>/, and nk is pronounced as /<b>ŋk</b>/. The current model can detect correct pronunciation but can't give the correct score for ŋ and ŋ:. We are developing a new model to address this problem.";
            }

            if (warn == WARNINGS.MENEP)
            {
                if (!warn_text.EndsWith("\n") && warn_text !="") warn_text += "\n";
                warn_text += count + ". ";
                count++;
                warn_text += "We detect a possible case of <b>boundary gemination</b> (loppukahdennus). The most common example is \"mene pois\", which is pronounced as mene<b>p</b> <b>p</b>ois. This is an advanced spoken Finnish problem and you should consult your Finnish teacher.";                
            }
        }

        return warn_text;
    }

//#########################################################################################

}