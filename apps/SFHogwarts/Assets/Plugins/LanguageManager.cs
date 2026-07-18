using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/**
 * * ¿Why is LanguageManager placed in /Plugins/ folder?
 * * https://docs.unity3d.com/Manual/ScriptCompileOrderFolders.html
 * * To give him compilation priority over any other script (so translation is loaded before anything else)
 * * LanguageManager.get("phrase here");
 * *
 */
public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;
    public static Dictionary<string, string> translation = new();
    public static Dictionary<string, string> fallbackTranslation = new();

    private static readonly SystemLanguage fallbackLanguage = SystemLanguage.English;

    public static SystemLanguage[] availableLanguages =
    {
        SystemLanguage.Spanish,
        SystemLanguage.English,
        SystemLanguage.German,
        SystemLanguage.French
    };

    public static SystemLanguage? _playerLanguage;

    private static readonly bool
        recordScreenshots =
            false; // to easy the translation process (have context), take a screenshot everytime that a phrase is shown

    private static TranslationsFile translationFile;
    private static readonly string translationPath = "translation/"; // always end with /
    private static bool gotMissingTranslations;
    public static bool isClosing;
    public static bool isLoadingTranslations;
    public static bool reloadRequested = false;

    public static SystemLanguage playerLanguage
    {
        get
        {
            if (_playerLanguage == null)
            {
                if (!PlayerPrefs.HasKey("Language"))
                    PlayerPrefs.SetString("Language", Application.systemLanguage.ToString());

                _playerLanguage = stringToSystemLanguage(PlayerPrefs.GetString("Language"));
            }

            return (SystemLanguage)_playerLanguage;
        }
        set
        {
            if (Array.IndexOf(availableLanguages, value) == -1)
            {
                Debug.LogError("Attempt to set invalid unavailable language: " + value);
                return;
            }

            PlayerPrefs.SetString("Language", value.ToString());
            _playerLanguage = value;

            reloadTranslations(true);
        }
    }

    private void Awake()
    {
        Instance = this;
        if (translation.Count == 0) reloadTranslations();
    }

    private void OnApplicationQuit()
    {
        isClosing = true;
    }

    public static void reloadTranslations(bool reloadUI = false)
    {
        isLoadingTranslations = true;
        translation = new Dictionary<string, string>();
        fallbackTranslation = new Dictionary<string, string>();

        if (Array.IndexOf(availableLanguages, playerLanguage) != -1) getTranslations(playerLanguage);

        if (Application.systemLanguage != fallbackLanguage) getTranslations(fallbackLanguage, true);
        isLoadingTranslations = false;

        if (reloadUI || reloadRequested)
        {
            var translatableTexts = FindObjectsOfType<LocalizedText>();

            foreach (var text in translatableTexts) text.reload();
        }
    }

    public static SystemLanguage stringToSystemLanguage(string language)
    {
        return (SystemLanguage)Enum.Parse(typeof(SystemLanguage), language, true);
    }

    public static string get(string key)
    {
#if UNITY_EDITOR
        if (translation.Count == 0) reloadTranslations();
#else
        if (translation.Count == 0) {
            if (!isLoadingTranslations) {
                reloadTranslations();
            } else {
                reloadRequested = true;
            }
        }
#endif
        if (translation.ContainsKey(key)) return translation[key];

        if (fallbackTranslation.ContainsKey(key)) return fallbackTranslation[key];

#if UNITY_EDITOR
        gotMissingTranslations = true;
        var phrase = new PhraseTranslation(key, key);
        translation.Add(phrase.key, phrase.translation);
        Debug.Log("NOT TRANSLATED:\n" + JsonUtility.ToJson(phrase, true) + ",\n");

        if (recordScreenshots)
        {
            if (hasScreenshot(key))
                Debug.Log("Screenshot already taken");
            else
                Instance.StartCoroutine(takeScreenshot(key));
        }
#endif
        return key;
    }

    /**
     * Save the missing+current translations in a json file
     */
    private void OnDestroy()
    {
        if (!gotMissingTranslations) return;

        var phrasesList = new PhraseTranslation[translation.Count];
        var i = 0;

        foreach (var phrase in translation)
        {
            phrasesList[i] = new PhraseTranslation(phrase.Key, phrase.Value);
            i++;
        }

        translationFile.translations = phrasesList;
        var json = JsonUtility.ToJson(translationFile, true);

        // remove Translations attr from the json
        var newLine = "\n"; // to not set to Environment.NewLine as JsonUtility.ToJson always sets \n
        json = json.Remove(json.LastIndexOf(newLine));
        json = "[\n" + deleteLines(json, 2);

        setupFolders();
        File.WriteAllText(translationPath + playerLanguage + ".json", json);
    }

    private static IEnumerator takeScreenshot(string key)
    {
        yield return new WaitForSeconds(1f);
        setupFolders();
        ScreenCapture.CaptureScreenshot(translationPath + "images/" + MD5(key) + ".png");
        Debug.Log("Screenshot taken");
    }

    private static bool hasScreenshot(string key)
    {
        return File.Exists(translationPath + "images/" + MD5(key) + ".png");
    }

    private static void setupFolders()
    {
        if (!Directory.Exists(translationPath))
        {
            Directory.CreateDirectory(translationPath);
            Directory.CreateDirectory(translationPath + "images/");
        }
    }

    private static string MD5(string strToEncrypt)
    {
        var ue = new UTF8Encoding();
        var bytes = ue.GetBytes(strToEncrypt);

        // encrypt bytes
        var md5 = new MD5CryptoServiceProvider();
        var hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16)
        var hashString = "";

        for (var i = 0; i < hashBytes.Length; i++) hashString += Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');

        return hashString.PadLeft(32, '0');
    }

    private static string deleteLines(string s, int linesToRemove)
    {
        return s.Split(Environment.NewLine.ToCharArray(),
                linesToRemove + 1
            ).Skip(linesToRemove)
            .FirstOrDefault();
    }

    private static void getTranslations(SystemLanguage language, bool fallback = false)
    {
        var file = JsonUtility.FromJson<TranslationsFile>("{\"translations\":" +
                                                          ((TextAsset)Resources.Load("i18n/" + language)).text.TrimEnd(
                                                              '\r', '\n') + "}");

        if (file.translations == null)
        {
            Debug.LogError("Translation file for " + language + " is invalid");
            return;
        }

        if (!fallback) translationFile = file;

        foreach (var phrase in file.translations)
            try
            {
                if (fallback)
                    fallbackTranslation.Add(phrase.key, phrase.translation);
                else
                    translation.Add(phrase.key, phrase.translation);
            }
            catch (Exception)
            {
                Debug.LogError("Duplicated key " + phrase.key + " for " + language);
            }
    }
}

[Serializable]
public class TranslationsFile
{
    public PhraseTranslation[] translations;
}

[Serializable]
public class PhraseTranslation
{
    public string key;
    public string translation;

    public PhraseTranslation(string k, string trans)
    {
        key = k;
        translation = trans;
    }
}