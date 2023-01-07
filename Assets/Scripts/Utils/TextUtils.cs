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

    public static string WrapPhonemeSO(SOPhonemeHelper phonemeSO, string colorStr, bool isBold = true) 
    /*
    *
    *   
    */
	{             
        string text = WrapTextColor(phonemeSO.phoneme, colorStr, isBold);

        // Some phoneme don't have ipa (especiall for loan word)
        if (phonemeSO.ipa!="") 
        {            
            text += $" /{WrapTextColor(phonemeSO.ipa, colorStr, isBold)}/";
        }

		return text;
	}

    public static string WrapTextColor(string text, string colorStr, bool isBold = true) 
	{             
        return $"<color={colorStr}>{(isBold?"<b>":"")}{text}{(isBold?"</b>":"")}</color>";
	}

    // TODO
    // Some vocabulary should be removed from Finnish vocab in wav2vec2
    // å
    // c,q,w,x, 
    // g alone
    public static string SantinizeText(string text) 
    /*
    *
    *   Santinize Text should be done at the unity level, since we will 
    *   compare the transcript with OPS operations after
    */

    {
        text = text.Replace("\n", " ");
        text = text.Trim(); // Remove trailing white space
        text = Regex.Replace(text, "  +", " "); // Replace extra spaces with just 1 space
        
        text = Regex.Replace(text, "[åÅ]", "oo"); //Replace å with oo
        // nk and
        // ng are not converted in app but should be converted during 
        text = Regex.Replace(text, "[qQ]", "k"); //Replace q with k
        text = Regex.Replace(text, "[wW]", "v"); //Replace w with v
        text = Regex.Replace(text, "[xX]", "ks"); //Replace x with ks
        text = Regex.Replace(text, "[zZ]", "ts"); //Replace z with ts
        text = Regex.Replace(text, "[0-9]", ""); //Remove numbers        
        text = Regex.Replace(text, "[-!$%^&*()_+|~=`{}\\[\\]:\";'<>?,.\\/@]", ""); //Remove symbols

                
        return text;
    }
}
