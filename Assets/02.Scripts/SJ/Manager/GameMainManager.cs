using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Player = Unity.Services.Lobbies.Models.Player;

/// <summary>
/// 게임 메인 씬 매니저 - 메인 메뉴, 캐릭터 선택, 매치메이킹 관리
/// </summary>
public class GameMainManager : MonoBehaviour
{
    private static GameMainManager _instance;
    public static GameMainManager Instance => _instance;
    
    // Relay 조인 코드 직접 전달을 위한 정적 변수
    public static string RelayJoinCode;

    [Header("씬 설정")]
    [SerializeField] private string _lobbySceneName = "03.GameLobby";

    [Header("UI 패널")] // 태우님 UI 가져오면 수정해야할 부분
    [SerializeField] private UI_MainMenu _mainMenuUI;
    [SerializeField] private UI_CharacterSelect _characterSelectUI;
    
    [Header("캐릭터 데이터")]
    [SerializeField] private CharacterData[] _characterDataList; // 캐릭터 데이터 배열
    
    
    // 플레이어 정보
    private string _playerId;
    private string _playerNickname;
    private int _playerMMR = 1000; // 기본값

    // 캐릭터 선택 정보
    private int _selectedCharacterIndex = -1; // 선택된 캐릭터 인덱스

    // 매치메이킹 정보
    private Coroutine _matchmakingCoroutine;
    private bool _isMatchmaking = false;
    private float _matchmakingStartTime;
    private Lobby _currentLobby;
    private Coroutine _lobbyHeartbeatCoroutine;
    private Coroutine _lobbyUpdateCoroutine;
    private string _lobbyId;
    private bool _matchFound = false;
    private bool _hasError = false;
    private string _errorMessage = "";

    [Header("매치메이킹 설정")]
    [SerializeField] private float _lobbyHeartbeatInterval = 15f; // 로비 하트비트 간격
    [SerializeField] private float _lobbyUpdateInterval = 1.5f; // 로비 업데이트 간격
    [SerializeField] private int _minPlayersToStart = 2; // 게임 시작에 필요한 최소 플레이어 수
    [SerializeField] private int _maxPlayers = 2; // 최대 플레이어 수
    [SerializeField] private int _mmrRange = 200; // 매칭 MMR 범위
    [SerializeField] private float _relayCodeRefreshInterval = 45f; // Relay 코드 갱신 간격 (초)
    
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

