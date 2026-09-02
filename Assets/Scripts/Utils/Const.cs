using System.Collections.Generic;
using UnityEngine;

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

    // ===================== UI FONT SIZE =====================
    // The sizes above, as constants so new UI stops guessing.
    //
    // These are Canvas units, not points or dp. They only make sense against the
    // CanvasScaler reference resolution, which is 900 x 1600 with ScreenMatchMode
    // MatchWidthOrHeight. If that reference ever changes, every one of these changes with
    // it. Counted across MainScene, existing text sits at 28-44 with 32 by far the most
    // common, so anything in the teens or low twenties is half-scale and will look wrong.
    //
    // The same factor applies to geometry: against this reference a comfortable tap
    // target is ~100 units tall, not ~50, and a 24-unit icon is too small to see.

    public const float FONT_TITLE = 40f; // screen and modal headings
    public const float FONT_HEADING = 36f; // sub-headings, primary button labels
    public const float FONT_BODY = 32f; // default readable text, the workhorse size
    public const float FONT_INFO = 30f; // explanations, instructions, consent statements
    public const float FONT_SMALL = 20f; // credits and footnotes only

    // Minimum comfortable tap target height, same units.
    public const float TAP_TARGET_HEIGHT = 100f;

    // =======================================================

    // ===================== CEFR SCALE =====================
    // The numeric axis the DTA server returns proficiency and the analytic dimensions on.
    // This is a shared contract - the server must use the same numbers for the same
    // labels or the stars will disagree with the scores. See docs/TO_FRONTEND.md item 13.
    //
    //   < A1 = 0 | A1 = 1 | A2 = 2 | A2+ = 2.5 | B1 = 3 | B2 = 4 | C1 = 5 | C2 = 6
    //
    // Note the model cannot resolve above B1+ (3.5), so B2 and up are unreachable in
    // practice today - which is why the star scale tops out at B1.

    public const float CEFR_BELOW_A1 = 0f;
    public const float CEFR_A1 = 1f;
    public const float CEFR_A2 = 2f;
    public const float CEFR_A2_PLUS = 2.5f;
    public const float CEFR_B1 = 3f;
    public const float CEFR_B2 = 4f;
    public const float CEFR_C1 = 5f;
    public const float CEFR_C2 = 6f;

    // =======================================================

    // ================ CEFR BAND BOUNDARIES ================
    // Where one displayed band ends and the next begins. NOT the same numbers as the scale
    // above, and conflating the two is what went wrong before: the anchor point for A2 is
    // 2.0, but the score at which a learner starts being *called* A2 is 1.55.
    //
    //   A1  < 1.55 | A2  < 2.40 | A2+ < 2.75 | B1 >= 2.75
    //
    // Server v1.3.0 moved these down from 2.0 / 2.5 / 3.0. The old cuts put 50.7% of
    // learners in A1 and 0.6% in B1, which is not a usable label.
    //
    // **These are a fallback and nothing more.** FeedbackRow reads the band from the
    // server's own cefr_label_fine; this copy is only reached when a response carries no
    // label at all. They will move again as the study collects data, and a build holding
    // stale numbers has no way to know it - which is exactly why the label is authoritative
    // and these are the last resort.

    public const float CEFR_BAND_A2 = 1.55f;
    public const float CEFR_BAND_A2_PLUS = 2.40f;
    public const float CEFR_BAND_B1 = 2.75f;

    // =======================================================

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

    // Microphone.Start returns a clip immediately, but the device is not delivering
    // samples yet - it takes a moment to spin up, and longer the first time after launch
    // while the audio stack is cold. Whatever sits at the head of the clip until then is
    // not the learner: it is the artefact heard as a click at the start of the first few
    // recordings. Two numbers deal with it.

    // How long to keep waiting for the device to produce its first sample before giving up
    // and reporting that recording could not start. Generous - a slow device should still
    // record, and the only cost of waiting is the moment before Stop becomes live.
    public const float MIC_START_TIMEOUT_SECS = 1f;

    // How much of the front of the recording to throw away once it is over. Waiting for
    // the first sample proves the device is running but says nothing about the quality of
    // what it produced getting there, so the head is discarded outright. Recording begins
    // well after the button is pressed, so this cannot reach a learner's first syllable.
    public const int MIC_WARMUP_DISCARD_MS = 100;

    public const int MAX_REC_NUMBERGAME_EASY = 3;
    public const int MAX_REC_NUMBERGAME_MEDIUM = 4;
    public const int MAX_REC_NUMBERGAME_HARD = 5;
    public const int MAX_REC_NUMBERGAME_RANK = 4;

    public const int NUMBERGAME_PREPARE_TIME = 2;

    public const string REPLAY_FILENAME = "recorded_speech";
    public const string DESCRIBE_FILENAME = "recorded_describe_speech";

    public const string NUMBERGAME_FILENAME = "recorded_number_game";

    public const string ASA_FILENAME = "recorded_ASA_speech";

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
    public const int TIME_OUT_SECS = 40;
    public const int TIME_OUT_ADVANCE_SECS = 30;

    // Timeout for POST /speech/assess on the DTA server.
    // The server abandons the scorer at 60s and answers with a retryable 503, so the
    // client has to stay connected longer than that or it aborts first and the user
    // sees a generic transport error instead of the 503 we can retry.
    // 60s server budget + margin for upload and response on mobile data.
    // This number is shared with the backend - see docs/TO_FRONTEND.md item 5.
    public const int TIME_OUT_ASA_SECS = 90;

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
    public static readonly string[] VOCABS_LIST =
    {
        "a",
        "ä",
        "b",
        "d",
        "e",
        "f",
        "h",
        "i",
        "j",
        "k",
        "l",
        "m",
        "n",
        "o",
        "ö",
        "p",
        "r",
        "s",
        "t",
        "u",
        "v",
        "y",
        " ",
        "å",
        "c",
        "g",
        "q",
        "w",
        "x",
    };

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

    // Highest announcement this user has acknowledged for the Advanced tab, written when
    // they open it. Deliberately not PREF_INS_ADVANCE: that one is already set to 1 for
    // everyone who has ever opened the tab - the exact people a new feature needs to reach
    // - and PopUpManager.FinnishPanel writes 1 back into it every time the instruction
    // popup closes, which would undo anything stored here.
    public const string PREF_SEEN_ADVANCED = "pref_seen_advanced";

    // =======================================================

    // ===================== FLASH CARD CONST =====================

    // Create a const dictionary for the flashcard deck
    public static readonly Dictionary<string, string> FLASH_CARD_DICT = new Dictionary<
        string,
        string
    >
    {
        { "FlashCard_OS_FIN_ENG", "FI-EN_OmaSuomi_1_CaptainA" },
        { "FlashCard_OS_ENG_FIN", "EN-FI_OmaSuomi_1_CaptainA" },
        { "FlashCard_HA_FIN_FIN", "FI-FI_HA_Hagaga" },
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
    public const string INSTRUCTION_MAIN =
        "Practice your pronunciation by typing any words or sentences in the box. The model doesn't work well with short words or spoken Finnish.\n"
        + "<b><color=#ff0000ff>Red </color></b>: flawed pronunciation. \n"
        + "<b><color=#ffa500ff>Yellow</color></b>: almost correct, or the AI model is not confident due to unfamiliar accents.\n"
        + "<b><color=#0000ffff>Blue</color></b>: correct pronunciation. \n\n"
        + "You can click on the result for more details. \n\n"
        + "Our current model is continually improving. If you consistently have flawed pronunciation, you should seek advice from a Finnish teacher. We will develope better models in the future.\n";
    public const string INSTRUCTION_VOCABULARY =
        "This mode has flashcards (similar to Anki) to help learn vocabulary. \n\n"
        + "You can also practice pronunciation with the <b>recording button</b> "
        + "and the audio generated by AI. \n\n"
        + "For each card, you can rate the difficulty from Again to Easy. "
        + "The easier you remember the card, the longer it will be hidden. "
        + "Your task is to learn 20 new cards a day. You can long press on the deck to learn more.\n\n"
        + "We welcome volunteers to help translate the cards "
        + "into other languages. Please get in touch with us if you are interested.";
    // Shown once, the first time the Advanced tab is opened. Deliberately short: it only
    // has to orient someone who has just arrived, and NEW_VERSION_TEXT already carries the
    // detail about the models, the project and the privacy notice.
    //
    // PuheNumero keeps one sentence rather than being dropped. Both features have a button
    // on that screen, so naming only one would leave a first-time user wondering what the
    // other does.
    public const string INSTRUCTION_ADVANCE =
        "The Automatic Speaking Assessment estimates your Finnish speaking level. "
        + "Choose a task, record your answer, and you will get an overall level plus "
        + "scores for fluency, pronunciation, accuracy and range.\n\n"
        + "You can also practise speaking numbers with PuheNumero.";
    public const string INSTRUCTION_PROFILE =
        "Your profile shows the average score you got for each phone. <b>Click</b> on the label "
        + "for instruction (text, photo, and video) on how to pronounce the phoneme correctly. \n\n"
        + "While our model does not always provide accurate results, the statistics here are reliable. "
        + "We suggest you seek advice from your Finnish teachers to improve your pronunciation. \n\n"
        + "Some phones can be more complicated than others, especially the Finnish <b>R</b>. "
        + "So please don't be frustrated if you get a low score. The point of this app is to "
        + "encourage you to practise speaking more.";
    public const string INSTRUCTION_PHONE =
        "The phone helper contains text, photos, and videos to help you practice your pronunciation. "
        + "Pay attention to your mouth and tongue position. You can also click on the video icon to see "
        + "the video instruction made by a Finnish teacher. \n\n"
        + "At the moment, we only focus on Finnish vowels. \n\n"
        + "The materials in this app are made by members of the Kielibuusti project. "
        + "We will add more materials in the future if it is available.";

    public const int APP_VERSION = 3;

    // The badge on the Advanced tab shows while PREF_SEEN_ADVANCED is below this number,
    // and opening the tab stores this number - so it appears once per user, per bump, and
    // stays until they actually go and look.
    //
    // Raise it to announce the next thing. A fresh install and an upgrade from an older
    // build both read 0, so neither needs detecting: no version string is parsed anywhere,
    // and this is the only line to touch. Independent of APP_VERSION, which paces the
    // what's-new popup instead.
    public const int VER_MAX_SHOW_ADVANCED = 4;
    public const string NEW_VERSION_TEXT =
        "Hi everyone! We have added a new <b>Automatic Speaking Assessment</b> function in the <b>Advanced</b> tab.\n\n"
        + "Using <b>Whisper</b> and <b>Qwen 3.5</b>, it estimates your Finnish speaking proficiency and provides four analytic scores: fluency, pronunciation, accuracy, and range.\n\n"
        + "The feature is the result of the <b>DigiTala in Action</b> project, a collaboration between <b>Aalto University</b>, "
        + "<b>University of Jyväskylä</b>, and <b>University of Helsinki</b>.\n\n"
        + "By using this function, your speaking data will also help us improve automatic speaking assessment for Finnish learners. "
        + "You can read the privacy notice before using it. Thank you for supporting our research!";

    // ====================================================================

    // ================= ASA USER-FACING TEXT =================
    // Every string the Automatic Speaking Assessment shows a learner, kept together so a
    // translator can be handed one block rather than sent hunting through the panels.
    //
    // Anything with {0}/{1} is a string.Format template - keep the placeholders when
    // translating, and note that word order around them may need to change per language.

    // --- relevance notices, shown above the score rows (FeedbackPanel) ---

    // DO NOT REWORD ASA_OFF_TOPIC OR ASA_PARTIAL. Both were written deliberately and
    // settled after several rounds. A server release changing what triggers a verdict is
    // not a reason to touch what the learner reads - raise it and leave the strings alone.

    // The recording did not address the task. "Check" not "listen": the task is written
    // text, there is nothing to play.
    public const string ASA_OFF_TOPIC =
        "Your answer did not seem to match the task. Please check the task again.";

    // The recording addressed the task only in part. These scores are real measurements,
    // so this is a nudge, not a correction - it must not read as "your result is void".
    // The second sentence is deliberate: relevance is judged by an LLM, which does get it
    // wrong, and a learner who answered well has no way to argue with a notice that
    // sounds certain.
    public const string ASA_PARTIAL =
        "Your answer only partly matched the task, so these scores may not reflect your "
        + "true level. This check is automatic and can be wrong.";

    // --- transcript (FeedbackPanel) ---

    public const string ASA_TRANSCRIPT_EMPTY = "(nothing was recognised in this recording)";

    // --- cohort position (ASAProfilePanel) ---
    // The server buckets the position and sends only the bucket; these render it. Do not
    // add a threshold or a rounding rule here - the ladders are retuned server-side as the
    // cohorts grow, and that has to reach installed apps without a client release.
    //
    // Nothing is shown at all for the bottom half of a cohort, so none of these strings
    // needs a "not ranked" variant.

    // {0} = bucket: 1, 5, 10, 25 or 50. {1} = CEFR band.
    public const string ASA_RANK_TOP_PERCENT = "You are in the top {0}% of {1} learners";

    // {0} = CEFR band.
    public const string ASA_RANK_WITHIN_LEVEL = "Your position among other {0} learners";

    // {0} = bucket: 2, 3, 5, 10, 25, 50 or 100. Rank 1 uses ASA_RANK_FIRST instead.
    public const string ASA_RANK_TOP_RANK = "Top {0}";

    public const string ASA_RANK_FIRST = "#1";

    // Shown when the server sends no position at all, which happens for the bottom half of
    // a cohort. Two things it must not do, and between them they rule out almost every
    // obvious sentence:
    //
    //   - It must not hint at where they sit. Any explanation of the absence amounts to
    //     "you are not in the top 50%", which is the message the bucketing exists to stop.
    //   - It must not carry a count of users. How many learners we have is not something
    //     the app discloses, so cohort_size is off limits even though the payload has it.
    //
    // What is left is a statement about the learner's own result and nothing else. It is
    // thin on purpose - the alternative was an empty panel that read as a broken screen.
    // One per box, so both stay filled and the panel keeps its shape rather than opening a
    // hole where the position used to be.
    public const string ASA_RANK_NO_POSITION = "Keep practising to see how you compare.";

    // {0} = CEFR band.
    public const string ASA_RANK_NO_POSITION_LABEL =
        "Keep practising to see your position among other {0} learners.";

    // --- ranking unavailable (ASAProfilePanel) ---
    // Three different reasons, and only the first is something the learner can act on, so
    // it is the only one phrased as an instruction.

    // {0} = required assessments, {1} = how many they have done.
    public const string ASA_RANK_NEED_MORE_TASKS =
        "Please complete at least {0} tasks to see your ranking. You have completed {1} "
        + "out of {0} so far.";

    // Depends on how many other learners share their level, so it is not phrased as
    // something they can fix.
    public const string ASA_RANK_COHORT_TOO_SMALL =
        "There are not enough learners at your level yet for comparison. Please check "
        + "back later.";

    public const string ASA_RANK_UNAVAILABLE =
        "Your ranking is not available at the moment. Please try again later.";

    // Fallback when the server returns a status we do not recognise yet.
    public const string ASA_RANK_UNAVAILABLE_GENERIC =
        "Your ranking is not available at the moment.";

    // --- level change (ASAProfilePanel) ---
    // Not user-facing text, but the two values the Advance and Revert buttons are built
    // on, kept beside the strings they govern.

    // The server CHECKs users.cefr_level against exactly these five, and the onboarding
    // form offers the same five, so a user can start anywhere on this ladder. The order
    // is the ladder: Advance and Revert are one step along it.
    public static readonly string[] ASA_LEVELS = { "A1", "A2", "B1", "B2", "C1_plus" };

    // Advance stops here. Revert has no ceiling of its own - it is offered from anywhere
    // except A1 - so someone who self-assessed as B2 or C1+ can walk down the ladder but
    // cannot climb back above B1.
    public const string ASA_ADVANCE_CEILING = "B1";

    // How far up their own level's cohort a learner has to be before Advance is offered.
    // Compared against the raw `percentile` from the comparison response, which is never
    // shown as a number - only this gate and the level ring read it.
    //
    // Set to match the server's `display.top_percent` ladder, whose buckets are 1, 5, 10,
    // 25 and 50. At 0.75 the button appears exactly when the badge says "top 25%", so what
    // the learner is told and what the screen offers them agree. A value between two
    // buckets - 0.8, say - would leave two learners both shown "top 25%" with different
    // buttons and nothing on screen to explain the difference.
    //
    // Relaxed from 0.9, which gated the feature to a tenth of each cohort. Going up is the
    // reversible direction: Revert is ungated and one tap away, so offering it too readily
    // costs a learner nothing they cannot undo, while offering it too rarely leaves them
    // stuck in a cohort they have outgrown.
    public const float ASA_ADVANCE_MIN_PERCENTILE = 0.75f;

    // How many completed tasks between asks for overall feedback.
    //
    // There are five tasks, so this is two full passes: by then a learner has seen every
    // task and repeated them, which is the first point they have something to say. Asking
    // every 5 caught them the moment they finished their first pass, with the feature
    // barely met. Raising it further trades away responses - four passes is more than most
    // testers reach, and someone never asked leaves no feedback at all.
    //
    // A backlog, not a birthday: the test is how many tasks have been done SINCE the last
    // ask, so a learner who passed the mark without ever opening the task list is still
    // caught the next time they do. The count restarts whenever they answer or decline.
    public const int ASA_OVERALL_FEEDBACK_EVERY = 10;

    // The TasksSent value when overall feedback was last put in front of this learner,
    // whether they answered it or dismissed it. Replaces the old OverallFeedbackSent flag,
    // which could not express this: it was cleared after every upload, so a plain
    // "have we asked?" boolean re-armed itself on the very next task.
    public const string PREF_OVERALL_FEEDBACK_ASKED_AT = "pref_overall_feedback_asked_at";

    // Backstop only. A blank Secret.ASA_SET_LEVEL_URL hides both buttons, so in practice
    // nobody reaches a state where this needs showing.
    public const string ASA_LEVEL_UNAVAILABLE =
        "Changing your level is not available at the moment.";

    // --- data deletion (ASAProfilePanel) ---

    public const string ASA_DELETE_WARNING =
        "This will permanently delete your Automatic Speaking Assessment data, including "
        + "your recordings, scores, and assessment account, from this device and our "
        + "servers.\n\n"
        + "This action cannot be undone. We will not be able to recover the deleted data "
        + "for you.";

    public const string ASA_DELETE_IN_PROGRESS = "Deleting your data...";

    // Only when the server confirmed the erase.
    public const string ASA_DELETE_CONFIRMED =
        "Your data has been deleted. You can start again from the beginning at any time.";

    // Also used when the request did not reach the server: the local data is gone either
    // way and the deletion is retried on every launch, so the outcome is the same from
    // the user's side.
    public const string ASA_DELETE_RECEIVED =
        "Your request has been received and your data is being removed.";

    public const string ASA_DELETE_CANCEL_LABEL = "Cancel";
    public const string ASA_DELETE_CLOSE_LABEL = "Close";

    // ====================================================================
}
