using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Word List", fileName = "Word List", order = 0)]
public class SOword : ScriptableObject 
{
    public string word;
    public string meaning;
    public string credit;

    public Topics[] topicList;
    
    [SerializeField] public AudioClip sampleAudio; 
}