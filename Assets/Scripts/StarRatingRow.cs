using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarRatingRow : MonoBehaviour
{
    public Image[] stars;
    public Sprite emptyStar;
    public Sprite fullStar;
    public Sprite partialStar;

    public void SetRating(float rating)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            if (rating >= i + 1)
            {
                stars[i].sprite = fullStar;
            }
            else if (rating > i && rating < i + 1)
            {
                stars[i].sprite = partialStar;
            }
            else
            {
                stars[i].sprite = emptyStar;
            }
        }
    }
}
