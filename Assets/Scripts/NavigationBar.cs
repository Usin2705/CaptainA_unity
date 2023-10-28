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

    [SerializeField] GameObject DescribePanel;

    [SerializeField] GameObject SuperMemoPanel;

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
            // This change the image color (the icon)
            // and the tab background to show active or inactive
            // The new Android material design use a small green bar
            // instead, so we do so by set the ActiveBar active/inactive

            /*            
            Image image = tabs[index].transform.Find("Image").GetComponent<Image>();
            Image image_bg = tabs[index].GetComponent<Image>();

            if (panel == activePanel) 
            {
                image.color = Const.IMAGE_ACTIVE_COLOR;
                image_bg.color = Const.BG_ACTIVE_COLOR;
                panel.SetActive(true);    
            } else {
                image.color = Const.IMAGE_INACTIVE_COLOR;
                image_bg.color = Const.BG_INACTIVE_COLOR;
                panel.SetActive(false);
            }
            */

            // Disable other extra panel before swap panel
            //  There're aditional panel (ExercisePanel) that will show on top
            //  of MainPanel (it hierachy is below MainPanel)
            //  We will also need to deactivate it when MainPanel is active

            
            // There're aditional panel (SuperMemoPanel) that will show on top
            // of MainPanel (its hierachy is below MainPanel)
            // We will also need to deactivate it when MainPanel is active            
            if (index !=1) SuperMemoPanel.SetActive(false);

            // There're aditional panel (DescribePanel) that will show on top
            // of SuperMemoPanel (its hierachy is below SuperMemoPanel)
            // We will also need to deactivate it when SuperMemoPanel is active
            if (index !=2) DescribePanel.SetActive(false);

            GameObject activeBar = tabs[index].transform.Find("ActiveBar").gameObject;
            activeBar.SetActive(panel == activePanel);
            panel.SetActive(panel == activePanel); 

        }
    }
}
