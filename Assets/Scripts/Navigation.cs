using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{

    [SerializeField] GameObject[] panels;
    // Panel orders:
    // 1: MainPanel (Freestyle)
    // 2: Topic
    // 3: Profile

    [SerializeField] GameObject[] tabs;
    // Tab orders:
    // 1: MainTab (Freestyle)
    // 2: Topic
    // 3: Profile

    public void NavigationBarClick(GameObject activePanel) 
    /*
    *  Can only be attached to OnClick with 1 variable,
    *  therefore we next tabs and index to address
    *  both the panel (active/inactive) and tab buttons
    *  (change color to show active/inactive)    
    */
    {
        foreach (GameObject panel in panels) 
        {
            int index = System.Array.IndexOf(panels, panel);
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
            
        }
    }
}