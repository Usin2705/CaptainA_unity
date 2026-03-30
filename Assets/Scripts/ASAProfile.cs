using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ASAPRofilePanel : MonoBehaviour
{
    [SerializeField]
    GameObject levelBarGO;

    [SerializeField]
    GameObject levelTextGO;

    void OnEnable()
    {
        UpdateLevelBar();
    }

    public void UpdateLevelBar()
    {
        levelBarGO.GetComponent<Image>().fillAmount = 0.7f;
    }
}
