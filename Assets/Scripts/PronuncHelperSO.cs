using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Phoneme Helper", fileName = "Phoneme Helper", order = 0)]
public class PhonemeHelper : ScriptableObject 
{
    public string phoneme;
    public string ipa;
    public string instruction;
    public string videoURL;    
    public string front_credit;
    public string side_credit;    
    public GameObject phonemePanelPrefab;
}