using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class NetworkManager : MonoBehaviour
{
	[SerializeField] GameObject surveyPopUpPanelGO;    
	static NetworkManager netWorkManager;
	// This is the URL to the ASR server
	// AUDIO_URL should be in http and not https
	// Because it would make the connection faster???
	// You can set the URL in Secret.cs

	// public static class Secret
	// {
	// 	public const string AUDIO_URL = "http://YOUR SERVER ADDRESS HERE"; //fill in this one
	// }

	string asrURL = Secret.AUDIO_URL; 
	string gptToken = Secret.CHATGPT_API; 

	// However, other URL should be in https for encryption purpose

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

    public IEnumerator GPTTranscribe(byte[] wavBuffer, GameObject transcriptGO, GameObject scoreButtonGO)
    
	{		
	    //IMultipartFormSection & MultipartFormFileSection  could be another solution,
		// but apparent it also require raw byte data to upload

		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"recorded_describe_speech.wav", mimeType: "audio/wav");
        form.AddField("model", "whisper-1");
		form.AddField("language", "fi");

        UnityWebRequest www = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
		www.SetRequestHeader("Authorization", "Bearer " + gptToken);

        // Send the request and wait for response
        yield return www.SendWebRequest();
		
        if (www.result != UnityWebRequest.Result.Success) {
			Debug.LogError("Error: " + www.error);
			Debug.LogError("Error: " + www.result);
			Debug.LogError("Error: " + www.downloadHandler.text);
		} else {
			OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(www.downloadHandler.text);			
			transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = response.text;
			GPTRating(scoreButtonGO, response.text);	
		}		
    }

private IEnumerator GPTRating(GameObject scoreButtonGO, string transcript)
    {
		// Prepare the form
		WWWForm form = new WWWForm();
		form.AddField("model", "gpt-3.5-turbo");

		// Create the messages JSON string using string formatting or interpolation
		// the $ symbol before the string allows you to insert variables directly into 
		// the string with {}. The transcript.Replace("\"", "\\\"") is used to escape 
		// any double quotes that might be present in the transcript string, ensuring that 
		// the JSON remains valid.
		string messagesJson = $@"
		[
			{{
				""role"": ""system"",
				""content"": ""You are a helpful assistant.""
			}},
			{{
				""role"": ""user"",
				""content"": ""{transcript.Replace("\"", "\\\"")}""
			}}
		]";
		form.AddField("messages", messagesJson);

        UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/chat/completions", form);
        // Set headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + gptToken);

        // Send the request and wait for response
        yield return request.SendWebRequest();
		
        if (request.result != UnityWebRequest.Result.Success) {
			Debug.LogError("Error: " + request.error);
			Debug.LogError("Error: " + request.result);
			Debug.LogError("Error: " + request.downloadHandler.text);
		} else {
			OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(request.downloadHandler.text);						
		}
    }

    public IEnumerator ServerPost(string transcript, byte[] wavBuffer, GameObject textErrorGO, TMPro.TextMeshProUGUI resultTextTMP, GameObject warningImageGO,
								GameObject resultPanelGO, GameObject recordButtonGO, TMPro.TextMeshProUGUI debugText)
    
	{		
	    //IMultipartFormSection & MultipartFormFileSection  could be another solution,
		// but apparent it also require raw byte data to upload

		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"speech_sample", mimeType: "audio/wav");
        form.AddField("transcript", transcript);
		form.AddField("model_code", "1");

        UnityWebRequest www = UnityWebRequest.Post(asrURL, form);

		www.timeout = Const.TIME_OUT_SECS;
		yield return www.SendWebRequest();
		
		recordButtonGO.SetActive(true);

		Debug.Log(www.result);

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
			Debug.Log(www.error);
			if (!string.IsNullOrEmpty(www.error)) {
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text =  www.downloadHandler.text ?? www.error;
			} else {
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Network error!";
			}
			textErrorGO.SetActive(true);

			throw new System.Exception(www.downloadHandler.text ?? www.error);
		} else {
			Debug.Log("Form upload complete!");

			Debug.Log(www.downloadHandler.text);

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
		// is updated (added onclick, show active) in their MainPanel (either MainPanel or ExercisePanel)

		// After TextResult text is updated,
		// it's safe to set onclick on result text on it's main panel
		// that's why we can set the Panel to active		
		string textResult = TextUtils.FormatTextResult(transcript, asrResult.score);		
		resultTextTMP.text = textResult;
		
		// Show or now show the warning image
		int warningNo = asrResult.warning.Count;		
		warningImageGO.SetActive(warningNo!=0);
		
		// Update the debug text
		debugText.text = asrResult.prediction;		
		
		if (resultPanelGO != null) resultPanelGO.SetActive(true);

		checkSurVey();
    }	

	public void checkSurVey() {
		int recordNumber = 1;
		
		// If this is not the first record, get the record number
		if (PlayerPrefs.HasKey(Const.PREF_RECORD_NUMBER)) {
			recordNumber = PlayerPrefs.GetInt(Const.PREF_RECORD_NUMBER) + 1;
		}
		//Debug.Log("Record number: " + recordNumber);
		PlayerPrefs.SetInt(Const.PREF_RECORD_NUMBER, recordNumber);
		PlayerPrefs.Save();

		if (recordNumber % Const.SURVEY_TRIGGER == 0) {
			// Only show survey if user has not has not done survey v1
			// No longer have option to refuse survey
			if (!PlayerPrefs.HasKey(Const.PREF_SURVEY_V1_DONE))  {
				//Debug.Log("Show survey");
				surveyPopUpPanelGO.SetActive(true);
			}
		}
	}
}
