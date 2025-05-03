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

    public string _localPlayerId;    // ������ ID (�̰� �Ƹ� �ܺο� ���������)

    private void Awake() {
        // �̱��� ���� ����
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        // TODO : �÷��̾� ID �Ҵ� �ڵ� �ʿ�
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


        // TODO : �׽�Ʈ �ڵ� �����
        UpdateBossHPFromServer("Player1", 0.5f);
        UpdateBossHPFromServer("Player2", 0.2f);
        StartGameTimer(180);
    }

    /// <summary>
    /// ������ HP�� �����Ǿ��� �� UI�� ������Ʈ. ���� �ۼ��������� �̸� ����ؼ� ����
    /// </summary>
    public void UpdateBossHPFromServer(string ownerPlayerID, float hpPercentage) {
        if (_bossUI == null) {
            Debug.LogError("GamePlayManager | UpdateBossHpFromServer bossUI�� �Ҵ���� ����");
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
    /// ���� �÷��̾��� HP�� �����Ǿ��� �� UI�� ������Ʈ. ���� �ۼ��������� �̸� ����ؼ� ����
    /// </summary>
    public void UpdatePlayerHPFromServer(float hpPercentage) {
        if(_playerUI== null) {
            Debug.LogError("GamePlayManager | UpdatePlayerHP playerUI�� �Ҵ���� ����");
            return;
        }

        _playerUI.UpdatePlayerHP(hpPercentage);
    }

    /// <summary>
    /// ����(���ݷ�, ����, ���ݼӵ�, ��Ÿ�ӹ��) ������Ʈ
    /// </summary>
    public void UpdatePlayerStatsFromServer(float atk, float def, float asp, int cool) {
        if (_playerUI == null) {
            Debug.LogError("GamePlayManager | UpdatePlayerStatsFromServer playerUI�� �Ҵ���� ����");
            return;
        }
        
        _playerUI.UpdateStatTexts(atk, def, asp, cool);
    }

    /// <summary>
    /// ��ų ��Ÿ�� ����
    /// </summary>
    public void StartSkillCooldownFromServer(int skillIndex, int cooldownSeconds) {
        if (_playerUI == null) {
            Debug.LogError("GamePlayManager | StartSkillCooldownFromServer playerUI�� �Ҵ���� ����");
            return;
        }

        _playerUI.StartSkillCooldown(skillIndex, cooldownSeconds);
    }

    /// <summary>
    /// Ÿ�̸� ���� UI ������Ʈ. ���� �ð����� UI���� ������
    /// </summary>
    public void StartGameTimer(int startSeconds) {
        if (_gameTimerUI == null) {
            Debug.LogError("GamePlayManager | StartGameTimer timerUI�� �Ҵ���� ����");
            return;
        }

        _gameTimerUI.StartTimer(startSeconds);
    }

    /// <summary>
    /// ���� ���� ���� ������ Ÿ�̸� ����
    /// </summary>
    public void StopGameTimer() {
        if (_gameTimerUI == null) {
            Debug.LogError("GamePlayManager | StopGameTimer timerUI�� �Ҵ���� ����");
            return;
        }
        _gameTimerUI.StopTimer();
    }

    /// <summary>
    /// ���� �ð� ��ȯ. ���� �����Ͽ� ���� ���� �ѱ�� ���� ���
    /// </summary>
    public float GetRemainingTime() {
        if (_gameTimerUI == null) {
            Debug.LogError("GamePlayManager | StopGameTimer timerUI�� �Ҵ���� ����");
            return 0.0f;
        }

        return _gameTimerUI.GetRemainingTime();
    }

    /// <summary>
    /// PlayerPrefs�� �� ������ ����
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

        // TODO ���� ID�� �ʿ��������
        PlayerPrefs.SetString(GameResultKeys.LocalPlayerIdKey, _localPlayerId);

        PlayerPrefs.Save();
        Debug.Log("GamePlayManager_t | ���� ��� ������ ���� �Ϸ�");
    }

    public void OnGameEnd() {
        // TODO : �Ʒ� ������ ���� �ʿ�
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
