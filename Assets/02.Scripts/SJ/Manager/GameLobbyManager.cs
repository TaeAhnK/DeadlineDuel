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
    [SerializeField] private float _preparationTime = 120f; // 준비 시간 (초)
    
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
    private NetworkVariable<float> _networkTime = new NetworkVariable<float>(120f);
    private NetworkVariable<bool> _gameStarting = new NetworkVariable<bool>(false);
    
    // 재시도 관련 변수
    private int _connectionAttempts = 0;
    private const int MAX_CONNECTION_ATTEMPTS = 10;
    
    [Header("스폰 설정")]
    [SerializeField] private Transform[] _spawnPointsLocations;
    [SerializeField] private GameObject _networkStartPositionPrefab;
    
    // 사용 가능한 스폰 포인트 추적을 위한 리스트
    private List<Transform> _availableSpawnPoints = new List<Transform>();
    // 스폰된 플레이어 추적을 위한 딕셔너리 추가
    private Dictionary<ulong, GameObject> _spawnedPlayers = new Dictionary<ulong, GameObject>();
    private bool _initialSpawnCompleted = false; // 초기 스폰이 완료되었는지 추적
    
    private void Start()
    {
        // 이전 씬에서 정보 가져오기
        LoadGameInfo();
        
        
        // UI 초기화
        _gameInfoText.text = "게임 준비 중...";
        _remainingTime = _preparationTime;
        if (IsServer)
        {
            _networkTime.Value = _preparationTime;
        }
        UpdateCountdownText();
        
        // 스폰 포인트 초기화
        InitializeSpawnPoints();
        
        // 네트워크 설정
        SetupNetwork();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 네트워크 변수 구독
        _networkTime.OnValueChanged += OnTimeChanged;
        _gameStarting.OnValueChanged += OnGameStartingChanged;
       
        if (IsServer)
        {
            // 서버(호스트)는 카운트다운 시작
            StartCoroutine(ServerCountdown());
            
            // 클라이언트 접속 이벤트 리스너 추가
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            // 플레이어 스폰 - 서버만 처리
            SpawnNetworkPlayers();
            
        }
    }
    
    
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 1)
            {
                _gameInfoText.text = "게임 준비 중...";
                Debug.Log($"클라이언트 접속 감지: ID {clientId}, 게임 준비 중으로 상태 변경");
                
                // 새로 접속한 클라이언트에 대해 플레이어 스폰
                SpawnPlayerForClient(clientId);
            }
        }
    }
    
    private void LoadGameInfo()
    {
        _lobbyId = PlayerPrefs.GetString("CurrentLobbyId", "");
        _selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        
        // 호스트 여부 확인 (02 씬에서 설정된 정보)
        _isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;
        
        Debug.Log($"로드된 게임 정보 - 로비ID: {_lobbyId}, 캐릭터인덱스: {_selectedCharacterIndex}, 호스트?: {_isHost}");
    }
    
    // 모든 연결된 클라이언트에 대해 플레이어 스폰
    private void SpawnNetworkPlayers()
    {
        if (!IsServer) return;
        
        Debug.Log($"플레이어 스폰 시작 - 연결된 클라이언트 수: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
        
        // 이미 스폰된 플레이어 초기화
        _spawnedPlayers.Clear();
        
        // 현재 연결된 모든 클라이언트(호스트 포함)에 대해 플레이어 스폰
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerForClient(clientId);
        }
    }
    
    // 특정 클라이언트에 대한 플레이어 스폰
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;
        
        // 이미 스폰되었는지 확인
        if (_spawnedPlayers.ContainsKey(clientId))
        {
            Debug.Log($"클라이언트 {clientId}의 플레이어는 이미 스폰되어 있습니다");
            return;
        }
        
        // 스폰 위치 선택
        Transform spawnPoint = GetRandomSpawnPoint();
        
        // 플레이어 게임 오브젝트 생성
        GameObject playerInstance = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // NetworkObject 컴포넌트 가져오기
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        
        if (networkObject == null)
        {
            Debug.LogError($"플레이어 프리팹에 NetworkObject 컴포넌트가 없습니다!");
            Destroy(playerInstance);
            return;
        }
        
        // 네트워크에 스폰하고 소유권 설정
        networkObject.SpawnAsPlayerObject(clientId);
        
        // 스폰된 플레이어 추적
        _spawnedPlayers.Add(clientId, playerInstance);
        
        Debug.Log($"클라이언트 {clientId}의 플레이어가 스폰되었습니다. 위치: {spawnPoint.position}");
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
            
            // 실패한 경우 지수 백오프로 대기 시간 증가
            if (!connected)
            {
                float waitTime = Mathf.Min(2f * Mathf.Pow(2, _connectionAttempts - 1), 20f); // 최대 20초까지 기하급수적 증가
                _gameInfoText.text = $"연결 실패. {waitTime:0.0}초 후 재시도... ({_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS})";
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        // 최대 시도 횟수 초과
        if (!connected)
        {
            _gameInfoText.text = "서버 연결 실패. 로비를 나가고 메인 화면으로 돌아갑니다...";
            Debug.LogError("최대 연결 시도 횟수 초과, 메인 씬으로 돌아갑니다");
        
            // 잠시 대기하여 사용자가 메시지를 읽을 수 있게 함
            yield return new WaitForSeconds(3f);
        
            // 로비 나가기 처리
            StartCoroutine(LeaveLobbyAndReturnToMain());
        }
    }
    // 로비 나가기 및 메인 씬으로 돌아가기
    private IEnumerator LeaveLobbyAndReturnToMain()
    {
        // 네트워크 연결 정리
        if (NetworkManager.Singleton != null)
        {
            // 연결되어 있다면 연결 해제
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.Shutdown();
                
                // 네트워크 종료가 완료될 때까지 짧게 대기
                yield return new WaitForSeconds(0.5f);
            }
        }
    
        // 로비에서 나가기 (클라이언트만 수행)
        if (!_isHost && !string.IsNullOrEmpty(_lobbyId))
        {
            // 로비 나가기 메서드 호출 (비동기 작업이지만 대기하지 않음)
            LeaveLobbyAsync(_lobbyId, AuthenticationService.Instance.PlayerId);
        
            // 로비 나가기가 완료될 때까지 짧게 기다림 (고정 시간)
            yield return new WaitForSeconds(1.5f);
        }
        
        // GameMainManager의 정적 변수 초기화
        GameMainManager.RelayJoinCode = null;
    
        // 관련 PlayerPrefs 데이터 정리
        PlayerPrefs.DeleteKey("RelayJoinCode");
        PlayerPrefs.DeleteKey("CurrentLobbyId");
        PlayerPrefs.DeleteKey("IsHost");
        // 캐릭터 선택 정보는 유지할 수 있음
        PlayerPrefs.Save();
        
        // 메인 씬으로 돌아가기 전에 GameMainManager 찾아서 초기화
        GameObject mainManagerObj = GameObject.Find("GameMainManager");
        if (mainManagerObj != null)
        {
            Debug.Log("GameMainManager 객체를 찾았습니다. 안전한 전환을 위해 비활성화합니다.");
            mainManagerObj.SetActive(false);
        }
    
        // GameMain 씬으로 이동
        SceneManager.LoadScene("02.GameMain"); 
    }
    
    
    // 로비 나가기를 비동기로 처리하는 별도 메서드 (코루틴 아님)
    private async void LeaveLobbyAsync(string lobbyId, string playerId)
    {
        try
        {
            // Unity Lobby Services에서 로비 나가기
            await Lobbies.Instance.RemovePlayerAsync(lobbyId, playerId);
            Debug.Log("로비에서 성공적으로 나갔습니다");
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 나가기 오류: {e.Message}");
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
            if (Time.time - startTime > 10f)
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
    
   
    
    // Relay 서버 설정 (호스트용)
    private IEnumerator SetupRelayServer()
    {
        _gameInfoText.text = "서버 설정 중...";

        // 이전에 저장된 조인 코드 확인 (재사용 안함, 항상 새로 생성)
        if (!string.IsNullOrEmpty(GameMainManager.RelayJoinCode) ||
            !string.IsNullOrEmpty(PlayerPrefs.GetString("RelayJoinCode", "")))
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
        
        // 호스트 시작 직전 로그
        Debug.Log("Relay 호스트 설정 완료, 네트워크 시작 직전");

        // 중요: 조인 코드를 가져오기 전에 먼저 Netcode transport 설정 및 호스트 시작
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

            // 호스트 즉시 시작 (중요! - 조인 코드 생성 전에 해야 함)
            NetworkManager.Singleton.StartHost();
            _isNetworkActive = true;
            _gameInfoText.text = "서버 설정 완료, 연결 정보 생성 중...";

            Debug.Log("Relay 호스트 설정 완료 및 네트워크 시작");
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay 서버 설정 오류: {e.Message}");
            _gameInfoText.text = $"서버 설정 오류: {e.Message}";
            yield break;
        }

        // 호스트 시작 후 조인 코드 가져오기
        Task<string> joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);
        
        // 호스트 시작 직후 로그
        Debug.Log("네트워크 시작 직후 상태");

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

        _gameInfoText.text = "다른 플레이어 연결 대기 중...";
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
        for (int retry = 0; retry < 5; retry++)
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
                Debug.LogError($"로비 업데이트 호출 오류 (시도 {retry + 1}/5): {e.Message}");
                taskStarted = false;
            }

            // 작업 시작 실패시 대기 후 다음 시도
            if (!taskStarted)
            {
                yield return new WaitForSeconds(1f * (retry + 1));
                continue;
            }

            // Task 대기
            yield return new WaitUntil(() => updateLobbyTask.IsCompleted);

            if (updateLobbyTask.IsFaulted)
            {
                Debug.LogError(
                    $"로비 업데이트 오류 (시도 {retry + 1}/5): {updateLobbyTask.Exception?.InnerException?.Message}");
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
    
    // Start나 OnNetworkSpawn에서 스폰 포인트 초기화
    private void InitializeSpawnPoints()
    {
        // 모든 스폰 포인트를 사용 가능한 리스트에 추가
        _availableSpawnPoints.Clear();
        foreach (Transform spawnPoint in _spawnPoints)
        {
            _availableSpawnPoints.Add(spawnPoint);
        }
    
        Debug.Log($"스폰 포인트 {_availableSpawnPoints.Count}개 초기화됨");
    }
    
    // 랜덤 스폰 포인트 선택
    private Transform GetRandomSpawnPoint()
    {
        // 스폰 포인트가 없는 경우 자신의 위치 반환
        if (_availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("사용 가능한 스폰 포인트가 없습니다. 기본 위치 사용.");
            return transform;
        }
    
        // 남은 스폰 포인트 중에서 랜덤으로 선택
        int randomIndex = UnityEngine.Random.Range(0, _availableSpawnPoints.Count);
        Transform selectedSpawnPoint = _availableSpawnPoints[randomIndex];
    
        // 사용한 스폰 포인트를 리스트에서 제거
        _availableSpawnPoints.RemoveAt(randomIndex);
    
        Debug.Log($"스폰 포인트 선택됨: {selectedSpawnPoint.name}, 남은 스폰 포인트: {_availableSpawnPoints.Count}");
    
        return selectedSpawnPoint;
    }
    
    private void OnDestroy()
    {
        // 등록된 이벤트 리스너 제거
        if (NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    
        // 네트워크 변수 구독 해제
        if (_networkTime != null)
        {
            _networkTime.OnValueChanged -= OnTimeChanged;
        }
    
        if (_gameStarting != null)
        {
            _gameStarting.OnValueChanged -= OnGameStartingChanged;
        }
        
        // 스폰된 플레이어 추적 데이터 정리
        _spawnedPlayers.Clear();
    }
}