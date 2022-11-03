public enum Topics
{
    Easy,
    Normal,
    Hard,
    Sentences,
    Greetings,
    Grocery,
    Business,
    Pair,
    Vocabulary,
    AudioBooks,
    All,
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
                return "One or (maximum) two Finnish sentences. Often have audio samples.";
            case Topics.Greetings:
                return "Common Finnish greetings.";                
            case Topics.Grocery:
                return "A list of words you should know when buying grocery in Finland";
            case Topics.Business:
                return "A list of words you should know if you want to work or start business in Finland";
            case Topics.Pair: 
                return "A pair of words with similar pronunciation. For example: a - ä, o - ö";
            case Topics.Vocabulary:
                return "All single, simple words. Useful to learn new vocabulary while practice pronunciation";                   
            case Topics.AudioBooks:
                return "Contain samples from Finnish book in Public Domain (no copyright). Always have audio samples from LibriVox.";                                                                                               
            case Topics.All:
                return "Contain all samples in the app.";                                                                                               
            default:
                return "";
        }
    }
}