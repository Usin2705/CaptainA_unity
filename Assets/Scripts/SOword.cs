using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Word List", fileName = "Word List", order = 0)]
public class SOword : ScriptableObject 
{
    [TextArea]
    public string word;
    [TextArea]
    public string definition;
    [TextArea]
    public string credit;

    public Topics[] topicList;
    
    [SerializeField] public AudioClip sampleAudio; 
}