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
    
    private void Start()
    {
        // 이전 씬에서 정보 가져오기
        LoadGameInfo();
        
        // 네트워크 설정
        SetupNetwork();
        
        // UI 초기화
        _gameInfoText.text = "게임 준비 중...";
        _remainingTime = _preparationTime;
        UpdateCountdownText();
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
                string relayJoinCode = PlayerPrefs.GetString("RelayJoinCode", "");
                if (!string.IsNullOrEmpty(relayJoinCode))
                {
                    StartCoroutine(JoinRelayServer(relayJoinCode));
                }
                else
                {
                    _gameInfoText.text = "연결 코드를 찾을 수 없습니다";
                }
            }
        }
    }
    
    // Relay 서버 설정 (호스트용)
    private IEnumerator SetupRelayServer()
{
    _gameInfoText.text = "서버 설정 중...";
    
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
    
    // 연결 코드 저장
    PlayerPrefs.SetString("RelayJoinCode", joinCode);
    PlayerPrefs.Save();
    
    // 로비에 연결 코드 업데이트
    if (!string.IsNullOrEmpty(_lobbyId))
    {
        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            }
        };
        
        Task updateLobbyTask = Lobbies.Instance.UpdateLobbyAsync(_lobbyId, options);
        yield return new WaitUntil(() => updateLobbyTask.IsCompleted);
        
        if (updateLobbyTask.IsFaulted)
        {
            Debug.LogWarning($"로비 업데이트 오류: {updateLobbyTask.Exception?.InnerException?.Message}");
            // 로비 업데이트 실패는 중요하지 않으므로 계속 진행
        }
    }
    
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
    
    // Relay 서버 접속 (클라이언트용)
    private IEnumerator JoinRelayServer(string joinCode)
    {
        _gameInfoText.text = "서버에 접속 중...";
    
        // Relay 서버 참가
        Task<JoinAllocation> joinTask = RelayService.Instance.JoinAllocationAsync(joinCode);
    
        // Task 완료 대기
        yield return new WaitUntil(() => joinTask.IsCompleted);
    
        // 오류 확인
        if (joinTask.IsFaulted)
        {
            Debug.LogError($"Relay 서버 참가 오류: {joinTask.Exception?.InnerException?.Message}");
            _gameInfoText.text = $"서버 접속 오류: {joinTask.Exception?.InnerException?.Message}";
            yield break;
        }
    
        // 참가 결과 가져오기
        JoinAllocation joinAllocation = joinTask.Result;
    
        try
        {
            // Netcode transport 설정
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
            Debug.LogError($"클라이언트 설정 오류: {e.Message}");
            _gameInfoText.text = $"클라이언트 설정 오류: {e.Message}";
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