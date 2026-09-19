using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageToggle : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    private async void Start()
    {
        await LocalizationSettings.InitializationOperation.Task;
        UpdateButtonText();
    }

    public async void ToggleLanguage()
    {
        await LocalizationSettings.InitializationOperation.Task;

        Locale current = LocalizationSettings.SelectedLocale;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale != current)
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (LocalizationSettings.SelectedLocale.Identifier.Code == "id")
            buttonText.text = "IND";
        else
            buttonText.text = "ENG";
    }
}