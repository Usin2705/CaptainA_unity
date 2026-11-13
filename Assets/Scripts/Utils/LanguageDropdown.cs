using System.Collections;
using System.Collections.Generic;
using SaySuomi.UserBehaviour;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Helper class to easily have the language selection dropdown in multiple panels.
/// </summary>
[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageDropdown : MonoBehaviour
{
    private TMP_Dropdown languageDropdown;
    private readonly List<Locale> dropdownLocales = new();

    private void Awake()
    {
        languageDropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        StartCoroutine(InitializeLanguageDropdown());
    }

    private IEnumerator InitializeLanguageDropdown()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var languageOptions = new List<string>();
        dropdownLocales.Clear();

        string selectedLanguage = PlayerPrefs.GetString("SelectedLanguage", "en");
        int index = -1;

        for (int i = 0; i < locales.Count; i++)
        {
            // Temporarily disable Bangla
            if (locales[i].Identifier.Code == "bn")
                continue;

            languageOptions.Add(
                locales[i].Identifier.Code switch
                {
                    // Temporarily disable Bangla
                    "bn" => "বাংলা",
                    "zh" => "简体中文",
                    "en" => "English",
                    "et" => "Eesti",
                    "fi" => "Suomi",
                    "fa" => "فارسی",
                    "ru" => "Русский",
                    "sv" => "Svenska",
                    "uk" => "Українська",
                    "vi" => "Tiếng Việt",
                    _ => locales[i].name,
                }
            );

            dropdownLocales.Add(locales[i]);

            if (locales[i].Identifier.Code == selectedLanguage)
                index = languageOptions.Count - 1;
        }

        if (index < 0)
            index = dropdownLocales.FindIndex(locale => locale.Identifier.Code == "en");

        if (index < 0)
            index = 0;

        if (LocalizationSettings.SelectedLocale != dropdownLocales[index])
            LocalizationSettings.SelectedLocale = dropdownLocales[index];

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(languageOptions);
        languageDropdown.SetValueWithoutNotify(index);
        languageDropdown.RefreshShownValue();

        languageDropdown.onValueChanged.RemoveListener(SwitchLanguage);
        languageDropdown.onValueChanged.AddListener(SwitchLanguage);

        LocalizationSettings.SelectedLocaleChanged -= OnLanguageSwitched;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageSwitched;
    }

    private void SwitchLanguage(int index)
    {
        var locale = dropdownLocales[index];
        string previous = LocalizationSettings.SelectedLocale?.Identifier.Code;
        LocalizationSettings.SelectedLocale = locale;

        PlayerPrefs.SetString("SelectedLanguage", locale.Identifier.Code);
        PlayerPrefs.Save();

        if (previous != null && previous != locale.Identifier.Code)
        {
            UserEvents.LanguagePicked(previous, locale.Identifier.Code);
        }
    }

    private void OnLanguageSwitched(Locale locale)
    {
        int index = dropdownLocales.IndexOf(locale);

        if (index < 0)
            return;

        languageDropdown.SetValueWithoutNotify(index);
        languageDropdown.RefreshShownValue();
    }

    private void OnDisable()
    {
        languageDropdown.onValueChanged.RemoveListener(SwitchLanguage);
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageSwitched;
    }
}
