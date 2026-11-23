using System;
using SaySuomi.Managers;
using UnityEngine.Localization;

namespace SaySuomi.Screens
{
    /// <summary>
    /// Localized text for server requests, from the "ASAPanel Labels" table.
    ///
    /// Each call returns a new LocalizedString. LocalizedTextHelper subscribes to the instance
    /// it is given, so sharing one between two labels would update both.
    /// </summary>
    public static class ServerNotices
    {
        private static readonly Guid Table = new Guid("d3a4387299faa8b81aed682b3107b9e1");

        public static LocalizedString Loading() => Entry(2573893778890752);

        public static LocalizedString LoadingFailed() => Entry(2574131100999680);

        public static LocalizedString LoadingCompleted() => Entry(2574532344897536);

        /// <summary>
        /// Shows a failed request on a label. The server's own wording is the only thing that
        /// separates one 400 from another, so it is shown as it stands when there is no
        /// dedicated notice for the status.
        /// </summary>
        public static void Show(LocalizedTextHelper label, ServerError error)
        {
            if (HasOwnNotice(error.ResponseCode) || string.IsNullOrEmpty(error.ServerMessage))
            {
                label.SetText(ForStatus(error.ResponseCode));
                return;
            }

            label.ClearText();
            label.textComponent.text = error.ServerMessage;
        }

        private static bool HasOwnNotice(long responseCode)
        {
            return responseCode == 0
                || responseCode == 403
                || responseCode == 404
                || responseCode == 422
                || responseCode == 413
                || responseCode == 503;
        }

        private static LocalizedString ForStatus(long responseCode)
        {
            return responseCode switch
            {
                0 => Entry(6320358917496832), // No connection
                403 => Entry(6321177507225600), // No consent
                404 => Entry(6321228962947072), // Account not found
                422 => Entry(6321274882187264), // Version mismatch
                413 => Entry(6321523981901824), // Recording too long
                503 => Entry(6321580026191872), // Server busy
                400 => Entry(6321609004638208), // Bad request
                _ => Entry(6321645155344384), // Default
            };
        }

        private static LocalizedString Entry(long key) => new LocalizedString(Table, key);
    }
}
