using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public static class TextUtils
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

    public static string SantinizeText(string text) 
    /*
    *
    *   Santinize Text should be done at the unity level, since we will 
    *   compare the transcript with OPS operations after
    */

    {
        text = text.Trim(); // Remove trailing white space
        text = Regex.Replace(text, "[zZ]", "ts"); //Replace z with ts
        text = Regex.Replace(text, "[0-9]", ""); //Remove numbers
        text = text.Replace("\n", " ");
        text = Regex.Replace(text, "[-!$%^&*()_+|~=`{}\\[\\]:\";'<>?,.\\/@]", ""); //Remove symbols

                
        return text;
    }
}
