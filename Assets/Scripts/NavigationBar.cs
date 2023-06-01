using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavigationBar : MonoBehaviour
{

    [SerializeField] GameObject[] panels;
    // Panel orders:
    // 1: MainPanel (Freestyle)
    // 2: Topic
    // 3: Vocab
    // 4: Profile

    [SerializeField] GameObject[] tabs;
    // Tab orders:
    // 1: MainTab (Freestyle)
    // 2: Topic
    // 3: Vocab
    // 4: Profile

    [SerializeField] GameObject ExercisePanel;

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
        PopUpManager popUpPanel = GameObject.FindObjectOfType<PopUpManager>();
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
            // of MainPanel & TopicPanel (it hierachy is below TopicPanel)
            // We will also need to deactivate it when MainPanel or TopicPanel is active
            if (index !=1) ExercisePanel.SetActive(false);
            if (index !=2) SuperMemoPanel.SetActive(false);

            GameObject activeBar = tabs[index].transform.Find("ActiveBar").gameObject;
            activeBar.SetActive(panel == activePanel);
            panel.SetActive(panel == activePanel); 

        }
    }
}
