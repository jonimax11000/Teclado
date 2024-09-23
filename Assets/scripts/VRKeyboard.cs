using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


public class VRKeyboard : MonoBehaviour
{
    public KeyboardControlator controlator;
    
    [HideInInspector] public bool caps, alter, closed, open, diaeresis, circunflex, virgulilla;
    
    // Start is called before the first frame update
    void Start()
    {
        caps = alter = closed = open = diaeresis = circunflex = virgulilla = false;
    }

    public void insertChar(string c)
    {
        controlator.InsertChar(c);
    }
    public void insertSpace()
    {
        controlator.InsertSpace();
    }
    
    public void deleteChar()
    {
        controlator.DeleteChar();
    }

    public void CapsPressed()
    {
        if (!caps)
        {
            caps = true;
        }
        else
        {
            caps = false;
        }
        alter = false;
        Change();
    }
    
    public void AlterPressed()
    {
        if (!alter)
        {
            closed = diaeresis=open=circunflex = virgulilla= false;
            alter = true;
        }
        else
        {
            alter = false;
        }
        caps = false;
        Change();
    }
    
    public void ClosedPressed()
    {
        if(alter)
        {
            alter = false;
        }
        if (!closed)
        {
            closed = true;
        }
        else
        {
            closed = false;
        }
        open = diaeresis=circunflex=virgulilla = false;
        Change();
    }
    
    public void OpenPressed()
    {
        if(alter)
        {
            alter = false;
        }
        if (!open)
        {
            open = true;
        }
        else
        {
            open = false;
        }
        
        closed = diaeresis=circunflex=virgulilla = false;
        Change();
    }
    
    public void DiaeresisPressed()
    {
        if(alter)
        {
            alter = false;
        }
        if (!diaeresis)
        {
            diaeresis = true;
        }
        else
        {
            diaeresis = false;
        }
        
        closed = open=circunflex=virgulilla = false;
        Change();
    }
    
    public void CircunflexPressed()
    {
        if(alter)
        {
            alter = false;
        }
        if (!circunflex)
        {
            circunflex = true;
        }
        else
        {
            circunflex = false;
        }
        closed = diaeresis=open=virgulilla = false;
        Change();
    }
    
    public void VirgulillaPressed()
    {
        if(alter)
        {
            alter = false;
        }
        if (!virgulilla)
        {
            virgulilla = true;
        }
        else
        {
            virgulilla = false;
        }
        
        closed = diaeresis=open=circunflex = false;
        Change();
    }
    public void Change()
    {
        int tam = GetComponentsInChildren<KeyboardButton>().Length;
        KeyboardButton[] buttons = new KeyboardButton[tam];
        buttons = GetComponentsInChildren<KeyboardButton>();
        for (int i = 0; i < tam; i++)
        {
            buttons[i].Change();
        }
    }
}
