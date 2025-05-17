using UnityEngine;
using UnityEngine.UI;

public class PopUpManager : MonoBehaviour
{
    [SerializeField] GameObject panel; // Assign your panel GameObject in the Inspector
    [SerializeField] TMPro.TextMeshProUGUI messageText; // Assign your Text component in the Inspector
    [SerializeField] Button okButton; // Assign your Button component in the Inspector

    string prefsKey;

    void Start()
    {
        // Set the panel to inactive at the start
        panel.SetActive(false);

        // Add listener to the OK button
        okButton.onClick.AddListener(OnOkButtonClick);
    }

    public void OpenPanel(string _prefsKey)
    {
        panel.SetActive(true);
        prefsKey = _prefsKey;
    }

    public void DisablePanel()
    {
        panel.SetActive(false);
    }


    public void FinnishPanel()
    {
        panel.SetActive(false);
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();
    }

    public void SetText(string message)
    {
        messageText.text = message;
    }

    public void OnOkButtonClick()
    {
        FinnishPanel();
    }
}