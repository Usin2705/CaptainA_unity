using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{    
    static AudioManager audioManager;
    [SerializeField] AudioClip recordNotif;
    
    int frequency = 16000;
    int maxRecTime = 5;

    AudioSource audioSource;
    AudioClip recording;

	void Awake() {
		if (audioManager != null) {
			Debug.LogError("Multiple AudioManagers");
			return;
		}
		audioSource = GetComponent<AudioSource>();
		audioManager = this;
	}

	public static AudioManager GetManager() {
		return audioManager;
	}

    public void RecordSound(){        
        audioSource.PlayOneShot(recordNotif,0.5f);
        recording = Microphone.Start("", false, maxRecTime, frequency);
    }

    public void StopAndReplay(){
        Microphone.End("");        
        Invoke(nameof(ReplayRecordedSample), 0.5f);
    }

    void ReplayRecordedSample(){
        audioSource.clip = recording;
        audioSource.Play();
    }
}


