using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// Dropdown de idiomas: activa el par de teclados (rígido y canvas) y cambia la distribución de la sesión.
public class LanguageSelector : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public KeyboardLayout layout;
        public GameObject rigid;
        public GameObject canvas;
    }

    public TMP_Dropdown dropdown;
    public KeyboardController controller;
    public List<Entry> entries = new List<Entry>();

    void Start()
    {
        entries = entries.OrderBy(e => e.layout.data.order).ToList();
        dropdown.ClearOptions();
        dropdown.AddOptions(entries.Select(e => e.layout.data.displayName).ToList());
        string system = Application.systemLanguage.ToString();
        int index = Math.Max(0, entries.FindIndex(e => e.layout.data.systemLanguage == system));
        dropdown.SetValueWithoutNotify(index);
        Select(index);
    }

    public void Select(int index)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].rigid != null) entries[i].rigid.SetActive(i == index);
            if (entries[i].canvas != null) entries[i].canvas.SetActive(i == index);
        }
        controller.Session.SetLayout(entries[index].layout.data);
    }
}
