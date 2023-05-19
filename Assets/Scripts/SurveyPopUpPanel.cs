using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurveyPopUpPanel : MonoBehaviour
{
    [SerializeField] GameObject surveyPopUpPanelGO;    

    public void OnNoButtonClick() {
        PlayerPrefs.SetInt(Const.PREF_NO_SURVEY, 1);
		PlayerPrefs.Save();
        surveyPopUpPanelGO.SetActive(false);
    }

    public void OnLaterButtonClick() {
        surveyPopUpPanelGO.SetActive(false);
    }

    public void OnYesButtonClick() {
        Application.OpenURL(Const.SURVEY_URL);
        PlayerPrefs.SetInt(Const.PREF_SURVEY_V1_DONE, 1);
		PlayerPrefs.Save();
        surveyPopUpPanelGO.SetActive(false);
    }

    void Update()
    {
        // Handle back button press on phone
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            surveyPopUpPanelGO.SetActive(false);
        }
    }
}
