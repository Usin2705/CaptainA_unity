using UnityEngine;

/// <summary>
/// The "there is something new here" dot shown on a navigation tab.
///
/// One integer per tab, holding the highest announcement that user has already been shown
/// there. The badge is on while that number is below the announcement currently being
/// made, and opening the tab raises it - so the dot appears once per user and clears
/// itself when they go and look, rather than on a timer or a dismiss button.
///
/// Storing a number rather than a flag is what makes this work for an existing user on
/// upgrade. A flag can only say "seen it", which is already true for anyone who ever
/// opened the tab; a number says "seen it up to here", and everything written by an older
/// build - including nothing at all - reads as 0. That covers the first launch of a fresh
/// install and an upgrade from any earlier version with the same comparison, so neither
/// case has to be detected and no version string is ever parsed.
///
/// To announce the next thing, raise the announcement constant in Const. Every user drops
/// below it again and the badge returns.
/// </summary>
public static class NewFeatureBadge
{
    /// <param name="prefKey">Per-tab key, so a change to one tab cannot clear another's badge.</param>
    /// <param name="announcement">What is being announced now - Const.VER_MAX_SHOW_ADVANCED and friends.</param>
    public static bool ShouldShow(string prefKey, int announcement)
    {
        return PlayerPrefs.GetInt(prefKey, 0) < announcement;
    }

    /// <summary>
    /// Records that this user has been shown the tab. Safe to call on every open: it only
    /// writes when the stored number would go up, so a user who has already seen a later
    /// announcement is never walked backwards into seeing an older badge again.
    /// </summary>
    public static void MarkSeen(string prefKey, int announcement)
    {
        if (PlayerPrefs.GetInt(prefKey, 0) >= announcement)
        {
            return;
        }

        PlayerPrefs.SetInt(prefKey, announcement);
        PlayerPrefs.Save();
    }
}
