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

    // The microphone actually in use, captured when recording starts so that stopping and
    // polling both address the same device the whole way through.
    private string recordingDevice;

    // The replay handler this class added, and the button it went on. Kept so it can be
    // taken off again: LoadAudioClip runs once per recording, and without this each run
    // left another copy behind. Two copies means two Stop-then-Play pairs in one frame,
    // which is audible as a stutter, and it got worse with every recording in a session.
    // MainPanel happened to be safe only because it clears the button first; ASAPanel did
    // not, so the leak lived there.
    private Button replayButton;
    private UnityEngine.Events.UnityAction replayHandler;

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

        if (!RecordSound(lengthSec))
        {
            onStarted?.Invoke(false);
            yield break;
        }

        // Microphone.Start having returned is not the same as the microphone running. It
        // hands back a clip straight away and the device fills it later, so reporting
        // "started" here used to be a guess - and the samples sitting in the clip until the
        // device caught up are what the first recordings played back as a click.
        //
        // GetPosition moving off zero is the device's own signal that it is delivering.
        float deadline = Time.realtimeSinceStartup + Const.MIC_START_TIMEOUT_SECS;

        while (Microphone.GetPosition(recordingDevice) <= 0)
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError(
                    $"Microphone '{recordingDevice}' never produced a sample within "
                        + $"{Const.MIC_START_TIMEOUT_SECS}s - giving up rather than counting "
                        + "down against a recording that is not happening."
                );

                Microphone.End(recordingDevice);
                onStarted?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        onStarted?.Invoke(true);
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

        // Remembered rather than looked up again later. Stopping is addressed to a device
        // by name, and re-reading devices[0] at that point assumes the list has not changed
        // since - a headset unplugged mid-recording is enough to end the wrong one.
        recordingDevice = Microphone.devices[0];

        audioSource.clip = Microphone.Start(recordingDevice, false, lengthSec, Const.FREQUENCY);

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

    // GetAudioAndASR was removed with the describe-the-picture grading path: it uploaded
    // the recording for Whisper transcription and GPT scoring, and nothing reached it once
    // that panel stopped being attached to anything. See docs/legacy_gpt_vision.md.

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
        int captured = Microphone.GetPosition(recordingDevice);
        bool wasRecording = Microphone.IsRecording(recordingDevice);

        Microphone.End(recordingDevice);

        if (audioSource.clip == null || !wasRecording || captured <= 0)
        {
            Debug.LogWarning(
                "Nothing was recorded - not saving. "
                    + $"clip={(audioSource.clip == null ? "null" : "present")}, "
                    + $"wasRecording={wasRecording}, capturedSamples={captured}."
            );
            return false;
        }

        DiscardMicWarmUp(audioSource.clip, captured);

        SavWav.Save(Const.ASA_FILENAME, audioSource.clip, trim: true);
        return true;
    }

    /// <summary>
    /// Silences the first Const.MIC_WARMUP_DISCARD_MS of a finished recording.
    ///
    /// Silenced rather than cut out, because SavWav's trim already drops leading silence -
    /// so zeroing the head makes the existing pass remove it, instead of adding a second
    /// place where the start of the audio is decided.
    ///
    /// This is what actually removes the click. Waiting for the device to deliver its
    /// first sample proves it is awake; it says nothing about whether the frames it
    /// produced while waking are worth keeping.
    /// </summary>
    ///
    /// <param name="capturedSamples">
    /// How much the device really wrote. Guards the case where the whole recording is
    /// shorter than the discard window - a very quick stop would otherwise be wiped out
    /// entirely and read as "nothing was recorded".
    /// </param>
    private static void DiscardMicWarmUp(AudioClip clip, int capturedSamples)
    {
        int warmUpSamples = Const.FREQUENCY * Const.MIC_WARMUP_DISCARD_MS / 1000;

        if (warmUpSamples <= 0 || capturedSamples <= warmUpSamples)
        {
            return;
        }

        // Zero-initialised by C#, so writing it over the head IS the silencing.
        float[] silence = new float[warmUpSamples * clip.channels];
        clip.SetData(silence, 0);
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
                        // Take the previous one off before adding another. Not
                        // RemoveAllListeners: the panel owning this button has its own
                        // handler on it - ASAPanel drives the replay progress bar from
                        // one - and clearing the lot would silently break that.
                        if (replayButton != null && replayHandler != null)
                        {
                            replayButton.onClick.RemoveListener(replayHandler);
                        }

                        replayButton = replayButtonGO.transform.GetComponent<Button>();
                        replayHandler = () => GetManager().PlayAudioClip(replayClip);
                        replayButton.onClick.AddListener(replayHandler);

                        replayButtonGO.SetActive(true);
                    }
                }
            }

            yield break;
        }
    }
}
