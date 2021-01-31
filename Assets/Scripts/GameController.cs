using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Translator translator;
    public Text textToTranslate;

    // Start is called before the first frame update
    void Start()
    {
        translator.Load(SystemLanguage.English);
        textToTranslate.text = translator.Resolve(textToTranslate.text);
    }

}
