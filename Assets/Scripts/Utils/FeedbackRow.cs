using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackRow : MonoBehaviour
{
    [SerializeField]
    private Image[] stars;

    [SerializeField]
    private Sprite emptyStar;

    [SerializeField]
    private Sprite filledStarBlue;

    [SerializeField]
    private Sprite filledStarYellow;

    [SerializeField]
    private Image levels;

    // Level icons are loaded by name instead of being wired in the Inspector. There are
    // five of these rows, so a serialised field per level meant twenty references to keep
    // in sync, and a missed one showed up as a blank label at runtime with no error - the
    // exact gap that left levelA2Plus unassigned when the A2+ tier was added.
    //
    // Adding a level is now: drop ic_level_<name>.png into Resources/app_icons and add a
    // branch below. Nothing to wire.
    //
    // This requires the icons to be Sprite Mode: SINGLE. Resources.Load<Sprite> returns
    // null for a Multiple texture, because its sprites are sub-assets rather than the
    // asset itself. Most icons in that folder are Multiple; the ic_level_* set was
    // deliberately converted for this.
    const string LEVEL_ICON_PATH = "app_icons/ic_level_";

    // Resources.Load hits the disk cache on every call, and SetValue runs for five rows
    // on every result, so the lookups are cached here. Static: the sprites are shared
    // assets, identical for every row.
    static readonly Dictionary<string, Sprite> levelIcons = new Dictionary<string, Sprite>();

    static Sprite LevelIcon(string level)
    {
        if (levelIcons.TryGetValue(level, out Sprite cached))
        {
            return cached;
        }

        Sprite loaded = Resources.Load<Sprite>(LEVEL_ICON_PATH + level);
        if (loaded == null)
        {
            Debug.LogError(
                $"FeedbackRow: no sprite at Resources/{LEVEL_ICON_PATH}{level}. "
                    + "Check the file exists and its Sprite Mode is Single, not Multiple."
            );
        }

        // Cached even when null, so a missing icon logs once rather than every frame.
        levelIcons[level] = loaded;
        return loaded;
    }

    /// <summary>
    /// Maps a score on the CEFR 0-6 axis to a star count and the matching level label.
    ///
    /// PLANNED FOR THE NEXT UPDATE - display the server's cefr_label_fine instead of
    /// deriving the label here. The server already derives it from the same score, so
    /// having the rule in two codebases means it can silently drift: move the A2+
    /// boundary off 2.5 server-side and this row keeps the old one, showing a label that
    /// contradicts the score.
    ///
    /// The data is there: v1.2.0 added dimension_labels alongside cefr_label_fine, so all
    /// five rows could read a server label today. Not switched in this release because it
    /// is a UI change we are not making now. See docs/TO_FRONTEND.md item 13.
    ///
    /// Note the thresholds below do NOT disappear after that switch: the star tier is
    /// still computed from the number, so these boundaries must keep matching the
    /// server's or the stars will disagree with the label sitting next to them.
    ///
    /// The star boundaries ARE the CEFR boundaries, so the label and the stars cannot
    /// drift apart - both are set together in one branch:
    ///
    ///   below A2   (&lt; 2.0)       1 star    A1
    ///   A2         (2.0 - 2.5)    2 stars   A2
    ///   A2+        (2.5 - 3.0)    3 stars   A2+
    ///   B1 and up  (&gt;= 3.0)      4 stars   B1
    ///
    /// NOTE the off-by-one: the first star is always lit and lives outside this array, so
    /// `stars` holds only stars two to four. fillStars is therefore one LESS than the
    /// count the user sees - 3 filled here reads as 4 stars on screen. The array is meant
    /// to have 3 elements; do not "fix" it to 4.
    ///
    /// One star is the floor: every recording that scores at all is at least A1, and an
    /// all-empty row would read as "we failed to score you" rather than "you are a
    /// beginner".
    ///
    /// B2 and above are deliberately absent. The model clips at B1+ (3.5), so nothing
    /// above B1 can be reached - a B2 label would be unreachable UI.
    /// </summary>
    public void SetValue(float rating, int row)
    {
        // Filled stars beyond the always-on first one, so 0 here means 1 star on screen.
        int fillStars;
        string level;

        if (rating < Const.CEFR_A2)
        {
            fillStars = 0;
            level = "a1";
        }
        else if (rating < Const.CEFR_A2_PLUS)
        {
            fillStars = 1;
            level = "a2";
        }
        else if (rating < Const.CEFR_B1)
        {
            fillStars = 2;
            level = "a2_plus";
        }
        else
        {
            fillStars = 3;
            level = "b1";
        }

        levels.sprite = LevelIcon(level);

        for (int i = 0; i < stars.Length; i++)
        {
            if (i < fillStars)
                if (row == 1)
                    stars[i].sprite = filledStarBlue;
                else
                    stars[i].sprite = filledStarYellow;
            else
                stars[i].sprite = emptyStar;
        }
    }
}
