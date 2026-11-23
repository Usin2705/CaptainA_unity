namespace SaySuomi.Managers
{
    // Deliberately not [System.Serializable]. Unity cannot serialize a ValueTuple, so the
    // attribute did nothing but raise a warning per field. This is a carrier: built in
    // AdvancedScreen.GetUserInput, handed to NetworkManager, never shown in the Inspector or saved.
    public class BackgroundFormData
    {
        public (string, string) gender;
        public (string, string) age;
        public (string, string) motherTongue;
        public (string, string) otherLanguages;
        public (string, string) selfAssessment;
    }
}
