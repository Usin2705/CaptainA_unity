using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SaySuomi.Managers;
using SaySuomi.Modals;
using SaySuomi.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class AdvancedScreen : AppScreen
    {
        [Header("Advanced Home")]
        [SerializeField]
        private Button asaButton;

        [SerializeField]
        private Button numberGameButton;

        [SerializeField]
        private TMP_Dropdown languageDropdown;

        [Header("Consent Modal")]
        [SerializeField]
        private GameObject consentPopUpGO;

        [SerializeField]
        private Button acceptButton;

        [SerializeField]
        private Button refuseButton;

        [Tooltip("Opens the privacy notice.")]
        [SerializeField]
        private Button linkButton;

        // "I have read and understood the Privacy Notice." on the consent sheet.
        // Continue stays disabled until this is ticked, so consent cannot be given by
        // reflex-tapping the primary button.
        [Tooltip("The accept button stays disabled until this is ticked.")]
        [SerializeField]
        private Toggle consentReadToggle;

        [Header("Background Form Modal")]
        [SerializeField]
        private GameObject backgroundPopUpGO;

        [Tooltip("Each toggle needs an OptionValue holding the value sent to the server.")]
        [SerializeField]
        private ToggleGroup genderOptions;

        [Tooltip("Each toggle needs an OptionValue holding the value sent to the server.")]
        [SerializeField]
        private ToggleGroup ageOptions;

        [Tooltip("Each toggle needs an OptionValue holding the value sent to the server.")]
        [SerializeField]
        private ToggleGroup selfAssessmentOptions;

        [SerializeField]
        private TMP_InputField motherTongueOptions;

        // Other languages is now one open-ended field, and unlike the rest of the form it is
        // optional - leaving it blank is a valid answer.
        [Tooltip("Optional: the learner may leave this blank.")]
        [SerializeField]
        private TMP_InputField languageOtherField;

        [SerializeField]
        private TextMeshProUGUI errorMessage;

        [SerializeField]
        private Button sendButton;

        private ModalScreen consentModal;
        private ModalScreen backgroundModal;

        private readonly List<Locale> dropdownLocales = new();

        public override bool IsTabRoot => true;

        protected override void Bind()
        {
            ButtonWiring.Replace(numberGameButton, () => OnNumberGameButtonClicked());
            ButtonWiring.Replace(asaButton, ProceedToASA);
            ButtonWiring.Replace(acceptButton, AcceptConsent);
            ButtonWiring.Replace(refuseButton, RefuseConsent);
            ButtonWiring.Replace(linkButton, LinkButtonPressed);
            ButtonWiring.Replace(sendButton, GetUserInput);

            // Continue follows the checkbox rather than being live from the start. Consent
            // that was given without the notice being acknowledged is not worth much, and
            // the disabled button is what tells the user why nothing happened.
            consentReadToggle.onValueChanged.AddListener(isOn => acceptButton.interactable = isOn);

            consentModal = ModalScreen.Attach(consentPopUpGO);
            backgroundModal = ModalScreen.Attach(backgroundPopUpGO);

            languageDropdown.onValueChanged.AddListener(SwitchLanguage);
        }

        private void OnEnable()
        {
            languageDropdown.gameObject.SetActive(false);
            StartCoroutine(InitializeLanguageDropdown());

            errorMessage.enabled = false;

            // Opening the tab is what clears its new-feature dot - the learner has now seen
            // whatever it was pointing at. Recorded here rather than on the tab button so that
            // every route in counts, and so the dot cannot be cleared by a tap that does not
            // actually arrive. TabBar reads this back and hides the dot.
            NewFeatureBadge.MarkSeen(Const.PREF_SEEN_ADVANCED, Const.VER_MAX_SHOW_ADVANCED);

            // Check if the instruction panel has been shown before
            if (!PlayerPrefs.HasKey(Const.PREF_INS_ADVANCE))
            {
                InstructionModal.Show(Const.PREF_INS_ADVANCE, Const.INSTRUCTION_ADVANCE);
            }
        }

        private void OnNumberGameButtonClicked(NGTaskType taskType = NGTaskType.EASY)
        {
            // Go to number game (not very polished or functional for now)
            Navigator.GetManager().Push<NumberGameScreen>().SetTaskType(taskType);
        }

        // The assessment used to sit behind a secret code while it was being trialled. It is a
        // public feature now, so the button goes straight through - consent and the background
        // form are the only gates that remain.
        private void ProceedToASA()
        {
            Navigator navigator = Navigator.GetManager();

            // Go to ASA task selection if consent has been given and the background form has been filled
            // Open relevant pop-ups otherwise
            if (
                PlayerPrefs.GetInt("ConsentGiven", 0) == 1
                && PlayerPrefs.GetInt("BackgroundFormCompleted", 0) == 1
            )
            {
                Debug.Log(PlayerPrefs.GetString("UserGuid"));
                navigator.Push<ASATaskListScreen>();
            }

            if (
                PlayerPrefs.GetInt("ConsentGiven", 0) == 1
                && PlayerPrefs.GetInt("BackgroundFormCompleted", 0) == 0
            )
            {
                navigator.Open(backgroundModal);
            }

            if (
                PlayerPrefs.GetInt("ConsentGiven", 0) == 0
                && PlayerPrefs.GetInt("BackgroundFormCompleted", 0) == 0
            )
            {
                ResetConsentSheet();
                navigator.Open(consentModal);
            }
        }

        // The sheet is a single reused object, so clear it every time it opens. Without this
        // a user who ticked the box, backed out, and came back would find Continue already
        // enabled and could consent without seeing the notice again.
        private void ResetConsentSheet()
        {
            consentReadToggle.isOn = false;
            acceptButton.interactable = false;
        }

        private void LinkButtonPressed()
        {
            Application.OpenURL(
                "https://aalto-speech.github.io/privacy-notices/saysuomi-privacy.html"
            );
        }

        private void AcceptConsent()
        {
            // Create new user after consent has been given
            string guid = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("UserGuid", guid);

            PlayerPrefs.SetInt("ConsentGiven", 1);

            PlayerPrefs.SetString("AppVersion", Application.version);

            string timestamp = System.DateTime.UtcNow.ToString("o");
            PlayerPrefs.SetString("ConsentTimestamp", timestamp);
            PlayerPrefs.Save();

            Navigator navigator = Navigator.GetManager();
            navigator.Back();
            navigator.Open(backgroundModal);
        }

        private void RefuseConsent()
        {
            Navigator.GetManager().Back();
        }

        private void GetUserInput()
        {
            // Set all relevant values for the user based on filled background form
            bool isValid = ValidateInformation();
            if (!isValid)
            {
                return;
            }

            var gender = genderOptions
                .ActiveToggles()
                .FirstOrDefault()
                .gameObject.GetComponent<OptionValue>()
                .value;

            var age = ageOptions
                .ActiveToggles()
                .FirstOrDefault()
                .gameObject.GetComponent<OptionValue>()
                .value;

            var motherTongue = new List<string> { motherTongueOptions.text };

            // Open-ended and optional: whatever the user typed, or an empty string. The
            // server accepts other_languages as free text and allows it to be empty.
            var otherLanguages = languageOtherField.text.Trim();

            var selfAssessment = selfAssessmentOptions
                .ActiveToggles()
                .FirstOrDefault()
                .gameObject.GetComponent<OptionValue>()
                .value;

            string timestamp = System.DateTime.UtcNow.ToString("o");
            PlayerPrefs.SetString("BackgroundTimestamp", timestamp);
            PlayerPrefs.Save();

            string motherWrapped = string.Join("\n", motherTongue);

            // Prepare the form to be sent to the server.
            // moved_to_finland and finnish_learning_duration are no longer collected. The
            // server made both nullable in v1.2.0, so omitting them is accepted - see
            // docs/TO_FRONTEND.md item 10.
            BackgroundFormData backgroundFormData = new()
            {
                gender = ("gender", gender),
                age = ("age_group", age),
                motherTongue = ("native_languages", motherWrapped),
                otherLanguages = ("other_languages", otherLanguages),
                selfAssessment = ("finnish_self_assessment", selfAssessment),
            };

            // Send consent and background form to server.
            //
            // Nothing below this point may run until the server confirms the user exists.
            // This used to mark BackgroundFormCompleted and open the task panel immediately,
            // before the request had even been sent - so a failed onboarding still let the
            // user into the assessment with a guid the server had never heard of. Every
            // assessment then answered 404, and because the form was already flagged complete
            // the app never asked again: a permanent dead end that only a reinstall cleared.
            sendButton.interactable = false;
            errorMessage.enabled = false;

            // Back waits too, because leaving now would open the task list behind the learner
            // when the server answers.
            backgroundModal.Dismissible = false;

            StartCoroutine(
                NetworkManager
                    .GetManager()
                    .ServerPost_guid(
                        POSTType.ASA_CONSENT,
                        backgroundFormData,
                        serverOk =>
                        {
                            sendButton.interactable = true;
                            backgroundModal.Dismissible = true;

                            if (!serverOk)
                            {
                                ShowOnboardingError();
                                return;
                            }

                            // Only now is the user real on the server.
                            PlayerPrefs.SetInt("BackgroundFormCompleted", 1);
                            PlayerPrefs.Save();

                            Navigator navigator = Navigator.GetManager();
                            navigator.Back();
                            navigator.Push<ASATaskListScreen>();
                        }
                    )
            );
        }

        // Keeps the user on the background form and tells them why they are still there.
        // The type is included because "something went wrong" is useless in a bug report,
        // and for a 422 the user genuinely cannot fix it by trying again.
        private void ShowOnboardingError()
        {
            NetworkManager network = NetworkManager.GetManager();
            string reason = string.IsNullOrEmpty(network.lastError)
                ? "Could not create your account."
                : network.lastError;

            if (!string.IsNullOrEmpty(network.lastErrorType))
            {
                reason += $"\n({network.lastErrorType})";
            }

            errorMessage.text = reason;
            errorMessage.enabled = true;
        }

        private bool ValidateInformation()
        {
            // Every remaining question is mandatory except other languages, which is an
            // open-ended field the user is allowed to leave blank.
            if (
                genderOptions.AnyTogglesOn()
                && ageOptions.AnyTogglesOn()
                && selfAssessmentOptions.AnyTogglesOn()
                && !string.IsNullOrWhiteSpace(motherTongueOptions.text)
            )
            {
                return true;
            }
            errorMessage.enabled = true;
            return false;
        }

        private IEnumerator InitializeLanguageDropdown()
        {
            yield return LocalizationSettings.InitializationOperation;

            var locales = LocalizationSettings.AvailableLocales.Locales;
            var languageOptions = new List<string>();
            dropdownLocales.Clear();

            string selectedLanguage = PlayerPrefs.GetString("SelectedLanguage", "en");
            int currentIndex = -1;

            for (int i = 0; i < locales.Count; i++)
            {
                // Temporarily disable Bangla and Persian
                if (locales[i].Identifier.Code == "bn" || locales[i].Identifier.Code == "fa")
                {
                    continue;
                }

                languageOptions.Add(
                    locales[i].Identifier.Code switch
                    {
                        // "bn" => "Bangla",
                        "zh" => "Chinese",
                        "en" => "English",
                        "et" => "Estonian",
                        "fi" => "Finnish",
                        // "fa" => "Persian",
                        "ru" => "Russian",
                        "sv" => "Swedish",
                        "uk" => "Ukrainian",
                        "vi" => "Vietnamese",
                        _ => locales[i].name,
                    }
                );

                dropdownLocales.Add(locales[i]);

                if (locales[i].Identifier.Code == selectedLanguage)
                {
                    currentIndex = languageOptions.Count - 1;
                }
            }

            if (currentIndex < 0)
            {
                currentIndex = dropdownLocales.FindIndex(locale => locale.Identifier.Code == "en");
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            LocalizationSettings.SelectedLocale = dropdownLocales[currentIndex];

            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(languageOptions);

            languageDropdown.SetValueWithoutNotify(currentIndex);
            languageDropdown.RefreshShownValue();

            languageDropdown.gameObject.SetActive(true);
        }

        private void SwitchLanguage(int currentIndex)
        {
            var locale = dropdownLocales[currentIndex];
            LocalizationSettings.SelectedLocale = locale;

            PlayerPrefs.SetString("SelectedLanguage", locale.Identifier.Code);
            PlayerPrefs.Save();
        }
    }
}
