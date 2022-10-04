using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Phoneme Helper", fileName = "Phoneme Helper", order = 0)]
public class PhonemeHelper : ScriptableObject 
{
    public string phoneme;
    public string ipa;
    public string instruction;
    public string videoURL;    
    public string front_credit;
    public string side_credit;    
    [SerializeField] public Sprite front_1;
    [SerializeField] public Sprite front_2;
    [SerializeField] public Sprite side_1;
    [SerializeField] public Sprite side_2;
}