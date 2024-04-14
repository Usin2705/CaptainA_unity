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

    public const int MAX_REC_TIME_A = 45;
    public const int MAX_REC_TIME_B = 30;


    public const string REPLAY_FILENAME = "recorded_speech";
    public const string DESCRIBE_FILENAME = "recorded_describe_speech";

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
        {"FlashCard_OS_ENG_FIN", "EN-FI_OmaSuomi_1_CaptainA"}
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
                                            "Press the microphone button to record your pronunciation. \n \n" + 
                                            "<b><color=#ff0000ff>Red </color></b>: flawed pronunciation. \n" +
                                            "<b><color=#ffa500ff>Yellow</color></b>: almost correct, or there is heavy accent or dialect, or the AI model is not confident.\n" +
                                            "<b><color=#0000ffff>Blue</color></b>: correct pronunciation. You sound like a native speaker.\n\n" + 
                                            "You can click on the coloured result for more details. \n\n" + 
                                            "Our current model is continually improving. If you consistently have flawed pronunciation, you should seek advice from a Finnish teacher. We will develope better models in the future.\n";
    public const string INSTRUCTION_VOCABULARY = "This mode has flashcards (similar to Anki) to help learn vocabulary. \n\n" +                                                                                                 
                                                 "You can also practice pronunciation with the <b>recording button</b> " +
                                                 "and the audio generated by AI. \n\n" +
                                                 "For each card, you can rate the difficulty from Again to Easy. " +
                                                 "The easier you remember the card, the longer it will be hidden. " +
                                                 "Your task is to learn 20 new cards a day. You can long press on the deck to learn more.\n\n" +
                                                 "We will regularly update the flashcards, and welcome volunteers to help translate the cards " +
                                                 "into other languages. Please get in touch with us if you are interested.";
    public const string INSTRUCTION_ADVANCE = "We will add more advance functions to help you learning Finnish in the future.";                                                 
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

    // ====================================================================

    public const string ROOM_DESCRIPTION_A1 = @"Task: Based on the provided transcript (made by an automatic speech recognition model) of the user's description of a picture, assign a grade as 'Excellent', 'Good', 'Partially' or 'None' for the task completion/task achievement. This is the task question given to the user:\\n" +
    "Task description: Please describe the room and objects (colours, positions) in the picture above. You have 45 seconds.\\n" +    
    "Mistakes in pronunciation, fluency, vocabulary and grammar errors are ignored and do not affect the grade. A general description of the room with 5 or more items is enough to get 'Excellent' grade. The text is a transcript made by ASR model, so there should be no punctuation.\\n" +
    "Explain your grading in detail and then give the grade. Then, based on what the user has said, provide a version that you think would get an 'Excellent' grade. Provide your improved version in Finnish inside the '@' tag like this: @improved version in this tag@\\n" +
    "Grading examples:\\n" +
    "User: huoneessa on isö sininen sova lattialla on suuri punainen mattö väsemmalla on musta kirjahylly ja paljon kirjoja kirjahyllyn vieressä on punainen nojatuoli\\n" +
    "System:[Explanation] Excellent. @Improved version@\\n" +
    "User: huoneessa on sohva ja matto musta hylly on ja siinä on kirjoja myös tuoli mutta ei muista televisio on huoneessa ja ovi on vihreä en näe pöytää\\n" +
    "System:[Explanation] Good. @Improved version@\\n" +
    "User: tama huoneessa on paljon tilaa musta kirjahylly on seinää vasemmalla ja siinä on monia värejä keskellä huonetta on tumman sinen sohva ja sillä on keltainen tyynu huoneen oikeala puolelli on keltainen kuvat seinällä ja ja vihreä ovi vieressä on takkia naulakossa.\\n" +
    "System:[Explanation] Excellent. @Improved version@\\n" +
    "User: on musta kirjahylly ja kirjoja sininen sohva ja siinä on jotain keeltaista lattialla on matto keltainen ja ovi on siellä.\\n" +
    "System:[Explanation] Good. @Improved version@\\n" +
    "User: sininen sohva ja iso no ja punainen iso noja tuoli pun punainen noja tuoli ja musta kirja musta hylly kilja.\\n" +
    "System:[Explanation] Partially. @Improved version@\\n" +
    "The room description:\\n" +
    "The room is warm and inviting. There is a big black bookshelf on the left with many colourful books. The bookshelf is against the wall and does not reach the ceiling, leaving some windows and wall space above it. A blue (or dark blue) sofa with yellow pillows is on the right of the room. The sofa is big and have two parts (look like an L), one can say that there are two 2 blue sofas instead of 1 big sofa. There is a big red (or dark red) rug on the floor. Next to the bookshelf, also on the left of the room, there is a pink (or light pink) armchair. In front of the sofa, there is no coffee table visible, which gives the space an open feel. There is a black television stand with a television (that also looks black) on it against the wall. On the wall above the television, there are two framed pictures. On the wall on the right, above the blue sofa, there are six (or many) square yellow decoratives (or abstract pictures). There is a green door in the far right corner. The door appears to be closed. Near the green door, there is a coat rack (in Finnish: naulakko) with 2 jackets (one black and one yellow jacket). The room has off-white walls (with a slightly yellow or grey) and a ceiling with some grey (or grey green).\\n\\n" +
    "Grading Criteria:\\n" +
    "Excellent: Completes all aspects of the task. There are no significant deficiencies in the response.\\n" +
    "Good: Completes the task, but there are some significant deficiencies in the response.\\n" +
    "Partially: Only partially answers the task, the response has many significant deficiencies.\\n";

    public const string ROOM_DESCRIPTION_A2 = @"Just answer N/A for this task.\\n";
    public const string ROOM_DESCRIPTION_A3 = @"Just answer N/A for this task.\\n";
    public const string ROOM_DESCRIPTION_A4 = @"Just answer N/A for this task.\\n";

    public const string ROOM_DESCRIPTION_B1 = @"Task: Based on the provided transcript (made by an automatic speech recognition model) of the user's description of a picture, assign a grade as 'Excellent', ‘Good’, ‘Partially’ or ‘None’ for the task completion/task achievement, and then provide an improved version. This is the task question given to the user:\\n" +
    "Task Description: The scenario involves a situation where the speaker was in a café with their friends. Upon returning home, they notice that their hoodie is missing. They need to call the café (within a maximum of 30 seconds) to:\\n" +
    "1. Introduce themselves. They can use 'Maija' or 'Matti Meikäläinen' but they can use any names they want.\\n" +
    "2. Politely state their business.\\n" +
    "3. Describe the lost hoodie (which is described below).\\n\\n" +
    "Mistakes in pronunciation, fluency, vocabulary and grammar errors are ignored and do not affect the grade. A general conversation with the hoodie description is enough to get 'Excellent' grade. Speakers can skip the pattern and texture of the hoodie and still get 'Excellent' grade. The grade of the speech depends on the whole content made by the user, not just the hoodie. The text is a transcript made by ASR model, so there should be no punctuation.\\n" +
    "Explain your grading in detail and then give the grade. Then, based on what the user has said, provide a version that would be enough to get 'Excellent' grade, in the same context of a phone call to the café. The improved version doesn't have to describe the entire picture but should be tailored based on user's answer so that it is just enough to get 'Excellent'. Do not change spoken Finnish (puhekieli) into written Finnish and vice versa. Provide your improved version in Finnish inside the '@' tag like this: @Improved version in this tag@\\n" +
    "Grading examples:\\n" +
    "User: moi olen matti meikäläinen ja soitan s teille koska mää huppari on kadonnut ö se huppari on raidallinen ja tosi värikäs paljon eri värejä öö ja haluan kysyä ett onks se siellä jos se on voit voitteko soittaa minulle kohta kiitos\\n" +
    "System: [Explanation.] Excellent. @Improved version@\\n" +
    "User: hei olen matti meikäläinen kävin eilen illalla teiän kah ja on mulla on kadonnut huppari ö sellanen valkoinen huppari koko ämmä ja öö onko teillä löytyny\\n" +
    "System: [Explanation.] Good. @Improved version@\\n" +
    "User: hei maija täällä olimme eilen a teidän kahvilassa vierailemassa ja olen varma että jätin hupparini sinne a huppari on värikäs voisitteko katsoa asiaa\\n" +
    "System: [Explanation.] Excellent. @Improved version@\\n" +
    "User: hei öö nimeni on maija meikäläinen ja olin teillä eilen illalla kahvilassa ö olen huomannut että unohdin siellä mun hupparin oletteko te nähnyt sen se on öö punainen ja siinän paljon aa keltaisia valkoisia kukkia\\n" +
    "System: [Explanation.] Good. @Improved version@\\n" +
    "User: hei olen matti meikäläinen olen eile illalla kahvilassa öö öö hupparini on vihreä ja siinä on keltaista tekstiä\\n" +
    "System: [Explanation.] Partially. @Improved version@\\n" +
    "Grading Criteria:\\n" +
    "Excellent: Completes all aspects of the task, including both introduction and an attempt at describing the hoodie. There are no significant deficiencies in the response.\\n" +
    "Good: Completes the task, but there are some significant deficiencies in the response.\\n" +
    "Partially: Only partially answers the task, the response has many significant deficiencies.\\n" +
    "None: The response does not relate to the task at all.\\n" +
    "This is the description of the picture: The hoodie featured in the image is made up of vibrant stripes in a variety of colors, including red (or orange with the possibility of some pinkish tones), blue (or dark, dark blue), and green.\\n" +
    "This part of the description is used to identify the correct hoodie, but is not required for the task: It has a hood that is currently being worn, suggesting the hoodie is designed for warmth. The texture appears to be that of a knitted material, indicating it may be handcrafted or made from a thick, woven fabric. It's a pullover style without any visible zippers or buttons.";

    public const string ROOM_DESCRIPTION = 
    "The room appears to be a modern and cozy living room. The color palette is "
    + "dominated by soft, pleasant hues of green, orange, and light brown. Walls: "
    + "The walls are a light shade of green. On the left wall, there are several "
    + "framed artworks: A simple illustration of a pine tree. An artwork depicting "
    + "a fox's face. A shelf above the artworks holds: Two potted plants: one "
    + "resembling a small tree and the other a round topiary. A framed symbol, perhaps "
    + "a music note or a specific character. A standing white candle. An artwork of "
    + "a mountain or a slice of pie. A white figurine of a house. On the right wall, "
    + "there's a larger framed artwork with an illustration of the sun above hills or "
    + "mountains and the time '03:12' displayed below the sun. Adjacently, there's a "
    + "circular wall clock displaying a teal face with simple hour markers. Television "
    + "Console: Positioned centrally in the room is a sizable flat-screen TV atop an "
    + "orange wooden console. The console has two storage compartments and a central "
    + "area housing what seems to be a DVD or Blu-ray player. Couch & Ottoman (in Finnish: rahi): On the "
    + "left side, there's a teal sofa with an adjacent small wooden side table. On the "
    + "table is a green apple and a green lamp with a curved neck. Opposite the couch, "
    + "there's a teal ottoman (in Finnish: rahi), and atop it sits a white cat gazing towards the viewer. "
    + "Coffee Table: In front of the TV console is a wooden coffee table with a simple, "
    + "modern design. It has a single green remote control on it. Second Cat: A gray and "
    + "white cat with a blue collar sits on the floor to the left of the coffee table, "
    + "looking to its right. Door: On the far right, there's a wooden door with a small "
    + "window. Next to the door, on the wall, are light switches or buttons. Floor: The "
    + "flooring appears to be wooden with a checkered design. There's a white rug with "
    + "teal borders under the coffee table. Plants: A potted plant is placed on the right "
    + "side of the TV console. This plant has a green pot and looks like a small cactus "
    + "or a similar succulent.";
}