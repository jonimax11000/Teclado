using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyboardButton : MonoBehaviour
{
    private Keyboard keyboard;
    public Boolean special_letter = false;
    public Boolean not_vowel = true;
    public Boolean not_Syllable_letter_for_Korean;
    
    public string[] normalForms;
    

    public string[] CapForms;

    public string alterForm;  


    private TextMeshProUGUI text;
    private string currentForm = "";
    // Start is called before the first frame update
    void Start()
    {
        ButtonVR buttonVR = GetComponentInChildren<ButtonVR>();
        CanvasButton canvasButton = GetComponentInChildren<CanvasButton>();
        keyboard = GetComponentInParent<Keyboard>();
        GetComponentInParent<KeyboardControlator>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        // Teclas como لا insertan dos caracteres: se conectan aunque la etiqueta no mida 1.
        if (text != null && (text.text.Length == 1 || (!special_letter && normalForms.Length > 0 && normalForms[0].Length > 1)))
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
                        buttonVR.onRelease.AddListener(delegate { keyboard.insertChar(currentForm); });
                    }
                    if (canvasButton is not null)
                    {
                        canvasButton.onRelease.AddListener(delegate { keyboard.insertChar(currentForm); });
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

        // Árabe: la etiqueta se dibuja unida (ﻻ) y se inserta el texto lógico (لا).
        if (text == null) return;
        currentForm = text.text;
        if (keyboard._Keyboardtype == Keyboard.KeyboardType.Arabic && !special_letter)
        {
            text.isRightToLeftText = RtlShaper.NeedsShaping(currentForm);
            text.text = RtlShaper.Shape(currentForm);
        }
    }
    
}
