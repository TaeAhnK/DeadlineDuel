using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;

public class GameLobbyManager : NetworkBehaviour
{
    [Header("씬 설정")]
    [SerializeField] private string _gameplaySceneName = "04.GamePlay";
    [SerializeField] private float _preparationTime = 60f; // 준비 시간 (초)
    
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private TextMeshProUGUI _gameInfoText;
    
    [Header("네트워크 설정")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    
    // 로비 정보
    private string _lobbyId;
    private int _selectedCharacterIndex;
    private bool _isHost = false;
    private bool _isNetworkActive = false;
    
    // 타이머
    private float _remainingTime;
    private bool _isCountdownActive = false;
    
    // 네트워크 변수
    private NetworkVariable<float> _networkTime = new NetworkVariable<float>(60f);
    private NetworkVariable<bool> _gameStarting = new NetworkVariable<bool>(false);
    
    // 재시도 관련 변수
    private int _connectionAttempts = 0;
    private const int MAX_CONNECTION_ATTEMPTS = 5;
    
    private void Start()
    {
        // 이전 씬에서 정보 가져오기
        LoadGameInfo();
        
        // 기존 코드와 현재 코드를 출력하여 디버깅
        string existingCode = GameMainManager.RelayJoinCode;
        string prefsCode = PlayerPrefs.GetString("RelayJoinCode", "");
    
        Debug.Log($"[중요] 세션 시작 - 정적 변수 코드: {existingCode}, PlayerPrefs 코드: {prefsCode}");
        
        // UI 초기화
        _gameInfoText.text = "게임 준비 중...";
        _remainingTime = _preparationTime;
        UpdateCountdownText();
        
        // 네트워크 설정
        SetupNetwork();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // 서버(호스트)는 카운트다운 시작
            StartCoroutine(ServerCountdown());
        }
        
        // 네트워크 변수 구독
        _networkTime.OnValueChanged += OnTimeChanged;
        _gameStarting.OnValueChanged += OnGameStartingChanged;
        
        // 플레이어 스폰
        SpawnNetworkPlayer();
    }
    
    private void LoadGameInfo()
    {
        _lobbyId = PlayerPrefs.GetString("CurrentLobbyId", "");
        _selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        
        // 호스트 여부 확인 (02 씬에서 설정된 정보)
        _isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;
        
        Debug.Log($"로드된 게임 정보 - 로비ID: {_lobbyId}, 캐릭터인덱스: {_selectedCharacterIndex}, 호스트?: {_isHost}");
    }
    
