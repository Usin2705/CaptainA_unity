using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhonemePanel : MonoBehaviour
{
    [SerializeField] GameObject phonemePanel;
    [SerializeField] GameObject backButton;

    
    [SerializeField] TextMeshProUGUI explainText;
    [SerializeField] GameObject frontMouth1;
    [SerializeField] GameObject frontMouth2;
    [SerializeField] GameObject sideMouth1;
    [SerializeField] GameObject sideMouth2;
    [SerializeField] TextMeshProUGUI creditText;
    
    PhonemeHelperSO phonemeSO;
    // Start is called before the first frame update

    public void ShowPhonemePanel(string phoneme) {
        // Find the scriptable object related to the phoneme
        phonemeSO = Resources.Load<PhonemeHelperSO>(Const.PHONE_HELP_PATH + phoneme.ToLower());
        phonemePanel.SetActive(true);
    }

    public void OnVideoButtonClick()
    {
        Application.OpenURL(phonemeSO.videoURL);
    }

    void OnEnable()
    {
        // In case the SO is null
        if (phonemeSO==null) phonemeSO = Resources.Load<PhonemeHelperSO>(Const.PHONE_HELP_PATH + "spacing");

        explainText.text = phonemeSO.phoneme + " /" + phonemeSO.ipa + "/ "  + phonemeSO.instruction;
        frontMouth1.GetComponent<Image>().sprite = phonemeSO.front_1;
        frontMouth2.GetComponent<Image>().sprite = phonemeSO.front_2;
        sideMouth1.GetComponent<Image>().sprite = phonemeSO.side_1;
        sideMouth2.GetComponent<Image>().sprite = phonemeSO.side_2;

        creditText.text = "Top images: " + phonemeSO.front_credit + ". Bottom images: " + phonemeSO.side_credit;
    }

    // Update is called once per frame
    void OnDisable()
    {
        
    }

    void Update()
    {
        // Handle back button press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        phonemePanel.SetActive(false);
    }



    
}
