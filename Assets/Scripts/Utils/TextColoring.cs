using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextColoring
{
    public const string BAD_COLOR = "#ff0000ff";    
    public const string AVG_COLOR = "#ffa500ff";
    public const string GOOD_COLOR = "#0000ffff";

    public const float BAD_SCORE = 0.49f;
    public const float AVG_SCORE = 0.92f;
    //public const float GOOD_SCORE = 0.92f;
    
	public static string FormatTextResult(string transcript, List<float> scoreList) 
	{
		// Make sure that stranscript length match with scoreList Length
		if (transcript.Length != scoreList.Count) {	
            Debug.LogError("transcript and score didn't match");
			return "";
		}

        string textResult = "";

		for (int i = 0; i < scoreList.Count; i++) 
        {
            string phoneColor = GOOD_COLOR;

            if (scoreList[i] < BAD_SCORE)  phoneColor = BAD_COLOR; 
            else if (scoreList[i] < AVG_SCORE) phoneColor = AVG_COLOR;
            
            textResult += "<b><color=" + phoneColor + ">" + transcript[i].ToString() + "</color></b>";

        }
        
		return textResult;
	}
}
