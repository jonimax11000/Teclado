using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


public class RigidBodyKeyboard : MonoBehaviour
{
    public KeyboardControlator controlator;

    private int num_korean_syllable;
    private string vowel, consonant;
    private List<string> korean_vowels = new List<string>(){ "ㅏ", "ㅐ", "ㅑ", "ㅒ", "ㅓ", "ㅔ", "ㅕ", "ㅖ", "ㅗ", "ㅘ", "ㅙ", "ㅚ", "ㅛ", "ㅜ", "ㅝ", "ㅞ", "ㅟ", "ㅠ", "ㅡ", "ㅢ", "ㅣ" };
    private List<string> first_korean_consonants = new List<string>(){"ㄱ","ㄲ","ㄴ","ㄷ","ㄸ","ㄹ","ㅁ","ㅂ","ㅃ","ㅅ","ㅆ","ㅇ","ㅈ",
        "ㅉ","ㅊ","ㅋ","ㅌ","ㅍ","ㅎ"};
    private List<string> second_korean_consonants = new List<string>(){ "", "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ", "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ", "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ" };


    public enum KeyboardType
    {
        Default,
        Korean,
    };

    public KeyboardType _Keyboardtype;

    [HideInInspector] public bool caps, alter, closed, open, diaeresis, circunflex, virgulilla;
    // Start is called before the first frame update
    void Start()
    {
        ResetKoreanSyllable();
        caps = alter = closed = open = diaeresis = circunflex = virgulilla = false;
    }

    public void insertChar(string c)
    {
        controlator.InsertChar(c);
    }
    
    public void insertWord(string c)
    {
        controlator.InsertWord(c);
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

    public void LeftPressed()
    {
        controlator.LeftPressed();
    }
    
    public void RightPressed()
    {
        controlator.RightPressed();
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

  
    public void Korean_Syllable_Formation(string letter, bool isConsonant, bool notSyllable)
    {
        if (!notSyllable)
        {
            if (num_korean_syllable == 0)
            {
                insertChar(letter);
                if (isConsonant)
                {
                    consonant = letter;
                    num_korean_syllable++;
                }
                else
                {
                    vowel = consonant = "";
                }
            }
            else if (num_korean_syllable == 1)
            {
                if (isConsonant)
                {
                    insertChar(letter);
                    consonant = letter;
                }
                else
                {
                    deleteChar();
                    vowel = letter;
                    int firstConsonantIndex = first_korean_consonants.IndexOf(consonant);
                    Debug.Log(firstConsonantIndex);

                    int vowelIndex = korean_vowels.IndexOf(vowel);
                    Debug.Log(vowelIndex);
                    int unicodeSyllable  = 0xAC00 + (firstConsonantIndex * 21 + vowelIndex) * 28;
                    
                    char syllable = (char)unicodeSyllable;
                    insertChar(syllable.ToString());
                    num_korean_syllable++;
                }
            }
            else if (num_korean_syllable == 2)
            {
                if (!isConsonant)
                {
                    insertChar(letter);
                    vowel = consonant = "";
                }
                else
                {
                    deleteChar();
                    
                    int firstConsonantIndex = first_korean_consonants.IndexOf(consonant);
                    int secondConsonantIndex = second_korean_consonants.IndexOf(letter);
                    int vowelIndex = korean_vowels.IndexOf(vowel);

                    int unicodeSyllable  = 0xAC00 + (firstConsonantIndex * 21 + vowelIndex) * 28+secondConsonantIndex;
                    
                    char syllable = (char)unicodeSyllable;
                    Debug.Log(syllable);
                    insertChar(syllable.ToString());
                }

                num_korean_syllable = 0;
            }
        }
        else
        {
            insertChar(letter);
            vowel = consonant = "";
            num_korean_syllable = 0;
        }
    }

    public void ResetKoreanSyllable()
    {
        vowel = consonant = "";
        num_korean_syllable = 0;
    }
}
