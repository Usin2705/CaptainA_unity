using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvancePanel : MonoBehaviour
{
    [SerializeField] GameObject advancePanelGO;
    [SerializeField] GameObject describePanelAGO;
    [SerializeField] GameObject describePanelBGO;
    [SerializeField] GameObject describePanelCGO;

    List<GameObject> listScrollItems = new List<GameObject>();            

    void OnEnable() 
    {
        describePanelAGO.SetActive(false);
        describePanelBGO.SetActive(false);
        describePanelCGO.SetActive(false);

        // Check if the instruction panel has been shown before
        if (!PlayerPrefs.HasKey(Const.PREF_INS_ADVANCE)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
            popUpPanel.OpenPanel(Const.PREF_INS_ADVANCE);
            popUpPanel.SetText(Const.INSTRUCTION_ADVANCE);
        }
    }

    public void OnDescribeAButtonClicked() 
    {
        describePanelAGO.SetActive(true);
    }

    public void OnDescribeBButtonClicked() 
    {
        describePanelBGO.SetActive(true);
    }

    public void OnDescribeCButtonClicked() 
    {
        describePanelCGO.SetActive(true);
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
