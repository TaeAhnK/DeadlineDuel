using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResultManager : MonoBehaviour
{
    private static GameResultManager _instance;
    public static GameResultManager Instance => _instance;

    [Header("UI Assign")]
    [SerializeField] private UI_GameResult _uiGameResult;

    private string _localPlayerId;

    private void Awake() {
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        InitializePlayerIdentity();
        LoadAndDisplayResults();
        //_uiGameResult.UpdateResultInfoFromServer(
        //    "test", "test2",
        //    "player1ID", 30, 30, 30,
        //    "2dddd", 50, 50, 100
        //);
    }

    private void InitializePlayerIdentity() {
        _localPlayerId = PlayerPrefs.GetString(GameResultKeys.LocalPlayerIdKey);
    }

    private void LoadAndDisplayResults() {
        string winnerId = PlayerPrefs.GetString(GameResultKeys.WinnerKey);

        string player1Id = PlayerPrefs.GetString(GameResultKeys.Player1IdKey);
        int player1Time = PlayerPrefs.GetInt(GameResultKeys.Player1TimeKey);
        float player1BossHP = PlayerPrefs.GetFloat(GameResultKeys.Player1BossHPKey);
        int player1Deaths = PlayerPrefs.GetInt(GameResultKeys.Player1DeathsKey);

        string player2Id = PlayerPrefs.GetString(GameResultKeys.Player2IdKey);
        int player2Time = PlayerPrefs.GetInt(GameResultKeys.Player2TimeKey);
        float player2BossHP = PlayerPrefs.GetFloat(GameResultKeys.Player2BossHPKey);
        int player2Deaths = PlayerPrefs.GetInt(GameResultKeys.Player2DeathsKey);

        bool isLocalWinner = (winnerId == _localPlayerId);

        _uiGameResult.UpdateResultInfoFromServer(
            _localPlayerId, winnerId,
            player1Id, player1Time, player1BossHP, player1Deaths,
            player2Id, player2Time, player2BossHP, player2Deaths
        );

        ClearSavedData();
    }

    private void ClearSavedData() {
        PlayerPrefs.DeleteKey(GameResultKeys.LocalPlayerIdKey);
        PlayerPrefs.DeleteKey(GameResultKeys.WinnerKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player1IdKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player2IdKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player1TimeKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player2TimeKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player1BossHPKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player2BossHPKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player1DeathsKey);
        PlayerPrefs.DeleteKey(GameResultKeys.Player2DeathsKey);
    }
}
