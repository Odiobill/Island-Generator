using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class Translator : MonoBehaviour
{
    private Dictionary<string, string> _lang;
    private static Translator _instance;

    // Create an accessible reference to the singleton instance
    public Translator Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _lang = new Dictionary<string, string>();
    }


    public void Load(SystemLanguage language, string path = "")
    {
        _lang.Clear();

        TextAsset file = Resources.Load<TextAsset>(path + language.ToString());
        if (file == null)
        {
            file = Resources.Load<TextAsset>(SystemLanguage.English.ToString());
        }

        foreach (string line in file.text.Split('\n'))
        {
            if (line.Contains('='))
            {
                string[] prop = line.Split('=');
                _lang[prop[0]] = prop[1];
            }
        }
    }

    public string Resolve(string key)
    {
        return _lang.ContainsKey(key) ? Regex.Unescape(_lang[key]) : key;
    }

}
