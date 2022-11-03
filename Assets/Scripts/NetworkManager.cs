using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class NetworkManager : MonoBehaviour
{
	static NetworkManager netWorkManager;

	string url = Secret.URL;

	public ASRResult asrResult {get; private set;}


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


    public IEnumerator ServerPost(string transcript, byte[] wavBuffer, GameObject textErrorGO, GameObject resultPanelGO, GameObject recordButtonGO, TMPro.TextMeshProUGUI debugText)
    {
	    //IMultipartFormSection & MultipartFormFileSection  could be another solution,
		// but apparent it also require raw byte data to upload

		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"speech_sample", mimeType: "audio/wav");
        form.AddField("transcript", transcript);
		form.AddField("model_code", "1");

        UnityWebRequest www = UnityWebRequest.Post(url, form);

		yield return www.SendWebRequest();

		recordButtonGO.SetActive(true);

		Debug.Log(www.result);

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
			Debug.Log(www.error);
			if ( www.result.ToString() != "") {
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text =  www.result.ToString();
			} else {
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Network error!";
			}
			textErrorGO.SetActive(true);

			throw new System.Exception(www.downloadHandler.text ?? www.error);
		} else {
			Debug.Log("Form upload complete!");

			if (www.downloadHandler.text == "invalid credentials") {
				Debug.Log("invalid credentials");
				textErrorGO.SetActive(true);
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "invalid credentials";

				yield break;
			}

			if (www.downloadHandler.text == "this account uses auth0") {
				Debug.Log("this account uses auth0");
				textErrorGO.SetActive(true);
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "this account uses auth0";
				yield break;
			}
        }
		
		textErrorGO.SetActive(false);
		asrResult = JsonUtility.FromJson<ASRResult>(www.downloadHandler.text);
		// Debug.Log(www.downloadHandler.text);
		// Debug.Log(transcript);
		// Debug.Log(asrResult.prediction);
		// Debug.Log(asrResult.score);
		// Debug.Log(asrResult.warning);
		// Debug.Log(asrResult.levenshtein);		

		SaveData.UpdateUserScores(transcript, asrResult.score);

		// Update text result
		// This part only update the TextResult text
		// The Replay Sample button, which is part of ResultPanelGO
		// is updated (added onclick, show active) in their MainPanel (either MainPanel or ExercisePanel)

		// After TextResult text is updated,
		// it's safe to set onclick on result text on it's main panel
		// that's why we can set the Panel to active		
		string textResult = TextUtils.FormatTextResult(transcript, asrResult.score);
		resultPanelGO.transform.Find("ResultText").GetComponent<TMPro.TextMeshProUGUI>().text = textResult;
		
		// Show or now show the warning image
		int warningNo = asrResult.warning.Count;
		GameObject WarningImageGO = resultPanelGO.transform.Find("WarningImage").gameObject;
		WarningImageGO.SetActive(warningNo!=0);
		

		debugText.text = asrResult.prediction;
		resultPanelGO.SetActive(true);
    }
}
