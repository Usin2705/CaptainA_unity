using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ASASecretCodePopUp : MonoBehaviour
{
    private const string SECRET_CODE = "dta2026test7765@mn";
    private const string PREF_ASA_VERIFIED = "ASASecretVerified";
    private const string PROMPT_TEXT =
        "If you know what you're doing, then please enter the secret text below:";
    private const string ERROR_TEXT = "Incorrect code. Try again.";

    [SerializeField]
    GameObject asaSecretPopUpPanelGO;

    [SerializeField]
    TMP_InputField secretCodeInputField;

    [SerializeField]
    TextMeshProUGUI promptLabel;

    [SerializeField]
    Button submitButton;

    [SerializeField]
    Button cancelButton;

    private System.Action onSecretCodeVerified;

    void Start()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitButtonClick);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClick);
    }

    public static bool IsASASecretVerified()
    {
        return PlayerPrefs.GetInt(PREF_ASA_VERIFIED, 0) == 1;
    }

    public void ShowSecretCodePopUp(System.Action onVerified)
    {
        if (IsASASecretVerified())
        {
            onVerified?.Invoke();
            return;
        }

        onSecretCodeVerified = onVerified;
        if (secretCodeInputField != null)
            secretCodeInputField.text = "";
        if (promptLabel != null)
            promptLabel.text = PROMPT_TEXT;
        if (asaSecretPopUpPanelGO != null)
            asaSecretPopUpPanelGO.SetActive(true);
    }

    public void OnSubmitButtonClick()
    {
        if (secretCodeInputField.text == SECRET_CODE)
        {
            PlayerPrefs.SetInt(PREF_ASA_VERIFIED, 1);
            PlayerPrefs.Save();

            ClosePopUp();
            onSecretCodeVerified?.Invoke();
        }
        else
        {
            if (promptLabel != null)
                promptLabel.text = ERROR_TEXT;
            secretCodeInputField.text = "";
        }
    }

    public void OnCancelButtonClick()
    {
        ClosePopUp();
    }

    private void ClosePopUp()
    {
        if (asaSecretPopUpPanelGO != null)
            asaSecretPopUpPanelGO.SetActive(false);
    }
}
