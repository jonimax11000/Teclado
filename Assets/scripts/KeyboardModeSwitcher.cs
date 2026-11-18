using System.Collections;
using UnityEngine;

// Alterna entre teclados rígidos (físicos) y de canvas.
public class KeyboardModeSwitcher : MonoBehaviour
{
    public GameObject rigidRoot;
    public GameObject canvasRoot;

    public void ChangeToCanvas()
    {
        rigidRoot.SetActive(false);
        StartCoroutine(Show(canvasRoot));
    }

    public void ChangeToRigid()
    {
        canvasRoot.SetActive(false);
        StartCoroutine(Show(rigidRoot));
    }

    IEnumerator Show(GameObject g)
    {
        yield return new WaitForSeconds(1);
        g.SetActive(true);
    }
}
