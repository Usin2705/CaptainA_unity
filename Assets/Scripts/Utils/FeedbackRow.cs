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

    // The four bands the server floors every label into, in order. The position in this
    // array IS the star tier, which is what keeps the label and the stars from ever
    // disagreeing: they are read off the same index.
    //
    // There is no <A1, no A1+/B1+, and nothing above B1 - the model clips at B1+ (3.5), so
    // a B2 tier would be unreachable UI.
    static readonly string[] BANDS = { "A1", "A2", "A2+", "B1" };

    /// <summary>
    /// Shows one dimension as a star tier plus its CEFR badge.
    /// </summary>
    ///
    /// <param name="labelFine">
    /// The server's own label for this dimension - `cefr_label_fine` for proficiency,
    /// `dimension_labels.&lt;name&gt;.label_fine` for the other four.
    ///
    /// **This is the value that decides what is shown.** The client used to derive the
    /// band from the score with its own thresholds, which meant the rule lived in two
    /// codebases and could drift apart silently. It did: v1.3.0 moved the boundaries to
    /// A1 &lt;1.55, A2 &lt;2.40, A2+ &lt;2.75 because the old cuts put half of all
    /// learners in A1, and they are expected to move again as the study collects data. A
    /// client deriving its own band would now disagree with the row stored against the
    /// learner - without throwing, without logging.
    /// </param>
    ///
    /// <param name="rating">
    /// Only a fallback, for a response that carries no label: an older server, or a shape
    /// we failed to parse. It uses Const.CEFR_BAND_*, which track the server's current
    /// cuts but will go stale the next time those move - a last resort to avoid a blank
    /// row, not a second opinion.
    /// </param>
    ///
    /// <param name="row">Picks the star colour - row 1 is blue, the rest yellow.</param>
    public void SetValue(float rating, int row, string labelFine = null)
    {
        // Filled stars beyond the always-on first one, so 0 here means 1 star on screen.
        //
        // NOTE the off-by-one: the first star is always lit and lives outside the `stars`
        // array, which holds only stars two to four. fillStars is therefore one LESS than
        // the count on screen - 3 filled here reads as 4 stars. The array is meant to have
        // 3 elements; do not "fix" it to 4.
        //
        // One star is the floor: every recording that scores at all is at least A1, and an
        // all-empty row would read as "we failed to score you" rather than "you are a
        // beginner".
        int fillStars;
        string level;

        int band = System.Array.IndexOf(BANDS, (labelFine ?? "").Trim());

        if (band >= 0)
        {
            fillStars = band;
            level = BANDS[band].Replace("+", "_plus").ToLowerInvariant();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(labelFine))
            {
                // A band we do not know. Worth a shout: it means the server added one and
                // this row is about to draw the wrong thing from the number instead.
                Debug.LogWarning(
                    $"FeedbackRow: unrecognised CEFR label '{labelFine}'. Falling back to "
                        + "the score, whose thresholds are the outdated pre-v1.3.0 ones."
                );
            }

            if (rating < Const.CEFR_BAND_A2)
            {
                fillStars = 0;
                level = "a1";
            }
            else if (rating < Const.CEFR_BAND_A2_PLUS)
            {
                fillStars = 1;
                level = "a2";
            }
            else if (rating < Const.CEFR_BAND_B1)
            {
                fillStars = 2;
                level = "a2_plus";
            }
            else
            {
                fillStars = 3;
                level = "b1";
            }
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
