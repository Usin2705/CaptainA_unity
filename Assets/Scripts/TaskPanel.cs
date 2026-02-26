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

    void OnEnable()
    {
        backButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => advancePanelGO.SetActive(true));
        backButtonGO.GetComponent<Button>().onClick.AddListener(() => taskPanelGO.SetActive(false));

        task1ButtonGO.GetComponent<Button>().onClick.AddListener(() => ASAPanelGO.SetActive(true));
        task1ButtonGO
            .GetComponent<Button>()
            .onClick.AddListener(() => taskPanelGO.SetActive(false));
    }
}
