using System.Collections;
using System.IO;
using UnityEngine;
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
	public string chatGPTTranscript {get; private set;}
	public string chatGPTGrading {get; private set;}


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
		// ""style"": ""vivid"",
		// ""style"": ""natural"",
		string jsonData = $@"
		{{
			""prompt"": ""{prompt.Replace("\"", "\\\"")}"",
			""model"": ""dall-e-3"",
			""n"": 1,
			""size"": ""1024x1024"",			
			""quality"": ""hd"",			
			""style"": ""natural"",
			""response_format"": ""url""
		}}";

		Debug.Log(jsonData);

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

    public IEnumerator GPTTranscribe(byte[] wavBuffer, GameObject transcriptGO, GameObject scoreButtonGO, bool isFinnish=true)
    
	{		
		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"recorded_describe_speech.wav", mimeType: "audio/wav");
		form.AddField("model", "whisper-1");
		if (isFinnish) form.AddField("language", "fi");

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
			chatGPTTranscript = response.text;
			StartCoroutine(GPTRating(scoreButtonGO, response.text, isFinnish));	
		}
		// For testing purpose
		//yield return GPTRating(scoreButtonGO, "Lattialla on sininen kissa, toinen kissa sohvatuolilla. Seinällä on kello oven yläpuolella.");			
		//yield return PostRequest("https://api.openai.com/v1/chat/completions", "Lattialla on sininen kissa, toinen kissa sohvatuolilla. Seinällä on kello oven yläpuolella.");			
    }
    // Function to encode the image to base64
    private string EncodeImageToBase64(string imagePath)
    {
        Debug.Log("Encoding image to base64");
		byte[] imageBytes = File.ReadAllBytes(imagePath);
        return System.Convert.ToBase64String(imageBytes);
    }
	
	private IEnumerator GPTRating(GameObject scoreButtonGO, string transcript, bool isFinnish=true)
    {	
		string imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");
        if (!File.Exists(imagePath)) {
			Debug.Log("Image not found, using default image");
			Texture2D texture = Resources.Load<Texture2D>("GenAI/describeImage");
			SaveData.SaveImageToFile(texture, "describeImage.png");
			imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");
		}

		string base64Image = EncodeImageToBase64(imagePath);

		string gradingInstructions;
		if (isFinnish) gradingInstructions = "The primary task for the users is to speak in Finnish. ";
		else gradingInstructions = "The primary task for the users is to speak in their secondary language. ";
		
		gradingInstructions += "The users speak into the microphone, and what you're reading is the transcription of their speech. Given this, be mindful of " +
			"potential typos or entirely incorrect words due to transcription errors. Your task is to give feedback and grade their speech. Be generous in grading pronunciation and grammar as they are not native speakers. The score is from 1.0 to 5.0, with 1 decimal number. Each user has " +
			"only 45 seconds to describe the room, so they don't need to cover every detail to score a full 5 points. The task is transcribed so a few minor errors in the transcript should not reduce their scores. \\n\\n" +			
			"Here's the grading template:\\n" +
			"-----------------------------\\n" +
			"Corrected/Suggested Description: [Your feedback on their description goes here. Please based your feedback on the transcript the user gave you and suggest better/corrected description based on the actual picture described below. Put the wrong text in the color tag <color=#ff0000ff>wrong text here</color> and put the corrected or suggested text in bold tag <b>corrected text here</b>. For example, if user using 'valkoi matto' instead of the correct 'valkoinen matto', you would use: <color=#ff0000ff>valkoi</color> <b>valkoinen</b> matto]\\n\\n" +			
			"(Any feedback from this point to the end should use English as the main language.)\\n" +
			"Accuracy of Description: [Feedback about how accurately they described the room based on the ground truth, such as wrong colors, items, or positions.]\\n" +
			"Score: [1-5]\\n\\n" +
			"Vocabulary: [Feedback on the items they mentioned and their use of specific terms. At a minimum, they should mention 2 to 3 items for a decent score. For a higher score, 3 to 5 items and 2 to 3 colors, 2 to 3 position of items should be mentioned.5 score would have at least 5 items, 3 colors and 3 positions]\\n" +
			"Score: [1-5]\\n\\n" +
			"Pronunciation (as represented in the transcription): [Feedback on any strange word choices or inaccuracies that might indicate pronunciation issues]\\n" +
			"Score: [1-5]\\n\\n" +
			"Grammar: [Feedback on grammar, sentence structure, and tenses. To get 4, users only make less than 3 minor mistakes every 4 sentences. To get 5, users can make maximum 1 minors mistakes every 4 sentences]\\n" +
			"Score: [1-5]\\n\\n" +
			"Overall grading: [A short summary of their performance]\\n" +
			"Score: [1-5]\\n" +
			"-----------------------------\\n\\n" +
			"Remember to compare their description with the image of the room to use as a reference (consider this the ground truth). \\n" +			 
			"Ensure that the final rating (a number from 1.0 to 5.0) is given as the last three characters of your response." + 
			"For example: [your response go here] Score: 3.5";

		gradingInstructions = gradingInstructions.Replace("\r", " ").Replace("\"", "\\\""); // Escape double quotes

		// Create the messages JSON string using string formatting or interpolation
		// the $ symbol before the string allows you to insert variables directly into 
		// the string with {}. The transcript.Replace("\"", "\\\"") is used to escape 
		// any double quotes that might be present in the transcript string, ensuring that 
		// the JSON remains valid.
		//""model"": ""gpt-4-vision-preview"",   
		//""model"": ""gpt-4"",
		//""model"": ""gpt-3.5-turbo"",	
        string jsonData = $@"
        {{
            ""model"": ""gpt-4-vision-preview"",  
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{gradingInstructions}""}},
                {{""role"": ""user"", ""content"": [
                    {{
                        ""type"": ""text"",
                        ""text"": ""{transcript.Replace("\"", "\\\"")}""
                    }},
                    {{
                        ""type"": ""image_url"",
                        ""image_url"": {{
                            ""url"": ""data:image/jpeg;base64,{base64Image}""
                        }}
                    }}
                ]}}
            ],
			""max_tokens"": 2500
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
				OpenAIChatResponse response = JsonUtility.FromJson<OpenAIChatResponse>(request.downloadHandler.text);
				if (response != null && response.choices.Length > 0)
					{
						string assistantResponse = response.choices[0].message.content;
						Debug.Log("Assistant says: " + assistantResponse);
						scoreButtonGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = assistantResponse.Substring(assistantResponse.Length - 3);
						chatGPTGrading = assistantResponse;
					}
					else
					{
						Debug.LogError("Invalid response or no choices available.");
					}
			}
		}
    }

    public IEnumerator ServerPost(string transcript, byte[] wavBuffer, GameObject textErrorGO, TMPro.TextMeshProUGUI resultTextTMP, GameObject warningImageGO,
								GameObject resultPanelGO, TMPro.TextMeshProUGUI debugText)
    
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
