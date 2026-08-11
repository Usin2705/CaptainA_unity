using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class AudioManager : MonoBehaviour
{
    static AudioManager audioManager;

    [SerializeField]
    AudioClip recordNotif;
    AudioSource audioSource;

    private AudioClip replayClip;

    void Awake()
    {
        // Destroy existing AudioManager(s) to avoid duplicates
        if (audioManager != null)
        {
            Debug.LogError("Multiple AudioManagers");
            return;
        }
        audioSource = GetComponent<AudioSource>();
        audioManager = this;

        RequestMicrophonePermission();
    }

    /// <summary>
    /// Asks for the microphone up front, at launch, rather than at the moment of the
    /// first recording.
    ///
    /// Asking late was the cause of a confusing freeze: Android shows the permission
    /// dialog and pauses the app, but the recording code carried on and indexed
    /// Microphone.devices[0] - which is EMPTY until permission is granted. That threw,
    /// the recording never started, and the countdown ran against nothing. The user had
    /// to leave the task and come back, by which point the device list was populated.
    ///
    /// Asking here means the dialog is answered long before any record button exists.
    /// </summary>
    public static void RequestMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    /// <summary>True when we are allowed to record and a microphone actually exists.</summary>
    public static bool CanRecord()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            return false;
        }
#endif
        return Microphone.devices.Length > 0;
    }

    public static AudioManager GetManager()
    {
        return audioManager;
    }

    /// <param name="onStarted">
    /// Called once the microphone is actually running, or with false if it could not be
    /// started. Worth waiting for rather than assuming: this method returns immediately
    /// but the device does not open for another 0.3s, and a stop issued inside that gap
    /// lands before the start it was meant to cancel - which leaves the microphone running
    /// unattended and saves whatever the clip held beforehand.
    /// </param>
    public void StartRecording(int lengthSec, System.Action<bool> onStarted = null)
    {
        /*
        *   We can skip this block since we no longer require a notification sound
        */

        //Debug.Log("Play record notif");
        //audioSource.PlayOneShot(recordNotif, 0.5f);
        //The notification sound cause some noise in the recording
        //So we need to delay it a little
        //The new notification sound is just 0.3f long
        //Invoke(nameof(RecordSound), 0.31f);
        // audioSource.Stop();
        StartCoroutine(StartRecordingSafe(lengthSec, onStarted));
    }

    IEnumerator StartRecordingSafe(int lengthSec, System.Action<bool> onStarted)
    {
        audioSource.Stop();

        yield return new WaitForSeconds(0.3f);

        onStarted?.Invoke(RecordSound(lengthSec));
    }

    public void StopReplaying()
    {
        audioSource.Stop();
    }

    /// <returns>True only if the microphone is now running.</returns>
    bool RecordSound(int lengthSec)
    {
        // Never index Microphone.devices without checking it. It is empty whenever the
        // permission has not been granted yet, and indexing [0] there throws
        // IndexOutOfRangeException from inside a coroutine - which surfaces as the
        // recording quietly never starting while the countdown runs.
        if (!CanRecord())
        {
            Debug.LogError(
                "Cannot record: microphone permission not granted, or no input device. "
                    + "Asking again - the user can grant it and retry."
            );
            RequestMicrophonePermission();
            return false;
        }

        audioSource.clip = Microphone.Start(
            Microphone.devices[0],
            false,
            lengthSec,
            Const.FREQUENCY
        );

        return audioSource.clip != null;
    }

    public void PlayAudioClip(AudioClip audioClip)
    {
        // Adding an if statement so that user won't abuse the play audio
        audioSource.Stop(); // stop any audio that might be playing
        audioSource.PlayOneShot(audioClip, 1.0f);
    }

    public AudioClip GetReplayClip()
    {
        return replayClip;
    }

    // Record ASA audio and send it to server
    // This is the one we have been using so far
    public void GetAudioAndPost_ASA(
        POSTType postType,
        string transcript,
        System.Action OnServerDone = null
    )
    {
        Microphone.End("");
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim: true);
        // SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim: true); // for debug purpose

        StartCoroutine(
            NetworkManager
                .GetManager()
                .ServerPost_ASA(postType, transcript, wavBuffer, OnServerDone)
        );
    }

    // Stop recording and post the audio to the legacy pronunciation server.
    // Used by the sentence exercise (MainPanel) and flashcard practice (SuperMemoPanel).
    // There used to be two overloads of this, one per panel, differing only in how they
    // passed their text fields around; they are merged - see NetworkManager.ServerPost
    // for what was reconciled. The panel owns its own error label and reads
    // NetworkManager.lastError when OnServerDone reports false.
    public void GetAudioAndPost(
        POSTType postType,
        string transcript,
        TMPro.TextMeshProUGUI resultText,
        TMPro.TextMeshProUGUI debugText = null,
        GameObject warningImageGO = null,
        GameObject resultPanelGO = null,
        System.Action<bool> OnServerDone = null
    )
    {
        Microphone.End("");
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim: true);
        SavWav.Save(Const.REPLAY_FILENAME, audioSource.clip, trim: true); // for debug purposes

        StartCoroutine(
            NetworkManager
                .GetManager()
                .ServerPost(
                    postType,
                    transcript,
                    wavBuffer,
                    resultText,
                    debugText,
                    warningImageGO,
                    resultPanelGO,
                    OnServerDone
                )
        );
    }

    public void GetAudioAndASR(
        GameObject transcriptGO,
        GameObject scoreButtonGO,
        DescribePanel.TaskType taskType,
        int taskNumber,
        bool isFinnish = true
    )
    {
        Microphone.End("");
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim: true);
        SavWav.Save(Const.DESCRIBE_FILENAME, audioSource.clip, trim: true); // for debug purpose

        //StartCoroutine(NetworkManager.GetManager().GPTTranscribe(wavBuffer, transcriptGO, scoreButtonGO, taskType, taskNumber, isFinnish));
        StartCoroutine(
            NetworkManager
                .GetManager()
                .GPTTranscribeWhisper(
                    wavBuffer,
                    transcriptGO,
                    scoreButtonGO,
                    taskType,
                    taskNumber,
                    isFinnish
                )
        );
    }

    public void GetAudioAndNG(
        string number,
        GameObject textErrorGO,
        TMPro.TextMeshProUGUI resultTextTMP
    )
    {
        Microphone.End("");
        byte[] wavBuffer = SavWav.GetWav(audioSource.clip, out uint length, trim: true);
        SavWav.Save(Const.NUMBERGAME_FILENAME, audioSource.clip, trim: true); // for debug purpose

        StartCoroutine(
            NetworkManager
                .GetManager()
                .NumberGamePost(number, wavBuffer, textErrorGO, resultTextTMP)
        );
    }

    /// <summary>
    /// Ends the recording and writes it to Const.ASA_FILENAME.
    /// </summary>
    ///
    /// <returns>
    /// False when nothing was actually captured, in which case no file is written and the
    /// previous one is left alone.
    /// </returns>
    ///
    /// <remarks>
    /// The check matters more than it looks. Microphone.Start allocates the whole
    /// lengthSec up front and fills it as it goes, so a clip that was never recorded into
    /// is not short - it is full-length silence. SavWav's trim does not save us either: it
    /// walks in from each end looking for a non-zero sample, and when every sample is zero
    /// neither loop ever breaks, so the bounds stay at the full buffer and it writes the
    /// entire thing. That is the "maximum length empty file" - 30 seconds of nothing,
    /// offered for replay and upload as if it were an answer.
    ///
    /// Microphone.GetPosition is how many samples the device has written, and it must be
    /// read before Microphone.End because stopping the device resets it to zero.
    /// </remarks>
    public bool StopRecording()
    {
        string device = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        int captured = Microphone.GetPosition(device);
        bool wasRecording = Microphone.IsRecording(device);

        Microphone.End("");

        if (audioSource.clip == null || !wasRecording || captured <= 0)
        {
            Debug.LogWarning(
                "Nothing was recorded - not saving. "
                    + $"clip={(audioSource.clip == null ? "null" : "present")}, "
                    + $"wasRecording={wasRecording}, capturedSamples={captured}."
            );
            return false;
        }

        SavWav.Save(Const.ASA_FILENAME, audioSource.clip, trim: true);
        return true;
    }

    public IEnumerator LoadAudioClip(string filename, GameObject replayButtonGO)
    {
        if (!string.IsNullOrEmpty(filename))
        {
            string path = System.IO.Path.Combine(
                Application.persistentDataPath,
                filename.EndsWith(".wav") ? filename : filename + ".wav"
            );

            // Need the file:// for GetAudioClip
            using (
                UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(
                    "file://" + path,
                    AudioType.WAV
                )
            )
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;

                yield return uwr.SendWebRequest();

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    Debug.LogError("Failed to reload replay audio clip");
                    Debug.LogError(uwr.result);
                    Debug.LogError(path);
                    replayClip = null;
                    if (replayButtonGO != null)
                    {
                        replayButtonGO.SetActive(false);
                    }
                    yield break;
                }

                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;

                if (dlHandler.isDone)
                {
                    Debug.Log("Replay audio clip is loaded");
                    replayClip = dlHandler.audioClip;
                    if (replayButtonGO != null)
                    {
                        replayButtonGO
                            .transform.GetComponent<Button>()
                            .onClick.AddListener(() => GetManager().PlayAudioClip(replayClip));
                        replayButtonGO.SetActive(true);
                    }
                }
            }

            yield break;
        }
    }
}
