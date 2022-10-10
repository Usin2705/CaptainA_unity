using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Phoneme Helper", fileName = "Phoneme Helper", order = 0)]
public class PhonemeHelperSO : ScriptableObject 
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
    
    /*
    *   Three option for animaClip:
    *   1: Fixed interval animation change pic every 1/4 sec
    *   2: Long Second animation change to second pic after 1/4 sec and keep it for 1sec
    *   3: No animation: in this case we can leave it empty (tested and work just fine)
    */
    
    [SerializeField] public AnimationClip animaClip; 
}