using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Text exampleText;
    public string playerName;

    // Start is called before the first frame update
    void Start()
    {
        Translator.Load(SystemLanguage.English);

        Dictionary<string, string> subKeys = new Dictionary<string, string>();
        subKeys.Add("PLAYERNAME", playerName);
        exampleText.text = Translator.Resolve(exampleText.text, subKeys);
    }
}
