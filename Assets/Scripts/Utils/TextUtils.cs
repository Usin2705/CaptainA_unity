using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public static class TextUtils
{    
    public static string FormatCardTypeNumber(int newCards, int learnCards, int dueCards) {
        string newCardsStr = WrapTextColor(newCards.ToString(), newCards > 0 ? Const.ANKI_NEW_COLOUR : Const.ANKI_GRAY_COLOUR);
        string learnCardsStr = WrapTextColor(learnCards.ToString(), learnCards > 0 ? Const.ANKI_LEARN_COLOUR : Const.ANKI_GRAY_COLOUR);
        string dueCardsStr = WrapTextColor(dueCards.ToString(), dueCards > 0 ? Const.ANKI_DUE_COLOUR : Const.ANKI_GRAY_COLOUR);

        return $"{newCardsStr} {learnCardsStr} {dueCardsStr}"; 
    }
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
        text = text.Replace("-", " "); // Replace - with space, should be done before removing extra space

        text = text.Trim(); // Remove trailing white space
        text = Regex.Replace(text, "  +", " "); // Replace extra spaces with just 1 space
        
        // TODO for later versions
        //text = Regex.Replace(text, "[åÅ]", "oo"); //Replace å with oo
        // nk and
        // ng are not converted in app but should be converted during wav2vec2 
        //text = Regex.Replace(text, "[qQ]", "k"); //Replace q with k
        //text = Regex.Replace(text, "[wW]", "v"); //Replace w with v
        //text = Regex.Replace(text, "[xX]", "ks"); //Replace x with ks

        text = Regex.Replace(text, "[zZ]", "ts"); //Replace z with ts
        text = Regex.Replace(text, "[0-9]", ""); //Remove numbers        
        text = Regex.Replace(text, "[!$%^&*()_+|~=`{}\\[\\]:\";'<>?,.\\/@]", ""); //Remove symbols (not -)
 
                
        return text;
    }
}
