using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhonemePanel : MonoBehaviour
{
    [SerializeField] GameObject phonemePanel;
        
    [SerializeField] TextMeshProUGUI explainText;
    [SerializeField] GameObject frontMouth1;
    [SerializeField] GameObject frontMouth2;
    [SerializeField] GameObject sideMouth1;
    [SerializeField] GameObject sideMouth2;
    [SerializeField] TextMeshProUGUI creditText;
    
    SOPhonemeHelper phonemeSO;
    // Start is called before the first frame update

    public void ShowPhonemePanel(string phoneme) {
        // Find the scriptable object related to the phoneme
        phonemeSO = Resources.Load<SOPhonemeHelper>(Const.PHONE_HELP_PATH + phoneme.ToLower());
        phonemePanel.SetActive(true);
    }

    public void OnVideoButtonClick()
    {
        Application.OpenURL(phonemeSO.videoURL);
    }

    void OnEnable()
    {        
        // In case the SO is null
        if (phonemeSO==null) phonemeSO = Resources.Load<SOPhonemeHelper>(Const.PHONE_HELP_PATH + "spacing");

        string phonemeString = TextUtils.WrapPhonemeSO(phonemeSO, Const.GOOD_COLOR, isBold:false);

        explainText.text = phonemeString  + " " + phonemeSO.instruction;
        frontMouth1.GetComponent<Image>().sprite = phonemeSO.front_1;
        frontMouth2.GetComponent<Image>().sprite = phonemeSO.front_2;
        sideMouth1.GetComponent<Image>().sprite = phonemeSO.side_1;
        sideMouth2.GetComponent<Image>().sprite = phonemeSO.side_2;
        phonemePanel.transform.GetComponent<Animation>().clip = phonemeSO.animaClip;
        phonemePanel.transform.GetComponent<Animation>().Play();

        creditText.text = "Top images: " + phonemeSO.front_credit + ".\nBottom images: " + phonemeSO.side_credit + ".";
    }
    
    void OnDisable()
    {
        phonemePanel.transform.GetComponent<Animation>().clip = null;
    }

    // Update is called once per frame
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
        phonemePanel.SetActive(false);
    }



    
}
