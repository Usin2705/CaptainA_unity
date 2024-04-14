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
	string advanceASRURL = Secret.ADVANCE_AUDIO_URL; 
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

    public IEnumerator GPTTranscribe(byte[] wavBuffer, GameObject transcriptGO, GameObject scoreButtonGO,  DescribePanel.TaskType taskType, int taskNumber, 
									 bool isFinnish=true)
    {		
		WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBuffer, fileName:"recorded_describe_speech.wav", mimeType: "audio/wav");
		form.AddField("language", "FI");

        UnityWebRequest www = UnityWebRequest.Post(advanceASRURL, form);
		www.timeout = Const.TIME_OUT_ADVANCE_SECS;
		
        // Send the request and wait for response
        yield return www.SendWebRequest();
		
        if (www.result != UnityWebRequest.Result.Success) {
			Debug.LogError("Error: " + www.error);
			Debug.LogError("Error: " + www.result);
			Debug.LogError("Error: " + www.downloadHandler.text);
		} else {
			Debug.Log(www.downloadHandler.text);
			string asrtranscript = JsonUtility.FromJson<TranscriptResult>(www.downloadHandler.text).prediction;
			transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = asrtranscript;
			chatGPTTranscript = asrtranscript;
			StartCoroutine(GPTRatingTextFi(scoreButtonGO, asrtranscript, taskType, taskNumber, isFinnish));	
		}

		// For testing purpose
		// yield return GPTRatingText(scoreButtonGO, "Huoneessa on iso. Sininen sova on oikea. Sen alla on paljon keltainen kuva. Punainen nuoja tuoli ja musta hullu on vasemmalla. Iso matto on lattialla ja viiveä ovi");			
		// yield return GPT_TTS("Huoneessa on iso. Sininen sova on oikea. Sen alla on paljon keltainen kuva. Punainen nuoja tuoli ja musta hullu on vasemmalla. Iso matto on lattialla ja viiveä ovi");			
		//yield return PostRequest("https://api.openai.com/v1/chat/completions", "Lattialla on sininen kissa, toinen kissa sohvatuolilla. Seinällä on kello oven yläpuolella.");			
    }

		private IEnumerator GPTRatingTextFi(GameObject scoreButtonGO, string transcript, DescribePanel.TaskType taskType, int taskNumber, bool isFinnish=true)
    {	

		string gradingInstructions = TextUtils.GetGradingInstruction(taskType, taskNumber);
		
		gradingInstructions = gradingInstructions.Replace("\r", " ").Replace("\"", "\\\""); // Escape double quotes

		// Create the messages JSON string using string formatting or interpolation
		// the $ symbol before the string allows you to insert variables directly into 
		// the string with {}. The transcript.Replace("\"", "\\\"") is used to escape 
		// any double quotes that might be present in the transcript string, ensuring that 
		// the JSON remains valid.
		//""model"": ""gpt-4-vision-preview"",   
		//""model"": ""gpt-4-0613"",
		//""model"": ""gpt-3.5-turbo-1106"",	
		string jsonData = $@"
		{{
			""model"": ""gpt-4-0613"",	
			""temperature"": 0.0,
			""messages"": [
				{{""role"": ""system"", ""content"": ""{gradingInstructions}""}},
				{{""role"": ""user"", ""content"": ""{transcript.Replace("\"", "\\\"")}""}}
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

						// Extract all text within "@" tags
        				string finnishTTS = TextUtils.ExtractTextWithinAtTags(assistantResponse);
                    	StartCoroutine(GPT_TTS(finnishTTS));

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

    public IEnumerator GPTTranscribeOpenAI(byte[] wavBuffer, GameObject transcriptGO, GameObject scoreButtonGO, bool isFinnish=true)
    
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
			Debug.Log(www.downloadHandler.text);
			OpenAIASRResponse response = JsonUtility.FromJson<OpenAIASRResponse>(www.downloadHandler.text);			
			transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = response.text;
			chatGPTTranscript = response.text;
			StartCoroutine(GPTRatingText(scoreButtonGO, response.text, isFinnish));	
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
    		request.SetRequestHeader("Content-Type", "application/json");
   			request.SetRequestHeader("Authorization", "Bearer " + gptToken);	

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

	private IEnumerator GPTRatingText(GameObject scoreButtonGO, string transcript, bool isFinnish=true)
    {	

		string gradingInstructions;
		if (isFinnish) gradingInstructions = "The primary task for the users is to speak in Finnish.\\n";
		else gradingInstructions = "The primary task for the users is to speak in their secondary language.\\n";
		
		gradingInstructions += @" Task: Evaluate the speaker's ability to complete a given task based on a transcript of their speech. The grading should be assigned as 'Good', 'Average', 'Bad', or 'None', and it should only be based on the task completion assessment. The accuracy of the description provided by the speaker should not affect the grade. Furthermore, mistakes in pronunciation, fluency, vocabulary, and grammar errors are to be ignored and do not impact the grade. The text is a transcript and, as such, contains no punctuation.\\n" +
								"Speakers are non-native, so you should expect a lot of mistake due to bad pronunciation. Try your best to guess what speakers trying to say based on the picture description and his transcript. His word may not be wrong, just baddly pronounced and was wrongly transcribed into text. Some common mistakes: missing or replace phone 'h' with other phones, confuse front phones (for example ä,ö,y) with back phone (a, o, u), pronounce 'r' as 'v' or 'd' or failed to pronounce 'r'.\\n" +
								"Explain your grading in detail and then give the grade. Then, based on what the user has said, provide a version that you think would get a 'Good' grade. Provide your improved version in Finnish inside the '@' tag like this: @improved version in this tag@\\n" +								
								"The task description:\\n" +
								"Describe the room in the picture. You must mention key items or furniture and their colours. And describe their positions, either relative to the picture or in relation to other items. You don't need to talk about every single item. The more you describe, the higher your score will be. You have 45 seconds to speak.\\n" +
								"Grading examples:\\n" +
								"User: huoneessa on iso sininen sohva lattialla on suuri punainen matto vasemmalla on musta kirjahylly ja paljon kirjoja kirjahyllyn vieressä on pieni punainen nojatuoli\\n" +
								"System:[Explanation]Good. @Improved version@\\n" +
								"User: huoneessa on sohva ja matto musta hylly on ja siinä on kirjoja myös tuoli mutta ei muista televisio on huoneessa ja ovi on vihreä en näe pöytää\\n" +
								"System:[Explanation]Average. @Improved version@\\n" +
								"User: tässä huoneessa on paljon tilaa musta kirjahylly on seinää vasemmalla ja siinä on monia värejä keskellä huonetta on tumman siinen sohva ja sillä on keltainen tyyny huoneen oikealla puolella on keltaisia kuvat seinällä ja vihreä ovi vieressä on takki naulakossa.\\n" +
								"System:[Explanation]Good. @Improved version@\\n" +
								"User: on musta kirjahylly ja kirjoja  sininen sohva ja siinä on jotain keltaista lattialla on matto keltainen ja ovi on siellä.\\n" +
								"System:[Explanation]Average. @Improved version@\\n" +
								"The room description:\\n" +
								"The room is warm and inviting. There is a big black bookshelf on the left with many books with colorful spines. The bookshelf is against the wall and does not reach the ceiling, leaving some windows and wall space above it. A blue (ior dark blue) sofa with yellow pillows is in the right (or in the middle) of the room. There is a big red (or dard red) rug on the floor. Next to the bookshelf, there is a small pink (or light pink) armchair. In front of the sofa, there is no coffee table visible, which gives the space an open feel. There is a black TV stand with a TV on it against the wall. On the wall above the TV, there are two framed pictures. On the wall on the right above the blue sofa, there are six (or many) square yellow decoratives (or abstract pictures). There is a green door on the far right corner. The door appears to be closed. Near the green door, there is a coat rack (in Finnish: naulakko) with 2 jackets (one black and one yellow jacket). The room has off-white walls (with a slightly yellow or grey) and a ceiling with some grey (or grey green).\\n" +
								"Grading Criteria:\\n" +
								"Good: Completes all aspects of the task, including both introduction and an attempt at describing the hoodie. There are no significant deficiencies in the response.\\n" +
								"Average: Completes the task, but there are some significant deficiencies in the response.\\n" +
								"Bad: Only partially answers the task, the response has many significant deficiencies.\\n" +
								"None: The response does not relate to the task at all.\\n";

		gradingInstructions = gradingInstructions.Replace("\r", " ").Replace("\"", "\\\""); // Escape double quotes

		// Create the messages JSON string using string formatting or interpolation
		// the $ symbol before the string allows you to insert variables directly into 
		// the string with {}. The transcript.Replace("\"", "\\\"") is used to escape 
		// any double quotes that might be present in the transcript string, ensuring that 
		// the JSON remains valid.
		//""model"": ""gpt-4-vision-preview"",   
		//""model"": ""gpt-4-0613"",
		//""model"": ""gpt-3.5-turbo-1106"",	
		string jsonData = $@"
		{{
			""model"": ""gpt-4-0613"",	
			""temperature"": 0.0,
			""messages"": [
				{{""role"": ""system"", ""content"": ""{gradingInstructions}""}},
				{{""role"": ""user"", ""content"": ""{transcript.Replace("\"", "\\\"")}""}}
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

						// Extract all text within "@" tags
        				string finnishTTS = TextUtils.ExtractTextWithinAtTags(assistantResponse);
                    	StartCoroutine(GPT_TTS(finnishTTS));

                    	// Replace "@" in assistantResponse with a new line
                    	assistantResponse = assistantResponse.Replace("@", "\n");
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
		else gradingInstructions = "The primary task for the users is to speak in their secondary language. ";
		
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
