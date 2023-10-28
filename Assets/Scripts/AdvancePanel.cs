using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvancePanel : MonoBehaviour
{
    [SerializeField] GameObject advancePanelGO;
    [SerializeField] GameObject exerPanelGO;

    List<GameObject> listScrollItems = new List<GameObject>();        
    

    // Start is called before the first frame update
    void OnEnable() 
    {
        if (!PlayerPrefs.HasKey(Const.PREF_INS_ADVANCE)) {
            PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
            popUpPanel.OpenPanel(Const.PREF_INS_ADVANCE);
            popUpPanel.SetText(Const.INSTRUCTION_ADVANCE);
        }
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
