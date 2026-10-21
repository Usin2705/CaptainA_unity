using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Helper class for localizing UI text components.
/// </summary>
public class LocalizedTextHelper
{
    private LocalizedString currentString;
    private readonly TextMeshProUGUI textComponent;

    public LocalizedTextHelper(TextMeshProUGUI targetString)
    {
        textComponent = targetString;
    }

    public void SetText(LocalizedString newString)
    {
        // Remove listener from previous run
        if (currentString != null)
        {
            currentString.StringChanged -= UpdateText;
        }

        currentString = newString;

        if (currentString != null)
        {
            currentString.StringChanged += UpdateText;
            currentString.RefreshString();
        }
    }

    private void UpdateText(string localizedText)
    {
        if (textComponent != null)
        {
            textComponent.text = localizedText;
        }
    }

    public void ClearText()
    {
        if (currentString != null)
        {
            currentString.StringChanged -= UpdateText;
            currentString = null;
        }
    }
}
