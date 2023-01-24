using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetailScorePanel : MonoBehaviour
{
    [SerializeField] GameObject detailScorePanel;    
    [SerializeField] PhonemePanel phonemePanel;  
    [SerializeField] GameObject resultPanelGO;
    [SerializeField] GameObject sampleButtonGO; // Replay sample audio
    [SerializeField] GameObject replayButtonGO; // Replay recorded audio
    [SerializeField] Animator animator; // Replay recorded audio

    [SerializeField] GameObject detailScorePrefab; // Refer to ListDetailPanel prefab
    [SerializeField] GameObject detailContent; // Content store the list of detail score
    [SerializeField] ScrollRect scrollRectWarning; // Use this to reset scroll of WarningScroll
    [SerializeField] ScrollRect scrollRectDetail; // Use this to reset scroll of DetailScoreScroll

    [SerializeField] TMPro.TextMeshProUGUI warningText; // Use this to reset scroll of DetailScoreScroll   

    int warningNo = 0; // number of warning of the ASRResult
    ASRResult asrResult;
    AudioClip sampleClip;
    AudioClip replayClip;
    bool isScrolledUp = false;

    List<GameObject> listScrollItems = new List<GameObject>(); // To store item from scroll list and delete after inactive
    
    public void ShowDetailScorePanel(string transcript, AudioClip _sampleClip, AudioClip _replayClip) 
    {
        // Set result text with color
        asrResult = NetworkManager.GetManager().asrResult;
        string textResult = TextUtils.FormatTextResult(transcript, asrResult.score);
        resultPanelGO.transform.Find("ResultText").GetComponent<TMPro.TextMeshProUGUI>().text = textResult;

        detailScorePanel.SetActive(true);

        // Set sampleclip and replay clip
        // Set SampleClip
        sampleClip = _sampleClip;

        if (sampleClip != null) {
            Debug.Log("Sample audio update");
            sampleButtonGO.SetActive(true);     

            Button sampleButton = sampleButtonGO.transform.GetComponent<Button>();               
            // Need to remove old OnClick Listeners, otherwise it will keep adding up
            sampleButton.onClick.RemoveAllListeners();       
            sampleButton.onClick.AddListener(() => AudioManager.GetManager().PlayAudioClip(sampleClip));            
        } else{
            sampleButtonGO.SetActive(false);
        }

        // Set replay clip
        replayClip = _replayClip;
        Button replayButton = replayButtonGO.transform.GetComponent<Button>();     
        // To be safe, remove all old listeners were add to this component
        replayButton.onClick.RemoveAllListeners();    
        if(replayClip!=null) {
            replayButton.onClick.AddListener(()=> AudioManager.GetManager().PlayAudioClip(replayClip));            
            replayButtonGO.SetActive(true);
        } else {
            replayButtonGO.SetActive(false);
        }

        warningNo = asrResult.warning.Count;
        warningText.text = WarningDetail.GetWarning(asrResult.warning);

        //################################### Initialize Detail Label prefab ################################### 
        List<float> scoreList = asrResult.score;
        for (int i = 0; i < asrResult.score.Count; i++) 
        {
            // Only display the BAD or AVG score to save space
            if (scoreList[i] < Const.AVG_SCORE) 
            {   
                string phoneme = transcript[i].ToString();
                
                // Skip if the phoneme is just space
                if (phoneme==" ") continue;

                SOPhonemeHelper phonemeSO = Resources.Load<SOPhonemeHelper>(Const.PHONE_HELP_PATH + phoneme.ToLower());

                GameObject detailScoreGO = Instantiate(detailScorePrefab, new Vector3(0,0,0), Quaternion.identity);
                detailScoreGO.transform.SetParent(detailContent.transform, false); // Register the big panel (ProfilePanel --> Content) as parent 

                // Find the Photo Button to register OnClick Function
                Button phonemeButton = detailScoreGO.transform.Find("ExplainPanel").transform.Find("ExplainButton").GetComponent<Button>();                        
                // Pass phoneme value to onClick function
                // To be safe, remove all old listeners were add to this component
                phonemeButton.onClick.RemoveAllListeners();    
                phonemeButton.onClick.AddListener(() => phonemePanel.ShowPhonemePanel(phoneme.ToLower()));

                string phoneTarget = TextUtils.WrapPhonemeSO(phonemeSO, Const.GOOD_COLOR, isBold:false);

                detailScoreGO.transform.Find("TargetActualPanel").
                              transform.Find("TargetPanel").
                              transform.Find("Phoneme").GetComponent<TMPro.TextMeshProUGUI>().text = phoneTarget;

                detailScoreGO.transform.Find("ExplainPanel").
                              transform.Find("ExplainText").GetComponent<TMPro.TextMeshProUGUI>().text = phonemeSO.instruction;                              

                // Check for substitution errors               
                // If there's substitutio errors in the ops
                // which mean user mispoken this phonename for others
                // then we can tell which phoneneme user made mistakes
                string actualColor = scoreList[i]<Const.BAD_SCORE?Const.BAD_COLOR:Const.AVG_COLOR;                        
                string phoneActualRate = scoreList[i]<Const.BAD_SCORE?Const.BAD_STRING:Const.AVG_STRING;                        
                string phoneActual = "";
                phoneActualRate = TextUtils.WrapTextColor(phoneActualRate, actualColor, isBold:false);

                foreach (OPS ops in asrResult.levenshtein)
                {
                    // TODO try to add more cases for our detection
                    // The replace check work best with equal length
                    // Incase we don't have equal length (there's deletion and insertion)
                    // they won't match 100% anymore
                    if (ops.tran_index==i && ops.ops=="replace" && transcript.Length == asrResult.prediction.Length)  
                    {
                        string phonemeSubs = asrResult.prediction[ops.pred_index].ToString();
                        SOPhonemeHelper phonemeSubsSO = Resources.Load<SOPhonemeHelper>(Const.PHONE_HELP_PATH + phonemeSubs.ToLower());                                                
                        phoneActual = TextUtils.WrapPhonemeSO(phonemeSubsSO, actualColor, isBold:false) + " ";
                        // If we found one we should stop looking
                        break;
                    }
                }

                detailScoreGO.transform.Find("TargetActualPanel").
                              transform.Find("ActualPanel").
                              transform.Find("Phoneme").GetComponent<TMPro.TextMeshProUGUI>().text = phoneActual + phoneActualRate;


                listScrollItems.Add(detailScoreGO);
            }
        }


        //######################################################################################################

        /*
        *   Since we do not destroy and recreate this Panel
        *   but only SetActive true and false. The position of ScrollRect
        *   don't reset each time. Therefore, we have to reset the scroll position
        *   to the default (top postion is 1.0f) each time we show it again
        */        
        scrollRectWarning.verticalNormalizedPosition = 1.0f;
        scrollRectDetail.verticalNormalizedPosition = 1.0f;
        detailScorePanel.SetActive(true);

        /*
        *
        *   There're two cases here:
        *   No warning --> default animation with warning box disable
        *   At least 1 warninig --> show warning box
        */
        if (warningNo == 0) {animator.Play("Default");}
        else {animator.Play("DefaultWarn");}
    }
    
    public void OnDetailScoreScroll(Vector2 value) 
    /*
    *   This function was attached to scrollRectDetail of detail score
    *   The idea is whenver user scroll up (y change from 1 to sth lower)
    *   we can move up the entire Panel so we can display more
    *   
    *   There're 3 options: 
    *   ScrollUp that only for case the result don't have warning        
    *   ScrollUpWarn that only for case the result have 1 warning       
    *   ScrollUpWarnLarge for at least 2 warning, the warning panel is expanded
    */

    {
        // Debug.Log(value.y);
        // Scroll down        
        if (value.y < 1.0f && !isScrolledUp && value.y!=0.0f) {
            isScrolledUp = true;
            if (warningNo == 0) animator.Play("ScrollUp");
            else if (warningNo == 1) animator.Play("ScrollUpWarn");
            else animator.Play("ScrollUpWarnLarge");            
        }
    }

    void Update()
    {
        // Handle back button press on phone
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    /*
    *   This function also attached to BackButton OnClick() in Unity
    */
    {   
        warningNo = 0;
        scrollRectWarning.verticalNormalizedPosition = 1.0f;
        scrollRectDetail.verticalNormalizedPosition = 1.0f;
        isScrolledUp=false;          
        detailScorePanel.SetActive(false);                
        
    }

    void OnDisable() 
    /*
    *   Need to Destroy all GO in the list to avoid create duplicate scorelist
    */
    {
        foreach (GameObject go in listScrollItems) 
        {
            Destroy(go);        
        }        
    }    
}

