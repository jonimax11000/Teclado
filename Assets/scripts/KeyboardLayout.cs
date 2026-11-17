using Teclado.Core;
using UnityEngine;

// Asset de distribución de un idioma. Se crea con Teclado → Importar distribución .klc.
[CreateAssetMenu(menuName = "Teclado/Distribución de teclado")]
public class KeyboardLayout : ScriptableObject
{
    public LayoutData data = new LayoutData();
}
