using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetailScorePanel : MonoBehaviour
{
    [SerializeField] GameObject detailScorePanel;    
    [SerializeField] GameObject resultPanelGO;
    [SerializeField] GameObject sampleButtonGO; // Replay sample audio
    [SerializeField] GameObject replayButtonGO; // Replay recorded audio
    [SerializeField] Animator animator; // Replay recorded audio

    [SerializeField] ScrollRect scrollRectWarning; // Use this to reset scroll of WarningScroll
    [SerializeField] ScrollRect scrollRectDetail; // Use this to reset scroll of DetailScoreScroll

    [SerializeField] TMPro.TextMeshProUGUI warningText; // Use this to reset scroll of DetailScoreScroll   

    int warningNo = 0; // number of warning of the ASRResult
    ASRResult asrResult;
    AudioClip sampleClip;
    AudioClip replayClip;
    bool isScrolledUp = false;
    
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
        // Scroll down        
        if (value.y < 0.95f && value.y >=0.5f && !isScrolledUp) {
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
        isScrolledUp=false;          
        detailScorePanel.SetActive(false);                
        
    }

    
}

