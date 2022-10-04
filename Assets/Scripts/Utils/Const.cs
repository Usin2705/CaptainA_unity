public static class Const
{
    // ================= TEXR COLOR SCORE ===================
    // Rich text color tag for each type of scoring
    public const string BAD_COLOR = "#ff0000ff";    
    public const string AVG_COLOR = "#ffa500ff";
    public const string GOOD_COLOR = "#0000ffff";    

    // Score range (less than) for each type of scoring
    public const float BAD_SCORE = 0.49f;
    public const float AVG_SCORE = 0.92f;
    //public const float GOOD_SCORE = 0.92f;

    // Decay rate allow users to update score with more weight put on 
    // latest score and not the historical average
    // 0.3f mean 30% weight on average + 70% on new score
    public const float DECAY_RATE = 0.3f; 
    // =======================================================


    
    // ===================== AUDIO CONST =====================
    public const int FREQUENCY = 16000;

    // Maximum recording time per seconds
    // The extra record time after the button release is trimmed
    public const int MAX_REC_TIME = 5;
    // =======================================================



    // ====================== TAB COLOR ======================
    public const Color32 IMAGE_ACTIVE_COLOR = new Color32(22,42,82,255);
    public const Color32 IMAGE_INACTIVE_COLOR = new Color32(105,105,105,255);

    public const Color32 BG_ACTIVE_COLOR = new Color32(200,200,200,255);
    public const Color32 BG_INACTIVE_COLOR = new Color32(230,230,230,255);
    // =======================================================
}