using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Base : MonoBehaviour
{
    public void BindEvent(Button button, Action action) {
        UIEventHandler handler = button.GetComponent<UIEventHandler>();
        if (handler == null) {
            handler = button.gameObject.AddComponent<UIEventHandler>();
        }

        handler.actionEvt += action;
        button.onClick.AddListener(handler.OnClick);
    }
}
