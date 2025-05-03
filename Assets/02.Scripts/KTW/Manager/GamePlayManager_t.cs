using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayManager_t : NetworkBehaviour
{
    private static GamePlayManager_t _instance;
    public static GamePlayManager_t Instance => _instance;

    [SerializeField] private string _resultSceneName = "05.GameResult";

    [Header("UI")]
    [SerializeField] private UI_Boss _bossUI;
    [SerializeField] private UI_PlayerUI _playerUI;
    [SerializeField] private UI_GameTimer _gameTimerUI;

    public string _localPlayerId;    // 본인의 ID (이걸 아마 외부에 들고있을것)

    private void Awake() {
        // 싱글톤 패턴 구현
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        // TODO : 플레이어 ID 할당 코드 필요
        _localPlayerId = "Player1";

        if (_bossUI == null) {
            _bossUI = FindObjectOfType<UI_Boss>();
        }
        if (_playerUI == null) {
            _playerUI = FindObjectOfType<UI_PlayerUI>();
        }

        if (_gameTimerUI == null) {
            _gameTimerUI = FindObjectOfType<UI_GameTimer>();
        }


        // TODO : 테스트 코드 지울것
        UpdateBossHPFromServer("Player1", 0.5f);
        UpdateBossHPFromServer("Player2", 0.2f);
        StartGameTimer(180);
    }

    /// <summary>
    /// 보스의 HP가 변동되었을 시 UI에 업데이트. 값은 퍼센테이지로 미리 계산해서 전달
    /// </summary>
    public void UpdateBossHPFromServer(string ownerPlayerID, float hpPercentage) {
        if (_bossUI == null) {
            Debug.LogError("GamePlayManager | UpdateBossHpFromServer bossUI가 할당되지 않음");
            return;
        }

        if (ownerPlayerID == _localPlayerId) {
            _bossUI.UpdatePlayerBossHP(hpPercentage); 
        }
        else {
            _bossUI.UpdateEnemyBossHP(hpPercentage);
        }
    }

    /// <summary>
    /// 로컬 플레이어의 HP가 변동되었을 시 UI에 업데이트. 값은 퍼센테이지로 미리 계산해서 전달
    /// </summary>
    public void UpdatePlayerHPFromServer(float hpPercentage) {
        if(_playerUI== null) {
            Debug.LogError("GamePlayManager | UpdatePlayerHP playerUI가 할당되지 않음");
            return;
        }

        _playerUI.UpdatePlayerHP(hpPercentage);
    }

    /// <summary>
    /// 스탯(공격력, 방어력, 공격속도, 쿨타임배수) 업데이트
    /// </summary>
    public void UpdatePlayerStatsFromServer(float atk, float def, float asp, int cool) {
        if (_playerUI == null) {
            Debug.LogError("GamePlayManager | UpdatePlayerStatsFromServer playerUI가 할당되지 않음");
            return;
        }
        
        _playerUI.UpdateStatTexts(atk, def, asp, cool);
    }

    /// <summary>
    /// 스킬 쿨타임 시작
    /// </summary>
    public void StartSkillCooldownFromServer(int skillIndex, int cooldownSeconds) {
        if (_playerUI == null) {
            Debug.LogError("GamePlayManager | StartSkillCooldownFromServer playerUI가 할당되지 않음");
            return;
        }

        _playerUI.StartSkillCooldown(skillIndex, cooldownSeconds);
    }

    /// <summary>
    /// 타이머 시작 UI 업데이트. 남은 시간값은 UI에서 관리함
    /// </summary>
    public void StartGameTimer(int startSeconds) {
        if (_gameTimerUI == null) {
            Debug.LogError("GamePlayManager | StartGameTimer timerUI가 할당되지 않음");
            return;
        }

        _gameTimerUI.StartTimer(startSeconds);
    }

    /// <summary>
    /// 게임 종료 등의 이유로 타이머 중지
    /// </summary>
    public void StopGameTimer() {
        if (_gameTimerUI == null) {
            Debug.LogError("GamePlayManager | StopGameTimer timerUI가 할당되지 않음");
            return;
        }
        _gameTimerUI.StopTimer();
    }

    /// <summary>
    /// 남은 시간 반환. 이후 저장하여 다음 씬에 넘기는 데에 사용
    /// </summary>
    public float GetRemainingTime() {
        if (_gameTimerUI == null) {
            Debug.LogError("GamePlayManager | StopGameTimer timerUI가 할당되지 않음");
            return 0.0f;
        }

        return _gameTimerUI.GetRemainingTime();
    }

    /// <summary>
    /// PlayerPrefs에 각 데이터 저장
    /// </summary>
    public void SaveGameResult(
        string winnerId,
        string player1Id, int player1Time, float player1BossHP, int player1Deaths,
        string player2Id, int player2Time, float player2BossHP, int player2Deaths
        ) {
        PlayerPrefs.SetString(GameResultKeys.WinnerKey, winnerId);

        PlayerPrefs.SetString(GameResultKeys.Player1IdKey, player1Id);
        PlayerPrefs.SetInt(GameResultKeys.Player1TimeKey, player1Time);
        PlayerPrefs.SetFloat(GameResultKeys.Player1BossHPKey, player1BossHP);
        PlayerPrefs.SetInt(GameResultKeys.Player1DeathsKey, player1Deaths);

        PlayerPrefs.SetString(GameResultKeys.Player2IdKey, player2Id);
        PlayerPrefs.SetInt(GameResultKeys.Player2TimeKey, player2Time);
        PlayerPrefs.SetFloat(GameResultKeys.Player2BossHPKey, player2BossHP);
        PlayerPrefs.SetInt(GameResultKeys.Player2DeathsKey, player2Deaths);

        // TODO 본인 ID는 필요없을지도
        PlayerPrefs.SetString(GameResultKeys.LocalPlayerIdKey, _localPlayerId);

        PlayerPrefs.Save();
        Debug.Log("GamePlayManager_t | 게임 결과 데이터 저장 완료");
    }

    public void OnGameEnd() {
        // TODO : 아래 변수들 조정 필요
        int remainingTime = (int)_gameTimerUI.GetRemainingTime();
        float playerBossHP = 50.0f;
        float enemyBossHP = 50.0f;
        int playerDeath = 5;
        int enemyDeath = 10;

        string winnerId = "Player1";
        string player1Id = "Player1";
        string player2Id = "Player2";

        SaveGameResult(
            winnerId,
            player1Id, remainingTime, playerBossHP, playerDeath,
            player2Id, remainingTime, enemyBossHP, enemyDeath
        );

        SceneManager.LoadScene(_resultSceneName);
    }
}
