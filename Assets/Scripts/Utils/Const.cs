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
// https://m3.material.io/theme-builder#/custom
// Primary color: #fcefce
// Secondary color: #19ddc9
// Tertiary color: #ffffff
// Neutral color: #f5f5f6

// TODO check mitä kuuluu word audio example.
// Extra frame (might be 6) is needed since entropy tend to yield weird result
// with the last two uu, one u score is just the leftover entropy not the middle highest score
// so uu without extra frame might result bad score for the first u and ingore the correct score for 2nd u
// Also see "hänellä ei ollut mitään opittavaa"
// The aa at the end only get the highest score for 1 a (due to entropy)

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
    public const int MAX_CHAR_ALLOWED = 80;

    // Adjustment value for the number game
    // The number game is a bit different, since we focus on number and not pronunciation
    public const float AVG_NUMBER_ADJ = 0.40f;

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

    public const int MAX_REC_TIME_A = 45; //45
    public const int MAX_REC_TIME_B = 30; //30


    public const int MAX_REC_NUMBERGAME_EASY = 3;
    public const int MAX_REC_NUMBERGAME_MEDIUM = 4;
    public const int MAX_REC_NUMBERGAME_HARD = 5;
    public const int MAX_REC_NUMBERGAME_RANK = 4;

    public const int NUMBERGAME_PREPARE_TIME = 2;


    public const string REPLAY_FILENAME = "recorded_speech";
    public const string DESCRIBE_FILENAME = "recorded_describe_speech";

    public const string NUMBERGAME_FILENAME = "recorded_number_game";

    // Used to set the recording time for the audio clip
    // The length of the audio clip depend on the number of characters
    // of the text to be recorded
    public const float SEC_PER_CHAR = 0.12f;

    // Always provide at least 1.5s extra time for recording
    // to avoid the speaker speak too slow and the recording stop
    public const float EXTRA_TIME = 1.5f;

    // =======================================================



    // ===================== NETWORK CONST =====================

    // Maximum waiting time for Unity web request
    public const int TIME_OUT_SECS = 20;
    public const int TIME_OUT_ADVANCE_SECS = 30;

    public const string FILE_NAME_POST = "speech_sample";
    // =======================================================



    // ====================== TAB COLOR ======================
    public static readonly Color32 IMAGE_ACTIVE_COLOR = new Color32(22, 42, 82, 255);
    public static readonly Color32 IMAGE_INACTIVE_COLOR = new Color32(105, 105, 105, 255);

    public static readonly Color32 BG_ACTIVE_COLOR = new Color32(200, 200, 200, 255);
    public static readonly Color32 BG_INACTIVE_COLOR = new Color32(255, 255, 255, 255);

    // mic color: 13E365    

    // =======================================================


    // ==================== RESOURCES PATH ====================
    public const string PHONE_HELP_PATH = "ScriptableObjects/PhonemeHelpers/";
    public const string WORD_LIST_PATH = "ScriptableObjects/Words/";
    public const string AUDIO_AI_PATH = "AudioAI/";
    public const string AUDIO_NATURAL_PATH = "AudioNatural/";
    public const string ILLUSTRATIONS_PATH = "Illustrations/";
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
    public const string PREF_RECORD_NUMBER = "pref_record_number";
    public const int SURVEY_TRIGGER = 50;
    public const string PREF_FLASHCARD_NUMBER = "pref_flashcard_number";
    public const int SURVEY_TRIGGER_FLASH_CARD = 200;
    public const string SURVEY_URL = "https://link.webropol.com/s/CaptainA";

    public const string PREF_SURVEY_V1_DONE = "pref_survey_v1_done";
    public const string PREF_NO_SURVEY = "pref_no_survey"; // No longer have NO option :D


    public const string PREF_SECRET_TEXT = "pref_text_secret";
    public const string PREF_INS_MAIN = "pref_instruction_main";
    public const string PREF_INS_ADVANCE = "pref_instruction_advance";
    public const string PREF_INS_VOCABULARY = "pref_instruction_vocabulary";
    public const string PREF_INS_PROFILE = "pref_instruction_profile";
    public const string PREF_INS_PHONE = "pref_instruction_phone";


    // =======================================================

    // ===================== FLASH CARD CONST =====================

    // Create a const dictionary for the flashcard deck
    public static readonly Dictionary<string, string> FLASH_CARD_DICT = new Dictionary<string, string> {
        {"FlashCard_OS_FIN_ENG", "FI-EN_OmaSuomi_1_CaptainA"},
        {"FlashCard_OS_ENG_FIN", "EN-FI_OmaSuomi_1_CaptainA"},
        {"FlashCard_HA_FIN_FIN", "FI-FI_HA_Hagaga"}
    };

    // Folder contains flashcard decks in Resources folder
    public const string FLASH_CARDS_PATH = "Flashcards/";

    public const float REQUIRED_HOLD_TIME = 1.0f;
    public const int ADDITIONAL_NEW_CARD = 50;

    public const int CARD_AGAIN = 0;
    public const int CARD_HARD = 3;
    public const int CARD_GOOD = 4;
    public const int CARD_EASY = 5;

    public const string ANKI_NEW_COLOUR = "#3B82F6";
    public const string ANKI_LEARN_COLOUR = "#DC2660";
    public const string ANKI_DUE_COLOUR = "#16A34F";
    public const string ANKI_GRAY_COLOUR = "#AFAFAF";
    // =======================================================

    // ===================== INSTRUCTION STRING CONST =====================
    public const string INSTRUCTION_MAIN = "Practice your pronunciation by typing any words or sentences in the box. The model doesn't work well with short words or spoken Finnish.\n" +
                                            "<b><color=#ff0000ff>Red </color></b>: flawed pronunciation. \n" +
                                            "<b><color=#ffa500ff>Yellow</color></b>: almost correct, or the AI model is not confident due to unfamiliar accents.\n" +
                                            "<b><color=#0000ffff>Blue</color></b>: correct pronunciation. \n\n" +
                                            "You can click on the result for more details. \n\n" +
                                            "Our current model is continually improving. If you consistently have flawed pronunciation, you should seek advice from a Finnish teacher. We will develope better models in the future.\n";
    public const string INSTRUCTION_VOCABULARY = "This mode has flashcards (similar to Anki) to help learn vocabulary. \n\n" +
                                                 "You can also practice pronunciation with the <b>recording button</b> " +
                                                 "and the audio generated by AI. \n\n" +
                                                 "For each card, you can rate the difficulty from Again to Easy. " +
                                                 "The easier you remember the card, the longer it will be hidden. " +
                                                 "Your task is to learn 20 new cards a day. You can long press on the deck to learn more.\n\n" +
                                                 "We welcome volunteers to help translate the cards " +
                                                 "into other languages. Please get in touch with us if you are interested.";
    public const string INSTRUCTION_ADVANCE = "You can practice speaking either writen number or spoken number with PuheNumero. Please note that this function has higher pronunciation requirement. \n\n" +
                                               "We will add more advance functions to help you learning Finnish in the future.";
    public const string INSTRUCTION_PROFILE = "Your profile shows the average score you got for each phone. <b>Click</b> on the label " +
                                              "for instruction (text, photo, and video) on how to pronounce the phoneme correctly. \n\n" +
                                              "While our model does not always provide accurate results, the statistics here are reliable. " +
                                              "We suggest you seek advice from your Finnish teachers to improve your pronunciation. \n\n" +
                                              "Some phones can be more complicated than others, especially the Finnish <b>R</b>. " +
                                              "So please don't be frustrated if you get a low score. The point of this app is to " +
                                              "encourage you to practise speaking more.";
    public const string INSTRUCTION_PHONE = "The phone helper contains text, photos, and videos to help you practice your pronunciation. " +
                                            "Pay attention to your mouth and tongue position. You can also click on the video icon to see " +
                                            "the video instruction made by a Finnish teacher. \n\n" +
                                            "At the moment, we only focus on Finnish vowels. \n\n" +
                                            "The materials in this app are made by members of the Kielibuusti project. " +
                                            "We will add more materials in the future if it is available.";
                                            
    public const int APP_VERSION = 2;
public const string NEW_VERSION_TEXT = 
    "Hi everyone! As you've probably noticed, the app now has a new name, icon, and user interface. " +
    "These changes are the result of excellent work by Aalo Kailu, Apollo Ailus, and Kia Raitanen last summer. " +
    "They are students from Aalto University who helped improve the app's UI/UX design as their ITP project.\n\n" +
    "You also see a demo of new feature - <b>PuheNumero</b> - in the <b>Advanced</b> tab. This demo is based on the thesis work of Sy Hoang Mai, " +
    "under the guidance of advisor Nhan Phan and supervisor Mikko Kurimo, with additional support from student Lauri Lappalainen.\n\n" +
    "These improvements are based on feedback from our users - so please keep sharing your suggestions! " +
    "While our resources are limited, we'll do our best to implement your ideas.";                                           

    // ====================================================================
}