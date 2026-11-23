namespace SaySuomi.Managers
{
    /// <summary>
    /// What a failed DTA request reported, for a screen to turn into a message.
    /// </summary>
    public sealed class ServerError
    {
        public ServerError(long responseCode, string serverMessage)
        {
            ResponseCode = responseCode;
            ServerMessage = serverMessage;
        }

        public long ResponseCode { get; }

        // The server's own wording, or null when the body was not the usual envelope.
        public string ServerMessage { get; }
    }
}
