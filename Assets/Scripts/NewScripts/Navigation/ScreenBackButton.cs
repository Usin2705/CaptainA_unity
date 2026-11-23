using UnityEngine;
using UnityEngine.UI;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// Adds a back button to a screen that has none, for the screens whose ShowsBackButton is
    /// true. iPhones have no back key, so without one the only way out would be the tab bar.
    /// </summary>
    internal static class ScreenBackButton
    {
        // Top-left, centred in the 130-unit header these screens share.
        private static readonly Vector2 Position = new Vector2(61f, -65f);

        /// <param name="template">An existing back button to copy, so they all look the same.</param>
        public static void AddTo(AppScreen screen, GameObject template)
        {
            GameObject button = Object.Instantiate(template, screen.transform, false);
            button.name = "SharedBackButton";

            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = Position;

            // Last, so nothing in the screen draws over it or takes its taps.
            rect.SetAsLastSibling();

            ButtonWiring.Replace(
                button.GetComponent<Button>(),
                () => Navigator.GetManager().Back()
            );
        }
    }
}
