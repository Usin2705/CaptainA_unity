using System;
using System.Collections;
using System.IO;
using SaySuomi.Managers;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class NumberGameScreen : AppScreen
    {
        [Header("Number")]
        [Tooltip("Holds the text showing the number.")]
        [SerializeField]
        private GameObject numberLabelGO;

        [Tooltip("Picks a new number.")]
        [SerializeField]
        private Button newButton;

        [Header("Difficulty")]
        [SerializeField]
        private Toggle easyToggle;

        [SerializeField]
        private Toggle mediumToggle;

        [SerializeField]
        private Toggle hardToggle;

        [SerializeField]
        private Toggle rankToggle;

        [Header("Recording")]
        [SerializeField]
        private GameObject recordButtonGO;

        [SerializeField]
        private GameObject progressBarGO;

        [SerializeField]
        private GameObject replayButtonGO;

        [SerializeField]
        private GameObject errorTextGO;

        private NGTaskType taskType = NGTaskType.EASY;

        private AudioClip replayClip;

        private float recordingTime = Const.MAX_REC_NUMBERGAME_EASY;
        private float currentTime = Const.MAX_REC_NUMBERGAME_EASY;

        private string number = "999";

        public override bool ShowsBackButton => true;

        protected override void Bind()
        {
            ButtonWiring.Replace(recordButtonGO.GetComponent<Button>(), OnRecordButtonClicked);
            ButtonWiring.Replace(newButton, OnNewButtonClick);
        }

        private void OnEnable()
        {
            Debug.Log("NumberGameScreen OnEnable()");

            // Hide the progress bar
            if (taskType == NGTaskType.EASY)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_EASY;
            }
            else if (taskType == NGTaskType.MEDIUM)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_MEDIUM;
            }
            else if (taskType == NGTaskType.HARD)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_HARD;
            }
            else if (taskType == NGTaskType.RANK)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_RANK;
            }
        }

        private void Start()
        {
            // Add listeners
            easyToggle.onValueChanged.AddListener(
                (isOn) =>
                {
                    if (isOn)
                        SetTaskType(NGTaskType.EASY);
                }
            );
            mediumToggle.onValueChanged.AddListener(
                (isOn) =>
                {
                    if (isOn)
                        SetTaskType(NGTaskType.MEDIUM);
                }
            );
            hardToggle.onValueChanged.AddListener(
                (isOn) =>
                {
                    if (isOn)
                        SetTaskType(NGTaskType.HARD);
                }
            );
            rankToggle.onValueChanged.AddListener(
                (isOn) =>
                {
                    if (isOn)
                        SetTaskType(NGTaskType.RANK);
                }
            );

            Debug.Log("NumberGameScreen Start()");
            easyToggle.isOn = true;
        }

        private void Update()
        {
            // Only run this code if the progress bar is active
            if (progressBarGO.activeSelf == true)
            {
                UpdateProgressBar();
            }
        }

        private void OnDisable()
        {
            // The progress bar is only up while recording. Without this the microphone keeps
            // running behind the next screen, with nothing left on screen to stop it.
            if (progressBarGO != null && progressBarGO.activeSelf)
            {
                progressBarGO.SetActive(false);
                recordButtonGO.SetActive(true);

                AudioManager audioManager = AudioManager.GetManager();
                if (audioManager != null)
                {
                    audioManager.CancelRecording();
                }
            }

            // Reset the toogles to the default state
            easyToggle.isOn = true;
        }

        public void SetTaskType(NGTaskType _taskType)
        {
            // Disable the replay button
            replayButtonGO.SetActive(false);

            taskType = _taskType;

            // Generate a new number
            int taskNumber = NumberGenerator.GenerateNumber(taskType);
            // Debug.Log("Task Number: " + taskNumber);

            // Get the text TMP component from the GameObject
            // The TMP is a child of the NumberLabelGO
            numberLabelGO.GetComponentInChildren<TextMeshProUGUI>().text = taskNumber.ToString();

            // Set the countdown time based on the task type
            if (taskType == NGTaskType.EASY)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_EASY;
            }
            else if (taskType == NGTaskType.MEDIUM)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_MEDIUM;
            }
            else if (taskType == NGTaskType.HARD)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_HARD;
            }
            else if (taskType == NGTaskType.RANK)
            {
                recordingTime = Const.MAX_REC_NUMBERGAME_RANK;
            }
        }

        public void OnFinnishTimer()
        {
            TextMeshProUGUI resultTextTMP = numberLabelGO.GetComponentInChildren<TextMeshProUGUI>();
            number = TextUtils.RemoveColorTag(resultTextTMP.text);

            StartCoroutine(DelayPost());

            IEnumerator DelayPost()
            {
                Debug.Log("number: " + number);

                // Kept because a new number can be drawn before the server answers.
                string postedNumber = number;

                AudioManager
                    .GetManager()
                    .GetAudioAndNG(
                        postedNumber,
                        serverOk =>
                        {
                            NetworkManager network = NetworkManager.GetManager();

                            errorTextGO.SetActive(!serverOk);
                            if (!serverOk)
                            {
                                errorTextGO.GetComponent<TextMeshProUGUI>().text =
                                    network.lastError;
                                return;
                            }

                            resultTextTMP.text = TextUtils.FormatTextResult(
                                postedNumber,
                                network.asrResult.score
                            );
                        }
                    );
                yield return new WaitForSeconds(0.2f);

                yield return StartCoroutine(LoadAudioClip(Const.NUMBERGAME_FILENAME));
                recordButtonGO.SetActive(true);

                Button replayButton = replayButtonGO.transform.GetComponent<Button>();
                replayButton.onClick.RemoveAllListeners();
                if (replayClip != null)
                {
                    replayButton.onClick.AddListener(() =>
                        AudioManager.GetManager().PlayAudioClip(replayClip)
                    );
                    replayButtonGO.SetActive(true);
                }
                else
                {
                    replayButtonGO.SetActive(false);
                }
            }
        }

        private void OnNewButtonClick()
        {
            // Disable the replay button
            replayButtonGO.SetActive(false);

            // Retrigger the current toggle to generate a new number
            if (easyToggle.isOn)
            {
                easyToggle.isOn = false;
                easyToggle.isOn = true;
            }
            else if (mediumToggle.isOn)
            {
                mediumToggle.isOn = false;
                mediumToggle.isOn = true;
            }
            else if (hardToggle.isOn)
            {
                hardToggle.isOn = false;
                hardToggle.isOn = true;
            }
            else if (rankToggle.isOn)
            {
                rankToggle.isOn = false;
                rankToggle.isOn = true;
            }
        }

        private void StartTimer()
        /*
        *
        */
        {
            // Start countdown so the user know how long the recording will be
            currentTime = recordingTime;

            // Hide the record button
            recordButtonGO.SetActive(false);

            // Show the countdown progress bar
            progressBarGO.SetActive(true);
        }

        private void UpdateProgressBar()
        {
            /*
            *   This function will update the progress bar
            */
            currentTime -= Time.deltaTime;
            progressBarGO.GetComponent<Image>().fillAmount = currentTime / recordingTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                progressBarGO.SetActive(false);
                OnFinnishTimer();
            }
        }

        private void OnRecordButtonClicked()
        {
            progressBarGO.SetActive(true);
            // Start recording
            AudioManager.GetManager().StartRecording((int)recordingTime);

            // Start the timer
            // Should not use invoke or delay as it will cause the timer to be inaccurate
            StartTimer();
        }

        private IEnumerator LoadAudioClip(string filename)
        /*
        *   This one should be called inside the Panel (not AudioManager)
        *   as it will update the replay audio with current replay audio
        *   Calling it inside AudioManager will lead to a few seconds
        *   of empty audioclip (somehow those wasn't trimmed in AudioManager
        *   but was trimmed from the wav file.
        *   Not very efficiency to reload but at least it work for now
        */
        {
            if (!String.IsNullOrEmpty(filename))
            {
                string path = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    filename.EndsWith(".wav") ? filename : filename + ".wav"
                );

                // Need the file:// for GetAudioClip
                // TODO check with iOS version does it need sth similar
                using (
                    var uwr = UnityWebRequestMultimedia.GetAudioClip(
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
                        yield break;
                    }

                    DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)
                        uwr.downloadHandler;

                    if (dlHandler.isDone)
                    {
                        Debug.Log("Replay audio clip is loaded");
                        replayClip = dlHandler.audioClip;
                    }
                }

                yield break;
            }
        }
    }
}
