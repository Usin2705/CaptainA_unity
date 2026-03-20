using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
    [SerializeField]
    GameObject advancePanelGO;

    [SerializeField]
    GameObject taskPanelGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    GameObject backButtonGO;

    [SerializeField]
    GameObject task1ButtonGO;

    [SerializeField]
    GameObject task2ButtonGO;

    [SerializeField]
    GameObject task3ButtonGO;

    [SerializeField]
    GameObject task4ButtonGO;

    [SerializeField]
    GameObject task5ButtonGO;

    [SerializeField]
    ASAPanel ASAPanel;

    [SerializeField]
    GameObject settingsButtonGO;

    [SerializeField]
    GameObject settingsBackButtonGO;

    [SerializeField]
    GameObject settingsPopupGO;

    [SerializeField]
    GameObject dimPanelGO;

    [SerializeField]
    public TMP_Text guidTextGO;

    [SerializeField]
    GameObject copyGuidButtonGO;

    public string guid;

    void OnEnable()
    {
        settingsPopupGO.SetActive(false);
        dimPanelGO.SetActive(false);

        guid = PlayerPrefs.GetString("user_guid");
        guidTextGO.text = guid;

        backButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => advancePanelGO.SetActive(true));
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => taskPanelGO.SetActive(false));

        task1ButtonGO.GetComponent<Button>().onClick.AddListener(() => ASAPanelGO.SetActive(true));
        task1ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => taskPanelGO.SetActive(false));

        task1ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(0);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task2ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(1);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task3ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(2);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task4ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(3);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });
        task5ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                ASAPanel.DisplayTask(4);
                ASAPanelGO.SetActive(true);
                taskPanelGO.SetActive(false);
            });

        settingsButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                settingsPopupGO.SetActive(true);
                dimPanelGO.SetActive(true);
            });

        settingsBackButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                settingsPopupGO.SetActive(false);
                dimPanelGO.SetActive(false);
            });

        copyGuidButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = guid;
            });
    }
}
