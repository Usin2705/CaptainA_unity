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

    public static string ExtractTextWithinAtTags(string text){
        string pattern = "@(.*?)@";
        MatchCollection matches = Regex.Matches(text, pattern);

        string extractedText = "";
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                extractedText += match.Groups[1].Value + " "; // Add a space or newline if needed
            }
        }
        return extractedText.Trim(); // Trim to remove any extra spaces at the ends
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
            Debug.LogError("transcript and score didn't match: " + transcript + " vs " + scoreList.Count + " ");
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

    public static string FormatEtymology(string etymology)
    {
    return Regex.Replace(etymology, @"\(([^)]+)\)", m => $"(<i>{m.Groups[1].Value}</i>)");
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

    /// <summary>
    /// Sanitizes the provided text to ensure it conforms to specific requirements.
    /// </summary>
    /// <remarks>
    /// <para>The sanitization process involves:</para>
    /// <list type="bullet">
    /// <item><description>Replacing newline characters with spaces.</description></item>
    /// <item><description>Replacing hyphens "-" with spaces.</description></item>
    /// <item><description>Removing trailing white spaces.</description></item>
    /// <item><description>Replacing multiple spaces with a single space.</description></item>
    /// <item><description>Replacing Z or z with ts.</description></item>
    /// <item><description>Removing numbers.</description></item>
    /// <item><description>Removing symbols except for hyphens (which are removed earlier).</description></item>
    /// </list>
    /// <para>This sanitization process is essential when comparing the transcript with OPS operations.</para>
    /// </remarks>
    /// <param name="text">The input text string that needs to be sanitized.</param>
    /// <returns>Returns the sanitized version of the input text.</returns>
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
                warn_text += "In Finnish, <b>n</b> and <b>p</b> together is pronounced as <b>mp</b>. The AI model still mark you correctly if you use either \"np\" or \"mp\"";
            }
            
            if (warn == WARNINGS.NGK)
            {
                if (!warn_text.EndsWith("\n") && warn_text !="") warn_text += "\n";
                warn_text += count + ". ";
                count++;
                warn_text += "The <b>ng</b> is pronounced as /<b>ŋː</b>/, and <b>nk</b> is pronounced as /<b>ŋk</b>/. The current model can detect correct pronunciation but can't give the correct score for ŋ and ŋ:. We are developing a new model to address this problem.";
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

    public static string GetGradingInstruction(DescribePanel.TaskType taskType, int taskNumber, bool isFinnish=true)
    {
        if (taskNumber < 0 ) taskNumber = 0;
        if (taskNumber > 1 ) taskNumber = 1;
        //string language_instruction = isFinnish ? "The primary task for the users is to speak in Finnish.\\n" : "The primary task for the users is to speak in English\\n";
        string language_instruction = "";
        
        switch (taskType)
        {
            case DescribePanel.TaskType.A:
            case DescribePanel.TaskType.A2:
                switch (taskNumber)
                {
                    case 0: return isFinnish ? language_instruction + Secret.ROOM_DESCRIPTION_A0 : language_instruction + Secret.ROOM_DESCRIPTION_A0_EN;
                    case 1: return isFinnish ? language_instruction + Secret.ROOM_DESCRIPTION_A1 : language_instruction + Secret.ROOM_DESCRIPTION_A1_EN;
                }
                break;
            case DescribePanel.TaskType.B:
            case DescribePanel.TaskType.B2:
                switch (taskNumber)
                {
                    case 0: return isFinnish ? language_instruction + Secret.MISSING_ITEM_B0 : language_instruction + Secret.MISSING_ITEM_B0_EN;
                    case 1: return isFinnish ? language_instruction + Secret.MISSING_ITEM_B1 : language_instruction + Secret.MISSING_ITEM_B1_EN;
                }
                break;
            case DescribePanel.TaskType.C:
            case DescribePanel.TaskType.C2:
                // only return 1 case
                default: return isFinnish ? language_instruction + Secret.RANDOM_ROOM_DESCRIPTION_C : language_instruction + Secret.RANDOM_ROOM_DESCRIPTION_C_EN;
        }

        return Secret.ROOM_DESCRIPTION_A0;
    }
}