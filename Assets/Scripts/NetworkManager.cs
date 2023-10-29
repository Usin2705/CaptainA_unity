using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Networking;
using UnityEngine.UI;


public class NetworkManager : MonoBehaviour
{
	[SerializeField] GameObject surveyPopUpPanelGO;    
	[SerializeField] Image imageComponent;    

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

	public IEnumerator GPTImageGenerate(string prompt)
    
	{	
		// OpenAI require Json format so this is the way to do it and not our normal webrequest
		string jsonData = $@"
		{{
			""prompt"": ""{prompt.Replace("\"", "\\\"")}"",
			""n"": 1,
			""size"": ""1024x1024""
		}}";

        using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/images/generations", "POST"))
		{
			// Convert JSON data to a byte array and set it as upload handler
    		byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
    		request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
			request.downloadHandler = new DownloadHandlerBuffer(); // Set the download handler

		    // Set headers
    		request.SetRequestHeader("Content-Type", "application/json");
   			request.SetRequestHeader("Authorization", "Bearer " + gptToken);		

			//Debug.Log(jsonData);
			// Send the request and yield until it's done
    		yield return request.SendWebRequest();

			Debug.Log(request.result);
			Debug.Log(request.downloadHandler.text);

			// Handle the response
			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError("Error: " + request.error);
				Debug.LogError("Error: " + request.result);
				Debug.LogError("Error: " + request.downloadHandler.text);
			}
			else
			{	
				//Debug.Log(request.downloadHandler.text);
				OpenAIImageResponse openAIImageResponse = JsonUtility.FromJson<OpenAIImageResponse>(request.downloadHandler.text);
				if(openAIImageResponse.data.Length > 0)
				{
					StartCoroutine(DownloadAndDisplayImage(openAIImageResponse.data[0].url));
				}
			}
		}
    }

	IEnumerator DownloadAndDisplayImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading image: " + request.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            SaveData.SaveImageToFile(texture, "describeImage.png");			

            // Display the image
            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 1024, 1024), new Vector2(0.5f, 0.5f), 100.0f);
            imageComponent.sprite = sprite;
        }
    }

    public IEnumerator GPTTranscribe(byte[] wavBuffer, GameObject transcriptGO, GameObject scoreButtonGO)
    
	{		
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
			OpenAIASRResponse response = JsonUtility.FromJson<OpenAIASRResponse>(www.downloadHandler.text);			
			transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = response.text;
			StartCoroutine(GPTRating(scoreButtonGO, response.text));	
		}
		// For testing purpose
		//yield return GPTRating(scoreButtonGO, "Lattialla on sininen kissa, toinen kissa sohvatuolilla. Seinällä on kello oven yläpuolella.");			
		//yield return PostRequest("https://api.openai.com/v1/chat/completions", "Lattialla on sininen kissa, toinen kissa sohvatuolilla. Seinällä on kello oven yläpuolella.");			
    }

	private IEnumerator GPTRating(GameObject scoreButtonGO, string transcript)
    {	
		string gradingInstructions = 
			"The primary task for the users is to speak in Finnish. However, if you detect another " +
			"language, grade them based on that language. The users speak into the microphone, and " +
			"what you're reading is the transcription of their speech. Considering that, anticipate " +
			"potential typos or entirely incorrect words due to transcription errors. Each user has " +
			"only 30 seconds to describe the room, so they don't need to cover every detail to score " +
			"a full 5 points.\n\n" +
			"Grade them on a scale of 1 to 5 based on the following criteria:\n" +
			"- The pronunciation, as represented by the transcribed text.\n" +
			"- Vocabulary: At a minimum, they should mention 2 to 3 items for a decent score. " +
			"For a higher score, 4 to 5 items should be mentioned. To achieve a full score, they " +
			"should also indicate color and position of the items.\n" +
			"- Provide feedback on inaccuracies, such as wrong colors, items, or positions. " +
			"Any inaccuracies should affect the grade, with a reduction of up to 1 point.\n\n" +			
			"The following is the complete description of the room to use as a reference (consider this the ground truth):---\n" +
			Const.ROOM_DESCRIPTION + 
			"---Lastly, ensure that the final rating (a number from 1 to 5) is given at exactly 3 last letter of your response.";

		gradingInstructions = gradingInstructions.Replace("\n", " ").Replace("\r", " ").Replace("\"", "\\\"");

		// Create the messages JSON string using string formatting or interpolation
		// the $ symbol before the string allows you to insert variables directly into 
		// the string with {}. The transcript.Replace("\"", "\\\"") is used to escape 
		// any double quotes that might be present in the transcript string, ensuring that 
		// the JSON remains valid.
		string jsonData = $@"
		{{
			""model"": ""gpt-4"",
			""messages"": [
				{{""role"": ""system"", ""content"": ""{gradingInstructions}""}},
				{{""role"": ""user"", ""content"": ""{transcript.Replace("\"", "\\\"")}""}}
			]
		}}";

		Debug.Log(jsonData);
		
		using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
		{        
			// Convert JSON data to a byte array and set it as upload handler					
    		byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
    		request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
			request.downloadHandler = new DownloadHandlerBuffer(); // Set the download handler

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
				Debug.Log(request.downloadHandler.text);
				OpenAIASRResponse response = JsonUtility.FromJson<OpenAIASRResponse>(request.downloadHandler.text);										
			}
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
