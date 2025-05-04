using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add on Button Object
/// </summary>
public class UIEventHandler : MonoBehaviour {
    public Action actionEvt = null;

    public void OnClick() {
        actionEvt?.Invoke();
    }

    
}
