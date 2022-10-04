using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextColoring
{    
	public static string FormatTextResult(string transcript, List<float> scoreList) 
    /*
    * The font used in the ResultText GO is already set as BOLD        
    * so no need to add BOLD tag <b> </b> in the tag
    *   
    */
	{
		// Make sure that stranscript length match with scoreList Length
		if (transcript.Length != scoreList.Count) {	
            Debug.LogError("transcript and score didn't match");
			return "";
		}

        string textResult = "";

		for (int i = 0; i < scoreList.Count; i++) 
        {
            string phoneColor = Const.GOOD_COLOR;

            if (scoreList[i] < Const.BAD_SCORE)  phoneColor = Const.BAD_COLOR; 
            else if (scoreList[i] < Const.AVG_SCORE) phoneColor = Const.AVG_COLOR;
            
            textResult += "<color=" + phoneColor + ">" + transcript[i].ToString() + "</color>";

        }
        
		return textResult;
	}
}
