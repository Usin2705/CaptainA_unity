using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReviewPanel : MonoBehaviour
{
    [SerializeField] 
    StarRatingRow fluencyRatingRow;

    void OnEnable()
    {
        //placeholder hard coded value
        float fluencyRating = 3.4f;
        fluencyRatingRow.SetRating(fluencyRating);
    }
}
