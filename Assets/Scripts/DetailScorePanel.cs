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
    ASRResult asrResult;
    AudioClip sampleClip;
    AudioClip replayClip;
    
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

        detailScorePanel.SetActive(true);
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
        detailScorePanel.SetActive(false);
    }
}

