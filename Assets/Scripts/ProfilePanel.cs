using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfilePanel : MonoBehaviour
{
    [SerializeField] GameObject phonemeScorePrefab;
    [SerializeField] GameObject content;

    List<GameObject> listItems = new List<GameObject>();                

    // const float EXTRA_PADDING = 50.0f;

    void OnEnable() 
    // Is called when SetActive(true)
    // The opposite is OnDisable() when SetActive(false)
    {
        UserData userData = SaveData.LoadFromJson();
        
        // Use Sort for sorting from small to large
        // Reverse() for large to small
        userData.phonemeScores.Sort();                
        
        foreach (PhonemeScore phonemeScore in userData.phonemeScores) 
        {
            string phoneme = phonemeScore.phoneme!=" "?phonemeScore.phoneme:"Spacing";            
            string noTries = "Time practice: " + phonemeScore.no_tries.ToString();   
            string score = ((int) (phonemeScore.average_score*100)).ToString() + "%";            

            string phoneColor = TextColoring.GOOD_COLOR;
            if (phonemeScore.average_score < TextColoring.BAD_SCORE)  phoneColor = TextColoring.BAD_COLOR; 
            else if (phonemeScore.average_score < TextColoring.AVG_SCORE) phoneColor = TextColoring.AVG_COLOR;                        
            score = "<color=" + phoneColor + ">" + score + "</color>";         

            GameObject phonemeScoreGO = Instantiate(phonemeScorePrefab, new Vector3(0,0,0), Quaternion.identity);
            phonemeScoreGO.transform.SetParent(content.transform, false);

            phonemeScoreGO.transform.Find("ScorePanel").
                           transform.Find("PhonemeBG").
                           transform.Find("Phoneme").GetComponent<TMPro.TextMeshProUGUI>().text = phoneme;

            phonemeScoreGO.transform.Find("ScorePanel").
                           transform.Find("Score").GetComponent<TMPro.TextMeshProUGUI>().text = score;

            phonemeScoreGO.transform.Find("NoTries").GetComponent<TMPro.TextMeshProUGUI>().text = noTries;

            listItems.Add(phonemeScoreGO);

            //phonemeScore.average_score;
            //phonemeScore.phoneme;
            //phonemeScore.no_tries;
        }
        // float prefabHeight = phonemeScorePrefab.transform.Find("ScorePanel").GetComponent<RectTransform>().sizeDelta.y;
        // prefabHeight += phonemeScorePrefab.transform.Find("NoTries").GetComponent<RectTransform>().sizeDelta.y;
        
        // content.GetComponent<RectTransform>().
        //         SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
        //                                   prefabHeight*(userData.phonemeScores.Count+1) + EXTRA_PADDING);


    }

    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        foreach (GameObject go in listItems) 
        {
            Destroy(go);
        }
    }

    public void OnListItemClick(GameObject listItemGO) 
    {
        Debug.Log("AAA");
    }
}
