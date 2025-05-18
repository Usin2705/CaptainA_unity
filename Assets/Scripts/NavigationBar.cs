using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

    private float swipeThreshold = 100f; // Minimum swipe distance to be considered a swipe
    private float thresholdFraction = 0.12f;
    private Vector2 startPos;
    private bool swipeHandled;
    private int currentPanelIndex = 0;
    private int previousPanelIndex = 0;


    void Awake()
    {
        swipeThreshold = Mathf.Min(Screen.width, Screen.height) * thresholdFraction;
    }

    void Update()
    {
    // Check for swipe right to left gesture using 
    // the new Input System
    // This is only for touch devices, and not for UnityEditor
    #if UNITY_ANDROID || UNITY_IOS
        TouchControl touch = Touchscreen.current?.primaryTouch;
        if (touch == null) return;

        // Check if the touch is in the first phase (pressed)
        if (touch.press.wasPressedThisFrame)
        {
            startPos = touch.position.ReadValue();
            swipeHandled = false;
        }
        // Check if the touch is in the e2nd phase (pressed and moved)
        // Finger is still down and hasn't triggered a swipe yet
        else if (touch.press.isPressed && !swipeHandled)
        {
            // Check if the touch is moved
            Vector2 delta = touch.position.ReadValue() - startPos;

            /* -------------------------------------------------------------------
            *  Dynamically pick the swipe threshold.
            *  If we are on the LAST panel (scroll view) we make the gesture
            *  harder to activate by doubling the normal threshold.
            * ------------------------------------------------------------------*/
            float effectiveThreshold = swipeThreshold;
            if (currentPanelIndex == panels.Length - 1)   // last tab?
            {
                effectiveThreshold *= 3.5f; // double the threshold
            }

            // Big enough horizontal move?  If yes, treat as a swipe.
            if (Mathf.Abs(delta.x) > effectiveThreshold)
            {
                swipeHandled = true;            // Avoid multi-fires
                if (delta.x < 0)    GoRight();  // finger moved left  → next tab right
                else                GoLeft();   // finger moved right → previous tab
            }
        }
    #endif
    }

    /// <summary>
    /// Handles the swipe left - go right gesture.
    /// This method is called when the user swipes left on the screen.
    /// It will navigate to the next panel in the array.
    /// </summary>
    void GoRight()
    {
        // Check if we are not on the last panel
        int target = Mathf.Min(panels.Length - 1, currentPanelIndex + 1);
        NavigationBarClick(panels[target]);
    }

    /// <summary>
    /// Handles the swipe right - go left gesture.
    /// This method is called when the user swipes right on the screen.
    /// It will navigate to the previous panel in the array.    
    /// </summary>
    void GoLeft()
    {
        // Check if we are not on the first panel        
        int target = Mathf.Max(0, currentPanelIndex - 1);
        NavigationBarClick(panels[target]);
    }

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
        PopUpManager popUpPanel = FindAnyObjectByType<PopUpManager>();
        popUpPanel.DisablePanel();

        // We don't need to loop through all the panels
        // just find the index of the active panel
        // and set the previous panel to inactive, and active the current panel
        int index = System.Array.IndexOf(panels, activePanel);
        previousPanelIndex = currentPanelIndex;
        currentPanelIndex = index;

        // Set the previous panel to inactive
        panels[previousPanelIndex].SetActive(false);

        // Change the source image of the tab button to show active/inactive
        ChangeSourceImage(previousPanelIndex, false);

        // Set the current panel to active
        activePanel.SetActive(true);

        ChangeSourceImage(currentPanelIndex, true);
    }

    private void ChangeSourceImage(int index, bool isActive)
    {
        // Change the source image of the tab button to show active/inactive
        // The name of the source image is ic_[name]_[active/inactive]
        // [name] is the name of the tab button            
        Image tabImage = tabs[index].GetComponent<Image>();
        string tabName = tabs[index].name;
        string sourceImageName = "ic_" + tabName + "_" + (isActive ? "active" : "inactive");
        Sprite sourceImage = Resources.Load<Sprite>("app_icons/" + sourceImageName);
        if (sourceImage != null)
        {
            tabImage.sprite = sourceImage;
        }
        else
        {
            Debug.LogError("Source image not found: " + sourceImageName);
        }
    }   
}