        // UI 초기 설정
        _mainMenuUI.gameObject.SetActive(true);
        _characterSelectUI.gameObject.SetActive(false);
    }

    private void Start()
    {
        // 이전 씬에서 플레이어 정보 가져오기
        GetPlayerInfoFromInitManager();
        
        // 이전 세션 데이터가 있다면 정리 (로비에서 돌아온 경우)
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("RelayJoinCode", "")) || 
            !string.IsNullOrEmpty(PlayerPrefs.GetString("CurrentLobbyId", "")))
        {
            Debug.Log("이전 세션 데이터 감지, 정리 중...");
            ClearPreviousSessionData();
        }
        // 캐릭터 데이터 검증
        ValidateCharacterData();
    }
    
    /// <summary>
    /// 캐릭터 데이터 검증 및 초기화
    /// </summary>
    private void ValidateCharacterData()
    {
        if (_characterDataList == null || _characterDataList.Length == 0)
        {
            Debug.LogError("캐릭터 데이터가 설정되지 않았습니다!");
            return;
        }

        // 캐릭터 버튼 수와 캐릭터 데이터 수 비교
        if (_characterSelectUI.GetSelectedCharacter().Equals(_characterDataList.Length))
        {
            Debug.LogWarning($"캐릭터 버튼 수({_characterSelectUI.GetSelectedCharacter().ToString()})와 캐릭터 데이터 수({_characterDataList.Length})가 일치하지 않습니다!");
        }

        // 프리팹 연결 확인
        for (int i = 0; i < _characterDataList.Length; i++)
        {
            if (_characterDataList[i].characterPrefab == null)
            {
                Debug.LogError($"캐릭터 {i}의 프리팹이 설정되지 않았습니다!");
            }

            // 스킬 프리팹 확인
            for (int j = 0; j < _characterDataList[i].skills.Length; j++)
            {
                if (_characterDataList[i].skills[j] != null && _characterDataList[i].skills[j].skillEffectPrefab == null)
                {
                    Debug.LogWarning($"캐릭터 {i}의 스킬 {j}에 이펙트 프리팹이 설정되지 않았습니다!");
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 진행 중인 모든 코루틴 정지
        if (_matchmakingCoroutine != null)
            StopCoroutine(_matchmakingCoroutine);

        if (_lobbyHeartbeatCoroutine != null)
            StopCoroutine(_lobbyHeartbeatCoroutine);

        if (_lobbyUpdateCoroutine != null)
            StopCoroutine(_lobbyUpdateCoroutine);

        // 로비 떠나기
        if (_isMatchmaking && !string.IsNullOrEmpty(_lobbyId))
        {
            LeaveLobbyAsync();
        }
    }

    /// <summary>
    /// 이전 씬(GameInitialization)에서 플레이어 정보 가져오기
    /// </summary>
    private void GetPlayerInfoFromInitManager()
    {
        GameInitManager initManager = GameInitManager.GetInstance();
        if (initManager != null)
        {
            _playerId = initManager.PlayerId;
            _playerNickname = initManager.PlayerNickname;
            _playerMMR = initManager.PlayerMMR;
            Debug.Log($"플레이어 정보 로드: {_playerNickname}, MMR: {_playerMMR}");
        }
        else
        {
            // 테스트용 기본값
            _playerId = AuthenticationService.Instance?.PlayerId ?? "TestPlayer";
            _playerNickname = "TestPlayer";
            _playerMMR = 1000;
            Debug.LogWarning("GameInitManager를 찾을 수 없어 기본값 사용");
        }
    }

    /// <summary>
    /// UI_CharacterSelect에서 사용
    /// </summary>
    public Sprite GetCharacterPortraitImage(int index) {
        return _characterDataList[index].portraitImage;
    }


    /// <summary>
    /// "Game Start" 버튼 클릭 처리
    /// </summary>
    public void OnStartGameClicked()
    {
        _mainMenuUI.gameObject.SetActive(false);
        _characterSelectUI.gameObject.SetActive(true);

        // 기본 상태 초기화
        _characterSelectUI.InitState();
    }
    
    public void SetSelectCharacterIndex(int index) {
        _selectedCharacterIndex = index;
    }

    /// <summary>
    /// "매칭 시작" 버튼 클릭 처리
    /// </summary>
    public void OnStartMatchmakingClicked()
    {
        if (_isMatchmaking)
        {
            StopMatchmaking();
            return;
        }
        if (_selectedCharacterIndex < 0)
        {
            Debug.LogWarning("캐릭터를 먼저 선택해주세요.");
            return;
        }
        
        // 매칭 상태 초기화
        _isMatchmaking = true;
        _matchFound = false;
        _hasError = false;
        _errorMessage = "";
        _matchmakingStartTime = Time.time; // 매칭 시작 시간 저장
        
        // UI 업데이트는 UI_CharacterSelect에서 처리함;
        
        // 매칭 프로세스 시작
        _matchmakingCoroutine = StartCoroutine(StartMatchmaking());
    }

    /// <summary>
    /// 매치메이킹 코루틴
    /// </summary>
    private IEnumerator StartMatchmaking()
    {
        Debug.Log("매치메이킹 시작");
        
        // 매칭 시작 시간 설정 - UI에 이미 표시했으므로 여기서도 초기화
        _matchmakingStartTime = Time.time;
        
        // 로비 검색 또는 생성
        _lobbyId = null;
        _currentLobby = null;
        
        // 로비 검색/생성 실행 (작업 완료 대기)
        StartCoroutine(FindOrCreateLobbyCoroutine());
        
        // 별도의 타이머 코루틴 시작 - 추가된 부분
        StartCoroutine(UpdateMatchingTimer());
        
        // 로비 작업 완료 대기
        while (_lobbyId == null && !_hasError && _isMatchmaking)
        {
            yield return null;
        }
        
        // 오류 확인
        if (_hasError)
        {
            Debug.LogError("로비 검색/생성 오류: " + _errorMessage);
            _characterSelectUI.MatchingText.text = "Matching Error: " + _errorMessage;
            
            // 3초 후 캐릭터 선택 화면으로 돌아가기
            yield return new WaitForSeconds(3f);
            StopMatchmaking();
            
            yield break;
        }
        
        // 매칭 취소 확인
        if (!_isMatchmaking)
        {
            yield break;
        }
        
        // 로비 하트비트 및 업데이트 코루틴 시작
        if (_lobbyHeartbeatCoroutine != null)
            StopCoroutine(_lobbyHeartbeatCoroutine);
            
        if (_lobbyUpdateCoroutine != null)
            StopCoroutine(_lobbyUpdateCoroutine);
            
        _lobbyHeartbeatCoroutine = StartCoroutine(SendLobbyHeartbeat());
        _lobbyUpdateCoroutine = StartCoroutine(PollLobbyForUpdates());
        
        // 매칭이 성사될 때까지 대기 (로비 업데이트 코루틴에서 상태 체크)
        while (_isMatchmaking && !_matchFound && !_hasError)
        {
            yield return null;
        }
        
        // 오류 확인
        if (_hasError)
        {
            Debug.LogError("매칭 진행 중 오류: " + _errorMessage);
            _characterSelectUI.MatchingText.text = "Matching Error: " + _errorMessage;
            
            // 3초 후 캐릭터 선택 화면으로 돌아가기
            yield return new WaitForSeconds(3f);
           StopMatchmaking();
            
            yield break;
        }
        
        // 매칭 취소 확인
        if (!_isMatchmaking)
        {
            yield break;
        }
        
        // 매칭 성사
        if (_matchFound)
        {
            _characterSelectUI.MatchingText.text = "Find Match! Getting ready for the game...";
            
            // RelayJoinCode를 여러 곳에서 확인하고 저장하는 로직
            string relayJoinCode = null;
        
            // 1. 로비 데이터에서 확인
            if (_currentLobby != null && _currentLobby.Data != null && 
                _currentLobby.Data.TryGetValue("RelayJoinCode", out var relayCodeData))
            {
                relayJoinCode = relayCodeData.Value;
                Debug.Log($"로비에서 Relay 조인 코드 가져옴: {relayJoinCode}");
            }
        
            // 2. 호스트인 경우 PlayerPrefs에서 확인
            if (string.IsNullOrEmpty(relayJoinCode) && IsLobbyOwner(_currentLobby))
            {
                relayJoinCode = PlayerPrefs.GetString("RelayJoinCode", "");
                Debug.Log($"PlayerPrefs에서 Relay 조인 코드 가져옴: {relayJoinCode}");
            }
        
            // 조인 코드가 있으면 저장
            if (!string.IsNullOrEmpty(relayJoinCode))
            {
                // 정적 변수에 저장
                RelayJoinCode = relayJoinCode;
            
                // PlayerPrefs에도 중복 저장
                PlayerPrefs.SetString("RelayJoinCode", relayJoinCode);
                PlayerPrefs.Save();
            
                Debug.Log($"저장된 Relay 조인 코드: {RelayJoinCode}");
            }
            else
            {
                Debug.LogWarning("매칭 성사되었으나 Relay 조인 코드를 찾을 수 없습니다!");
            }
            
            // 잠시 대기 후 다음 씬으로 이동
            yield return new WaitForSeconds(2f);
            
            SaveSelectedCharacterInfo();
            
            // 다음 씬으로 이동
            SceneManager.LoadScene(_lobbySceneName);
        }
    }
    
    private IEnumerator UpdateMatchingTimer()
    {
        while (_isMatchmaking && !_matchFound && !_hasError)
        {
            // UI 참조가 유효한지 확인
            if (_characterSelectUI != null && _characterSelectUI.MatchingText != null)
            {
                // 매칭 시간 업데이트
                float elapsedTime = Time.time - _matchmakingStartTime;
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        
                // 초가 10초 미만일 때 앞에 0 추가
                string secondsStr = seconds < 10 ? $"0{seconds}" : seconds.ToString();
        
                _characterSelectUI.MatchingText.text = $"Matching... {minutes}:{secondsStr}";
            }
            else
            {
                Debug.LogWarning("캐릭터 선택 UI 또는 MatchingText가 null입니다");
                
                Debug.Log($"_characterSelectUI가 null인가: {_characterSelectUI == null}");
                if (_characterSelectUI != null)
                {
                    Debug.Log($"MatchingText가 null인가: {_characterSelectUI.MatchingText == null}");
                }
            }
        
            // 0.5초마다 업데이트
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// 선택된 캐릭터 정보 저장
    /// </summary>
    private void SaveSelectedCharacterInfo()
    {
        // 기본 정보 저장
        PlayerPrefs.SetString("CurrentLobbyId", _lobbyId);
        PlayerPrefs.SetInt("SelectedCharacterIndex", _selectedCharacterIndex);
        
        // 호스트 여부 저장
        bool isHost = _currentLobby != null && _currentLobby.HostId == AuthenticationService.Instance.PlayerId;
        PlayerPrefs.SetInt("IsHost", isHost ? 1 : 0);
        
        // 스킬 정보 저장 (필요시)
        if (_selectedCharacterIndex >= 0 && _selectedCharacterIndex < _characterDataList.Length)
        {
            CharacterData selectedChar = _characterDataList[_selectedCharacterIndex];
            
            // 이름, 프리팹 경로 등 저장
            PlayerPrefs.SetString("CharacterName", selectedChar.name);
            
            // 스킬 쿨다운 정보 저장
            for (int i = 0; i < selectedChar.skills.Length && i < 4; i++)
            {
                if (selectedChar.skills[i] != null)
                {
                    PlayerPrefs.SetFloat($"SkillCooldown_{i}", selectedChar.skills[i].cooldown);
                    PlayerPrefs.SetFloat($"SkillDamage_{i}", selectedChar.skills[i].damage);
                }
            }
        }
        
        PlayerPrefs.Save();
        
        Debug.Log($"캐릭터 정보 저장 완료: 인덱스 {_selectedCharacterIndex}, 호스트: {isHost}");
    }

    /// <summary>
    /// 로비 검색 또는 생성 코루틴
    /// </summary>
    private IEnumerator FindOrCreateLobbyCoroutine()
    {
        Debug.Log("로비 검색 또는 생성 시작");

        // 로비 검색 작업 시작
        Task<List<Lobby>> findLobbiesTask = FindAvailableLobbies();

        // 작업 완료 대기
        while (!findLobbiesTask.IsCompleted && _isMatchmaking)
        {
            yield return null;
        }

        // 매칭 취소 확인
        if (!_isMatchmaking)
        {
            yield break;
        }

        // 오류 확인
        if (findLobbiesTask.IsFaulted)
        {
            _hasError = true;
            _errorMessage = findLobbiesTask.Exception?.InnerException?.Message ?? "알 수 없는 오류";
            yield break;
        }

        // 결과 가져오기
        List<Lobby> availableLobbies = findLobbiesTask.Result;

        // 적합한 MMR 범위의 로비 찾기
        Lobby joinedLobby = null;

        foreach (Lobby lobby in availableLobbies)
        {
            if (!_isMatchmaking) yield break;

            if (lobby.Data.TryGetValue("MinMMR", out var minMMRData) &&
                lobby.Data.TryGetValue("MaxMMR", out var maxMMRData))
            {
                int minMMR = int.Parse(minMMRData.Value);
                int maxMMR = int.Parse(maxMMRData.Value);

                if (_playerMMR >= minMMR && _playerMMR <= maxMMR)
                {
                    // 로비 참가 시도
                    Task<Lobby> joinLobbyTask = JoinLobby(lobby.Id);

                    // 작업 완료 대기
                    while (!joinLobbyTask.IsCompleted && _isMatchmaking)
                    {
                        yield return null;
                    }

                    // 매칭 취소 확인
                    if (!_isMatchmaking)
                    {
                        yield break;
                    }

                    // 로비 참가 성공 확인
                    if (!joinLobbyTask.IsFaulted)
                    {
                        joinedLobby = joinLobbyTask.Result;
                        break;
                    }
                }
            }
        }

        // 적합한 로비를 찾지 못한 경우 새로 생성
        if (joinedLobby == null && _isMatchmaking)
        {
            // MMR 범위 계산
            int minMMR = Math.Max(0, _playerMMR - _mmrRange);
            int maxMMR = _playerMMR + _mmrRange;

            // 로비 옵션 설정
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayerData(),
                Data = new Dictionary<string, DataObject>
                {
                    { "MinMMR", new DataObject(DataObject.VisibilityOptions.Public, minMMR.ToString()) },
                    { "MaxMMR", new DataObject(DataObject.VisibilityOptions.Public, maxMMR.ToString()) },
                    { "S1", new DataObject(DataObject.VisibilityOptions.Public, "Waiting") }, // 게임 상태 (S1: 문자열 1)
                    { "CharacterCount", new DataObject(DataObject.VisibilityOptions.Public, "0") } // 캐릭터 선택 카운트
                }
            };

            // 로비 이름 생성 (닉네임 + 랜덤 숫자)
            string lobbyName = $"{_playerNickname}'s Lobby {UnityEngine.Random.Range(1000, 9999)}";

            // 로비 생성
            Task<Lobby> createLobbyTask = CreateLobby(lobbyName, _maxPlayers, createOptions);

            // 작업 완료 대기
            while (!createLobbyTask.IsCompleted && _isMatchmaking)
            {
                yield return null;
            }

            // 매칭 취소 확인
            if (!_isMatchmaking)
            {
                yield break;
            }

            // 오류 확인
            if (createLobbyTask.IsFaulted)
            {
                _hasError = true;
                _errorMessage = createLobbyTask.Exception?.InnerException?.Message ?? "알 수 없는 오류";
                yield break;
            }

            joinedLobby = createLobbyTask.Result;
        }

        // 로비 참가/생성 성공 확인
        if (joinedLobby != null)
        {
            _lobbyId = joinedLobby.Id;
            _currentLobby = joinedLobby;
            Debug.Log($"로비 {(_lobbyId == joinedLobby.Id ? "참가" : "생성")} 성공: {_lobbyId}");

            // 현재 선택된 캐릭터 정보 업데이트
            Task updatePlayerTask = UpdatePlayerCharacterAsync(_selectedCharacterIndex);

            // 작업 완료 대기
            while (!updatePlayerTask.IsCompleted && _isMatchmaking)
            {
                yield return null;
            }

            // 오류 확인
            if (updatePlayerTask.IsFaulted)
            {
                Debug.LogWarning($"플레이어 캐릭터 정보 업데이트 실패: {updatePlayerTask.Exception?.InnerException?.Message}");
                // 치명적 오류는 아니므로 계속 진행
            }
        }
        else
        {
            _hasError = true;
            _errorMessage = "Cannot find or create a suitable lobby.";
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
        
        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            }
        };
        
        Task updateLobbyTask = null;
        
        try
        {
            updateLobbyTask = Lobbies.Instance.UpdateLobbyAsync(_lobbyId, options);
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 업데이트 호출 오류: {e.Message}");
            yield break;
        }
        
        // Task 완료 대기
        yield return new WaitUntil(() => updateLobbyTask.IsCompleted);
        
        // 결과 확인
        if (updateLobbyTask.IsFaulted)
        {
            Debug.LogError($"로비 업데이트 오류: {updateLobbyTask.Exception?.InnerException?.Message}");
        }
        else
        {
            Debug.Log("로비에 Relay 코드 업데이트 성공!");
            
            // 업데이트 확인을 위한 로비 정보 가져오기
            Task<Lobby> getLobbyTask = null;
            
            try
            {
                getLobbyTask = Lobbies.Instance.GetLobbyAsync(_lobbyId);
            }
            catch (Exception e)
            {
                Debug.LogError($"로비 정보 요청 오류: {e.Message}");
                yield break;
            }
            
            yield return new WaitUntil(() => getLobbyTask.IsCompleted);
            
            if (getLobbyTask.IsFaulted)
            {
                Debug.LogError($"로비 정보 가져오기 오류: {getLobbyTask.Exception?.InnerException?.Message}");
            }
            else
            {
                Lobby lobby = getLobbyTask.Result;
                if (lobby.Data.TryGetValue("RelayJoinCode", out var codeData))
                {
                    Debug.Log($"로비 데이터 확인: RelayJoinCode = {codeData.Value} (길이: {codeData.Value.Length})");
                    RelayJoinCode = codeData.Value;
                }
                else
                {
                    Debug.LogError("로비 데이터에 RelayJoinCode가 없습니다!");
                }
            }
        }
    }

    /// <summary>
    /// 로비 정보 업데이트 가져오기
    /// </summary>
    private async Task<Lobby> GetLobbyUpdate()
    {
        if (string.IsNullOrEmpty(_lobbyId))
            throw new InvalidOperationException("로비 ID가 없습니다.");
            
        try
        {
            Lobby lobby = await Lobbies.Instance.GetLobbyAsync(_lobbyId);
            return lobby;
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 정보 가져오기 오류: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 로비 상태 업데이트 코루틴
    /// </summary>
    private IEnumerator UpdateLobbyStateCoroutine(string newState)
    {
        if (string.IsNullOrEmpty(_lobbyId))
        {
            Debug.LogError("로비 ID가 없어 상태를 업데이트할 수 없습니다");
            yield break;
        }

        Debug.Log($"로비 상태를 {newState}로 업데이트 시도 중");
        
        // 로비 상태 업데이트
        Task updateTask = UpdateLobbyState(newState);
        
        // 작업 완료 대기
        while (!updateTask.IsCompleted)
        {
            yield return null;
        }
        
        // 오류 확인
        if (updateTask.IsFaulted)
        {
            Debug.LogError($"로비 상태 업데이트 오류: {updateTask.Exception?.InnerException?.Message}");
        }
        else
        {
            Debug.Log($"로비 상태를 '{newState}'로 변경 완료");
            
            // 즉시 상태 변경 확인
            Task<Lobby> verifyTask = GetLobbyUpdate();
            yield return new WaitUntil(() => verifyTask.IsCompleted);
        
            if (!verifyTask.IsFaulted)
            {
                Lobby updated = verifyTask.Result;
                if (updated.Data.TryGetValue("S1", out var stateObj))
                {
                    Debug.Log($"확인된 로비 상태: {stateObj.Value}");
                }
            }
        }
    }

    /// <summary>
    /// 로비 상태 업데이트
    /// </summary>
    private async Task UpdateLobbyState(string newState)
    {
        if (string.IsNullOrEmpty(_lobbyId))
            return;
            
        try
        {
            await Lobbies.Instance.UpdateLobbyAsync(_lobbyId, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "S1", new DataObject(DataObject.VisibilityOptions.Public, newState) }
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 상태 업데이트 오류: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 준비 완료된 플레이어 수 카운트
    /// </summary>
    private int CountReadyPlayers(Lobby lobby)
    {
        int readyCount = 0;
        Debug.Log($"전체 {lobby.Players.Count}명의 플레이어 중 준비된 플레이어 수 계산:");
    
        foreach (Player player in lobby.Players)
        {
            bool isReady = false;
            if (player.Data.TryGetValue("IsReady", out var isReadyData))
            {
                isReady = isReadyData.Value == "true";
                string nickname = player.Data.TryGetValue("Nickname", out var nameData) ? nameData.Value : "Unknown";
                Debug.Log($"  - 플레이어 {nickname}: 준비 상태 = {isReady}");
            }
            else
            {
                Debug.LogWarning($"  - 플레이어 {player.Id}에 IsReady 상태가 없습니다");
            }
        
            if (isReady)
                readyCount++;
        }
    
        Debug.Log($"총 준비된 플레이어: {readyCount}");
        return readyCount;
    }

    /// <summary>
    /// 현재 플레이어가 로비 오너인지 확인
    /// </summary>
    private bool IsLobbyOwner(Lobby lobby)
    {
        bool isOwner = lobby.HostId == AuthenticationService.Instance.PlayerId;
        Debug.Log($"내가 로비 소유자인가? {isOwner} (호스트: {lobby.HostId}, 나: {AuthenticationService.Instance.PlayerId})");
        return isOwner;
    }

    /// <summary>
    /// 매치메이킹 중단
    /// </summary>
    private void StopMatchmaking()
    {
    
        if (!_isMatchmaking) return;
        
        _isMatchmaking = false;
        
        // 코루틴 중단
        if (_matchmakingCoroutine != null)
            StopCoroutine(_matchmakingCoroutine);
            
        if (_lobbyHeartbeatCoroutine != null)
            StopCoroutine(_lobbyHeartbeatCoroutine);
            
        if (_lobbyUpdateCoroutine != null)
            StopCoroutine(_lobbyUpdateCoroutine);
        
        // UI 복원 -> TODO UI_CharacterSelect에서 했으나 취소 메시지 넣을지.
        // _startMatchmakingButtonText.text = "매칭 시작";
        // _matchingStatusText.text = "매칭이 취소되었습니다.";
            
        // 로비 떠나기
        LeaveLobbyAsync();
    }
    

    /// <summary>
    /// 로비 떠나기
    /// </summary>
    private async void LeaveLobbyAsync()
    {
        if (!string.IsNullOrEmpty(_lobbyId))
        {
            try
            {
                await Lobbies.Instance.RemovePlayerAsync(_lobbyId, AuthenticationService.Instance.PlayerId);
                Debug.Log("로비에서 나감");
                
                _lobbyId = null;
                _currentLobby = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"로비 떠나기 오류: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 메인 메뉴로 돌아가기
    /// </summary>
    public void OnBackToMainClicked()
    {
        _characterSelectUI.gameObject.SetActive(false);
        _mainMenuUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// 이용 가능한 로비 목록 찾기
    /// </summary>
    private async Task<List<Lobby>> FindAvailableLobbies()
    {
        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 25, // 최대 25개 로비 검색
                Filters = new List<QueryFilter>
                {
                    // // 빈 자리가 있는 로비만
                    // new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //
                    // // 게임 상태가 "대기 중"인 로비만
                    // new QueryFilter(QueryFilter.FieldOptions.S1, "Waiting", QueryFilter.OpOptions.EQ)
                }
            };
            
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
            Debug.Log($"{queryResponse.Results.Count}개의 로비 찾음");
            
            // 각 로비의 상세 정보 출력
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log($"로비 ID: {lobby.Id}, 이름: {lobby.Name}, 플레이어: {lobby.Players.Count}/{lobby.MaxPlayers}");
    
                // 로비 데이터 출력
                foreach (var dataItem in lobby.Data)
                {
                    Debug.Log($"  - 데이터 키: {dataItem.Key}, 값: {dataItem.Value.Value}");
                }
            }
            return queryResponse.Results;
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 검색 오류: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 로비 참가
    /// </summary>
    private async Task<Lobby> JoinLobby(string lobbyId)
    {
        try
        {
            Lobby joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"로비 참가 성공: {lobbyId}");
            return joinedLobby;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"로비 참가 실패: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 로비 생성
    /// </summary>
    private async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, CreateLobbyOptions options)
    {
        try
        {
            Lobby createdLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"새 로비 생성: {createdLobby.Id}");
            
            // 짧은 대기 시간
            await Task.Delay(1000); // 1초 대기
            
            return createdLobby;
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 생성 오류: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 플레이어 데이터 생성
    /// </summary>
    private Player GetPlayerData()
    {
        string charName = _selectedCharacterIndex >= 0 && _selectedCharacterIndex < _characterDataList.Length ? 
            _characterDataList[_selectedCharacterIndex].name : "Unknown";
            
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "Nickname", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _playerNickname) },
                { "MMR", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _playerMMR.ToString()) },
                { "CharacterIndex", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _selectedCharacterIndex.ToString()) },
                { "CharacterName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, charName) },
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "false") }
            }
        };
    }

    /// <summary>
    /// 플레이어 캐릭터 정보 업데이트
    /// </summary>
    private async Task UpdatePlayerCharacterAsync(int characterIndex)
    {
        if (string.IsNullOrEmpty(_lobbyId))
            return;
        
        string charName = characterIndex >= 0 && characterIndex < _characterDataList.Length ?
            _characterDataList[characterIndex].name : "Unknown";
            
        try
        {
            await Lobbies.Instance.UpdatePlayerAsync(_lobbyId, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "CharacterIndex", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, characterIndex.ToString()) },
                    { "CharacterName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, charName) },
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
                }
            });
            
            Debug.Log($"플레이어 캐릭터 정보 업데이트: {characterIndex} ({charName})");
        }
        catch (Exception e)
        {
            Debug.LogError($"플레이어 정보 업데이트 오류: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 로비 하트비트 전송 코루틴 (로비 활성 유지)
    /// </summary>
    private IEnumerator SendLobbyHeartbeat()
    {
        WaitForSeconds heartbeatInterval = new WaitForSeconds(_lobbyHeartbeatInterval);
        
        while (_isMatchmaking && !string.IsNullOrEmpty(_lobbyId))
        {
            yield return heartbeatInterval;
            
            if (!_isMatchmaking) yield break;
            
            bool success = false;
            string errorMsg = "";
            
            // 하트비트 전송
            Task heartbeatTask = SendHeartbeat();
            
            // 작업 완료 대기
            float startTime = Time.time;
            while (!heartbeatTask.IsCompleted)
            {
                // 타임아웃 체크 (10초)
                if (Time.time - startTime > 10f)
                {
                    errorMsg = "하트비트 타임아웃";
                    break;
                }
                yield return null;
            }
            
            // 오류 확인
            if (heartbeatTask.IsFaulted)
            {
                success = false;
                errorMsg = heartbeatTask.Exception?.InnerException?.Message ?? "알 수 없는 오류";
            }
            else
            {
                success = true;
            }
            
            if (!success)
            {
                Debug.LogError($"하트비트 오류: {errorMsg}");
                
                // 오류가 "404 Not Found"인 경우 로비가 이미 없는 것이므로 매칭 중단
                if (errorMsg.Contains("404") || errorMsg.Contains("Not Found"))
                {
                    _isMatchmaking = false;
                    _hasError = true;
                    _errorMessage = "The lobby is closed.";
                }
            }
        }
    }

    /// <summary>
    /// 하트비트 전송
    /// </summary>
    private async Task SendHeartbeat()
    {
        if (string.IsNullOrEmpty(_lobbyId))
            return;
            
        try
        {
            await Lobbies.Instance.SendHeartbeatPingAsync(_lobbyId);
            Debug.Log("로비 하트비트 전송");
        }
        catch (Exception e)
        {
            Debug.LogError($"하트비트 오류: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 로비 업데이트 확인 코루틴
    /// </summary>
    private IEnumerator PollLobbyForUpdates()
    {
        WaitForSeconds updateInterval = new WaitForSeconds(_lobbyUpdateInterval);
    
        while (_isMatchmaking && !string.IsNullOrEmpty(_lobbyId))
        {
            yield return updateInterval;
    
            if (!_isMatchmaking) yield break;
    
            bool success = false;
            string errorMsg = "";
            Lobby updatedLobby = null;
    
            // 로비 정보 업데이트
            Task<Lobby> lobbyUpdateTask = GetLobbyUpdate();
    
            // 작업 완료 대기
            float startTime = Time.time;
            while (!lobbyUpdateTask.IsCompleted)
            {
                // 타임아웃 체크 (10초)
                if (Time.time - startTime > 10f)
                {
                    errorMsg = "로비 업데이트 타임아웃";
                    break;
                }
    
                yield return null;
            }
    
            // 오류 확인
            if (lobbyUpdateTask.IsFaulted)
            {
                success = false;
                errorMsg = lobbyUpdateTask.Exception?.InnerException?.Message ?? "알 수 없는 오류";
            }
            else
            {
                success = true;
                updatedLobby = lobbyUpdateTask.Result;
            }
    
            // 업데이트 실패 처리
            if (!success)
            {
                Debug.LogError($"로비 업데이트 오류: {errorMsg}");
    
                // 로비가 존재하지 않는 경우
                if (errorMsg.Contains("404") || errorMsg.Contains("Not Found"))
                {
                    _isMatchmaking = false;
                    _hasError = true;
                    _errorMessage = "The lobby is closed.";
                }
    
                continue; // 다음 업데이트 시도
            }
    
            // 로비 정보 업데이트 성공
            _currentLobby = updatedLobby;
    
            // 플레이어 수 확인
            int playerCount = updatedLobby.Players.Count;
            int readyPlayers = CountReadyPlayers(updatedLobby);
    
            Debug.Log($"로비 상태: {playerCount}/{updatedLobby.MaxPlayers} 플레이어, {readyPlayers} 준비 완료");
    
            // 게임 시작 조건 확인
            if (playerCount >= _minPlayersToStart && readyPlayers == playerCount)
            {
                // 로비 상태가 "Waiting"인 경우에만 처리
                if (updatedLobby.Data.TryGetValue("S1", out var gameState) && gameState.Value == "Waiting")
                {
                    // 로비 오너인 경우만 상태 변경
                    if (IsLobbyOwner(updatedLobby))
                    {
                        // 게임 상태를 "Starting"으로 변경 작업 시작
                        StartCoroutine(UpdateLobbyStateCoroutine("Starting"));
                    }
                }
            }
    
            // 게임 시작 확인
            if (updatedLobby.Data.TryGetValue("S1", out var state) && state.Value == "Starting")
            {
                _matchFound = true;
                Debug.Log("매치 성사! 게임 시작 준비");
            }
            
            foreach (Player player in updatedLobby.Players)
            {
                string isReady = "unknown";
                if (player.Data.TryGetValue("IsReady", out var readyData))
                    isReady = readyData.Value;
    
                string nickname = player.Data.TryGetValue("Nickname", out var nameData) ? nameData.Value : "Unknown";
                Debug.Log($"플레이어 {nickname} (ID: {player.Id}), 준비 상태: {isReady}");
            }
    
            // 현재 로비 상태 로깅
            string currentState = updatedLobby.Data.TryGetValue("S1", out var stateData) ? stateData.Value : "Unknown";
            Debug.Log($"현재 로비 상태: {currentState}, 호스트 ID: {updatedLobby.HostId}, 내 ID: {AuthenticationService.Instance.PlayerId}");
        }
    }
    
    public static void ClearPreviousSessionData()
    {
        // 정적 변수 초기화
        RelayJoinCode = null;
    
        // PlayerPrefs에서 이전 세션 데이터 삭제
        PlayerPrefs.DeleteKey("RelayJoinCode");
        PlayerPrefs.DeleteKey("CurrentLobbyId");
  
    
        // 기타 필요한 키 삭제
        // PlayerPrefs.DeleteKey("IsHost");
    
        PlayerPrefs.Save();
    
        Debug.Log("이전 세션 데이터가 초기화되었습니다.");
    }
}