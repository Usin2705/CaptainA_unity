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

public static class Const
{
    // ================= TEXR COLOR SCORE ===================
    // Rich text color tag for each type of scoring
    public const string BAD_COLOR = "#ff0000ff";    
    public const string AVG_COLOR = "#ffa500ff";
    public const string GOOD_COLOR = "#0000ffff";    

    // Score range (less than) for each type of scoring
    public const float BAD_SCORE = 0.49f;
    public const float AVG_SCORE = 0.90f;
    //public const float GOOD_SCORE = 0.92f;

    // Weight % allow users to update score with more weight put on 
    // latest score and not the historical average
    // 0.7f mean 30% weight on average + 70% on new score
    public const float SCORE_WEIGHT = 0.7f; 
    // =======================================================


    
    // ===================== AUDIO CONST =====================
    public const int FREQUENCY = 16000;

    // Maximum recording time per seconds
    // The extra record time after the button release is trimmed
    public const int MAX_REC_TIME = 10;
    // =======================================================



    // ====================== TAB COLOR ======================
    public static readonly Color32 IMAGE_ACTIVE_COLOR = new Color32(22,42,82,255);
    public static readonly Color32 IMAGE_INACTIVE_COLOR = new Color32(105,105,105,255);

    public static readonly Color32 BG_ACTIVE_COLOR = new Color32(200,200,200,255);
    public static readonly Color32 BG_INACTIVE_COLOR = new Color32(255,255,255,255);
    // =======================================================
// mic color: 13E365    

    // ==================== RESOURCES PATH ====================
    public const string PHONE_HELP_PATH = "ScriptableObjects/PhonemeHelpers/";
}