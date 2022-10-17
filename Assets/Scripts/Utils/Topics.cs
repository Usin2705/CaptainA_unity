public enum Topics
{
    Easy,
    Normal,
    Hard,
    Sentences,
    General,
    Grocery,
    Business,
    Temp2,
    Temp3,
    Temp4,
    AudioBooks,
}

public static class TopicDetail
{
    public static string GetDetail (Topics topic) 
    {
        switch (topic) 
        {
            case Topics.Easy:
                return "Short words/sentence. There're almost no difficult alphabets in the samples.";
            case Topics.Normal:
                return "Longer words/sentence. There're almost no difficult pair in the samples.";
            case Topics.Hard:
                return "Very long words/sentence. Might have difficult pairs (a-ä) and double letters.";
            case Topics.Sentences:
                return "Sample is one or (maximum) two sentences that commonly use in Finland.";
            case Topics.General:
                return " ";                
            case Topics.Grocery:
                return "A list of words you should know when buying grocery in Finland";
            case Topics.Business:
                return "A list of words you should know when if you want to start business in Finland";
            case Topics.Temp2:
                return " ";
            case Topics.Temp3:
                return " ";   
            case Topics.Temp4:
                return " ";  
            case Topics.AudioBooks:
                return "Contain samples from Finnish book in Public Domain (no copyright). Always have audio samples from LibriVox.";                                                                                               
            default:
                return "";
        }
    }


}