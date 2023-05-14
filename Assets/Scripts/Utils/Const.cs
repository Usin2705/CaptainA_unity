using UnityEngine;
using System.Collections.Generic;


// Android asset studio
// https://romannurik.github.io/AndroidAssetStudio/
// Padding: 0%
// Asset size: 200dp max 
// Asset padding: 20dp
// Color 0 0 0 100

// Android material designs
// https://material.io/resources/

// Android color picker
// https://material.io/resources/color/

// Theme picker
// https://m3.material.io/theme-builder#/custom

// Current theme:
// https://material.io/resources/color/#!/?view.left=0&view.right=0&primary.color=fcefce&secondary.color=19ddc9

// TODO check mitä kuuluu word audio example.
// Extra frame (might be 6) is needed since entropy tend to yield weird result
// with the last two uu, one u score is just the leftover entropy not the middle highest score
// so uu without extra frame might result bad score for the first u and ingore the correct score for 2nd u
// Also see "hänellä ei ollut mitään opittavaa"
// The aa at the end only get the highest score for 1 a (due to entropy)

// TODO 
// Add more Grocery, AudioBooks

public static class Const
{

    //Font size:
    /*
    *   Biggest label: 40: Top text, App name, Input text, Result Text...
    *   Big label: 36: Correct Sound, Actual Sound, for differentiate from avg text but not the biggest label
    *   Average text: 30 dictionary, info, explanation
    *   Small text: 20:  credit
    *
    *
    */


    // ================= TEXR COLOR SCORE ===================
    // Rich text color tag for each type of scoring
    public const string BAD_COLOR = "#ff0000ff";    
    public const string AVG_COLOR = "#ff8000";
    public const string GOOD_COLOR = "#0000ffff";    

    // Maximum for bad string is 9 length (for UI to look nice in most case), ideal was "Incorrect"
    public const string BAD_STRING = "Flawed";    
    public const string AVG_STRING = "Almost correct";
    // The onboarding is manual text input so we don't use this for now
    public const string GOOD_STRING = "Correct";    

    /* Score range (less than) for each type of scoring
    *  The bad score actually depend on the model
    *  Without finetuned, 3rd quartile (75%) of error from FA score
    *  is about 45%, which then 0.49f sound good (mathematically also good)
    *  Then it's hard to select avg_score, but let's just pick 0.92f
    *  
    *  For finetuned models with digitala, 3rd quartile only about 29%, so  
    *  we can select something around 0.3f or 0.35f
    *  This will also allow more room for addtional score range 
    *  (from bad to more than 50% would be another option)
    *
    *  The average score then should also be lower, different model will have 
    *  different scale, but a rule of thumb would be anything below 0.9f is not 
    *  NATIVE level, so avg score should be around 0.9f
    *  Some finetuned model could result in lower AVG_SCORE
    *  And some model allow for addtional scale (BAD, AVG, GOOD, EXCELLENT)
    *  
    */
    public const float BAD_SCORE = 0.20f;
    public const float AVG_SCORE = 0.80f;
    //public const float GOOD_SCORE = 0.92f;

    // Weight % allow users to update score with more weight put on 
    // latest score and not the historical average
    // 0.7f mean 30% weight on average + 70% on new score
    // No longer using this, we use average instead (which is not as good)
    public const float SCORE_WEIGHT = 0.7f; 
    // =======================================================


    
    // ===================== AUDIO CONST =====================
    public const int FREQUENCY = 16000;

    // Maximum recording time per seconds
    // The extra record time after the button release is trimmed
    public const int MAX_REC_TIME = 8;

    public const string REPLAY_FILENAME = "recorded_speech";

    // Used to set the recording time for the audio clip
    // The length of the audio clip depend on the number of characters
    // of the text to be recorded
    public const float SEC_PER_CHAR = 0.1f;

    // Always provide at least 1.5s extra time for recording
    // to avoid the speaker speak too slow and the recording stop
    public const float EXTRA_TIME = 1.5f;

    // =======================================================



    // ===================== NETWORK CONST =====================

    // Maximum waiting time for Unity web request
    public const int TIME_OUT_SECS = 20;
    // =======================================================



    // ====================== TAB COLOR ======================
    public static readonly Color32 IMAGE_ACTIVE_COLOR = new Color32(22,42,82,255);
    public static readonly Color32 IMAGE_INACTIVE_COLOR = new Color32(105,105,105,255);

    public static readonly Color32 BG_ACTIVE_COLOR = new Color32(200,200,200,255);
    public static readonly Color32 BG_INACTIVE_COLOR = new Color32(255,255,255,255);

    // mic color: 13E365    

    // =======================================================
    

    // ==================== RESOURCES PATH ====================
    public const string PHONE_HELP_PATH = "ScriptableObjects/PhonemeHelpers/";
    public const string WORD_LIST_PATH = "ScriptableObjects/Words/";
    // ========================================================

    
    
    // ====================== VOCAB LIST ======================
    
    // This one is less overhead than ImmutableList (and people recommend to not use ReadOnlyCollection)
    public static readonly string[] VOCABS_LIST =  {"a", "ä", "b", "d", "e", "f", "h", "i", "j", 
                                                    "k", "l", "m", "n", "o", "ö", "p", "r", "s", "t", 
                                                    "u", "v", "y", " ", "å", "c", "g", "q", "w", "x", };
    // ========================================================    


    // ====================== EXERCISE ======================    
    public const int MAX_QUEUE = 5;
    public const int MAX_NO_TRIES = 3;
    // ======================================================== 

    // ===================== PLAYER PREFS CONST =====================

    // Check if user open the game for the first time
    public const string PREF_FIRST_KEY = "IsFirstTime";

    // Countdown to make sure users only learn 20 new cards per day
    // and 200 review cards per day
    public const string PREF_DATE_FLASHCARD = "Datetime_Flashcard_Start";

    // The number of new and review cards for the current day
    public const string PREF_NEW_COUNT = "flashcard_new_count";
    public const string PREF_REVIEW_COUNT = "flashcard_review_count";
    // =======================================================

    // ===================== FLASH CARD CONST =====================

    // Name of flashcard deck available in the app
    public static readonly string[] FLASK_CARD_FILES = {
        "EN-FI_OmaSuomi_1_Finnish1_CaptainA",
        "FI-EN_OmaSuomi_1_Finnish1_CaptainA",
    };
    
    public const int CARD_AGAIN = 0;
    public const int CARD_HARD = 3;
    public const int CARD_GOOD = 4;
    public const int CARD_EASY = 5;    

    public const string ANKI_NEW_COLOUR = "#3B82F6";    
    public const string ANKI_LEARN_COLOUR = "#DC2660";    
    public const string ANKI_DUE_COLOUR = "#16A34F";    
    public const string ANKI_GRAY_COLOUR = "#AFAFAF";    
    // =======================================================
}