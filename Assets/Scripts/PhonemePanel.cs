using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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
    /*
    *   This function also attached to VideoButton OnClick() in Unity
    */
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

        if (!PlayerPrefs.HasKey(Const.PREF_INS_PHONE)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();        
            popUpPanel.OpenPanel(Const.PREF_INS_PHONE);
            popUpPanel.SetText(Const.INSTRUCTION_PHONE);
        }
    }
    
    void OnDisable()
    {
        phonemePanel.transform.GetComponent<Animation>().clip = null;
        
        PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
        popUpPanel.DisablePanel();
    }

    // Update is called once per frame
    void Update()
    {
        // Set focus to the panel
        // so that the back button won't go back to the URL
        EventSystem.current.SetSelectedGameObject(phonemePanel, null);

        // Handle back button press on new Input System
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
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
