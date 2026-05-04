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
    private Sprite levelB1;

    [SerializeField]
    private Sprite levelB2;

    public void SetValue(float rating, int row)
    {
        int fillStars;

        if (rating >= 0 && rating < 1)
        {
            fillStars = 0;
            levels.sprite = levelA1;
        }
        else if (rating >= 1 && rating < 2)
        {
            fillStars = 1;
            levels.sprite = levelA2;
        }
        else if (rating >= 2 && rating < 3)
        {
            fillStars = 2;
            levels.sprite = levelB1;
        }
        else
        {
            fillStars = 3;
            levels.sprite = levelB2;
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
