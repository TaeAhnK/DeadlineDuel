using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_GameResult : MonoBehaviour {
    [Header("UI Assign")]
    [SerializeField] private Button toLobbyButton;
    [SerializeField] private Button viewStatsButton;
    [SerializeField] private GameObject winTextObject;
    [SerializeField] private GameObject loseTextObject;
    [SerializeField] private GameObject playerInfoChunk;
    [SerializeField] private GameObject enemyInfoChunk;

    private void Start() {
        toLobbyButton.onClick.AddListener(() => OnClickToLobbyButton());
        viewStatsButton.onClick.AddListener(() => OnClickViewStatsButton());
    }

    private void OnClickToLobbyButton() {
        Debug.Log("로비 버튼클릭");
    }

    private void OnClickViewStatsButton() {
        Debug.Log("스탯 버튼클릭");
    }

    public void SetWinTextSetActive(bool isWin) {
        winTextObject.SetActive(isWin);
        loseTextObject.SetActive(!isWin);
    }
    
    /// <summary>
    /// 서버에서 받은 정보로 UI 업데이트
    /// </summary>
    public void UpdateResultInfoFromServer(string localPlayerID, string winnerPlayerID,
        string player1ID, int player1Time, float player1BossHP, int player1DeathCount,
        string player2ID, int player2Time, float player2BossHP, int player2DeathCount
        ) {
        bool isWinner = (localPlayerID == winnerPlayerID);

        UpdateResultChunk(playerInfoChunk, player1ID, player1Time, player1BossHP, player1DeathCount);
        UpdateResultChunk(enemyInfoChunk, player2ID, player2Time, player2BossHP, player2DeathCount);

        SetWinTextSetActive(isWinner);
    }

    private void UpdateResultChunk(GameObject chunk, string id, int timeSeconds, float bossHPPercent, int deathCount) {
        if (chunk == null) {
            Debug.LogError("InfoChunk가 할당되지 않았습니다.");
            return;
        }

        // 시간 int → mm:ss 포맷으로 변환
        int min = timeSeconds / 60;
        int sec = timeSeconds % 60;
        string timeString = $"{min:00}:{sec:00}";

        // 자식 순서대로 값 업데이트
        TMP_Text idText = chunk.transform.GetChild(0).GetComponent<TMP_Text>();
        TMP_Text timeText = chunk.transform.GetChild(1).GetComponent<TMP_Text>();
        TMP_Text bossHPText = chunk.transform.GetChild(2).GetComponent<TMP_Text>();
        TMP_Text deathText = chunk.transform.GetChild(3).GetComponent<TMP_Text>();

        if (idText != null) idText.text = id;
        if (timeText != null) timeText.text = $"Time : {timeString}";
        if (bossHPText != null) bossHPText.text = $"Boss HP : {bossHPPercent:0.00}%";
        if (deathText != null) deathText.text = $"Death : {deathCount}";
    }

}
