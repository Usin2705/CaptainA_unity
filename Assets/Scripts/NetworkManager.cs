using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;


public class NetworkManager : MonoBehaviour
{
    [SerializeField] GameObject buttonRecordGO;     
	static NetworkManager netWorkManager;

    void Awake() {
		if (netWorkManager != null) {
			Debug.LogError("Multiple NetWorkManagers");
			Destroy(gameObject);
			return;
		}
		netWorkManager = this;
	}
    
	public static NetworkManager GetManager() {
		return netWorkManager;
	}

    void OnDestroy() {

	}

    public IEnumerator ServerPost(string transcript, byte[] wavBuffer)//, TextMeshPro errorText) 
    {
	    WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"speech_sample", mimeType: "audio/wav");
        form.AddField("transcript", transcript);
		form.AddField("model_code", "1");
		
        UnityWebRequest www = UnityWebRequest.Post("", form);

		yield return www.SendWebRequest();

		buttonRecordGO.SetActive(true);

		Debug.Log(www.result);

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
			Debug.Log(www.error);
			if (www.downloadHandler.text != "") {
				//errorText.text = www.downloadHandler.text;
			} else {
				//errorText.text = "Network error: " + www.error;
			}
			//errorText.gameObject.SetActive(true);
			throw new System.Exception(www.downloadHandler.text ?? www.error);
		} else {
			Debug.Log("Form upload complete!");

			if (www.downloadHandler.text == "invalid credentials") {
				Debug.Log("invalid credentials");
				//errorText.gameObject.SetActive(true);
				//errorText.text = "error_invalid";
				
				yield break;
			}

			if (www.downloadHandler.text == "this account uses auth0") {
				Debug.Log("this account uses auth0");
				//errorText.gameObject.SetActive(true);
				//errorText.text = "error_auth0";
				yield break;
			}
        }

		Debug.Log(www.downloadHandler.text);
    }
}