    private void SetupNetwork()
    {
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // 호스트인 경우 Relay 서버를 설정
            if (_isHost)
            {
                StartCoroutine(SetupRelayServer());
            }
            // 클라이언트인 경우 기존 Relay 서버에 연결
            else
            {
                StartCoroutine(ClientConnectWithRetry());
            }
        }
    }
    
    // 클라이언트 연결 시도 (재시도 로직 포함)
    private IEnumerator ClientConnectWithRetry()
    {
        _connectionAttempts = 0;
        bool connected = false;
        
        while (!connected && _connectionAttempts < MAX_CONNECTION_ATTEMPTS)
        {
            _connectionAttempts++;
            
            // 3가지 방법으로 조인 코드 가져오기 시도
            string relayJoinCode = GetJoinCodeFromAllSources();
            
            if (!string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.Log($"연결 시도 {_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS} - 조인 코드: {relayJoinCode}");
                _gameInfoText.text = $"서버에 연결 중... (시도 {_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS})";
                
                // 연결 시도
                yield return StartCoroutine(JoinRelayServer(relayJoinCode));
                
                // 연결 성공 여부 확인
                if (_isNetworkActive)
                {
                    connected = true;
                    break;
                }
            }
            else
            {
                Debug.LogWarning($"시도 {_connectionAttempts} - 조인 코드를 찾을 수 없습니다. 로비에서 직접 조회를 시도합니다.");
                
                // 로비에서 직접 조회 시도
                yield return StartCoroutine(FetchJoinCodeFromLobby());
                
                // 바로 다시 시도하지 않고 잠시 대기
                yield return new WaitForSeconds(1f);
            }
            
            // 실패한 경우 대기 후 다시 시도
            if (!connected)
            {
                float waitTime = Mathf.Min(2f * _connectionAttempts, 5f); // 최대 5초까지 기하급수적 대기
                _gameInfoText.text = $"연결 실패. {waitTime:0.0}초 후 재시도... ({_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS})";
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        // 최대 시도 횟수 초과
        if (!connected)
        {
            _gameInfoText.text = "서버 연결 실패. 재연결 버튼을 클릭하거나 게임을 다시 시작하세요.";
            Debug.LogError("최대 연결 시도 횟수 초과");
        }
    }
    
    // 모든 소스에서 조인 코드 가져오기 시도
    private string GetJoinCodeFromAllSources()
    {
        string joinCode = null;
        
        // 1. GameMainManager의 정적 변수에서 확인
        if (!string.IsNullOrEmpty(GameMainManager.RelayJoinCode))
        {
            joinCode = GameMainManager.RelayJoinCode;
            Debug.Log($"[중요] GameMainManager에서 조인 코드 찾음: {joinCode}");
            return joinCode;
        }
        
        // 2. PlayerPrefs에서 확인
        joinCode = PlayerPrefs.GetString("RelayJoinCode", "");
        if (!string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"[중요] PlayerPrefs에서 조인 코드 찾음: {joinCode}");
        
            // 정적 변수가 비어있으면 이 값으로 업데이트
            if (string.IsNullOrEmpty(GameMainManager.RelayJoinCode))
            {
                GameMainManager.RelayJoinCode = joinCode;
            }
        
            return joinCode;
        }
        
        // 3. 로비에서 직접 가져온 코드가 있으면 모든 위치에 동기화
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.Log("[중요] 모든 소스에서 조인코드를 찾지 못했습니다.");
        }
        return null;
    }
    
    // 로비에서 직접 조인 코드 가져오기
    private IEnumerator FetchJoinCodeFromLobby()
    {
        if (string.IsNullOrEmpty(_lobbyId))
        {
            Debug.LogError("로비 ID가 없어 조인 코드를 로비에서 가져올 수 없습니다");
            yield break;
        }
        
        Debug.Log($"로비 {_lobbyId}에서 조인 코드 직접 조회 시도");
        _gameInfoText.text = "로비에서 연결 정보 조회 중...";
        
        Task<Lobby> getLobbyTask = null;
        
        try
        {
            getLobbyTask = Lobbies.Instance.GetLobbyAsync(_lobbyId);
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 정보 요청 실패: {e.Message}");
            yield break;
        }
        
        // 최대 5초간 대기
        float startTime = Time.time;
        while (!getLobbyTask.IsCompleted)
        {
            if (Time.time - startTime > 5f)
            {
                Debug.LogWarning("로비 정보 요청 타임아웃");
                yield break;
            }
            yield return null;
        }
        
        if (getLobbyTask.IsFaulted)
        {
            Debug.LogError($"로비 정보 가져오기 오류: {getLobbyTask.Exception?.InnerException?.Message}");
            yield break;
        }
        
        try
        {
            Lobby lobby = getLobbyTask.Result;
            
            if (lobby.Data != null && lobby.Data.TryGetValue("RelayJoinCode", out var relayCodeData))
            {
                string joinCode = relayCodeData.Value;
                Debug.Log($"로비에서 조인 코드 직접 조회 성공: {joinCode}");
                
                // 찾은 코드 저장
                GameMainManager.RelayJoinCode = joinCode;
                PlayerPrefs.SetString("RelayJoinCode", joinCode);
                PlayerPrefs.Save();
                
                // 바로 연결 시도
                StartCoroutine(JoinRelayServer(joinCode));
            }
            else
            {
                Debug.LogWarning("로비에 RelayJoinCode가 없습니다");
                
                // 호스트 정보 출력
                Debug.Log($"로비 호스트 ID: {lobby.HostId}");
                
                // 로비의 모든 데이터 키 출력
                foreach (var key in lobby.Data.Keys)
                {
                    Debug.Log($"로비 데이터 키: {key}, 값: {lobby.Data[key].Value}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 데이터 처리 중 오류: {e.Message}");
        }
    }
    
    // 연결 재시도 버튼 클릭 처리 (혹시몰라서 유지)
    private void RetryConnection()
    {
        
        // 연결 재시도
        StartCoroutine(ClientConnectWithRetry());
    }
    
    // Relay 서버 설정 (호스트용)
    private IEnumerator SetupRelayServer()
    {
        _gameInfoText.text = "서버 설정 중...";
        
        // 이전에 저장된 조인 코드 확인 (재사용 안함, 항상 새로 생성)
        if (!string.IsNullOrEmpty(GameMainManager.RelayJoinCode) || !string.IsNullOrEmpty(PlayerPrefs.GetString("RelayJoinCode", "")))
        {
            Debug.Log("경고: 이전 세션의 조인 코드가 감지되었습니다. 새 코드를 생성합니다.");
            GameMainManager.RelayJoinCode = null;
            PlayerPrefs.DeleteKey("RelayJoinCode");
            PlayerPrefs.Save();
        }
        
        // Relay 할당 생성
        Task<Allocation> allocationTask = RelayService.Instance.CreateAllocationAsync(8);
        
        // Task 완료 대기
        yield return new WaitUntil(() => allocationTask.IsCompleted);
        
        // 오류 확인
        if (allocationTask.IsFaulted)
        {
            Debug.LogError($"Relay 할당 생성 오류: {allocationTask.Exception?.InnerException?.Message}");
            _gameInfoText.text = $"서버 설정 오류: {allocationTask.Exception?.InnerException?.Message}";
            yield break;
        }
        
        // 할당 결과 가져오기
        Allocation allocation = allocationTask.Result;
        
        // 조인 코드 가져오기
        Task<string> joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);
        
        if (joinCodeTask.IsFaulted)
        {
            Debug.LogError($"조인 코드 가져오기 오류: {joinCodeTask.Exception?.InnerException?.Message}");
            _gameInfoText.text = $"서버 설정 오류: {joinCodeTask.Exception?.InnerException?.Message}";
            yield break;
        }
        
        string joinCode = joinCodeTask.Result;
        Debug.Log($"Relay 조인 코드 생성 성공: {joinCode} (길이: {joinCode.Length})");
        
        // 연결 코드 저장 - 여러 곳에 중복 저장하여 안정성 확보
        PlayerPrefs.SetString("RelayJoinCode", joinCode);
        PlayerPrefs.Save();
        
        // GameMainManager의 정적 변수에도 저장
        GameMainManager.RelayJoinCode = joinCode;
        
        // 로비에 연결 코드 업데이트
        StartCoroutine(UpdateLobbyWithRelayCode(joinCode));
        
        try
        {
            // Netcode transport 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            // 호스트 시작
            NetworkManager.Singleton.StartHost();
            _isNetworkActive = true;
            _gameInfoText.text = "다른 플레이어 연결 대기 중...";
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay 서버 설정 오류: {e.Message}");
            _gameInfoText.text = $"서버 설정 오류: {e.Message}";
        }
    }

    /// <summary>
    /// Relay 조인 코드를 로비 데이터에 업데이트
    /// </summary>
    private IEnumerator UpdateLobbyWithRelayCode(string joinCode)
    {
        if (string.IsNullOrEmpty(_lobbyId))
        {
            Debug.LogError("로비 ID가 없어 Relay 코드를 업데이트할 수 없습니다.");
            yield break;
        }

        Debug.Log($"로비 {_lobbyId}에 Relay 코드 업데이트 중: {joinCode} (길이: {joinCode.Length})");

        // 로비 업데이트 최대 3회 시도
        for (int retry = 0; retry < 3; retry++)
        {
            Task updateLobbyTask = null;
            bool taskStarted = false;

            try
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        // 공개 가시성 설정 확인
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
                };

                updateLobbyTask = Lobbies.Instance.UpdateLobbyAsync(_lobbyId, options);
                taskStarted = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"로비 업데이트 호출 오류 (시도 {retry + 1}/3): {e.Message}");
                taskStarted = false;
            }

            // 작업 시작 실패시 대기 후 다음 시도
            if (!taskStarted)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Task 대기
            yield return new WaitUntil(() => updateLobbyTask.IsCompleted);

            if (updateLobbyTask.IsFaulted)
            {
                Debug.LogError(
                    $"로비 업데이트 오류 (시도 {retry + 1}/3): {updateLobbyTask.Exception?.InnerException?.Message}");
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            Debug.Log("로비에 Relay 코드 업데이트 성공!");

            // 업데이트 확인
            Task<Lobby> getLobbyTask = null;
            
            try
            {
                getLobbyTask = Lobbies.Instance.GetLobbyAsync(_lobbyId);
            }
            catch (Exception e)
            {
                Debug.LogError($"로비 정보 요청 오류: {e.Message}");
                continue;
            }

            yield return new WaitUntil(() => getLobbyTask.IsCompleted);

            if (getLobbyTask.IsFaulted)
            {
                Debug.LogError($"로비 정보 가져오기 오류: {getLobbyTask.Exception?.InnerException?.Message}");
            }
            else
            {
                try
                {
                    Lobby lobby = getLobbyTask.Result;
                    if (lobby.Data.TryGetValue("RelayJoinCode", out var codeData))
                    {
                        Debug.Log($"로비 데이터 확인: RelayJoinCode = {codeData.Value} (길이: {codeData.Value.Length})");
                        yield break; // 성공했으므로 종료
                    }
                    else
                    {
                        Debug.LogError("로비 데이터에 RelayJoinCode가 없습니다!");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"로비 데이터 처리 중 오류: {e.Message}");
                }
            }
        }

        Debug.LogError("로비에 Relay 코드 업데이트 실패 (최대 시도 횟수 초과)");
    }

    // Relay 서버 접속 (클라이언트용)
    private IEnumerator JoinRelayServer(string joinCode)
    {
        Debug.Log($"Relay 서버 접속 시도. 조인 코드: {joinCode} (길이: {joinCode.Length})");

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("조인 코드가 비어 있습니다!");
            _gameInfoText.text = "오류: 조인 코드가 비어 있습니다.";
            yield break;
        }

        _gameInfoText.text = "서버에 접속 중...";

        Task<JoinAllocation> joinTask = null;

        try
        {
            // Relay 서버 참가
            joinTask = RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay 참가 Task 생성 중 예외 발생: {e.Message}");
            yield break;
        }

        // 작업 대기 (최대 10초)
        float startTime = Time.time;
        while (!joinTask.IsCompleted)
        {
            if (Time.time - startTime > 10f)
            {
                Debug.LogWarning("Relay 참가 타임아웃");
                yield break;
            }

            yield return null;
        }

        // 오류 확인
        if (joinTask.IsFaulted)
        {
            Debug.LogError($"Relay 서버 참가 오류: {joinTask.Exception?.InnerException?.Message}");
            yield break;
        }

        try
        {
            JoinAllocation joinAllocation = joinTask.Result;
            Debug.Log("Relay 참가 성공!");

            // 클라이언트 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // 클라이언트 시작
            NetworkManager.Singleton.StartClient();
            _isNetworkActive = true;
            _gameInfoText.text = "서버에 접속 완료, 준비 중...";
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay 클라이언트 설정 중 예외 발생: {e.Message}");
            _isNetworkActive = false;
        }
    }

    // 서버 카운트다운
    private IEnumerator ServerCountdown()
    {
        _isCountdownActive = true;
        
        while (_remainingTime > 0 && _isCountdownActive)
        {
            // 네트워크를 통해 시간 동기화
            _networkTime.Value = _remainingTime;
            
            yield return new WaitForSeconds(1f);
            _remainingTime -= 1f;
        }
        
        // 카운트다운 완료, 게임 시작
        _gameStarting.Value = true;
        
        // 다음 씬 로드 대기 시간
        yield return new WaitForSeconds(2f);
        
        // 네트워크 매니저를 통해 모든 클라이언트에 씬 전환 요청
        NetworkManager.Singleton.SceneManager.LoadScene(_gameplaySceneName, LoadSceneMode.Single);
    }
    
    // 네트워크 시간 변경 콜백
    private void OnTimeChanged(float previousValue, float newValue)
    {
        _remainingTime = newValue;
        UpdateCountdownText();
    }
    
    // 게임 시작 상태 변경 콜백
    private void OnGameStartingChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            _countdownText.text = "게임 시작!";
            _gameInfoText.text = "다음 단계로 이동합니다...";
        }
    }
    
    // 카운트다운 텍스트 업데이트
    private void UpdateCountdownText()
    {
        int minutes = Mathf.FloorToInt(_remainingTime / 60f);
        int seconds = Mathf.FloorToInt(_remainingTime % 60f);
        _countdownText.text = $"남은 시간: {minutes:00}:{seconds:00}";
    }
    
    // 플레이어 스폰
    private void SpawnNetworkPlayer()
    {
        if (!IsServer)
            return;
            
        // 각 클라이언트에 대해 플레이어 스폰
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            // 랜덤 스폰 포인트 선택
            Transform spawnPoint = GetRandomSpawnPoint();
            
            // 플레이어 생성
            GameObject playerInstance = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
            
            // 네트워크 오브젝트 스폰
            networkObject.SpawnAsPlayerObject(clientId);
            
            // 필요시 캐릭터 커스터마이즈 (별도 메서드 필요)
            CustomizePlayer(playerInstance, clientId);
        }
    }
    
    // 랜덤 스폰 포인트 선택
    private Transform GetRandomSpawnPoint()
    {
        if (_spawnPoints.Length == 0)
            return transform;
            
        return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
    }
    
    // 플레이어 캐릭터 커스터마이징
    private void CustomizePlayer(GameObject playerObject, ulong clientId)
    {
        // 각 클라이언트의 캐릭터 인덱스 가져오기
        // (실제 구현에서는 클라이언트 ID와 캐릭터 인덱스 맵핑이 필요)
        int characterIndex = _selectedCharacterIndex;
        
        // 플레이어 커스터마이즈 컴포넌트 찾기
        PlayerCustomizer customizer = playerObject.GetComponent<PlayerCustomizer>();
        if (customizer != null)
        {
            customizer.SetCharacterModel(characterIndex);
        }
    }
}