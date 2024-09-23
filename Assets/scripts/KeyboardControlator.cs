using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

public class KeyboardControlator : MonoBehaviour
{
    public TMP_InputField field;
    public GameObject RigidKeyboards;
    public GameObject CanvasKeyboards;
    private int caretpositon;
    public float blinkSpeed = 0.05f;

    [SerializeField] private GameObject[] RigidBodies_Keyboards;
    [SerializeField] private GameObject[] Canvas_Keyboards;
    
    [SerializeField] private GameObject Languaje_selector_Dropdown;
    private TMP_Dropdown _languaje_selector;
    [HideInInspector] public string language;
    private List<string> validate_languajes = new List<string>()
    {
        "English","Spanish",
    };
    
    private void Start()
    {
        _languaje_selector = Languaje_selector_Dropdown.GetComponent<TMP_Dropdown>();
        string language = Application.systemLanguage.ToString();
        if (validate_languajes.Contains(language))
        {
            _languaje_selector.value = validate_languajes.IndexOf(language);
        }
        else
        {
            _languaje_selector.value = 0;
        }
        ChangeLanguage(_languaje_selector.value);
        ChangeLanguage(2);
    }

    public void ChangeLanguage(int index)
    {
        for (int i = 0; i < RigidBodies_Keyboards.Length; i++)
        {
            RigidBodies_Keyboards[i].SetActive(false);
            Canvas_Keyboards[i].SetActive(false);
        }
        RigidBodies_Keyboards[index].SetActive(true);
        Canvas_Keyboards[index].SetActive(true);

        switch (index)
        {
            case 0:
                language = "en";
                break;
            case 1:
                language = "es";
                break;
            default:
                language = "no-exist";
                break;
        }
    }

    public void InsertChar(string c)
    {
        caretpositon = field.caretPosition;
        field.text =field.text.Insert(caretpositon,c);
        caretpositon += c.Length;
        field.caretPosition = caretpositon;
    }
    
    public void InsertSpace()
    {
        caretpositon = field.caretPosition;
        field.text = field.text.Insert(caretpositon++," ");
        field.caretPosition = caretpositon;
    }
    
    public void DeleteChar()
    {
        caretpositon = field.caretPosition;
        if (caretpositon > 0)
        {
            --caretpositon;
            field.text = field.text.Remove(caretpositon, 1);
            field.caretPosition = caretpositon;
        }
    }

    public void ChangeToCanvas()
    {
        RigidKeyboards.SetActive(false);
        StartCoroutine(Change(CanvasKeyboards));
    }

    public void ChangeToRigid()
    {
        CanvasKeyboards.SetActive(false);
        StartCoroutine(Change(RigidKeyboards));
    }

    IEnumerator Change(GameObject g)
    {
        yield return new WaitForSeconds(1);
        g.SetActive(true);
    }
}
