using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class KeyboardButton : MonoBehaviour
{
    private Keyboard keyboard;
    public Boolean special_letter = false;
    public Boolean not_vowel = true;
    public Boolean not_Syllable_letter_for_Korean;
    public Dictionary<string, letterData> letterDatabase;
    
    public string[] normalForms;
    

    public string[] CapForms;

    public string alterForm;  


    private TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        ButtonVR buttonVR = GetComponentInChildren<ButtonVR>();
        CanvasButton canvasButton = GetComponentInChildren<CanvasButton>();
        keyboard = GetComponentInParent<Keyboard>();
        GetComponentInParent<KeyboardControlator>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        if (Keyboard.KeyboardType.Korean == keyboard._Keyboardtype)
        {
            TextAsset jsonTextAsset = Resources.Load<TextAsset>("Arabic");
            // Convertir el JSON en un objeto LettersDatabase
            letterDatabase = JsonConvert.DeserializeObject<Dictionary<string, letterData>>(jsonTextAsset.text);
            if (jsonTextAsset != null)
            {
                // Deserializar el JSON
                letterDatabase = JsonConvert.DeserializeObject<Dictionary<string, letterData>>(jsonTextAsset.text);

                // Obtener el nombre del GameObject
                string key = gameObject.name;

                // Comprobar si el nombre del GameObject existe en las teclas del JSON
                if (letterDatabase.ContainsKey(key))
                {
                    letterData tecla = letterDatabase[key];

                    // Asignar todas las formas a normalForms
                    normalForms = new string[5];
                    normalForms[0] = key;
                    normalForms[1] = tecla.formas.aislada; // Forma aislada
                    normalForms[2] = tecla.formas.inicial; // Forma inicial
                    normalForms[3] = tecla.formas.medial; // Forma medial
                    normalForms[4] = tecla.formas.final; // Forma final

                    // Asignar las formas con Shift a CapForms
                    CapForms = new string[1];
                    CapForms[0] = tecla.tecla_con_shift; // Forma con Shift

                    // Asignar la forma con AltGr a alterForm
                    alterForm = tecla.tecla_con_altgr; // Forma con AltGr

                }
            }
        }

        if (text !=null && text.text.Length == 1)
        {
            Change();
            switch (keyboard._Keyboardtype)
            {
                case(Keyboard.KeyboardType.Default):
                    if (buttonVR is not null)
                    {
                        buttonVR.onRelease.AddListener(delegate
                        {
                            keyboard.insertChar(text.text);
                        });
                    }

                    if (canvasButton is not null)
                    {
                        canvasButton.onRelease.AddListener(delegate { keyboard.insertChar(text.text); });
                    }

                    break;
                case(Keyboard.KeyboardType.Korean):
                    if (buttonVR is not null)
                    {
                        buttonVR.onRelease.AddListener(delegate
                        {
                            keyboard.Korean_Syllable_Formation(text.text, not_vowel,
                                not_Syllable_letter_for_Korean);
                        });
                    }

                    if (canvasButton is not null)
                    {
                        canvasButton.onRelease.AddListener(delegate
                        {
                            keyboard.Korean_Syllable_Formation(text.text, not_vowel,
                                not_Syllable_letter_for_Korean);
                        });
                    }
                    break;
                case(Keyboard.KeyboardType.Arabic):
                    if (buttonVR is not null)
                    {
                        buttonVR.onRelease.AddListener(delegate
                        {
                            keyboard.Korean_Syllable_Formation(text.text, not_vowel,
                                not_Syllable_letter_for_Korean);
                        });
                    }

                    if (canvasButton is not null)
                    {
                        canvasButton.onRelease.AddListener(delegate
                        {
                            keyboard.Korean_Syllable_Formation(text.text, not_vowel,
                                not_Syllable_letter_for_Korean);
                        });
                    }
                    break;
                default:
                    if (buttonVR is not null)
                    {
                        buttonVR.onRelease.AddListener(delegate { keyboard.insertChar(text.text); });
                    }
                    if (canvasButton is not null)
                    {
                        canvasButton.onRelease.AddListener(delegate { keyboard.insertChar(text.text); });
                    }

                    break;
            }
        }
    }
    
    public void Change()
    {
        keyboard = GetComponentInParent<Keyboard>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        
        if (!special_letter)
        {
            if (not_vowel)
            {
                if (!keyboard.caps && !keyboard.alter)
                {
                    text.text = normalForms[0];
                }
                else if (keyboard.caps)
                {
                    text.text = CapForms[0];
                }
                else if (keyboard.alter)
                {
                    text.text = alterForm;
                }
            }
            else
            {
                if (!keyboard.caps && !keyboard.alter)
                {
                    if (!keyboard.closed && !keyboard.open && !keyboard.diaeresis && !keyboard.circunflex &&
                        !keyboard.virgulilla)
                    {
                        text.text = normalForms[0];
                    }
                    else if (keyboard.closed)
                    {
                        text.text = normalForms[1];
                    }
                    else if (keyboard.open)
                    {
                        text.text = normalForms[2];
                    }
                    else if (keyboard.diaeresis)
                    {
                        text.text = normalForms[3];
                    }
                    else if (keyboard.circunflex)
                    {
                        text.text = normalForms[4];
                    }
                    else if (keyboard.virgulilla)
                    {
                        text.text = normalForms[5];
                    }
                }
                else if (keyboard.caps)
                {
                    if (!keyboard.closed && !keyboard.open && !keyboard.diaeresis && !keyboard.circunflex &&
                        !keyboard.virgulilla)
                    {
                        text.text = CapForms[0];
                    }
                    else if (keyboard.closed)
                    {
                        text.text = CapForms[1];
                    }
                    else if (keyboard.open)
                    {
                        text.text = CapForms[2];
                    }
                    else if (keyboard.diaeresis)
                    {
                        text.text = CapForms[3];
                    }
                    else if (keyboard.circunflex)
                    {
                        text.text = CapForms[4];
                    }
                    else if (keyboard.virgulilla)
                    {
                        text.text = CapForms[5];
                    }
                }
                else if (keyboard.alter)
                {
                    text.text = alterForm;
                }
            }
        }
    }
    
}

// Clase que representa los datos de una tecla
[System.Serializable]
public class letterData
{
    public string tecla_sin_shift;     // Forma sin Shift (normal)
    public string tecla_con_shift;    // Forma con Shift
    public string tecla_con_altgr;    // Forma con AltGr
    public Forms formas;             // Objeto con las diferentes formas
}

[System.Serializable]
public class Forms
{
    public string aislada;            // Forma aislada
    public string inicial;            // Forma inicial
    public string medial;             // Forma medial
    public string final;              // Forma final
}

