using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CanvasButton : MonoBehaviour,  IPointerDownHandler, IPointerUpHandler
{
    AudioSource sound;
    public UnityEvent onPress;
    public UnityEvent onRelease;

    void Start()
    {
        sound = GetComponent<AudioSource>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPress.Invoke();
        sound.Play();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onRelease.Invoke();
    }
}
