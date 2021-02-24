using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Translator
{
    private static Dictionary<string, string> _lang = new Dictionary<string, string>();

    public static void Load(SystemLanguage language, string path = "")
    {
        _lang.Clear();

        var file = Resources.Load<TextAsset>(path + language);
        if (file != null)
        {
            foreach (var line in file.text.Split('\n'))
            {
                if (line.Contains('='))
                {
                    var part = line.Split('=');
                    _lang[part[0]] = part[1];
                }
            }
        }
        else
        {
            Debug.LogError("Cannot load '" + path + language + "'");
        }
    }

    public static string Resolve(string key, Dictionary<string, string> subKeys = null)
    {
        var translation = _lang.ContainsKey(key) ? Regex.Unescape(_lang[key]) : key;
        if (subKeys != null)
        {
            foreach (var subKey in subKeys.Keys)
            {
                translation = translation.Replace(subKey, subKeys[subKey]);
            }
        }
        return translation;
    }
}