using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KeyboardControlator : MonoBehaviour
{
    public TMP_InputField field;
    public GameObject RigidKeyboards;
    public GameObject CanvasKeyboards;
    public float blinkSpeed = 0.05f;

    [SerializeField] private GameObject[] RigidBodies_Keyboards;
    [SerializeField] private GameObject[] Canvas_Keyboards;
    
    [SerializeField] private GameObject Languaje_selector_Dropdown;
    private TMP_Dropdown _languaje_selector;
    [HideInInspector] public string language;
    private List<string> validate_languajes = new List<string>()
    {
        "English","Spanish","French","German","Russian",
        "Korean","Greece","Arabic"
    };
    
    // Cursor lógico propio: TMP_InputField recorta caretPosition según los glifos
    // mostrados, y con RTL (ligadura لا = 2 letras, 1 glifo) eso corrompe el texto.
    public int Caret { get; private set; }
    public bool ActiveKeyboardIsRtl { get; private set; }
    public event Action CaretChanged;

    private bool _writing;

    private void Start()
    {
        if (field != null) field.onValueChanged.AddListener(OnFieldChanged);
        if (Languaje_selector_Dropdown == null) return;

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

        var kb = RigidBodies_Keyboards[index].GetComponent<Keyboard>();
        ActiveKeyboardIsRtl = kb != null && kb._Keyboardtype == Keyboard.KeyboardType.Arabic;
        CaretChanged?.Invoke();
    }

    public void SetCaret(int logicalIndex)
    {
        int c = Mathf.Clamp(logicalIndex, 0, field.text.Length);
        if (CaretMapper.IsInsideLigature(field.text, c)) c++;
        Caret = c;
        CaretChanged?.Invoke();
    }

    public void InsertChar(string c)
    {
        int at = ClampedCaret();
        SetText(field.text.Insert(at, c));
        Caret = OutOfLigature(at + c.Length);
        CaretChanged?.Invoke();
    }

    public void IsertLinerBreak()
    {
        InsertChar("\n");
    }

    public void InsertWord(string c)
    {
        InsertChar(c);
    }

    public void InsertSpace()
    {
        InsertChar(" ");
    }

    public void DeleteChar()
    {
        int at = ClampedCaret() - 1;
        if (at < 0) return;
        SetText(field.text.Remove(at, 1));
        Caret = OutOfLigature(at);
        CaretChanged?.Invoke();
    }

    // Las flechas siguen la dirección visual: en un párrafo RTL, ◀ avanza en el texto lógico.
    public void LeftPressed()
    {
        Move(RTLTMPro.TextUtils.IsRTLInput(field.text) ? 1 : -1);
    }

    public void RightPressed()
    {
        Move(RTLTMPro.TextUtils.IsRTLInput(field.text) ? -1 : 1);
    }

    private void Move(int step)
    {
        int c = Mathf.Clamp(ClampedCaret() + step, 0, field.text.Length);
        if (CaretMapper.IsInsideLigature(field.text, c)) c = Mathf.Clamp(c + step, 0, field.text.Length);
        Caret = c;
        CaretChanged?.Invoke();
    }

    // El texto puede haber cambiado sin avisar (SetTextWithoutNotify, field reasignado...).
    private int ClampedCaret() => Mathf.Min(Caret, field.text.Length);

    private int OutOfLigature(int c) => CaretMapper.IsInsideLigature(field.text, c) ? c + 1 : c;

    private void SetText(string text)
    {
        _writing = true;
        field.text = text;
        _writing = false;
    }

    // Texto cambiado desde fuera del teclado (el integrador lo vacía, lo rellena...).
    private void OnFieldChanged(string text)
    {
        if (_writing) return;
        Caret = text.Length;
        CaretChanged?.Invoke();
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
    
    IEnumerator Line()
    {
        while (true)
        {
            InsertChar("|");
            yield return new WaitForSeconds(blinkSpeed);
            DeleteChar();
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
}
