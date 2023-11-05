using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the navigation bar within the game, handling the activation and deactivation of panels and tabs.
/// This class is responsible for the UI logic related to the navigation bar which includes changing the 
/// appearance of tabs and managing different panels such as MainPanel, Vocab, Others, and Profile.
/// <br/>
/// To change the order or add new panels and tabs, you need to change the order of the panels and tabs
/// in the inspector. The order of the panels and tabs must be the same. For example, the first panel in the
/// panels array must be the first tab in the tabs array.
/// <br/>
/// You also need to change the OnClick event of the tabs in the inspector to call the NavigationBarClick
/// method with the corresponding panel as the parameter.
/// </summary>
public class NavigationBar : MonoBehaviour
{
    /// <summary>
    /// Array of panel GameObjects in the UI.
    /// The panels are ordered as follows:
    /// 1. MainPanel (Freestyle)
    /// 2. Vocab
    /// 3. Others
    /// 4. Profile
    /// </summary>
    [SerializeField] GameObject[] panels;

    /// <summary>
    /// Array of tab GameObjects in the UI.
    /// The tabs are ordered as follows:
    /// 1. MainTab (Freestyle)
    /// 2. Vocab
    /// 3. Others
    /// 4. Profile
    /// </summary>
    [SerializeField] GameObject[] tabs;

    public void NavigationBarClick(GameObject activePanel) 
    /*
    *  Can only be attached to OnClick with 1 variable,
    *  therefore we loop tabs and index to address
    *  both the panel (active/inactive) and tab buttons
    *  (change color to show active/inactive)    
    */

    {
        // Disable the instruction panel first if it's active
        // Other Panel can open the instruction panel if they need to
        PopUpManager popUpPanel = GameObject.FindAnyObjectByType<PopUpManager>();
        popUpPanel.DisablePanel();

        foreach (GameObject panel in panels) 
        {
            int index = System.Array.IndexOf(panels, panel);

            GameObject activeBar = tabs[index].transform.Find("ActiveBar").gameObject;
            activeBar.SetActive(panel == activePanel);
            panel.SetActive(panel == activePanel); 

        }
    }
}
