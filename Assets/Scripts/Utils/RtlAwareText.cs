using RTLTMPro;
using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro UI text that also displays Persian correctly. Right-to-left text is shaped and
/// ordered by RTLTMPro; text in every other language passes through unchanged.
///
/// Differences from RTLTMPro's own RTLTextMeshPro:
/// - Any text containing Persian or Arabic letters is fixed, not only text whose first letter is
///   one. The CEFR labels ("A1 (مبتدی)") start with a Latin letter.
/// - <see cref="text"/> returns the text as it was set, not the reordered display form, so code
///   that copies one label into another (NetworkManager.lastError) does not fix it twice.
/// - Text typed into the normal TextMeshPro inspector box is kept. RTLTextMeshPro would replace
///   it with its own stored copy on the next refresh.
/// - In play mode, left-aligned text is right-aligned while it shows right-to-left text.
/// </summary>
[ExecuteAlways]
public class RtlAwareText : RTLTextMeshPro
{
    [SerializeField]
    [Tooltip(
        "Right-align this text in play mode while it shows right-to-left text. "
            + "Off for the language dropdown, whose list stays left-aligned."
    )]
    bool mirrorAlignmentForRtl = true;

    bool ltrAlignmentKnown;
    HorizontalAlignmentOptions ltrAlignment;

    public override string text
    {
        get => string.IsNullOrEmpty(originalText) ? base.text : originalText;
        set
        {
            bool rtl = ContainsRtl(value);
            if (rtl && parseCtrlCharacters)
            {
                value = UnescapeControlCharacters(value);
            }
            ForceFix = rtl;
            // Keep digits as the translator wrote them: "1." and "A1" stay Western, "۱۰۰"
            // stays Persian. RTLTMPro's default turns Western digits into Persian ones;
            // PreserveNumbers keeps them but mis-orders Persian digits ("۱۰۰" -> "۰۰۱"), so it
            // is only used when the text has no Persian digits. No translation mixes both.
            PreserveNumbers = rtl && !ContainsNativeDigits(value);
            base.text = value;
            MatchAlignment();
        }
    }

    // The string tables write line breaks as a literal backslash-n, which TextMeshPro turns into
    // a line break while parsing. RTLTMPro reverses the text before that, so "\n" would reach
    // TextMeshPro as "n\" and show up as text. Turn the escapes into real characters first.
    static string UnescapeControlCharacters(string value)
    {
        if (value.IndexOf('\\') < 0)
        {
            return value;
        }
        return value.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
    }

    protected override void OnEnable()
    {
        SyncOriginalWithDisplayed();
        base.OnEnable();
        MatchAlignment();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SyncOriginalWithDisplayed();
        base.OnValidate();
    }
#endif

    // m_text is what is displayed. While that is plain left-to-right text it is also the source
    // text (typed in the inspector, or overridden on a prefab instance), so keep RTLTMPro's
    // stored copy in step with it.
    void SyncOriginalWithDisplayed()
    {
        if (!ContainsRtl(m_text))
        {
            originalText = m_text;
        }
    }

    void MatchAlignment()
    {
        if (!mirrorAlignmentForRtl || !Application.isPlaying)
        {
            return;
        }
        if (!ltrAlignmentKnown)
        {
            ltrAlignment = horizontalAlignment;
            ltrAlignmentKnown = true;
        }
        HorizontalAlignmentOptions wanted =
            isRightToLeftText && ltrAlignment == HorizontalAlignmentOptions.Left
                ? HorizontalAlignmentOptions.Right
                : ltrAlignment;
        if (horizontalAlignment != wanted)
        {
            horizontalAlignment = wanted;
        }
    }

    // Persian (U+06F0-06F9) or Arabic-Indic (U+0660-0669) digits.
    static bool ContainsNativeDigits(string value)
    {
        foreach (char c in value)
        {
            if (c is >= '۰' and <= '۹' or >= '٠' and <= '٩')
            {
                return true;
            }
        }
        return false;
    }

    static bool ContainsRtl(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }
        foreach (char c in value)
        {
            // Qualified: the project has its own global TextUtils, which would win.
            if (RTLTMPro.TextUtils.IsRTLCharacter(c))
            {
                return true;
            }
        }
        return false;
    }
}
