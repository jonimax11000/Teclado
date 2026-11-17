using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

// Una tecla: muestra el texto de la capa activa y avisa al pulsarse.
public class KeyView : MonoBehaviour
{
    public int scanCode;
    public TextMeshProUGUI label;
    public event Action<int> Pressed;

    void Start()
    {
        var vr = GetComponentInChildren<ButtonVR>(true);
        if (vr != null) vr.onRelease.AddListener(Fire);
        var canvas = GetComponentInChildren<CanvasButton>(true);
        if (canvas != null) canvas.onRelease.AddListener(Fire);
    }

    void Fire() => Pressed?.Invoke(scanCode);

    public void SetLabel(string logical)
    {
        if (label == null) return;
        logical ??= "";
        bool onlyMarks = logical.Length > 0 && logical.All(c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark);
        if (onlyMarks)
        {
            label.isRightToLeftText = false;
            label.text = "◌" + logical;   // las marcas combinantes se muestran sobre un círculo punteado
            return;
        }
        label.isRightToLeftText = RtlShaper.NeedsShaping(logical);
        label.text = RtlShaper.Shape(logical);
    }
}
