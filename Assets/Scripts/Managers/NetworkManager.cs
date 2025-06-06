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
	string numberGameURL = Secret.NUMBER_AUDIO_URL;
	string gptToken = Secret.CHATGPT_API; 
	string gptAzureToken = Secret.AZUREGPT_API; 

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

	// This function is used to get the URL for the POST request
	private string GetPOSTURL(POSTType postType)
	{
		switch (postType)
		{
			case POSTType.MDD_TASK:
				return Secret.AUDIO_URL;
			case POSTType.LLM_TASK:
				return null;
			case POSTType.PuheNumero_TASK:
				return Secret.NUMBER_AUDIO_URL;
			default:
				return asrURL;
		}
	}

	// This function is used to get the form for the POST request
	private WWWForm GetPOSTForm(POSTType postType, string transcript, byte[] wavBuffer) {
		WWWForm form = new WWWForm();
		form.AddBinaryData("file", wavBuffer, fileName:Const.FILE_NAME_POST, mimeType: "audio/wav");
		form.AddField("transcript", transcript);
		form.AddField("model_code", "1");

		return form;
	}

    public IEnumerator ServerPost(POSTType postType, string transcript, byte[] wavBuffer, GameObject textErrorGO, GameObject resultTextGO,
								GameObject resultPanelGO, 
								GameObject debugTextGO = null, System.Action OnServerDone = null, GameObject warningImageGO = null)
    
	{
        WWWForm form = GetPOSTForm(postType, transcript, wavBuffer);
		string postURL = GetPOSTURL(postType);

		// Use a `using` statement for UnityWebRequest to handle resource cleanup
		// This is a good practice to avoid memory leaks
		using (UnityWebRequest uwr = UnityWebRequest.Post(postURL, form))
		{
			uwr.timeout = Const.TIME_OUT_SECS;
			yield return uwr.SendWebRequest();

			Debug.Log(uwr.result);

			if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
				Debug.Log(uwr.error);
				
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = string.IsNullOrEmpty(uwr.error) ? "Network error!" : "Server error!";		
				
				OnServerDone?.Invoke();
				throw new System.Exception(uwr.downloadHandler.text ?? uwr.error);

			} else {
				Debug.Log("Form upload complete!");

				Debug.Log(uwr.downloadHandler.text);

				if (uwr.downloadHandler.text == "invalid credentials") {
					Debug.Log("invalid credentials");				
					textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "invalid credentials";
					
					OnServerDone?.Invoke();
					yield break;
				}

				if (uwr.downloadHandler.text == "this account uses auth0") {
					Debug.Log("this account uses auth0");
					textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "this account uses auth0";
					
					OnServerDone?.Invoke();
					yield break;
				}
			}
			
			// textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Here are your results. \n Great effort!";
			textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Here are your results:";
			asrResult = JsonUtility.FromJson<ASRResult>(uwr.downloadHandler.text);

			// Only save data if the transcript is text and not number
			// as we also have the number game
			if (postType != POSTType.PuheNumero_TASK)
			{
				// update the users score to the userdata
				SaveData.UpdateUserScores(transcript, asrResult.score);
			}
			

			// Update text result
				// This part only update the TextResult text		
				// is updated (added onclick, show active) in their MainPanel (either MainPanel or ExercisePanel)

				// After TextResult text is updated,
				// it's safe to set onclick on result text on it's main panel
				// that's why we can set the Panel to active		
				string textResult = TextUtils.FormatTextResult(transcript, asrResult.score);		
			resultTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = textResult;
			
			// Set resultTextGO to bold following design guideline
			resultTextGO.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

			// Show or now show the warning image
			if (warningImageGO != null) {
				int warningNo = asrResult.warning.Count;		
				warningImageGO.SetActive(warningNo!=0);
			}
			
			// Update the debug text
			if (debugTextGO != null) {
				// Set the debug text to show the prediction
				// This is for testing purpose only
				debugTextGO.SetActive(true);
				debugTextGO.GetComponent<TMPro.TextMeshProUGUI>().text = asrResult.prediction;		
			}			
			
			// This function is not active in the current version
			if (resultPanelGO != null) resultPanelGO.SetActive(true);

			checkSurVey();			
		}
		OnServerDone?.Invoke();
    }	

	public IEnumerator GPTImageGenerate(string prompt)
    
	{	

		// OpenAI require Json format so this is the way to do it and not our normal webrequest
		// ""style"": ""vivid"",
		// ""style"": ""natural"",
		// ""quality"": ""standard"",			
		// ""quality"": ""hd"",			
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

    public IEnumerator GPTTranscribeWhisper(byte[] wavBuffer, GameObject transcriptGO, GameObject scoreButtonGO,  DescribePanel.TaskType taskType, int taskNumber, 
									 bool isFinnish=true)
    
	{		
		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"recorded_describe_speech.wav", mimeType: "audio/wav");
		form.AddField("model", "whisper-1");
		
		// If not Finnish, set the language to English
		if (!isFinnish) 
		{
			form.AddField("language", "EN");
		}
		else 
		// The default language is Finnish
		{
			form.AddField("language", "FI");
		}

        UnityWebRequest www = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
		www.SetRequestHeader("Authorization", "Bearer " + gptToken);

        // Send the request and wait for response
        yield return www.SendWebRequest();
		
        if (www.result != UnityWebRequest.Result.Success) {
			Debug.LogError("Error: " + www.error);
			Debug.LogError("Error: " + www.result);
			Debug.LogError("Error: " + www.downloadHandler.text);
		} else {
			Debug.Log(www.downloadHandler.text);
			OpenAIASRResponse response = JsonUtility.FromJson<OpenAIASRResponse>(www.downloadHandler.text);			
			transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = response.text;
			chatGPTTranscript = response.text;
			StartCoroutine(GPTRatingText(scoreButtonGO, response.text, taskType, taskNumber, isFinnish));	
			//StartCoroutine(GPTRatingTextFi(scoreButtonGO, response.text, taskType, taskNumber, isFinnish));	
		}

		// For testing purpose
		// yield return GPTRatingText(scoreButtonGO, "Huoneessa on iso. Sininen sova on oikea. Sen alla on paljon keltainen kuva. Punainen nuoja tuoli ja musta hullu on vasemmalla. Iso matto on lattialla ja viiveä ovi");			
		// yield return GPT_TTS("Huoneessa on iso. Sininen sova on oikea. Sen alla on paljon keltainen kuva. Punainen nuoja tuoli ja musta hullu on vasemmalla. Iso matto on lattialla ja viiveä ovi");			
		//yield return PostRequest("https://api.openai.com/v1/chat/completions", "Lattialla on sininen kissa, toinen kissa sohvatuolilla. Seinällä on kello oven yläpuolella.");			
    }
    // Function to encode the image to base64
    private string EncodeImageToBase64(string imagePath)
    {
        Debug.Log("Encoding image to base64");
		byte[] imageBytes = File.ReadAllBytes(imagePath);
        return System.Convert.ToBase64String(imageBytes);
    }

	public IEnumerator GPT_TTS(string transcript)
	{
		transcript = transcript.Replace("\r", " ").Replace("\"", "\\\""); // Escape double quotes
		string jsonData = $@"
		{{
			""model"": ""tts-1"",	
			""input"": ""{transcript}"",
			""voice"": ""nova""
		}}";

		Debug.Log(jsonData);

		using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/audio/speech", "POST"))
		{        
			// Convert JSON data to a byte array and set it as upload handler					
    		byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
    		request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
			request.downloadHandler = new DownloadHandlerBuffer(); // Set the download handler

		    // Set headers    		
   			request.SetRequestHeader("Authorization", "Bearer " + gptToken);	
			request.SetRequestHeader("Content-Type", "application/json");

			// Send the request and wait for response
			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success) {
				Debug.LogError("Error: " + request.error);
				Debug.LogError("Error: " + request.result);
				Debug.LogError("Error: " + request.downloadHandler.text);
			} else {				
				byte[] results = request.downloadHandler.data;
				// For example, save the MP3 file locally
            	string filePath = Path.Combine(Application.persistentDataPath, "speech.mp3");				
            	File.WriteAllBytes(filePath, results);
				Debug.Log("TTS done, saved to: " + filePath);
			}
		}
	}

	private IEnumerator GPTRatingText(GameObject scoreButtonGO, string transcript, DescribePanel.TaskType taskType, int taskNumber, bool isFinnish=true)
    {	
		string gradingInstructions = TextUtils.GetGradingInstruction(taskType, taskNumber, isFinnish);		
		gradingInstructions = gradingInstructions.Replace("\r", " ").Replace("\"", "\\\""); // Escape double quotes

		// Create the messages JSON string using string formatting or interpolation
		// the $ symbol before the string allows you to insert variables directly into 
		// the string with {}. The transcript.Replace("\"", "\\\"") is used to escape 
		// any double quotes that might be present in the transcript string, ensuring that 
		// the JSON remains valid.
		//""model"": ""gpt-4-vision-preview"",   
		//""model"": ""gpt-4-0613"",
		//""model"": ""gpt-3.5-turbo-1106"",	

		
		// This setup is for GPT-4, GPT-4o use diffent setup
		// string jsonData = $@"
		// {{
		// 	""model"": ""gpt-4-0613"",
		// 	""temperature"": 0.0,
		// 	""seed"": 1011,
		// 	""messages"": [
		// 		{{
		// 			""role"": ""system"", 
		// 			""content"": ""{gradingInstructions}""
		// 		}},
		// 		{{
		// 			""role"": ""user"", 
		// 			""content"": ""{transcript.Replace("\"", "\\\"")}""
		// 		}}
		// 	],
		// 	""max_tokens"": 2500
		// }}";

		string jsonData = $@"
		{{
			""model"": ""gpt-4o-2024-05-13"",
			""messages"": [
				{{
					""role"": ""system"",
					""content"": [
						{{
							""type"": ""text"",
							""text"": ""{gradingInstructions}""
						}}
					]
				}},
				{{
					""role"": ""user"",
					""content"": [
						{{
							""type"": ""text"",
							""text"": ""{transcript.Replace("\"", "\\\"")}""
						}}
					]
				}}
			],
			""temperature"": 0,
			""max_tokens"": 2500,
			""response_format"": {{
				""type"": ""text""
			}}
		}}";

		if (taskType == DescribePanel.TaskType.C || taskType == DescribePanel.TaskType.C2) {
			// If the task is C or C2, we need to include the image in the request
			// since the prompt is not include the image description (random image generation)
			
			// Load the image from the Resources folder
			string imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");
			if (!File.Exists(imagePath)) {
				Debug.Log("Image not found, using default image");
				Texture2D texture = Resources.Load<Texture2D>("GenAI/describeImage");
				SaveData.SaveImageToFile(texture, "describeImage.png");
				imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");
			}

			string base64Image = EncodeImageToBase64(imagePath);

			jsonData = $@"
				{{
					""model"": ""gpt-4o-2024-05-13"",
					""messages"": [
						{{
							""role"": ""system"",
							""content"": [
								{{
									""type"": ""text"",
									""text"": ""{gradingInstructions}""
								}}					
							]
						}},
						{{
							""role"": ""user"",
							""content"": [
								{{
									""type"": ""text"",
									""text"": ""{transcript.Replace("\"", "\\\"")}""
								}},
								{{
									""type"": ""image_url"",
									""image_url"": 
										{{
											""url"": ""data:image/jpeg;base64,{base64Image}""
										}}	
								}}	
							]
						}}
					],
					""temperature"": 0,
					""max_tokens"": 3500,
					""response_format"": {{
						""type"": ""text""
					}}
				}}";
		}

		// Debug.Log(jsonData);
		
		using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
		// Azure API
		//using (UnityWebRequest request = new UnityWebRequest(Secret.AALTO_GPT4O_URL, "POST"))
		{        
			// Convert JSON data to a byte array and set it as upload handler					
    		byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
    		request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
			request.downloadHandler = new DownloadHandlerBuffer(); // Set the download handler

		    // Set headers
    		request.SetRequestHeader("Authorization", "Bearer " + gptToken);	
			// Azure API
   			//request.SetRequestHeader("Ocp-Apim-Subscription-Key", gptAzureToken);	

			request.SetRequestHeader("Content-Type", "application/json");

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

						// Extract all text within "@" tags
        				string finnishTTS = TextUtils.ExtractTextWithinAtTags(assistantResponse);
                    	// StartCoroutine(GPT_TTS(finnishTTS));

                    	// Replace "@" in assistantResponse with a new line
                    	assistantResponse = assistantResponse.Replace("@", "\n");
						scoreButtonGO.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Done";
						scoreButtonGO.SetActive(true);						
						chatGPTGrading = assistantResponse;
					}
					else
					{
						Debug.LogError("Invalid response or no choices available.");
					}
			}
		}
    }

	private IEnumerator GPTRatingVision(GameObject scoreButtonGO, string transcript, bool isFinnish=true)
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
		else gradingInstructions = "The primary task for the users is to speak in English. ";
		
		gradingInstructions += @"The users speak into the microphone, and what you're reading is the transcription of their speech. Given this, be mindful of " +
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
		
		//using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
		using (UnityWebRequest request = new UnityWebRequest("https://aalto-openai-apigw.azure-api.net/v1/openai/gpt4-vision-preview/chat/completions", "POST"))
		{        
			// Convert JSON data to a byte array and set it as upload handler					
    		byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
    		request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
			request.downloadHandler = new DownloadHandlerBuffer(); // Set the download handler

		    // Set headers
    		request.SetRequestHeader("Content-Type", "application/json");
   			//request.SetRequestHeader("Authorization", "Bearer " + gptToken);	
			request.SetRequestHeader("Ocp-Apim-Subscription-Key", gptAzureToken);

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
        form.AddBinaryData("file", wavBuffer, fileName:Const.FILE_NAME_POST, mimeType: "audio/wav");
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

    public IEnumerator NumberGamePost(string number, byte[] wavBuffer, GameObject textErrorGO, TMPro.TextMeshProUGUI resultTextTMP)
    
	{		
	    //IMultipartFormSection & MultipartFormFileSection  could be another solution,
		// but apparent it also require raw byte data to upload

		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"speech_sample", mimeType: "audio/wav");
        form.AddField("target_number", number); // Numbers to perform force alignment scoring, comma separated (e.g: '18,19')

        UnityWebRequest www = UnityWebRequest.Post(numberGameURL, form);

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

		Debug.Log("Text" + www.downloadHandler.text);

		// SaveData.UpdateUserScores(number, asrResult.score);

		// Update text result
		// This part only update the TextResult text		
		// is updated (added onclick, show active) in their MainPanel (either MainPanel or ExercisePanel)

		// After TextResult text is updated,
		// it's safe to set onclick on result text on it's main panel
		// that's why we can set the Panel to active		
		string textResult = TextUtils.FormatTextResult(number, asrResult.score);		
		resultTextTMP.text = textResult;
		
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
