using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class NetworkManager : MonoBehaviour
{
    [SerializeField]
    GameObject surveyPopUpPanelGO;

    [SerializeField]
    GameObject feedbackPanelGO;

    [SerializeField]
    GameObject ASAPanelGO;

    [SerializeField]
    Image imageComponent;

    [SerializeField]
    GameObject loadingPopUpProfileGO;

    [SerializeField]
    GameObject feedbackLoadingIconGO;

    [SerializeField]
    GameObject profileLoadingIconGO;

    [SerializeField]
    GameObject dimPanelASAGO;

    [SerializeField]
    GameObject resultsButtonGO;

    [SerializeField]
    GameObject feedbackLoadingBackButtonGO;

    [SerializeField]
    GameObject profileLoadingBackButtonGO;

    [SerializeField]
    TMP_InputField feedbackTextGO;

    [SerializeField]
    GameObject feedbackLoadingErrorTextGO;

    // The "Title" label on the ASA loading popup. It starts out reading "Sending the
    // audio..." and has to be moved on once the upload finishes, otherwise the popup
    // still claims to be sending while the results button is sitting there waiting.
    [SerializeField]
    TMPro.TextMeshProUGUI feedbackLoadingTitleText;

    [SerializeField]
    GameObject profileLoadingErrorTextGO;

    [SerializeField]
    GameObject profilePanelGO;

    [SerializeField]
    GameObject dimPanelGO;

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

    // However, other URL should be in https for encryption purpose

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

    public ASRResultASA asrResultASA { get; private set; }
    public string chatGPTTranscript { get; private set; }
    public string chatGPTGrading { get; private set; }

    [SerializeField]
    AdvancePanel AdvancePanel;

    [SerializeField]
    ASAPanel ASAPanel;

    [SerializeField]
    ASAProfilePanel ASAProfilePanel;

    [SerializeField]
    TaskPanel taskPanel;

    [SerializeField]
    private TMPro.TextMeshProUGUI feedbackErrorText;

    [SerializeField]
    private TMPro.TextMeshProUGUI profileErrorText;

    void Awake()
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

    public static NetworkManager GetManager()
    {
        return netWorkManager;
    }

    void Start()
    {
        // Finish any deletion that was asked for but never confirmed by the server.
        RetryPendingDeletion();
    }

    void OnDestroy() { }

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
    private WWWForm GetPOSTForm_guid(AdvancePanel.BackgroundFormData backgroundFields)
    {
        WWWForm form = new WWWForm();
        form.AddField("app_version", PlayerPrefs.GetString("AppVersion"));
        form.AddField("guid", PlayerPrefs.GetString("UserGuid"));
        form.AddField("consent_timestamp", PlayerPrefs.GetString("ConsentTimestamp"));

        form.AddField(backgroundFields.gender.Item1, backgroundFields.gender.Item2);
        form.AddField(backgroundFields.age.Item1, backgroundFields.age.Item2);
        form.AddField(backgroundFields.motherTongue.Item1, backgroundFields.motherTongue.Item2);
        form.AddField(backgroundFields.otherLanguages.Item1, backgroundFields.otherLanguages.Item2);
        form.AddField(backgroundFields.selfAssessment.Item1, backgroundFields.selfAssessment.Item2);

        // moved_to_finland and finnish_learning_duration are deliberately absent: those
        // questions were dropped from the background form. The server still marks both
        // REQUIRED, so onboarding answers 422 until it is updated - docs/TO_BACKEND.md item 10.

        form.AddField("background_form_timestamp", PlayerPrefs.GetString("BackgroundTimestamp"));
        form.AddField("consent_accepted", PlayerPrefs.GetInt("ConsentGiven"));
        form.AddField("background_form_completed", 1);

        return form;
    }

    // Send form and reate a new user
    /// <param name="OnServerDone">
    /// Called once with true only if the user genuinely exists on the server afterwards.
    /// The caller must not record the user as onboarded on false: doing so strands them
    /// with a guid the server has never heard of, and nothing ever asks again.
    /// </param>
    public IEnumerator ServerPost_guid(
        POSTType postType,
        AdvancePanel.BackgroundFormData backgroundFields,
        System.Action<bool> OnServerDone = null
    )
    {
        WWWForm form = GetPOSTForm_guid(backgroundFields);
        string postURL = GetPOSTURL(postType);

        using UnityWebRequest uwr = UnityWebRequest.Post(postURL, form);
        uwr.timeout = Const.TIME_OUT_SECS;
        yield return uwr.SendWebRequest();

        if (
            uwr.result == UnityWebRequest.Result.ConnectionError
            || uwr.result == UnityWebRequest.Result.ProtocolError
        )
        {
            if (uwr.responseCode == 409)
            {
                // The server already knows this guid. That is not a failure: the user is
                // onboarded, so let them through instead of blocking them at the door.
                Debug.LogWarning("Onboarding: this guid is already registered, continuing.");
                lastError = null;
                lastErrorType = null;

                OnServerDone?.Invoke(true);
                yield break;
            }

            lastError = DescribeError(uwr);

            OnServerDone?.Invoke(false);
            yield break;
        }

        Debug.Log("Form upload complete!");
        Debug.Log(uwr.downloadHandler.text);

        lastError = null;
        lastErrorType = null;
        OnServerDone?.Invoke(true);
    }

    // Holds the guid of a deletion the server has not confirmed yet. Deliberately NOT
    // cleared by ClearLocalASAData - it is the only thing left that can finish the job.
    const string PREF_PENDING_DELETE = "PendingDeleteGuid";

    [System.Serializable]
    private class DeleteResponse
    {
        public string status;
    }

    /// <summary>
    /// What the server said about the last deletion: "deleted" if the data is already
    /// gone, "pending" if it hit an error and a maintainer will finish it, null if the
    /// server did not say.
    ///
    /// This is what decides the confirmation wording. "Deleted" lets the UI state plainly
    /// that the data is gone; anything else has to use the softer "removal is underway",
    /// because promising a deletion that has not happened is the one thing not to get
    /// wrong here.
    /// </summary>
    public string lastDeleteStatus { get; private set; }

    /// <summary>True only when the server confirmed the data is already gone.</summary>
    public bool DataConfirmedDeleted => lastDeleteStatus == "deleted";

    // Reads {"status": ...} out of the 202 body. The field is optional: if the server has
    // not added it yet the status stays null and the UI falls back to the careful wording,
    // so this ships safely before the server side exists.
    static string ReadDeleteStatus(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        try
        {
            DeleteResponse parsed = JsonUtility.FromJson<DeleteResponse>(body);
            return parsed != null ? parsed.status : null;
        }
        catch (System.Exception)
        {
            return null;
        }
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

    /// <summary>
    /// Lodges the deletion request with the server. Deliberately POST /request/user and
    /// NOT DELETE /users: the latter needs an admin key, and any key shipped inside the
    /// app is extractable from the APK and would let anyone delete anyone. Handing the
    /// deletion to the server keeps the client credential-free.
    ///
    /// The server deletes immediately on receipt and answers "request received" either
    /// way - it does not report whether the deletion itself succeeded, and logs failures
    /// for a maintainer instead. So a 2xx here means "the server has it and is acting on
    /// it now", which is why the retry stops at that point: anything after arrival is the
    /// server's to finish, and retrying would only duplicate a request it already holds.
    /// </summary>
    IEnumerator DeleteUserOnServer(string guid, System.Action<bool> done)
    {
        string url = GetPOSTURL(POSTType.DATA_DEL_REQUEST);

        WWWForm form = new WWWForm();
        form.AddField("guid", guid);
        form.AddField("type", "delete");

        using (UnityWebRequest uwr = UnityWebRequest.Post(url, form))
        {
            uwr.timeout = Const.TIME_OUT_SECS;

            // A guardrail, not a security boundary. It raises the bar against casual or
            // scripted abuse of this endpoint; it cannot stop anyone who decompiles the
            // APK, because every string constant in here is recoverable.
            //
            // That is an acceptable trade HERE and only here: the worst a leaked key buys
            // is the ability to *request* deletions, which the server reviews and can
            // undo. It must therefore be a key that works on this route alone - never the
            // admin key, which would make the same leak destructive on DELETE /users.
            uwr.SetRequestHeader("X-Client-Key", Secret.SERVER_DELETE_KEY);

            yield return uwr.SendWebRequest();

            bool ok =
                uwr.result != UnityWebRequest.Result.ConnectionError
                && uwr.result != UnityWebRequest.Result.ProtocolError;

            if (ok)
            {
                Debug.Log("Server accepted the deletion request for " + guid);
                PlayerPrefs.DeleteKey(PREF_PENDING_DELETE);
                PlayerPrefs.Save();
                lastError = null;
                lastErrorType = null;
                lastDeleteStatus = ReadDeleteStatus(uwr.downloadHandler.text);
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
        PlayerPrefs.DeleteKey("OverallFeedbackSent");

        // Left over from the removed secret-code gate; harmless, but it was part of the
        // ASA flow so it goes with the rest.
        PlayerPrefs.DeleteKey("ASASecretVerified");

        PlayerPrefs.Save();
        Debug.Log("Local ASA user data cleared.");
    }

    // Get the form for creating the profile panel
    private WWWForm GetPOSTForm_profile()
    {
        WWWForm form = new WWWForm();
        form.AddField("guid", PlayerPrefs.GetString("UserGuid"));

        return form;
    }

    public IEnumerator ServerPost_profile(POSTType postType, System.Action OnServerDone = null)
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
                profileLoadingIconGO.SetActive(false);
                profileLoadingErrorTextGO.SetActive(true);
                profileLoadingBackButtonGO.SetActive(true);

                // Shows a readable message and logs the server's own detail.type.
                ErrorHandling(uwr, profileErrorText);
                lastError = profileErrorText.text;

                OnServerDone?.Invoke();
                yield break;
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log(uwr.downloadHandler.text);
                loadingPopUpProfileGO.SetActive(false);
                profilePanelGO.SetActive(true);
                dimPanelGO.SetActive(false);
                taskPanel.isLoading = false;

                ASAProfilePanel.Stats Stats = JsonUtility.FromJson<ASAProfilePanel.Stats>(
                    uwr.downloadHandler.text
                );
                // If cohort size is too small or not enough tasks sent, perfcentile will be -1f
                if (Stats.percentile == -1f)
                {
                    ASAProfilePanel.InsufficientStats InsufficientStats =
                        JsonUtility.FromJson<ASAProfilePanel.InsufficientStats>(
                            uwr.downloadHandler.text
                        );
                    ASAProfilePanel.NotEnoughTasks(InsufficientStats);
                }
                else
                {
                    ASAProfilePanel.UpdateText(Stats);
                    ASAProfilePanel.UpdateLevelBar(Stats);
                    ASAProfilePanel.ShowButtons(Stats);
                }
            }
        }

        OnServerDone?.Invoke();
    }

    // Get the form for ASA audio recording
    private WWWForm GetPOSTForm_ASA(POSTType postType, string transcript, byte[] wavBuffer)
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
        int currentTask = ASAPanel.currentTaskSelected;
        form.AddField("task_id", currentTask);

        return form;
    }

    // Send ASA audio recording to server and get evaluation
    public IEnumerator ServerPost_ASA(
        POSTType postType,
        string transcript,
        byte[] wavBuffer,
        System.Action OnServerDone = null,
        GameObject warningImageGO = null
    )
    {
        WWWForm form = GetPOSTForm_ASA(postType, transcript, wavBuffer);

        string postURL = GetPOSTURL(postType);

        // The popup is reused for every recording, so put the title back to its sending
        // state here rather than at the call site. Otherwise the second recording spins
        // underneath whatever the first one finished with.
        SetLoadingTitle("Sending the audio...");

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
                feedbackLoadingIconGO.SetActive(false);
                feedbackLoadingErrorTextGO.SetActive(true);
                feedbackLoadingBackButtonGO.SetActive(true);
                SetLoadingTitle("Could not send the audio");

                // Shows a readable message and logs the server's own detail.type.
                ErrorHandling(uwr, feedbackErrorText);
                lastError = feedbackErrorText.text;

                OnServerDone?.Invoke();
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
                PlayerPrefs.SetInt("OverallFeedbackSent", 0);
                PlayerPrefs.Save();

                Debug.Log(PlayerPrefs.GetInt("TasksSent"));
            }

            asrResultASA = JsonUtility.FromJson<ASRResultASA>(uwr.downloadHandler.text);
            // Save the assessment id got from the server. It will be sent to the server in feedback serverposts.
            PlayerPrefs.SetInt("AssessmentId", asrResultASA.assessment_id);

            ASAPanel.isLoading = false;

            feedbackLoadingIconGO.SetActive(false);
            resultsButtonGO.SetActive(true);
            SetLoadingTitle("Your results are ready. Tap below to see them.");
        }
        OnServerDone?.Invoke();
    }

    // The ASA loading popup is a shared prefab instance, so its title is whatever the
    // last request left behind. Every caller goes through here so the null check for an
    // unassigned reference lives in one place.
    private void SetLoadingTitle(string message)
    {
        if (feedbackLoadingTitleText != null)
        {
            feedbackLoadingTitleText.text = message;
        }
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
        // at all - the server answers 422 if it is present. See docs/TO_BACKEND.md item 1.
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

    // Which feedback types are about one scored recording, and so must reference the
    // assessment they are about. The other two (comparison_ui, overall_experience) are
    // about the UI and the app, so there is no assessment for them to point at.
    private static bool RequiresAssessmentId(string feedback_type)
    {
        return feedback_type == "self_assessment"
            || feedback_type == "result_accuracy"
            || feedback_type == "result_understanding";
    }

    // Send feedback to server
    public IEnumerator ServerPost_feedback(
        POSTType postType,
        string feedback_type,
        string grade,
        string comment,
        System.Action OnServerDone = null,
        GameObject warningImageGO = null
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

                OnServerDone?.Invoke();
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
        OnServerDone?.Invoke();
    }

    /// <summary>
    /// Uploads a recording to the legacy pronunciation server and shows the score.
    /// Used by the sentence exercise (MainPanel) and by flashcard practice
    /// (SuperMemoPanel).
    ///
    /// This replaces two near-identical overloads that had drifted apart, one per panel.
    /// Notes on what changed, in case a panel looks wrong after the merge:
    ///
    /// - There is no error-label parameter any more. The two panels used theirs very
    ///   differently: MainPanel passed its always-visible PromptText, SuperMemoPanel
    ///   passed a hidden label it toggles. Serving both from in here is what made the
    ///   two copies diverge. Success is now reported through OnServerDone and the
    ///   wording and show/hide belong to the panel, which is the only place that knows
    ///   what its own label is for. On failure the panel reads lastError.
    /// - Failures no longer throw. A throw inside a coroutine cannot be caught by the
    ///   caller - Unity just logs it and kills the coroutine - so it was an unhandleable
    ///   error dressed up as a handleable one. OnServerDone(false) instead.
    /// - Every optional argument is null-checked. The SuperMemoPanel copy assumed
    ///   warningImageGO and debugText were always supplied.
    /// - Scores are always saved. The old "skip when PuheNumero_TASK" guard was dead:
    ///   the number game posts through NumberGamePost and never reaches this method.
    /// - The result text is no longer forced to bold here. That is a style decision, so
    ///   MainPanel now applies it; the flashcards never wanted it.
    /// </summary>
    /// <param name="OnServerDone">
    /// Called exactly once: true if the score was applied, false on any failure. After a
    /// failure, <see cref="lastError"/> holds a short message safe to show the user.
    /// </param>
    public IEnumerator ServerPost(
        POSTType postType,
        string transcript,
        byte[] wavBuffer,
        TMPro.TextMeshProUGUI resultText,
        TMPro.TextMeshProUGUI debugText = null,
        GameObject warningImageGO = null,
        GameObject resultPanelGO = null,
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
                Debug.LogError(
                    $"{postType} failed: {uwr.error} | {uwr.downloadHandler.text}"
                );
                lastError = string.IsNullOrEmpty(uwr.error) ? "Network error!" : "Server error!";

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

            // Update text result
            // This part only update the TextResult text
            // is updated (added onclick, show active) in their MainPanel (either MainPanel or ExercisePanel)

            // After TextResult text is updated,
            // it's safe to set onclick on result text on it's main panel
            // that's why we can set the Panel to active
            resultText.text = TextUtils.FormatTextResult(transcript, asrResult.score);

            // Show or now show the warning image
            if (warningImageGO != null)
            {
                warningImageGO.SetActive(asrResult.warning.Count != 0);
            }

            // Set the debug text to show the prediction
            // This is for testing purpose only
            if (debugText != null)
            {
                debugText.gameObject.SetActive(true);
                debugText.text = asrResult.prediction;
            }

            // This function is not active in the current version
            if (resultPanelGO != null)
                resultPanelGO.SetActive(true);

            checkSurVey();
        }
        OnServerDone?.Invoke(true);
    }

    public IEnumerator GPTImageGenerate(string prompt)
    {
        // OpenAI require Json format so this is the way to do it and not our normal webrequest
        // ""style"": ""vivid"",
        // ""style"": ""natural"",
        // ""quality"": ""standard"",
        // ""quality"": ""hd"",
        string jsonData =
            $@"
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

        using (
            UnityWebRequest request = new UnityWebRequest(
                "https://api.openai.com/v1/images/generations",
                "POST"
            )
        )
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
                OpenAIImageResponse openAIImageResponse = JsonUtility.FromJson<OpenAIImageResponse>(
                    request.downloadHandler.text
                );
                if (openAIImageResponse.data.Length > 0)
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
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, 1024, 1024),
                new Vector2(0.5f, 0.5f),
                100.0f
            );
            imageComponent.sprite = sprite;
        }
    }

    public IEnumerator GPTTranscribeWhisper(
        byte[] wavBuffer,
        GameObject transcriptGO,
        GameObject scoreButtonGO,
        DescribePanel.TaskType taskType,
        int taskNumber,
        bool isFinnish = true
    )
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "file",
            wavBuffer,
            fileName: "recorded_describe_speech.wav",
            mimeType: "audio/wav"
        );
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

        UnityWebRequest www = UnityWebRequest.Post(
            "https://api.openai.com/v1/audio/transcriptions",
            form
        );
        www.SetRequestHeader("Authorization", "Bearer " + gptToken);

        // Send the request and wait for response
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
            Debug.LogError("Error: " + www.result);
            Debug.LogError("Error: " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            OpenAIASRResponse response = JsonUtility.FromJson<OpenAIASRResponse>(
                www.downloadHandler.text
            );
            transcriptGO.GetComponent<TMPro.TextMeshProUGUI>().text = response.text;
            chatGPTTranscript = response.text;
            StartCoroutine(
                GPTRatingText(scoreButtonGO, response.text, taskType, taskNumber, isFinnish)
            );
            //StartCoroutine(GPTRatingTextFi(scoreButtonGO, response.text, taskType, taskNumber, isFinnish));
        }

        // For testing purpose
        // yield return GPTRatingText(scoreButtonGO, "Huoneessa on iso. Sininen sova on oikea. Sen alla on paljon keltainen kuva. Punainen nuoja tuoli ja musta hullu on vasemmalla. Iso matto on lattialla ja viiveÃ¤ ovi");
        // yield return GPT_TTS("Huoneessa on iso. Sininen sova on oikea. Sen alla on paljon keltainen kuva. Punainen nuoja tuoli ja musta hullu on vasemmalla. Iso matto on lattialla ja viiveÃ¤ ovi");
        //yield return PostRequest("https://api.openai.com/v1/chat/completions", "Lattialla on sininen kissa, toinen kissa sohvatuolilla. SeinÃ¤llÃ¤ on kello oven ylÃ¤puolella.");
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

    private IEnumerator GPTRatingText(
        GameObject scoreButtonGO,
        string transcript,
        DescribePanel.TaskType taskType,
        int taskNumber,
        bool isFinnish = true
    )
    {
        string gradingInstructions = TextUtils.GetGradingInstruction(
            taskType,
            taskNumber,
            isFinnish
        );
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

        string jsonData =
            $@"
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

        if (taskType == DescribePanel.TaskType.C || taskType == DescribePanel.TaskType.C2)
        {
            // If the task is C or C2, we need to include the image in the request
            // since the prompt is not include the image description (random image generation)

            // Load the image from the Resources folder
            string imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");
            if (!File.Exists(imagePath))
            {
                Debug.Log("Image not found, using default image");
                Texture2D texture = Resources.Load<Texture2D>("GenAI/describeImage");
                SaveData.SaveImageToFile(texture, "describeImage.png");
                imagePath = Path.Combine(Application.persistentDataPath, "describeImage.png");
            }

            string base64Image = EncodeImageToBase64(imagePath);

            jsonData =
                $@"
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

        using (
            UnityWebRequest request = new UnityWebRequest(
                "https://api.openai.com/v1/chat/completions",
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
                Debug.Log(request.downloadHandler.text);
                OpenAIChatResponse response = JsonUtility.FromJson<OpenAIChatResponse>(
                    request.downloadHandler.text
                );
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

    public IEnumerator NumberGamePost(
        string number,
        byte[] wavBuffer,
        GameObject textErrorGO,
        TMPro.TextMeshProUGUI resultTextTMP
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
            Debug.Log(www.error);
            if (!string.IsNullOrEmpty(www.error))
            {
                textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text =
                    www.downloadHandler.text ?? www.error;
            }
            else
            {
                textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "Network error!";
            }
            textErrorGO.SetActive(true);

            throw new System.Exception(www.downloadHandler.text ?? www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");

            Debug.Log(www.downloadHandler.text);

            if (www.downloadHandler.text == "invalid credentials")
            {
                Debug.Log("invalid credentials");
                textErrorGO.SetActive(true);
                textErrorGO.GetComponent<TMPro.TextMeshProUGUI>().text = "invalid credentials";

                yield break;
            }

            if (www.downloadHandler.text == "this account uses auth0")
            {
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
                //Debug.Log("Show survey");
                surveyPopUpPanelGO.SetActive(true);
            }
        }
    }

    [System.Serializable]
    public class ASRResultASA
    {
        public string transcript;
        public Scores scores;
        public int assessment_id;
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

    /// <summary>
    /// Turns a failed DTA request into something worth showing a learner, and puts the
    /// server's own words in the console where they belong. Previously the raw
    /// UnityWebRequest.error went straight on screen, so a deleted account showed up as
    /// "HTTP/1.1 404 Not Found" while the useful part - USER_NOT_FOUND - was discarded.
    ///
    /// Branching is on the HTTP status rather than on detail.type on purpose: `detail` is
    /// an object for normal errors but an array for 422, and JsonUtility cannot handle a
    /// field that changes shape. The body is parsed best-effort for the log line only.
    /// See docs/TO_BACKEND.md item 6 - we have asked for the envelope to be published in
    /// the OpenAPI schema so this can become machine-checkable.
    /// </summary>
    private string DescribeError(UnityWebRequest uwr)
    {
        string body = uwr.downloadHandler != null ? uwr.downloadHandler.text : null;

        string type = null;
        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                ApiErrorEnvelope envelope = JsonUtility.FromJson<ApiErrorEnvelope>(body);
                if (envelope != null && envelope.detail != null)
                {
                    type = envelope.detail.type;
                }
            }
            catch (System.Exception)
            {
                // 422 (detail is an array) and any non-JSON body land here. The status
                // code below is enough to pick a message.
            }
        }

        Debug.LogError($"{uwr.url} -> {uwr.responseCode} {type} | {body} | {uwr.error}");
        lastErrorType = type;

        switch (uwr.responseCode)
        {
            case 0:
                return "No connection. Check your network and try again.";
            case 403:
                return "Consent is required to use this feature.";
            case 404:
                return "Your account was not found. You may need to set up the app again.";
            case 422:
                // The client sent something the server rejected - the user cannot fix
                // this by retrying, so say so rather than inviting them to try again.
                return "This version of the app could not be accepted by the server. "
                    + "Please update the app, or contact us if it is already up to date.";
            case 413:
                // We cap the recording length before uploading, so reaching this means
                // our cap and the server's disagree - a bug, not something the learner did.
                return "That recording is too long to send.";
            case 503:
                return "The server is busy. Please try again in a moment.";
            default:
                return "Something went wrong. Please try again.";
        }
    }

    public void ErrorHandling(UnityWebRequest uwr, TextMeshProUGUI errorText)
    {
        // Handle the errors for server posts
        errorText.text = DescribeError(uwr);
    }
}
