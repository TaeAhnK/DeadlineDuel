using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayManager : NetworkBehaviour
{
    private static GamePlayManager _instance;
    public static GamePlayManager Instance => _instance;

    [Header("Scene Management")]
    [SerializeField] private string _resultSceneName = "05.GameResult";

    [Header("Spawning")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private GameObject[] bossSpawnPillars;
    
    [Header("UI")]
    [SerializeField] private UI_Boss _bossUI;
    [SerializeField] private UI_PlayerUI _playerUI;
    [SerializeField] private UI_GameTimer _gameTimerUI;
    [SerializeField] private UI_BuffDebuff _buffDebuffUI;
    
    [Header("Game Settings")]
    [SerializeField] private float gameTime = 180f; // 기본 3분
    
    // 플레이어 추적용 변수
    private Dictionary<string, NetworkObject> connectedPlayers = new Dictionary<string, NetworkObject>();
    private Dictionary<string, bool> playerPillarInteraction = new Dictionary<string, bool>();
    private Dictionary<string, int> playerDeathCount = new Dictionary<string, int>();
    private NetworkObject spawnedBoss;
    
    // 게임 상태 네트워크 변수
    private NetworkVariable<bool> isBossSpawned = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);
    private NetworkVariable<float> bossHpPercentage = new NetworkVariable<float>(1.0f);
    
    // 로컬 플레이어 정보
    public string localPlayerId { get; private set; }
    
    // 보스 스폰 이벤트
    public System.Action OnBossSpawned;  
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        FindRequiredComponents();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // 서버 측 게임 상태 초기화 및 콜백 등록
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // 게임 상태 변경 콜백 설정
            isBossSpawned.OnValueChanged += OnBossSpawnStateChanged;
            isGameOver.OnValueChanged += OnGameStateChanged;
            bossHpPercentage.OnValueChanged += OnBossHpChanged;
        }
        
        // 클라이언트 ID를 기준으로 로컬 플레이어 ID 설정
        localPlayerId = "Player" + NetworkManager.Singleton.LocalClientId.ToString();
        
        // 클라이언트에서 로컬 플레이어 초기화
        InitializeLocalPlayer();
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            // 콜백 해제
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            
            isBossSpawned.OnValueChanged -= OnBossSpawnStateChanged;
            isGameOver.OnValueChanged -= OnGameStateChanged;
            bossHpPercentage.OnValueChanged -= OnBossHpChanged;
        }
        
        base.OnNetworkDespawn();
    }
    
    private void FindRequiredComponents()
    {
        // 필요한 UI 컴포넌트들 찾기
        if (_bossUI == null) _bossUI = FindObjectOfType<UI_Boss>();
        if (_playerUI == null) _playerUI = FindObjectOfType<UI_PlayerUI>();
        if (_gameTimerUI == null) _gameTimerUI = FindObjectOfType<UI_GameTimer>();
        if (_buffDebuffUI == null) _buffDebuffUI = FindObjectOfType<UI_BuffDebuff>();
    }
    
    #region Server-side Game Management
    
    private void OnClientConnected(ulong clientId)
    {
        string playerId = "Player" + clientId.ToString();
        Debug.Log($"플레이어 연결됨: {playerId}");
        
        // 플레이어를 추적 딕셔너리에 추가
        playerPillarInteraction.Add(playerId, false);
        playerDeathCount.Add(playerId, 0);
        
        // 플레이어 스폰 처리
        SpawnPlayerServerRpc(clientId);
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        string playerId = "Player" + clientId.ToString();
        Debug.Log($"플레이어 연결 해제됨: {playerId}");
        
        if (connectedPlayers.ContainsKey(playerId))
        {
            NetworkObject playerObj = connectedPlayers[playerId];
            if (playerObj != null && playerObj.IsSpawned)
            {
                playerObj.Despawn();
            }
            connectedPlayers.Remove(playerId);
        }
        
        playerPillarInteraction.Remove(playerId);
        playerDeathCount.Remove(playerId);
        
        // 연결 해제로 인해 게임을 종료해야 하는지 확인
        CheckGameState();
    }
    
    [ServerRpc]
    private void SpawnPlayerServerRpc(ulong clientId)
    {
        // 서버만 실행해야 함
        if (!IsServer) return;
        
        string playerId = "Player" + clientId.ToString();
        
        // 사용 가능한 스폰 지점 찾기
        Transform spawnPoint = GetAvailableSpawnPoint(playerId);
        
        if (spawnPoint == null)
        {
            Debug.LogError($"{playerId}를 위한 스폰 지점이 없습니다");
            return;
        }
        
        // 플레이어 인스턴스화 및 네트워크에 스폰
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        
        if (networkObject != null)
        {
            networkObject.SpawnAsPlayerObject(clientId);
            
            // Object_Base 컴포넌트가 있으면 플레이어 ID 설정
            Object_Base playerBase = playerInstance.GetComponent<Object_Base>();
            if (playerBase != null)
            {
                playerBase.SetPlayerIdServerRpc(playerId);
            }
            
            connectedPlayers[playerId] = networkObject;
        }
        else
        {
            Debug.LogError("플레이어 프리팹에 NetworkObject 컴포넌트가 없습니다");
            Destroy(playerInstance);
        }
    }
    
    private Transform GetAvailableSpawnPoint(string playerId)
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            return null;
            
        // 플레이어 ID를 기준으로 인덱스 결정
        int playerIndex = int.Parse(playerId.Replace("Player", ""));
        int spawnIndex = playerIndex % playerSpawnPoints.Length;
        
        return playerSpawnPoints[spawnIndex];
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PillarInteractionServerRpc(string playerId)
    {
        if (!IsServer) return;
        
        Debug.Log($"플레이어 {playerId}가 기둥과 상호작용함");
        
        if (playerPillarInteraction.ContainsKey(playerId))
        {
            playerPillarInteraction[playerId] = true;
            
            // 모든 플레이어가 기둥과 상호작용했는지 확인
            CheckBossSpawnCondition();
        }
    }
    
    private void CheckBossSpawnCondition()
    {
        // 연결된 모든 플레이어가 기둥과 상호작용했는지 확인
        bool allInteracted = true;
        foreach (var interaction in playerPillarInteraction)
        {
            if (!interaction.Value)
            {
                allInteracted = false;
                break;
            }
        }
        
        // 모든 플레이어가 기둥과 상호작용했다면 보스 스폰
        if (allInteracted && !isBossSpawned.Value)
        {
            SpawnBoss();
        }
    }
    
    private void SpawnBoss()
    {
        if (!IsServer) return;
    
        Debug.Log("보스 스폰 중");
    
        // 보스 스폰 지점에 보스 인스턴스화
        GameObject bossInstance = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
        NetworkObject bossNetObject = bossInstance.GetComponent<NetworkObject>();
    
        if (bossNetObject != null)
        {
            bossNetObject.Spawn();
            spawnedBoss = bossNetObject;
        
            // 보스 이벤트 설정
            Object_Base bossBase = bossInstance.GetComponent<Object_Base>();
            if (bossBase != null)
            {
                bossBase.OnDamageTaken += UpdateBossHP;
                bossBase.OnObjectDestroyed += OnBossDefeated;
            }
        
            // 보스 스폰 시 게임 타이머 시작
            StartGameTimer();
        
            // 클라이언트에 알리기 위해 네트워크 변수 업데이트
            isBossSpawned.Value = true;
        
            // 보스 스폰 이벤트 호출
            OnBossSpawned?.Invoke();
        }
        else
        {
            Debug.LogError("보스 프리팹에 NetworkObject 컴포넌트가 없습니다");
            Destroy(bossInstance);
        }
    }
    
    private void UpdateBossHP(float currentHP, float maxHP)
    {
        if (!IsServer) return;
        
        float hpPercentage = Mathf.Clamp01(currentHP / maxHP);
        bossHpPercentage.Value = hpPercentage;
        
        Debug.Log($"보스 HP 업데이트: {hpPercentage:P}");
    }
    
    private void OnBossDefeated(Object_Base obj)
    {
        if (!IsServer) return;
        
        Debug.Log("보스 처치됨!");
        
        // 보스 처치 조건으로 게임 종료
        EndGame(true);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerDeathServerRpc(string playerId)
    {
        if (!IsServer) return;
        
        if (playerDeathCount.ContainsKey(playerId))
        {
            playerDeathCount[playerId]++;
            // 플레이어 사망 시 게임 종료
            if (!isGameOver.Value)
            {
                // 사망하지 않은 다른 플레이어를 승자로 지정
                string winnerId = DetermineWinnerOnPlayerDeath(playerId);
                EndGame(false, winnerId);
            }
        }
    }
    
    private string DetermineWinnerOnPlayerDeath(string deadPlayerId)
    {
        // 사망한 플레이어가 아닌 다른 플레이어를 찾아 승자로 지정
        foreach (var player in connectedPlayers.Keys)
        {
            if (player != deadPlayerId)
            {
                Debug.Log($"플레이어 {deadPlayerId} 사망으로 인해 {player}가 승리함");
                return player;
            }
        }
    
        // 다른 플레이어가 없으면 "None"
        return "None";
    }
    private IEnumerator GameTimerCoroutine()
    {
        float remainingTime = gameTime;
    
        while (remainingTime > 0 && !isGameOver.Value)
        {
            yield return new WaitForSeconds(1.0f);
            remainingTime -= 1.0f;
        }
    
        // 시간 종료, 게임 종료
        if (!isGameOver.Value)
        {
            EndGame(false);
        }
    }
    
    private void EndGame(bool bossDefeated, string specificWinnerId = null)
    {
        if (!IsServer || isGameOver.Value) return;

        Debug.Log($"게임 종료. 보스 처치 여부: {bossDefeated}");

        // 명시적 승자가 있으면 해당 플레이어를, 없으면 게임 조건에 따라 승자 결정
        string winnerId = specificWinnerId ?? DetermineWinner(bossDefeated);

        // 모든 플레이어에 대한 게임 결과 수집
        List<GamePlayerResult> resultsList = new List<GamePlayerResult>();

        foreach (var player in connectedPlayers)
        {
            string id = player.Key;

            GamePlayerResult result = new GamePlayerResult
            {
                playerId = id,
                remainingTime = (int)_gameTimerUI.GetRemainingTime(),
                deathCount = playerDeathCount.ContainsKey(id) ? playerDeathCount[id] : 0,
                bossHP = bossDefeated ? 0 : bossHpPercentage.Value * 100f
            };

            resultsList.Add(result);
        }

        // 게임 종료 상태 설정
        isGameOver.Value = true;

        // 결과 구조체 생성
        GameResults gameResults = new GameResults
        {
            winnerId = winnerId,
            results = resultsList.ToArray()
        };

        // 결과를 클라이언트에 전송하고 결과 씬으로 전환
        GameOverClientRpc(gameResults);
    }
    
    private string DetermineWinner(bool bossDefeated)
    {
        // 보스 처치의 경우, 승자는 가장 많은 데미지를 준 플레이어
        // 시간 종료 조건의 경우, 승자는 보스 HP가 가장 높은 플레이어(데미지를 적게 받은)
        // 이 로직은 필요에 따라 확장 가능
        
        if (connectedPlayers.Count == 0) return "None";
        if (connectedPlayers.Count == 1) return connectedPlayers.Keys.First();
        
        // 기본 승자 로직(첫 번째 플레이어)
        return connectedPlayers.Keys.First();
    }
    
    private void CheckGameState()
    {
        // 플레이어 수 또는 기타 조건으로 인해 게임을 종료해야 하는지 확인
        if (connectedPlayers.Count < 1 && !isGameOver.Value)
        {
            EndGame(false);
        }
    }
    
    #endregion
    
    #region Client-side Game Management
    
    private void InitializeLocalPlayer()
    {
        Debug.Log($"로컬 플레이어 초기화: {localPlayerId}");
        
        // 로컬 플레이어 ID를 표시하도록 UI 업데이트
        if (_playerUI != null)
        {
            _playerUI.SetPlayerID(localPlayerId);
        }
    }
    
    private void OnBossSpawnStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("보스 스폰 알림 수신됨");
        
            // 보스 UI 업데이트
            if (_bossUI != null)
            {
                // 보스 UI 초기화 - 플레이어 ID를 전달하여 이름 설정
                _bossUI.InitializedUI(localPlayerId, GetOpponentPlayerId());
            }
            else
            {
                Debug.LogWarning("BossUI를 찾을 수 없습니다.");
            }
        }
    }
    
    // 상대방 플레이어 ID 찾기 (간단한 구현)
    private string GetOpponentPlayerId()
    {
        foreach (var player in connectedPlayers.Keys)
        {
            if (player != localPlayerId)
            {
                return player;
            }
        }
    
        // 상대방을 찾지 못한 경우 기본값
        return "Opponent";
    }
    
    private void OnGameStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("게임 종료 알림 수신됨");
            StopGameTimer();
        }
    }
    
    private void OnBossHpChanged(float previousValue, float newValue)
    {
        // 보스 HP UI 업데이트
        UpdateBossHPFromServer(localPlayerId, newValue);
    }
    
    /// <summary>
    /// 플레이어 소유권에 따라 보스 HP UI 업데이트
    /// </summary>
    public void UpdateBossHPFromServer(string ownerPlayerID, float hpPercentage)
    {
        if (_bossUI == null)
        {
            Debug.LogError("GamePlayManager | UpdateBossHpFromServer bossUI가 할당되지 않음");
            return;
        }

        if (ownerPlayerID == localPlayerId)
        {
            _bossUI.UpdatePlayerBossHP(hpPercentage); 
        }
        else
        {
            _bossUI.UpdateEnemyBossHP(hpPercentage);
        }
    }
    
    /// <summary>
    /// 플레이어 HP UI 업데이트
    /// </summary>
    public void UpdatePlayerHPFromServer(float hpPercentage)
    {
        if (_playerUI == null)
        {
            Debug.LogError("GamePlayManager | UpdatePlayerHP playerUI가 할당되지 않음");
            return;
        }

        _playerUI.UpdatePlayerHP(hpPercentage);
    }
    
    /// <summary>
    /// 플레이어 스탯 UI 업데이트
    /// </summary>
    public void UpdatePlayerStatsFromServer(float atk, float def, float asp, int cool)
    {
        if (_playerUI == null)
        {
            Debug.LogError("GamePlayManager | UpdatePlayerStatsFromServer playerUI가 할당되지 않음");
            return;
        }
        
        _playerUI.UpdateStatTexts(atk, def, asp, cool);
    }
    
    /// <summary>
    /// 스킬 쿨다운 UI 시작
    /// </summary>
    public void StartSkillCooldownFromServer(int skillIndex, int cooldownSeconds)
    {
        if (_playerUI == null)
        {
            Debug.LogError("GamePlayManager | StartSkillCooldownFromServer playerUI가 할당되지 않음");
            return;
        }

        _playerUI.StartSkillCooldown(skillIndex, cooldownSeconds);
    }
    
    /// <summary>
    /// 게임 결과를 PlayerPrefs에 저장
    /// </summary>
    private void SaveGameResult(string winnerId, Dictionary<string, GamePlayerResult> results)
    {
        PlayerPrefs.SetString(GameResultKeys.WinnerKey, winnerId);
        PlayerPrefs.SetString(GameResultKeys.LocalPlayerIdKey, localPlayerId);
        
        // 두 플레이어의 데이터 저장 (더 많은 플레이어를 지원하도록 수정 가능)
        int playerIdx = 1;
        foreach (var result in results)
        {
            string playerIdKey = playerIdx == 1 ? GameResultKeys.Player1IdKey : GameResultKeys.Player2IdKey;
            string playerTimeKey = playerIdx == 1 ? GameResultKeys.Player1TimeKey : GameResultKeys.Player2TimeKey;
            string playerBossHPKey = playerIdx == 1 ? GameResultKeys.Player1BossHPKey : GameResultKeys.Player2BossHPKey;
            string playerDeathsKey = playerIdx == 1 ? GameResultKeys.Player1DeathsKey : GameResultKeys.Player2DeathsKey;
            
            PlayerPrefs.SetString(playerIdKey, result.Value.playerId);
            PlayerPrefs.SetInt(playerTimeKey, result.Value.remainingTime);
            PlayerPrefs.SetFloat(playerBossHPKey, result.Value.bossHP);
            PlayerPrefs.SetInt(playerDeathsKey, result.Value.deathCount);
            
            playerIdx++;
            if (playerIdx > 2) break; // 2명의 플레이어 데이터만 저장
        }
        
        PlayerPrefs.Save();
        Debug.Log("GamePlayManager | 게임 결과 데이터 저장됨");
    }
    
    #endregion
    
    #region ClientRPCs
    
    [ClientRpc]
    private void StartGameTimerClientRpc(int startSeconds)
    {
        if (_gameTimerUI == null)
        {
            Debug.LogError("GamePlayManager | StartGameTimer timerUI가 할당되지 않음");
            return;
        }

        _gameTimerUI.StartTimer(startSeconds);
    }
    
    [ClientRpc]
    private void GameOverClientRpc(GameResults results)
    {
        Debug.Log($"게임 종료. 승자: {results.winnerId}");

        // 게임 타이머 정지
        StopGameTimer();

        // Dictionary로 변환하여 기존 SaveGameResult 메서드와 호환성 유지
        Dictionary<string, GamePlayerResult> resultsDict = new Dictionary<string, GamePlayerResult>();
        foreach (var result in results.results)
        {
            resultsDict[result.playerId] = result;
        }

        // 게임 결과 저장
        SaveGameResult(results.winnerId, resultsDict);

        // 결과 씬 로드
        StartCoroutine(LoadResultSceneCoroutine());
    }
    
    private IEnumerator LoadResultSceneCoroutine()
    {
        // 데이터가 저장되도록 약간의 지연 추가
        yield return new WaitForSeconds(1.0f);
    
        // 결과 씬 로드
        SceneManager.LoadScene(_resultSceneName);
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// 보스 스폰 기둥과 상호작용
    /// </summary>
    public void InteractWithPillar(int pillarIndex)
    {
        if (pillarIndex < 0 || pillarIndex >= bossSpawnPillars.Length)
        {
            Debug.LogError($"유효하지 않은 기둥 인덱스: {pillarIndex}");
            return;
        }
        
        // 플레이어 상호작용 등록을 위해 서버에 요청
        PillarInteractionServerRpc(localPlayerId);
    }
    
    /// <summary>
    /// 게임 타이머 시작
    /// </summary>
    public void StartGameTimer()
    {
        // 서버에서만 실제 타이머 시작 및 클라이언트 동기화 처리
        if (IsServer)
        {
            // 클라이언트에 타이머 시작 알림
            StartGameTimerClientRpc((int)gameTime);
        
            // 서버 측 타이머 시작
            StartCoroutine(GameTimerCoroutine());
        }
        else
        {
            // 클라이언트가 직접 호출하면 서버에 시작 요청
            RequestStartGameTimerServerRpc();
        }
    }
    
    // 서버 측에서만 실행되는 타이머 시작 메서드
    [ServerRpc(RequireOwnership = false)]
    private void RequestStartGameTimerServerRpc()
    {
        if (!IsServer) return;
    
        // 이미 타이머가 시작되었는지 확인하는 로직 추가 가능
        StartGameTimer();
    }
    
    /// <summary>
    /// 게임 타이머 정지
    /// </summary>
    public void StopGameTimer()
    {
        if (_gameTimerUI == null)
        {
            Debug.LogError("GamePlayManager | StopGameTimer timerUI가 할당되지 않음");
            return;
        }
        _gameTimerUI.StopTimer();
    }
    
    /// <summary>
    /// 남은 게임 시간 가져오기
    /// </summary>
    public float GetRemainingTime()
    {
        if (_gameTimerUI == null)
        {
            Debug.LogError("GamePlayManager | GetRemainingTime timerUI가 할당되지 않음");
            return 0.0f;
        }

        return _gameTimerUI.GetRemainingTime();
    }
    
    #endregion
}

// 플레이어 결과 정보를 저장하는 헬퍼 클래스
[System.Serializable]
public struct GamePlayerResult : INetworkSerializable
{
    public string playerId;
    public int remainingTime;
    public float bossHP;
    public int deathCount;
    
    // INetworkSerializable 구현
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref remainingTime);
        serializer.SerializeValue(ref bossHP);
        serializer.SerializeValue(ref deathCount);
    }
}

// 게임 결과 전체를 담는 구조체
[System.Serializable]
public struct GameResults : INetworkSerializable
{
    public string winnerId;
    public GamePlayerResult[] results;
    
    // INetworkSerializable 구현
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref winnerId);
        
        // 배열 길이 직렬화
        int length = serializer.IsReader ? 0 : results.Length;
        serializer.SerializeValue(ref length);
        
        // 배열 직렬화
        if (serializer.IsReader)
        {
            results = new GamePlayerResult[length];
        }
        
        for (int i = 0; i < length; i++)
        {
            serializer.SerializeValue(ref results[i]);
        }
    }
}