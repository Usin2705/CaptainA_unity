using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Onboarding : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] GameObject[] onboard1;
    [SerializeField] GameObject[] onboard2;
    [SerializeField] GameObject[] onboard3;
    [SerializeField] GameObject[] onboard4;

    [SerializeField] GameObject onboardPanel;
    [SerializeField] GameObject mainPanel;
    [SerializeField] PhonemePanel phonemePanel;
    [SerializeField] GameObject backButton;
    [SerializeField] GameObject nextButton;

    int onboard_index = 0;

    void Start() {
        ToggleOnboard();  
    }

    public void NextOnboard() {
        if (onboard_index >= 3) FinishOnboard();
    
        else 
        {
            onboard_index += 1;
            ToggleOnboard();   
        }                   
    }

    public void PreviousOnboard() {
        onboard_index -= 1;
        if (onboard_index < 0) onboard_index = 0;

        nextButton.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "NEXT";
        ToggleOnboard();                                
    }

    public void FinishOnboard() {
        PlayerPrefs.SetInt(Const.PREF_FIRST_KEY, 1);
        PlayerPrefs.Save();
        foreach (GameObject go in onboard4) go.SetActive(false);
        onboardPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void ToggleOnboard() {
        backButton.SetActive(onboard_index>0);     

        switch (onboard_index) 
        {
            case 0:
                foreach (GameObject go in onboard1) go.SetActive(true);
                foreach (GameObject go in onboard2) go.SetActive(false);
                foreach (GameObject go in onboard3) go.SetActive(false);
                foreach (GameObject go in onboard4) go.SetActive(false);
                break;                
            
            case 1:
                foreach (GameObject go in onboard1) go.SetActive(false);
                foreach (GameObject go in onboard2) go.SetActive(true);
                foreach (GameObject go in onboard3) go.SetActive(false);
                foreach (GameObject go in onboard4) go.SetActive(false);

                break;
            
            case 2:
                foreach (GameObject go in onboard1) go.SetActive(false);
                foreach (GameObject go in onboard2) go.SetActive(false);
                foreach (GameObject go in onboard3) go.SetActive(true);
                foreach (GameObject go in onboard4) go.SetActive(false);

                break;

            case 3:
                nextButton.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "FINISH";
                phonemePanel.ShowPhonemePanel("a");
                foreach (GameObject go in onboard1) go.SetActive(false);
                foreach (GameObject go in onboard2) go.SetActive(false);
                foreach (GameObject go in onboard3) go.SetActive(false);
                foreach (GameObject go in onboard4) go.SetActive(true);

                break;
            
            default:
                break;
        }
    }
}
