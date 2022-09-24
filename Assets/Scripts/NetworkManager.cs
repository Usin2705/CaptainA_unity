using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;


public class NetworkManager : MonoBehaviour
{
    [SerializeField] GameObject buttonRecordGO;     
	[SerializeField] GameObject textErrorGO;
	static NetworkManager netWorkManager;

	ASRResult asrResult;
	UserData userData;

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
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = www.downloadHandler.text;
			} else {
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Network error!";
			}
			textErrorGO.SetActive(true);

			throw new System.Exception(www.downloadHandler.text ?? www.error);
		} else {
			Debug.Log("Form upload complete!");

			if (www.downloadHandler.text == "invalid credentials") {
				Debug.Log("invalid credentials");
				textErrorGO.gameObject.SetActive(true);
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "invalid credentials";
				
				yield break;
			}

			if (www.downloadHandler.text == "this account uses auth0") {
				Debug.Log("this account uses auth0");
				textErrorGO.gameObject.SetActive(true);
				textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "this account uses auth0";
				yield break;
			}
        }

		textErrorGO.gameObject.SetActive(false);
		asrResult = JsonUtility.FromJson<ASRResult>(www.downloadHandler.text);	
		UpdateUserScores(transcript, asrResult.score);
		

    }

	public UserData LoadFromJson(){
		// Path.Combine combines strings into a file path
        // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
        string filePath = Path.Combine(Application.persistentDataPath, "UserData.json");

		if(File.Exists(filePath))
        {
            // Read the json from the file into a string
            string dataAsJson = File.ReadAllText(filePath);    
            // Pass the json to JsonUtility, and tell it to create a UserData object from it            
			userData = JsonUtility.FromJson<UserData>(dataAsJson);
			return userData;
        }
        else
        {
			// If no UserData file is created, create an empty UserData file
			userData = new UserData("", 0, new List<PhonemeScore>());		
			SaveIntoJson(userData);
			return userData;
        }
    }

	public void SaveIntoJson(UserData userData){
        string data = JsonUtility.ToJson(userData);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/UserData.json", data);
    }

	public void UpdateUserScores(string transcript, List<float> scoreList) {
		// Make sure that stranscript length match with scoreList Length
		if (transcript.Length != scoreList.Count) {	
			return;
		}
		userData = LoadFromJson();

		for (int i = 0; i < scoreList.Count; i++) 
		{
			string phoneme = transcript[i].ToString();		
			
			int index = userData.IndexOf(phoneme);
			// Find phoneme within the list
			// Ideally, Dictionary work better but Dictionary is not Serializable and 
			// therefore can't be Save or Load easily with JSON			
			if (index!=-1)
			{
				PhonemeScore phonemeScore = userData.phonemeScores[index];
				userData.phonemeScores[index].average_score =  (phonemeScore.average_score*phonemeScore.no_tries + scoreList[i])/(phonemeScore.no_tries+1);
				userData.phonemeScores[index].no_tries++;
			} else 			
			{
				userData.phonemeScores.Add(new PhonemeScore(phoneme, scoreList[i], 1));
			}
		}

		SaveIntoJson(userData);
    }
}
