using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SaySuomi.EditorTools
{
    /// <summary>
    /// Starts, stops and configures FakeServer/fake_server.py. See docs/NEW/FAKE_SERVER.md.
    /// </summary>
    public sealed class FakeServerWindow : EditorWindow
    {
        private const string BaseUrl = "http://127.0.0.1:8765";
        private const string ControlUrl = BaseUrl + "/__fake/";
        private const string PythonCommandKey = "SaySuomi.FakeServer.PythonCommand";

        // A SessionState value, because a domain reload clears static fields.
        private const string StartedHereKey = "SaySuomi.FakeServer.StartedHere";
        private const double PollSeconds = 1;

        private static readonly string[] RelevanceOptions =
        {
            "on_topic",
            "partial",
            "off_topic",
            "none",
        };
        private static readonly string[] ProfileOptions =
        {
            "ok",
            "top",
            "bottom",
            "insufficient",
            "cohort",
            "unavailable",
        };
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2),
        };

        private static Process startedProcess;

        private bool isRunning;
        private bool isPolling;
        private double nextPollTime;
        private ControlReply settings;
        private string failText = "";
        private string errorMessage;

        private static string PythonCommand
        {
            get =>
                EditorPrefs.GetString(
                    PythonCommandKey,
                    Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3"
                );
            set => EditorPrefs.SetString(PythonCommandKey, value);
        }

        private void OnEnable()
        {
            nextPollTime = 0;
        }

        [MenuItem("Tools/Fake Server")]
        private static void Open()
        {
            GetWindow<FakeServerWindow>("Fake Server");
        }

        [InitializeOnLoadMethod]
        private static void StopWithUnity()
        {
            EditorApplication.quitting += StopServerStartedHere;
        }

        private static void StopServerStartedHere()
        {
            if (!SessionState.GetBool(StartedHereKey, false))
            {
                return;
            }

            try
            {
                Client.GetAsync(ControlUrl + "stop").Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException)
            {
                // Already stopped, which is the result this wanted.
            }
        }

        private static string DelayText(float[] delay)
        {
            string low = delay[0].ToString(CultureInfo.InvariantCulture);
            string high = delay[1].ToString(CultureInfo.InvariantCulture);
            return low == high ? low : $"{low}-{high}";
        }

        // JsonUtility cannot read a dictionary, so this reads the fail map from the raw reply and
        // writes it in the form that the server accepts.
        private static string ReadFailText(string json)
        {
            Match match = Regex.Match(json, "\"fail\":\\s*\\{([^}]*)\\}");
            return match.Groups[1].Value.Replace("\"", "").Replace(" ", "");
        }

        private void OnGUI()
        {
            DrawStatus();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }

            if (!Secret.ASA_URL.StartsWith(BaseUrl, StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox(
                    "Secret.cs does not point at this server, so the app will not use it. "
                        + "See docs/NEW/FAKE_SERVER.md.",
                    MessageType.Warning
                );
            }

            if (isRunning && settings != null)
            {
                DrawSettings();
            }
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + PollSeconds;
            ReportFailedStart();
            Refresh();
        }

        private void DrawStatus()
        {
            EditorGUILayout.HelpBox(
                isRunning ? $"Running at {BaseUrl}" : "Stopped",
                isRunning ? MessageType.Info : MessageType.None
            );

            if (isRunning)
            {
                if (GUILayout.Button("Stop"))
                {
                    Stop();
                }
                return;
            }

            string command = EditorGUILayout.DelayedTextField(
                new GUIContent("Python Command", "The command that runs Python on this computer."),
                PythonCommand
            );
            if (command != PythonCommand)
            {
                PythonCommand = command;
            }

            if (GUILayout.Button("Start"))
            {
                StartServer();
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            DrawTextSetting(
                "Delay",
                "delay",
                DelayText(settings.delay),
                "Seconds before each response, such as 2-5, 5 or 0."
            );
            DrawTextSetting(
                "Fail",
                "fail",
                failText,
                "Requests to fail, such as assess:503,profile:500. Empty means none."
            );
            DrawTextSetting(
                "Warning",
                "warning",
                string.Join(",", settings.warning),
                "Pronunciation warnings, such as 0,1. Empty means none."
            );
            DrawOptionSetting("Relevance", "relevance", RelevanceOptions, settings.relevance);
            DrawOptionSetting("Profile", "profile", ProfileOptions, settings.profile);

            if (GUILayout.Button("Reset Settings"))
            {
                Change("reset", "1");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Received", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Users", settings.users.ToString());
            EditorGUILayout.LabelField("Events", settings.events_received.ToString());
        }

        private void DrawTextSetting(string label, string key, string value, string tooltip)
        {
            // Delayed, so a half-typed value is not sent and refused on every key press.
            string entered = EditorGUILayout.DelayedTextField(
                new GUIContent(label, tooltip),
                value
            );
            if (entered != value)
            {
                Change(key, entered);
            }
        }

        private void DrawOptionSetting(string label, string key, string[] options, string value)
        {
            int current = Array.IndexOf(options, value);
            int chosen = EditorGUILayout.Popup(label, current, options);
            if (chosen != current)
            {
                Change(key, options[chosen]);
            }
        }

        private void StartServer()
        {
            errorMessage = null;
            string script = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "FakeServer", "fake_server.py")
            );

            // Output is not redirected, because nothing would read it after a domain reload, and
            // the server would block once the pipe filled.
            var startInfo = new ProcessStartInfo(PythonCommand, $"\"{script}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                startedProcess = Process.Start(startInfo);
                SessionState.SetBool(StartedHereKey, true);
                nextPollTime = 0;
            }
            catch (Exception exception)
                when (exception is Win32Exception || exception is InvalidOperationException)
            {
                errorMessage = $"Could not run \"{PythonCommand}\". Check the Python command.";
            }
        }

        private async void Stop()
        {
            errorMessage = null;
            isRunning = false;
            settings = null;
            startedProcess = null;
            SessionState.EraseBool(StartedHereKey);

            try
            {
                await Client.GetAsync(ControlUrl + "stop");
            }
            catch (Exception exception)
                when (exception is HttpRequestException || exception is OperationCanceledException)
            {
                // Already stopped, which is the result this wanted.
            }
        }

        private void ReportFailedStart()
        {
            if (startedProcess == null || !startedProcess.HasExited || isRunning)
            {
                return;
            }

            errorMessage =
                $"The server exited with code {startedProcess.ExitCode}. "
                + "Run it from a terminal to see why. See docs/NEW/FAKE_SERVER.md.";
            startedProcess = null;
            SessionState.EraseBool(StartedHereKey);
            Repaint();
        }

        private void Change(string key, string value)
        {
            errorMessage = null;
            Send($"{ControlUrl}set?{key}={Uri.EscapeDataString(value)}");
        }

        private async void Refresh()
        {
            if (isPolling)
            {
                return;
            }

            isPolling = true;
            await SendAsync(ControlUrl);
            isPolling = false;
        }

        private async void Send(string url)
        {
            await SendAsync(url);
        }

        private async Task SendAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await Client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();
                ControlReply reply = JsonUtility.FromJson<ControlReply>(json);

                isRunning = true;
                if (response.IsSuccessStatusCode)
                {
                    settings = reply;
                    failText = ReadFailText(json);
                }
                else
                {
                    errorMessage = reply.error;
                }
            }
            catch (Exception exception)
                when (exception is HttpRequestException || exception is OperationCanceledException)
            {
                isRunning = false;
                settings = null;
            }

            // Unity reports a closed window as null, and the reply may arrive after it closes.
            if (this != null)
            {
                Repaint();
            }
        }

        [Serializable]
        private class ControlReply
        {
            public float[] delay;
            public string relevance;
            public string profile;
            public int[] warning;
            public int users;
            public int events_received;
            public string error;
        }
    }
}
