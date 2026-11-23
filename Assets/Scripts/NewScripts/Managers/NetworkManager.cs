using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SaySuomi.Managers
{
    public class NetworkManager : MonoBehaviour
    {
        // Holds the guid of a deletion the server has not confirmed yet. Deliberately NOT
        // cleared by ClearLocalASAData - it is the only thing left that can finish the job.
        private const string PREF_PENDING_DELETE = "PendingDeleteGuid";

        // How many times the server has answered 403 to this deletion. A rejected key is not a
        // retryable condition, so this exists purely to allow exactly one more attempt before
        // giving up, rather than retrying on every launch for the life of the install.
        private const string PREF_DELETE_KEY_REJECTED = "PendingDeleteKeyRejected";

        private static NetworkManager netWorkManager;

        // This is the URL to the ASR server
        // AUDIO_URL should be in http and not https
        // Because it would make the connection faster???
        // You can set the URL in Secret.cs

        // public static class Secret
        // {
        // 	public const string AUDIO_URL = "http://YOUR SERVER ADDRESS HERE"; //fill in this one
        // }

        private string asrURL = Secret.AUDIO_URL;
        private string numberGameURL = Secret.NUMBER_AUDIO_URL;
        private string gptToken = Secret.CHATGPT_API;

        // However, other URL should be in https for encryption purpose

        // Raised when a recording reaches the survey threshold. Static so a listener does not
        // depend on this object's start-up order.
        public static event System.Action SurveyDue;

        // The recording the last assessment was stored as, or -1 before any result has come
        // back. Feedback about a recording has to point at one; see RequiresAssessmentId.
        public static int CurrentAssessmentId => PlayerPrefs.GetInt("AssessmentId", -1);

        public ASRResult asrResult { get; private set; }

        // Short message describing the last ServerPost failure, safe to show the user.
        // Null after a successful post. The server's own wording goes to the console
        // instead - it is meant for us, not for the learner.
        public string lastError { get; private set; }

        // The server's own detail.type for the last failure - USER_NOT_FOUND,
        // VALIDATION_ERROR and so on. Null when the body was not the usual envelope.
        // Worth showing next to lastError: the prose is for the user, the type is what makes
        // a bug report or a support message actionable.
        public string lastErrorType { get; private set; }

        // The last failed profile, level or ASA request, for screens that localize the message
        // themselves. Read it from that request's OnServerDone.
        public ServerError lastServerError { get; private set; }

        public ASRResultASA asrResultASA { get; private set; }

        // chatGPTTranscript and chatGPTGrading were removed here. They carried the Whisper
        // transcript and the GPT grade to GPTGradingPanel, and nothing had assigned either of
        // them since the describe-the-picture grader was deleted - both read as null. The
        // panel went with them; see docs/legacy_gpt_vision.md.

        // Whether the level endpoint is configured. A fresh clone has a blank Secret.cs, so
        // screens check this before showing progress for a request that would never be sent.
        public bool CanSetLevel => !string.IsNullOrEmpty(GetPOSTURL(POSTType.ASA_SET_LEVEL));

        /// <summary>
        /// What happened to the last deletion: "deleted" once the server has erased the data,
        /// null if it has not answered yet or the request never reached it.
        ///
        /// DELETE /users erases synchronously, so there is no half-state to report - the older
        /// "pending" value belonged to the retired POST /request/user route, which only ever
        /// acknowledged receipt. Anything other than "deleted" means the request is still
        /// queued locally and will be retried.
        /// </summary>
        public string lastDeleteStatus { get; private set; }

        /// <summary>True only when the server confirmed the data is already gone.</summary>
        public bool DataConfirmedDeleted => lastDeleteStatus == "deleted";

        private void Awake()
        {
            // Destroy existing NetWorkManager(s) to avoid duplicates
            if (netWorkManager != null)
            {
                Debug.LogError("Multiple NetWorkManagers");
                Destroy(gameObject);
                return;
            }
            netWorkManager = this;
        }

        private void Start()
        {
            // Finish any deletion that was asked for but never confirmed by the server.
            RetryPendingDeletion();
        }

        public static NetworkManager GetManager()
        {
            return netWorkManager;
        }

        /// <summary>
        /// Removes everything this device stores about the ASA user, so the app is back to
        /// its pre-onboarding state. Deliberately scoped: pronunciation scores, flashcard
        /// progress, survey state and instruction popups belong to the rest of the app and
        /// are left alone.
        /// </summary>
        public static void ClearLocalASAData()
        {
            // Identity and consent - without these the app re-runs onboarding.
            PlayerPrefs.DeleteKey("UserGuid");
            PlayerPrefs.DeleteKey("ConsentGiven");
            PlayerPrefs.DeleteKey("ConsentTimestamp");
            PlayerPrefs.DeleteKey("BackgroundFormCompleted");
            PlayerPrefs.DeleteKey("BackgroundTimestamp");
            PlayerPrefs.DeleteKey("AppVersion");

            // Assessment history held on the device.
            PlayerPrefs.DeleteKey("AssessmentId");
            PlayerPrefs.DeleteKey("TasksSent");
            PlayerPrefs.DeleteKey(Const.PREF_OVERALL_FEEDBACK_ASKED_AT);

            // The flag this replaced. Still deleted so an account erased on a device that has
            // been through an older build does not leave it behind.
            PlayerPrefs.DeleteKey("OverallFeedbackSent");

            // Left over from the removed secret-code gate; harmless, but it was part of the
            // ASA flow so it goes with the rest.
            PlayerPrefs.DeleteKey("ASASecretVerified");

            PlayerPrefs.Save();
            Debug.Log("Local ASA user data cleared.");
        }

        // Send form and reate a new user
        /// <param name="OnServerDone">
        /// Called once with true only if the user genuinely exists on the server afterwards.
        /// The caller must not record the user as onboarded on false: doing so strands them
        /// with a guid the server has never heard of, and nothing ever asks again.
        /// </param>
        public IEnumerator ServerPost_guid(
            POSTType postType,
            BackgroundFormData backgroundFields,
            System.Action<bool> OnServerDone = null
        )
        {
            string postURL = GetPOSTURL(postType);

            // A 409 means the server already holds this guid. It must never be waved through.
            // The guid IS the participant: continuing would file this person's recordings
            // against somebody else's record, and the two sets of data could not be separated
            // afterwards. So we mint a fresh guid and try again.
            //
            // A v4 guid does not collide by chance, so a 409 means something is actually
            // wrong - a double submit is the likeliest cause - and it is logged as an error
            // even though we recover from it.
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // Rebuilt each attempt: the form reads the guid out of PlayerPrefs when it is
                // constructed, so a regenerated guid needs a new form.
                WWWForm form = GetPOSTForm_guid(backgroundFields);

                using (UnityWebRequest uwr = UnityWebRequest.Post(postURL, form))
                {
                    uwr.timeout = Const.TIME_OUT_SECS;
                    yield return uwr.SendWebRequest();

                    bool failed =
                        uwr.result == UnityWebRequest.Result.ConnectionError
                        || uwr.result == UnityWebRequest.Result.ProtocolError;

                    if (!failed)
                    {
                        Debug.Log("Form upload complete!");
                        Debug.Log(uwr.downloadHandler.text);

                        lastError = null;
                        lastErrorType = null;
                        OnServerDone?.Invoke(true);
                        yield break;
                    }

                    if (uwr.responseCode != 409)
                    {
                        lastError = DescribeError(uwr);
                        OnServerDone?.Invoke(false);
                        yield break;
                    }

                    Debug.LogError(
                        $"Onboarding: the server already holds guid "
                            + $"{PlayerPrefs.GetString("UserGuid")} (attempt {attempt} of "
                            + $"{maxAttempts}). A v4 guid does not collide by chance - check "
                            + "for a duplicate submit."
                    );

                    if (attempt < maxAttempts)
                    {
                        RegenerateUserGuid();
                    }
                }
            }

            // Every attempt collided. Do not let the user through: there is still no guid the
            // server has accepted, so nothing they record afterwards could be stored.
            lastError = "Could not create your account. Please try again.";
            lastErrorType = "GUID_COLLISION";
            OnServerDone?.Invoke(false);
        }

        /// <summary>
        /// Deletes this user's data on the server, then wipes the local copy.
        ///
        /// The failure case is the one that matters. If the request never lands, the local
        /// wipe still happens - the user asked to be forgotten and we honour that on the
        /// device - but that would normally orphan their rows on the server forever, because
        /// the app has just thrown away the only guid that identifies them.
        ///
        /// So the guid is written to PREF_PENDING_DELETE *before* anything is wiped, and
        /// retried on every launch until the server confirms. A failed delete is a deferred
        /// delete, not a silent one.
        /// </summary>
        /// <param name="OnServerDone">true only if the server confirmed the deletion.</param>
        public IEnumerator ServerPost_deleteUser(System.Action<bool> OnServerDone = null)
        {
            string guid = PlayerPrefs.GetString("UserGuid");
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("Delete requested but no guid is stored; clearing locally only.");
                ClearLocalASAData();
                OnServerDone?.Invoke(true);
                yield break;
            }

            // Remember it before the wipe, so a failure can still be finished later.
            PlayerPrefs.SetString(PREF_PENDING_DELETE, guid);
            PlayerPrefs.Save();

            bool deleted = false;
            yield return DeleteUserOnServer(guid, ok => deleted = ok);

            ClearLocalASAData();
            OnServerDone?.Invoke(deleted);
        }

        /// <summary>
        /// Finishes any deletion that was requested but never confirmed. Called on launch.
        /// </summary>
        public void RetryPendingDeletion()
        {
            string pending = PlayerPrefs.GetString(PREF_PENDING_DELETE, "");
            if (string.IsNullOrEmpty(pending))
            {
                return;
            }

            Debug.LogWarning("Retrying an unconfirmed data deletion for guid " + pending);
            StartCoroutine(DeleteUserOnServer(pending, null));
        }

        /// <param name="OnServerDone">
        /// Called once with the response body, or null on failure. After a failure, read
        /// <see cref="lastServerError"/>.
        /// </param>
        public IEnumerator ServerPost_profile(POSTType postType, System.Action<string> OnServerDone)
        {
            WWWForm form = GetPOSTForm_profile();
            string postURL = GetPOSTURL(postType);

            using UnityWebRequest uwr = UnityWebRequest.Post(postURL, form);
            {
                uwr.timeout = Const.TIME_OUT_SECS;
                yield return uwr.SendWebRequest();

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    RecordFailure(uwr);

                    OnServerDone?.Invoke(null);
                    yield break;
                }

                Debug.Log("Form upload complete!");
                Debug.Log(uwr.downloadHandler.text);

                OnServerDone?.Invoke(uwr.downloadHandler.text);
            }
        }

        /// <summary>
        /// Moves the user to a different CEFR level from the profile screen.
        ///
        /// This is not the onboarding self-assessment. That value records what the user said
        /// about themselves at sign-up and is never revised - overwriting it would destroy the
        /// only evidence of how well people judge their own Finnish. This is the level they
        /// are practising at, and it is what the cohort ranking is keyed to.
        ///
        /// The new level is deliberately not applied locally on success. The caller reloads
        /// the profile instead, so the level, the cohort, the rank and the percentile all
        /// arrive together from the server rather than being half-guessed from a change we
        /// assume took.
        ///
        /// The caller shows its own progress and failure, reading lastServerError on false.
        /// </summary>
        /// <param name="cefrLevel">One of A1, A2, B1, B2, C1_plus - see Const.ASA_LEVELS.</param>
        /// <param name="OnServerDone">Called once with whether the server stored the change.</param>
        public IEnumerator ServerPost_setLevel(
            POSTType postType,
            string cefrLevel,
            System.Action<bool> OnServerDone = null
        )
        {
            string postURL = GetPOSTURL(postType);

            // An empty URL throws out of UnityWebRequest rather than failing the request, so
            // it is caught here: the coroutine reports a refusal instead of dying mid-tap.
            // Worth keeping after the endpoint exists - a fresh clone has a blank Secret.cs.
            if (string.IsNullOrEmpty(postURL))
            {
                Debug.LogWarning(
                    "Level change to "
                        + cefrLevel
                        + " was not sent: Secret.ASA_SET_LEVEL_URL "
                        + "is empty. The endpoint is still to be built - see docs/TO_BACKEND.md."
                );
                lastError = Const.ASA_LEVEL_UNAVAILABLE;
                lastErrorType = "ENDPOINT_NOT_CONFIGURED";
                OnServerDone?.Invoke(false);
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("guid", PlayerPrefs.GetString("UserGuid"));

            // The target level, not a direction. "advance"/"revert" would make a duplicate
            // request move the user two steps; an absolute level lands them in the same place
            // however many times it arrives.
            form.AddField("cefr_level", cefrLevel);

            // Built by hand for the same reason DeleteUserOnServer is: UnityWebRequest has no
            // PATCH helper that sends a body, and this endpoint reads both values from form
            // fields.
            using (UnityWebRequest uwr = new UnityWebRequest(postURL, "PATCH"))
            {
                uwr.uploadHandler = new UploadHandlerRaw(form.data);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = Const.TIME_OUT_SECS;

                foreach (var header in form.headers)
                {
                    uwr.SetRequestHeader(header.Key, header.Value);
                }

                yield return uwr.SendWebRequest();

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    RecordFailure(uwr);

                    OnServerDone?.Invoke(false);
                    yield break;
                }

                Debug.Log("Level change stored: " + cefrLevel);
                lastError = null;
                lastErrorType = null;
            }

            OnServerDone?.Invoke(true);
        }

        // Send ASA audio recording to server and get evaluation
        /// <param name="taskId">The server's id for the task, not an array index.</param>
        /// <param name="OnServerDone">
        /// Called once: true once asrResultASA holds the evaluation. After a failure, read
        /// <see cref="lastServerError"/>.
        /// </param>
        public IEnumerator ServerPost_ASA(
            POSTType postType,
            int taskId,
            byte[] wavBuffer,
            System.Action<bool> OnServerDone = null
        )
        {
            WWWForm form = GetPOSTForm_ASA(taskId, wavBuffer);

            string postURL = GetPOSTURL(postType);

            // Use a `using` statement for UnityWebRequest to handle resource cleanup
            // This is a good practice to avoid memory leaks
            using (UnityWebRequest uwr = UnityWebRequest.Post(postURL, form))
            {
                // Scoring needs longer than the other posts: the server itself waits up to
                // 60s before giving up and returning a 503 we can retry.
                uwr.timeout = Const.TIME_OUT_ASA_SECS;
                yield return uwr.SendWebRequest();

                Debug.Log(uwr.result);

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    RecordFailure(uwr);

                    OnServerDone?.Invoke(false);
                    yield break;
                }
                else
                {
                    Debug.Log("Form upload complete!");

                    Debug.Log(uwr.downloadHandler.text);

                    // Update the sent tasks count
                    int value = PlayerPrefs.GetInt("TasksSent", 0);
                    value++;
                    PlayerPrefs.SetInt("TasksSent", value);
                    PlayerPrefs.Save();

                    Debug.Log(PlayerPrefs.GetInt("TasksSent"));
                }

                asrResultASA = JsonUtility.FromJson<ASRResultASA>(uwr.downloadHandler.text);
                // Save the assessment id got from the server. It will be sent to the server in feedback serverposts.
                PlayerPrefs.SetInt("AssessmentId", asrResultASA.assessment_id);

                // The server echoes the task it actually scored. A mismatch means the wrong
                // task embedding was used, which produces a plausible but wrong score with no
                // error anywhere - the one integration bug that testing cannot see. Loud on
                // purpose; the result is not trustworthy.
                if (asrResultASA.task_id != 0 && asrResultASA.task_id != taskId)
                {
                    Debug.LogError(
                        $"TASK ID MISMATCH: sent {taskId}, server scored {asrResultASA.task_id}. "
                            + "This score was produced against the wrong task and must not be trusted."
                    );
                }

                // An off-topic answer is still an answer: the recording was accepted, scored
                // and stored, so the results screen opens exactly as it does for any other
                // result. The scores will be 0.0, which the rows render as one star. What
                // stops that reading as a verdict on the learner's Finnish is the warning
                // ASAResultsScreen puts above them - not hiding the result.
                if (asrResultASA.IsOffTopic)
                {
                    Debug.LogWarning("Relevance check: off_topic - " + asrResultASA.content.reason);
                }
            }
            OnServerDone?.Invoke(true);
        }

        // Send feedback to server
        /// <param name="OnServerDone">
        /// Called once with whether the server took it. FeedbackAutoSend needs to know: it
        /// remembers what has already been delivered so it can skip identical re-sends, and a
        /// failure must not be remembered as delivered or the retry never happens.
        /// </param>
        public IEnumerator ServerPost_feedback(
            POSTType postType,
            string feedback_type,
            string grade,
            string comment,
            System.Action<bool> OnServerDone = null
        )
        {
            WWWForm form = GetPOSTForm_feedback(postType, feedback_type, grade, comment);

            string postURL = GetPOSTURL(postType);

            // Use a `using` statement for UnityWebRequest to handle resource cleanup
            // This is a good practice to avoid memory leaks
            using (UnityWebRequest uwr = UnityWebRequest.Post(postURL, form))
            {
                uwr.timeout = Const.TIME_OUT_SECS;
                yield return uwr.SendWebRequest();

                Debug.Log(uwr.result);

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    // Feedback is fire-and-forget from the user's point of view: there is no
                    // error UI for it, so record why it failed and let them carry on.
                    lastError = DescribeError(uwr);

                    OnServerDone?.Invoke(false);
                    yield break;
                }
                else
                {
                    Debug.Log("Form upload complete!");

                    Debug.Log(uwr.downloadHandler.text);
                }

                // asrResultASA = JsonUtility.FromJson<ASRResultASA>(uwr.downloadHandler.text);

                Debug.Log("Here we are (feedback edition)");
            }
            OnServerDone?.Invoke(true);
        }

        /// <summary>
        /// Uploads a recording to the legacy pronunciation server and stores the score in
        /// asrResult. Used by the sentence exercise (FreestyleScreen) and by flashcard practice
        /// (FlashcardSessionScreen), which each show the result their own way.
        ///
        /// Failures do not throw. A throw inside a coroutine cannot be caught by the caller -
        /// Unity just logs it and kills the coroutine - so they report OnServerDone(false).
        /// </summary>
        /// <param name="OnServerDone">
        /// Called exactly once: true if the score was applied, false on any failure. After a
        /// failure, <see cref="lastError"/> holds a short message safe to show the user.
        /// </param>
        public IEnumerator ServerPost(
            POSTType postType,
            string transcript,
            byte[] wavBuffer,
            System.Action<bool> OnServerDone = null
        )
        {
            WWWForm form = GetPOSTForm(postType, transcript, wavBuffer);

            string postURL = GetPOSTURL(postType);

            // Use a `using` statement for UnityWebRequest to handle resource cleanup
            // This is a good practice to avoid memory leaks
            using (UnityWebRequest uwr = UnityWebRequest.Post(postURL, form))
            {
                uwr.timeout = Const.TIME_OUT_SECS;
                yield return uwr.SendWebRequest();

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    // The server's own words are for us, not for the learner.
                    Debug.LogError($"{postType} failed: {uwr.error} | {uwr.downloadHandler.text}");
                    lastError = string.IsNullOrEmpty(uwr.error)
                        ? "Network error!"
                        : "Server error!";

                    OnServerDone?.Invoke(false);
                    yield break;
                }

                Debug.Log("Form upload complete!");
                Debug.Log(uwr.downloadHandler.text);

                // Plain-text replies from an older auth-enabled backend. They are not JSON,
                // so parsing below would fail. Kept because the legacy server may still
                // produce them - drop this once that is confirmed dead.
                string body = uwr.downloadHandler.text;
                if (body == "invalid credentials" || body == "this account uses auth0")
                {
                    Debug.LogWarning("Server returned: " + body);
                    lastError = body;

                    OnServerDone?.Invoke(false);
                    yield break;
                }

                lastError = null;
                asrResult = JsonUtility.FromJson<ASRResult>(body);

                // update the users score to the userdata
                SaveData.UpdateUserScores(transcript, asrResult.score);

                checkSurVey();
            }
            OnServerDone?.Invoke(true);
        }

        // GPTImageGenerate (DALL-E 3) and DownloadAndDisplayImage were removed here along with
        // the imageComponent field they drew into. They belonged to the describe-the-picture
        // task, whose panel is no longer attached to anything - see docs/legacy_gpt_vision.md.

        // GPTTranscribeWhisper, GPTRatingText and EncodeImageToBase64 were removed here.
        // They were the describe-the-picture grading chain: upload the recording, get a
        // Whisper transcript, then score it against a reference description held in
        // Secret.cs. Nothing reached them once that panel stopped being attached to
        // anything, and the reference descriptions went with them. See
        // docs/legacy_gpt_vision.md.
        public IEnumerator GPT_TTS(string transcript)
        {
            transcript = transcript.Replace("\r", " ").Replace("\"", "\\\""); // Escape double quotes
            string jsonData =
                $@"
		{{
			""model"": ""tts-1"",
			""input"": ""{transcript}"",
			""voice"": ""nova""
		}}";

            Debug.Log(jsonData);

            using (
                UnityWebRequest request = new UnityWebRequest(
                    "https://api.openai.com/v1/audio/speech",
                    "POST"
                )
            )
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

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + request.error);
                    Debug.LogError("Error: " + request.result);
                    Debug.LogError("Error: " + request.downloadHandler.text);
                }
                else
                {
                    byte[] results = request.downloadHandler.data;
                    // For example, save the MP3 file locally
                    string filePath = Path.Combine(Application.persistentDataPath, "speech.mp3");
                    File.WriteAllBytes(filePath, results);
                    Debug.Log("TTS done, saved to: " + filePath);
                }
            }
        }

        /// <param name="OnServerDone">
        /// Called once: true once asrResult holds the score. After a failure,
        /// <see cref="lastError"/> holds the message to show.
        /// </param>
        public IEnumerator NumberGamePost(
            string number,
            byte[] wavBuffer,
            System.Action<bool> OnServerDone = null
        )
        {
            //IMultipartFormSection & MultipartFormFileSection  could be another solution,
            // but apparent it also require raw byte data to upload

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", wavBuffer, fileName: "speech_sample", mimeType: "audio/wav");
            form.AddField("target_number", number); // Numbers to perform force alignment scoring, comma separated (e.g: '18,19')

            UnityWebRequest www = UnityWebRequest.Post(numberGameURL, form);

            www.timeout = Const.TIME_OUT_SECS;
            yield return www.SendWebRequest();

            Debug.Log(www.result);

            if (
                www.result == UnityWebRequest.Result.ConnectionError
                || www.result == UnityWebRequest.Result.ProtocolError
            )
            {
                Debug.LogError(
                    $"Number game post failed: {www.error} | {www.downloadHandler.text}"
                );
                lastError = string.IsNullOrEmpty(www.error)
                    ? "Network error!"
                    : www.downloadHandler.text ?? www.error;

                OnServerDone?.Invoke(false);
                yield break;
            }

            Debug.Log("Form upload complete!");

            Debug.Log(www.downloadHandler.text);

            if (
                www.downloadHandler.text == "invalid credentials"
                || www.downloadHandler.text == "this account uses auth0"
            )
            {
                Debug.Log(www.downloadHandler.text);
                lastError = www.downloadHandler.text;

                OnServerDone?.Invoke(false);
                yield break;
            }

            lastError = null;
            asrResult = JsonUtility.FromJson<ASRResult>(www.downloadHandler.text);
            // Debug.Log(www.downloadHandler.text);
            // Debug.Log(transcript);
            // Debug.Log(asrResult.prediction);
            // Debug.Log(asrResult.score);
            // Debug.Log(asrResult.warning);
            // Debug.Log(asrResult.levenshtein);

            Debug.Log("Text" + www.downloadHandler.text);

            // SaveData.UpdateUserScores(number, asrResult.score);

            checkSurVey();

            OnServerDone?.Invoke(true);
        }

        public void checkSurVey()
        {
            int recordNumber = 1;

            // If this is not the first record, get the record number
            if (PlayerPrefs.HasKey(Const.PREF_RECORD_NUMBER))
            {
                recordNumber = PlayerPrefs.GetInt(Const.PREF_RECORD_NUMBER) + 1;
            }
            //Debug.Log("Record number: " + recordNumber);
            PlayerPrefs.SetInt(Const.PREF_RECORD_NUMBER, recordNumber);
            PlayerPrefs.Save();

            if (recordNumber % Const.SURVEY_TRIGGER == 0)
            {
                // Only show survey if user has not has not done survey v1
                // No longer have option to refuse survey
                if (!PlayerPrefs.HasKey(Const.PREF_SURVEY_V1_DONE))
                {
                    SurveyDue?.Invoke();
                }
            }
        }

        /// <summary>
        /// Creates and stores a new identity for this device, replacing any existing one.
        ///
        /// The consent timestamp is deliberately left alone: the user consented once, and
        /// that fact does not stop being true because the identifier under it changed.
        /// </summary>
        private static string MintUserGuid()
        {
            string guid = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("UserGuid", guid);
            PlayerPrefs.Save();

            return guid;
        }

        /// <summary>
        /// Issues this device a new identity. Only for recovering from a guid the server has
        /// already registered.
        /// </summary>
        private static void RegenerateUserGuid()
        {
            Debug.LogWarning("Onboarding: retrying with a new guid " + MintUserGuid());
        }

        /// <summary>
        /// The stored guid, minting one if there is none yet.
        ///
        /// Used by onboarding only. Onboarding is the one endpoint entitled to create an
        /// identity; everywhere else the guid must already exist, and quietly minting one
        /// there would replace a clear "you are not onboarded" failure with a user the server
        /// has never heard of.
        ///
        /// Normally AcceptConsent does the minting, but it only runs when ConsentGiven is 0.
        /// A partly cleared PlayerPrefs - ConsentGiven still 1, UserGuid gone - goes straight
        /// to the background form, which then posted an empty guid with nothing checking it.
        /// </summary>
        private static string EnsureUserGuid()
        {
            string guid = PlayerPrefs.GetString("UserGuid", "");
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }

            guid = MintUserGuid();
            Debug.LogWarning(
                "Onboarding: no guid was stored - consent had been recorded without one, "
                    + "which a partial PlayerPrefs reset can cause. Minted "
                    + guid
            );
            return guid;
        }

        // Which feedback types are about one scored recording, and so must reference the
        // assessment they are about. The other two (comparison_ui, overall_experience) are
        // about the UI and the app, so there is no assessment for them to point at.
        private static bool RequiresAssessmentId(string feedback_type)
        {
            return feedback_type == "self_assessment"
                || feedback_type == "result_accuracy"
                || feedback_type == "result_understanding";
        }

        private static string DescribeError(long responseCode, string serverMessage)
        {
            return responseCode switch
            {
                0 => "No connection. Check your network and try again.",

                403 => "Consent is required to use this feature.",

                404 => "Your account was not found. You may need to set up the app again.",

                // The client sent something the server rejected - the user cannot fix
                // this by retrying, so say so rather than inviting them to try again.
                422 => "This version of the app could not be accepted by the server. "
                    + "Please update the app, or contact us if it is already up to date.",

                // We cap the recording length before uploading, so reaching this means
                // our cap and the server's disagree - a bug, not something the learner did.
                413 => "That recording is too long to send.",

                503 => "The server is busy. Please try again in a moment.",

                // The server's own wording is the only thing that says WHICH bad request
                // this is: "Unknown task_id 0" and a rejected filename are both 400 and
                // otherwise indistinguishable. A generic apology here cost us a day
                // finding the task_id offset, so show what the server said.
                400 => string.IsNullOrEmpty(serverMessage)
                    ? "The app sent something the server could not accept."
                    : serverMessage,

                // Same reasoning: an unrecognised status with a message is still far more
                // use than "something went wrong", to the user and in a bug report.
                _ => string.IsNullOrEmpty(serverMessage)
                    ? "Something went wrong. Please try again."
                    : serverMessage,
            };
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
                case POSTType.ASA_TASK:
                    return Secret.ASA_URL;
                case POSTType.ASA_CONSENT:
                    return Secret.ASA_CONSENT_URL;
                case POSTType.USER_ASA_FEEDBACK:
                    return Secret.USER_ASA_FEEDBACK_URL;
                case POSTType.ASA_PROFILE:
                    return Secret.ASA_PROFILE_URL;
                case POSTType.ASA_SET_LEVEL:
                    return Secret.ASA_SET_LEVEL_URL;
                case POSTType.DATA_DEL_REQUEST:
                    return Secret.DATA_DEL_REQUEST_URL;
                default:
                    return asrURL;
            }
        }

        // This function is used to get the form for the POST request
        private WWWForm GetPOSTForm(POSTType postType, string transcript, byte[] wavBuffer)
        {
            WWWForm form = new WWWForm();
            // FILE_NAME_POST, not ASA_FILENAME: this form goes to the legacy pronunciation
            // server, the same one NumberGamePost uses. ASA_FILENAME belongs to the DTA
            // server and was almost certainly copy-paste.
            form.AddBinaryData(
                "file",
                wavBuffer,
                fileName: Const.FILE_NAME_POST,
                mimeType: "audio/wav"
            );
            form.AddField("transcript", transcript);
            form.AddField("model_code", "1");

            return form;
        }

        // Get the form for creating a new user
        private WWWForm GetPOSTForm_guid(BackgroundFormData backgroundFields)
        {
            WWWForm form = new WWWForm();
            form.AddField("app_version", PlayerPrefs.GetString("AppVersion"));
            form.AddField("guid", EnsureUserGuid());
            form.AddField("consent_timestamp", PlayerPrefs.GetString("ConsentTimestamp"));

            form.AddField(backgroundFields.gender.Item1, backgroundFields.gender.Item2);
            form.AddField(backgroundFields.age.Item1, backgroundFields.age.Item2);
            form.AddField(backgroundFields.motherTongue.Item1, backgroundFields.motherTongue.Item2);
            form.AddField(
                backgroundFields.otherLanguages.Item1,
                backgroundFields.otherLanguages.Item2
            );
            form.AddField(
                backgroundFields.selfAssessment.Item1,
                backgroundFields.selfAssessment.Item2
            );

            // moved_to_finland and finnish_learning_duration are deliberately absent: those
            // questions were dropped from the background form. The server made both nullable
            // in v1.2.0, so omitting them is accepted - docs/TO_FRONTEND.md item 10.

            form.AddField(
                "background_form_timestamp",
                PlayerPrefs.GetString("BackgroundTimestamp")
            );
            form.AddField("consent_accepted", PlayerPrefs.GetInt("ConsentGiven"));
            form.AddField("background_form_completed", 1);

            return form;
        }

        /// <summary>
        /// Erases the user on the server: DELETE /users, with the guid as a form field and
        /// SERVER_DELETE_KEY in the X-Delete-Key header.
        ///
        /// Not POST /request/user. That route also carries an export request type, and this
        /// app never exports user data, so the route is being retired rather than left
        /// half-used - see docs/TO_BACKEND.md.
        ///
        /// This one erases synchronously and says so: 204 means the data is gone, 403 means
        /// the key did not match, 500 means the erase itself failed. There is no "request
        /// received, we will get to it" state to interpret, which is why 204 is reported
        /// straight through as a confirmed deletion.
        /// </summary>
        private IEnumerator DeleteUserOnServer(string guid, System.Action<bool> done)
        {
            string url = GetPOSTURL(POSTType.DATA_DEL_REQUEST);

            WWWForm form = new WWWForm();
            form.AddField("guid", guid);

            // Built by hand rather than with UnityWebRequest.Delete: that helper sends no
            // body, and this endpoint reads the guid from a form field.
            using (UnityWebRequest uwr = new UnityWebRequest(url, "DELETE"))
            {
                uwr.uploadHandler = new UploadHandlerRaw(form.data);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = Const.TIME_OUT_SECS;

                foreach (var header in form.headers)
                {
                    uwr.SetRequestHeader(header.Key, header.Value);
                }

                // Required. The server compares this to its own key and answers 403 on a
                // mismatch.
                uwr.SetRequestHeader("X-Delete-Key", Secret.SERVER_DELETE_KEY);

                yield return uwr.SendWebRequest();

                bool ok =
                    uwr.result != UnityWebRequest.Result.ConnectionError
                    && uwr.result != UnityWebRequest.Result.ProtocolError;

                if (ok)
                {
                    Debug.Log("Server deleted the data for " + guid);
                    PlayerPrefs.DeleteKey(PREF_PENDING_DELETE);
                    PlayerPrefs.DeleteKey(PREF_DELETE_KEY_REJECTED);
                    PlayerPrefs.Save();
                    lastError = null;
                    lastErrorType = null;

                    // A 2xx from this route means the erase is done, not merely accepted.
                    lastDeleteStatus = "deleted";
                }
                else if (uwr.responseCode == 403)
                {
                    // The delete key did not match. No number of retries can change that - the
                    // key either matches or it does not - so this must not join the launch
                    // retry queue, which would otherwise run forever.
                    //
                    // The user is told their request was received, same as any other failure,
                    // and that is accurate: the server logs the rejected guid at WARNING from
                    // v1.3.0, so a maintainer finishes the erase by hand. The difference is
                    // ours to resolve, not theirs to worry about.
                    //
                    // This only ever happens to a misconfigured build, and when it does it
                    // fails for every user of that build at once - hence the noise below.
                    lastError = DescribeError(uwr);
                    lastDeleteStatus = "key_rejected";

                    int attempts = PlayerPrefs.GetInt(PREF_DELETE_KEY_REJECTED, 0) + 1;

                    if (attempts < 2)
                    {
                        // One retry, in case the first 403 was something transient in front of
                        // the server rather than the key itself.
                        PlayerPrefs.SetInt(PREF_DELETE_KEY_REJECTED, attempts);
                        PlayerPrefs.Save();

                        Debug.LogError(
                            $"DELETION REJECTED for guid {guid}: the server did not accept "
                                + "SERVER_DELETE_KEY (403). Retrying once on the next launch."
                        );
                    }
                    else
                    {
                        // Give up rather than retry forever. The guid stops being pending, so
                        // this log is the last record of it on the device.
                        PlayerPrefs.DeleteKey(PREF_PENDING_DELETE);
                        PlayerPrefs.DeleteKey(PREF_DELETE_KEY_REJECTED);
                        PlayerPrefs.Save();

                        Debug.LogError(
                            $"DELETION ABANDONED for guid {guid}: SERVER_DELETE_KEY was "
                                + "rejected twice (403). THE SERVER STILL HOLDS THIS USER'S "
                                + "DATA. Fix the key in Secret.cs, then have a maintainer "
                                + "erase this guid by hand - the server logged it too."
                        );
                    }
                }
                else
                {
                    lastError = DescribeError(uwr);
                    // Loud, and with the guid. This is the one failure the server cannot see:
                    // the request never reached it, so only this log and the pending retry
                    // stand between the user and data that silently survives.
                    Debug.LogError(
                        $"DELETION REQUEST FAILED to reach the server for guid {guid} - "
                            + $"{uwr.responseCode} {lastErrorType}. Kept as pending and retried "
                            + "on next launch."
                    );
                }

                done?.Invoke(ok);
            }
        }

        // Get the form for creating the profile panel
        private WWWForm GetPOSTForm_profile()
        {
            WWWForm form = new WWWForm();
            form.AddField("guid", PlayerPrefs.GetString("UserGuid"));

            return form;
        }

        // Get the form for ASA audio recording
        private WWWForm GetPOSTForm_ASA(int taskId, byte[] wavBuffer)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData(
                "file",
                wavBuffer,
                // Same recording the user just listened to, hence the same constant.
                // The ".wav" is required by the DTA server: it rejects a filename that does
                // not end in .wav with 400 BAD_REQUEST, even though the bytes are fine.
                fileName: Const.ASA_FILENAME + ".wav",
                mimeType: "audio/wav"
            );
            form.AddField("guid", PlayerPrefs.GetString("UserGuid"));
            form.AddField("task_id", taskId);

            return form;
        }

        // Get form for feedback (grade and optional comment)
        private WWWForm GetPOSTForm_feedback(
            POSTType postType,
            string feedback_type,
            string grade,
            string comment
        )
        {
            WWWForm form = new();

            int value = grade[^1] - '0';
            form.AddField("guid", PlayerPrefs.GetString("UserGuid"));
            form.AddField("reaction_value", value);
            form.AddField("feedback_classification", feedback_type);

            // Only feedback about a specific recording carries an assessment_id.
            // Feedback about a screen or about the app as a whole must not send the field
            // at all - the server answers 422 if it is present. See docs/TO_FRONTEND.md item 1.
            if (RequiresAssessmentId(feedback_type))
            {
                int assessmentId = PlayerPrefs.GetInt("AssessmentId", -1);
                if (assessmentId < 0)
                {
                    Debug.LogError(
                        $"Feedback '{feedback_type}' needs an assessment_id but none is stored. "
                            + "The server will reject this with 422."
                    );
                }
                form.AddField("assessment_id", assessmentId);
            }

            form.AddField("comment", comment);

            // Debug.Log("Rating value: " + grade);
            //Debug.Log("Comment: " + comment);
            return form;
        }

        /// <summary>
        /// Turns a failed DTA request into something worth showing a learner, and puts the
        /// server's own words in the console where they belong. Previously the raw
        /// UnityWebRequest.error went straight on screen, so a deleted account showed up as
        /// "HTTP/1.1 404 Not Found" while the useful part - USER_NOT_FOUND - was discarded.
        ///
        /// Branching is on the HTTP status rather than on detail.type on purpose: `detail` is
        /// an object for normal errors but an array for 422, and JsonUtility cannot handle a
        /// field that changes shape. The body is parsed best-effort for the log line only.
        /// The envelope is published in the OpenAPI schema as of v1.2.0, and 422 keeps its
        /// array shape - see docs/TO_FRONTEND.md item 6.
        /// </summary>
        private string DescribeError(UnityWebRequest uwr)
        {
            return DescribeError(uwr.responseCode, GetServerErrorMessage(uwr));
        }

        /// <summary>
        /// Records a failed DTA request for the caller to read: lastError for plain text, and
        /// lastServerError for screens that localize the message themselves.
        /// </summary>
        private void RecordFailure(UnityWebRequest uwr)
        {
            string serverMessage = GetServerErrorMessage(uwr);
            lastError = DescribeError(uwr.responseCode, serverMessage);
            lastServerError = new ServerError(uwr.responseCode, serverMessage);
        }

        private string GetServerErrorMessage(UnityWebRequest uwr)
        {
            string body = uwr.downloadHandler?.text;
            string type = null;
            string serverMessage = null;

            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    ApiErrorEnvelope envelope = JsonUtility.FromJson<ApiErrorEnvelope>(body);
                    if (envelope != null && envelope.detail != null)
                    {
                        type = envelope.detail.type;
                        serverMessage = envelope.detail.message;
                    }
                }
                catch (System.Exception)
                {
                    // 422 (detail is an array) and any non-JSON body land here. The status
                    // code is enough to pick a message.
                }
            }

            Debug.LogError($"{uwr.url} -> {uwr.responseCode} {type} | {body} | {uwr.error}");
            lastErrorType = type;
            return serverMessage;
        }

        [System.Serializable]
        public class ASRResultASA
        {
            public string transcript;
            public Scores scores;
            public int assessment_id;

            // Echoed back so we can prove the server scored the task we meant. A mismatch is
            // silent otherwise - the score looks perfectly plausible, just for a different
            // question. See docs/TO_FRONTEND.md item 9.
            public int task_id;

            // Server-derived labels. Exactly four values ever: A1, A2, A2+, B1 - floored into
            // the band, not rounded, so they agree with the star tiers by construction. There
            // is no <A1, no A1+/B1+, and nothing above B1, so there is nothing to fold or cap.
            public string cefr_label;
            public string cefr_label_fine;

            // Labels for the four analytic dimensions. Note proficiency is NOT in here - it
            // uses cefr_label_fine above.
            public DimensionLabels dimension_labels;

            // True when the score sat on the model's ceiling or floor. We do not surface it:
            // the four bands already cap the display at B1.
            public bool clipped;

            // Null when the relevance check did not run - older server, judge disabled, or
            // judge errored. Null means "not checked", NEVER "off topic".
            public ContentCheck content;

            /// <summary>
            /// The recording did not appear to answer the task.
            ///
            /// From server v1.3.0 the scores alongside this are the model's real measurements.
            /// Until v1.2.0 they were forced to 0.0 as a penalty, which is why this used to
            /// gate the display; the penalty is gone and the verdict now only drives the
            /// notice. A build talking to an older server still receives the zeros.
            ///
            /// v1.3.0 also made this deliberately hard to trigger - it needs p(bad) >= 0.70 -
            /// so it now mostly means silence or an answer in another language. A fluent answer
            /// on the wrong subject comes back `partial` instead.
            ///
            /// Absent or unrecognised content is treated as on-topic, so a server that never
            /// ran the judge still shows results normally.
            /// </summary>
            public bool IsOffTopic => content != null && content.relevance == "off_topic";

            /// <summary>
            /// The answer only partly addressed the task. The scores are real and must be
            /// shown as normal; this only warrants a gentle tip alongside them.
            /// </summary>
            public bool IsPartial => content != null && content.relevance == "partial";
        }

        [System.Serializable]
        public class DimensionLabels
        {
            public DimensionLabel accuracy;
            public DimensionLabel fluency;
            public DimensionLabel pronunciation;
            public DimensionLabel range;
        }

        [System.Serializable]
        public class DimensionLabel
        {
            public string label;
            public string label_fine;
        }

        [System.Serializable]
        public class ContentCheck
        {
            // on_topic | partial | off_topic
            public string relevance;

            // The server already applies its own 0.6 threshold before returning off_topic -
            // an unsure verdict comes back as partial instead. Do not add a second threshold
            // on top; branch on relevance alone.
            public float confidence;

            // A fixed English string for logs, not localisable prose. Localise from
            // relevance instead.
            public string reason;

            public string judge;
        }

        [System.Serializable]
        public class Scores
        {
            public float accuracy;
            public float fluency;
            public float proficiency;
            public float pronunciation;
            public float range;
        }

        // The DTA server's error envelope: {"detail": {"type": ..., "message": ...}}
        [System.Serializable]
        private class ApiErrorEnvelope
        {
            public ApiErrorDetail detail;
        }

        [System.Serializable]
        private class ApiErrorDetail
        {
            public string type;
            public string message;
        }
    }
}
