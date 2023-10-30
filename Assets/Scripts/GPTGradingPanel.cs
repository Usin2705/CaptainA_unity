using UnityEngine;

public class GPTGradingPanel : MonoBehaviour
{
    [SerializeField] GameObject gptGradingPanel;    
    [SerializeField] TMPro.TextMeshProUGUI transcriptText;    
    [SerializeField] TMPro.TextMeshProUGUI gptGradingText;       
    
    public void ShowGPTGradingPanel() 
    {
        string transcript = NetworkManager.GetManager().chatGPTTranscript;
        string grading = NetworkManager.GetManager().chatGPTGrading;
        
        transcriptText.text = transcript;
        gptGradingText.text = grading;

        gptGradingPanel.SetActive(true);
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
        gptGradingPanel.SetActive(false); 
    }
}

