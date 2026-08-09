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

    [SerializeField]
    private Sprite levelA1;

    [SerializeField]
    private Sprite levelA2;

    [SerializeField]
    private Sprite levelA2Plus;

    [SerializeField]
    private Sprite levelB1;

    /// <summary>
    /// Maps a score on the CEFR 0-6 axis to a star count and the matching level label.
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

        if (rating < Const.CEFR_A2)
        {
            fillStars = 0;
            levels.sprite = levelA1;
        }
        else if (rating < Const.CEFR_A2_PLUS)
        {
            fillStars = 1;
            levels.sprite = levelA2;
        }
        else if (rating < Const.CEFR_B1)
        {
            fillStars = 2;
            levels.sprite = levelA2Plus;
        }
        else
        {
            fillStars = 3;
            levels.sprite = levelB1;
        }

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
