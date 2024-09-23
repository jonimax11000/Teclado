using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyboardButton : MonoBehaviour
{
    private VRKeyboard keyboard;
    public Boolean special_letter = false;
    public Boolean not_vowel = true;
    
    public string[] normalForms;

    public string[] CapForms;

    public string alterForm;  


    private TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        ButtonVR buttonVR = GetComponentInChildren<ButtonVR>();
        CanvasButton canvasButton = GetComponentInChildren<CanvasButton>();
        keyboard = GetComponentInParent<VRKeyboard>();
        GetComponentInParent<KeyboardControlator>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        if (text !=null && text.text.Length == 1)
        {
            Change();
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

        }
    }
    
    public void Change()
    {
        keyboard = GetComponentInParent<VRKeyboard>();
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
