using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class UI_Lobby : MonoBehaviour
{
    [Header("UI Assign")]
    public TMP_Text readyText;

    public void SetPlayerReadyStatus(int ready, int total) {
        readyText.text = $"Players Loading : {ready}/{total}"; // �ϴ� 2�� �ƴ϶� total ���
    }

    public void SetAllReady() {
        readyText.text = "All players ready!";
    }

    public void SetCountdown(int seconds) {
        if (seconds == 1) {
            readyText.text = $"All players ready! Start after {seconds} second"; ;
        }
        else {
            readyText.text = $"All players ready! Start after {seconds} seconds"; ;
        }
            
    }


}
