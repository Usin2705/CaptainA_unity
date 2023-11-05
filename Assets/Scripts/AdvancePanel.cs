using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvancePanel : MonoBehaviour
{
    [SerializeField] GameObject advancePanelGO;
    [SerializeField] GameObject describePanelGO;

    List<GameObject> listScrollItems = new List<GameObject>();            

    void OnEnable() 
    {
        describePanelGO.SetActive(false);

        // Check if the instruction panel has been shown before
        if (!PlayerPrefs.HasKey(Const.PREF_INS_ADVANCE)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
            popUpPanel.OpenPanel(Const.PREF_INS_ADVANCE);
            popUpPanel.SetText(Const.INSTRUCTION_ADVANCE);
        }
    }

    public void OnDescribeButtonClicked() 
    {
        describePanelGO.SetActive(true);
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
