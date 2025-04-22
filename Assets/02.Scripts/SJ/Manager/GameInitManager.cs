using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Core.Environments;

/// <summary>
/// 게임 초기화 및 로그인을 담당하는 매니저
/// </summary>
public class GameInitManager : MonoBehaviour
{
    private static GameInitManager _instance;
    public static GameInitManager Instance { get; private set; }

    [Header("씬 설정")]
    [SerializeField] private string _mainMenuSceneName = "02.GameMain";
    [SerializeField] private float _minLoadingTime = 1.5f; // 최소 로딩 시간 (UX를 위해)

    [Header("UI 참조")]
    [SerializeField] private GameObject _splashScreen; // 스플래시 화면
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Image _progressBarFill;
    [SerializeField] private RectTransform _progressBarHandle;
    [SerializeField] private Button _retryButton;
    [SerializeField] private TextMeshProUGUI _loadingErrorText; // 로딩 패널의 오류 텍스트
    
    [Header("로그인 UI")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private TextMeshProUGUI _loginErrorText; 
    
    [Header("서비스 설정")]
    [SerializeField] private string _environmentName = "production"; // Unity 서비스 환경 이름
    
    

    // 플레이어 정보
    public string PlayerId { get; private set; }
    public string PlayerNickname { get; private set; }
    public int PlayerMMR { get; private set; } = 1000; // 기본 MMR 값

    // 상태 관리
    private bool _isInitialized = false;
    private bool _isLoggedIn = false;
    private float _initStartTime;

    // 에러 관리
    private bool _hasError = false;
    private string _lastErrorMessage = "";

    private void Awake()
    {
        // 싱글톤 패턴 설정
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
        
        // 이전 세션 데이터 정리
        GameMainManager.ClearPreviousSessionData();

        // UI 초기 설정
        _splashScreen.SetActive(true);
        _loadingPanel.SetActive(false);
        _loginPanel.SetActive(false);
        _retryButton.gameObject.SetActive(false);
        _loadingErrorText.gameObject.SetActive(false);
        _loginErrorText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // 이벤트 리스너 등록
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
        _retryButton.onClick.AddListener(RetryInitialization);
        
        // 스플래시 화면 표시 후 초기화 시작
        StartCoroutine(ShowSplashThenInitialize());
    }

    /// <summary>
    /// 스플래시 화면을 표시한 후 초기화 시작
    /// </summary>
    private IEnumerator ShowSplashThenInitialize()
    {
        // 스플래시 화면 2초간 표시
        yield return new WaitForSeconds(2f);
        
        // 로딩 화면으로 전환
        _splashScreen.SetActive(false);
        _loadingPanel.SetActive(true);
        
        // 초기화 시작
        StartCoroutine(InitializeGame());
    }
    
    // 진행 상태 업데이트 (애니메이션 적용)
    private void UpdateProgress(float progress, string statusMessage = null)
    {
        // 진행바 채우기 애니메이션
        _progressBarFill.DOFillAmount(progress, 0.3f).SetEase(Ease.OutQuad);
    
        // 로딩바 프레임의 너비 구하기
        RectTransform frameRectTransform = _progressBarFill.transform.parent as RectTransform;
        float frameWidth = frameRectTransform.rect.width;
    
        // 핸들이 이동할 수 있는 실제 가용 범위 계산
        float handleWidth = _progressBarHandle.rect.width;
    
        // 핸들이 로딩바 내부에 완전히 들어가도록 위치 범위 조정
        // 왼쪽 끝 = 프레임 왼쪽 + 핸들 너비/2
        // 오른쪽 끝 = 프레임 오른쪽 - 핸들 너비/2
        float leftEdge = -frameWidth/2 + handleWidth/2;
        float rightEdge = frameWidth/2 - handleWidth/2;
    
        // 계산된 범위 내에서 진행 상태에 따라 위치 결정
        float targetX = Mathf.Lerp(leftEdge, rightEdge, progress);
    
        // 핸들 위치 애니메이션
        _progressBarHandle.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutQuad);
        
        // 상태 메시지 업데이트 (제공된 경우)
        if (!string.IsNullOrEmpty(statusMessage))
        {
            _statusText.text = statusMessage;
        }
    }
    
    // 로딩 패널에서 오류 표시
    private void ShowLoadingError(string errorMessage)
    {
        if (_loadingErrorText != null)
        {
            _loadingErrorText.text = errorMessage;
            _loadingErrorText.gameObject.SetActive(true);
        }
    }

// 로그인 패널에서 오류 표시
    private void ShowLoginError(string errorMessage)
    {
        if (_loginErrorText != null)
        {
            _loginErrorText.text = errorMessage;
            _loginErrorText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 게임 초기화 코루틴
    /// </summary>
    private IEnumerator InitializeGame()
    {
        _initStartTime = Time.time;
        _hasError = false;
        _lastErrorMessage = "";
    
        UpdateProgress(0.1f, "게임 초기화 중...");
    
        yield return new WaitForSeconds(0.5f); // 로딩 화면을 잠시 표시
    
        // Unity 서비스 초기화
        UpdateProgress(0.3f, "Unity 서비스 초기화 중...");
    
        yield return StartCoroutine(InitializeUnityServicesCoroutine());
    
        // 오류 확인
        if (_hasError)
        {
            // 오류가 발생한 경우, 재시도 버튼 표시
            _statusText.text = "초기화 중 오류가 발생했습니다.";
            _retryButton.gameObject.SetActive(true);
            UpdateProgress(0f);
        
            ShowLoadingError($"오류: {_lastErrorMessage}");
        
            yield break; // 코루틴 종료
        }
    
        // 인증 상태 확인 (자동 로그인 시도)
        if (AuthenticationService.Instance.SessionTokenExists)
        {
            UpdateProgress(0.5f, "자동 로그인 중...");
            yield return StartCoroutine(SignInWithSessionTokenCoroutine());
        }
    
        if (_isLoggedIn)
        {
            // 로그인 성공 후 남은 과정은 ContinueAfterLogin()으로 이동
            StartCoroutine(ContinueAfterLogin());
        }
        else
        {
            // 자동 로그인 실패 시 로그인 화면 표시
            UpdateProgress(0f); // 진행 상태 초기화 또는 낮은 값 유지
            _loadingPanel.SetActive(false);
            _loginPanel.SetActive(true);
        
            // 이전에 사용한 닉네임이 있으면 입력 필드에 미리 채움
            if (PlayerPrefs.HasKey("PlayerNickname"))
            {
                _nicknameInput.text = PlayerPrefs.GetString("PlayerNickname");
            }
        }
    }
    
    /// <summary>
    /// 로그인 성공 후 남은 초기화 과정을 진행
    /// </summary>
    private IEnumerator ContinueAfterLogin()
    {
        // 플레이어 데이터 로드
        UpdateProgress(0.8f, "플레이어 데이터 로드 중...");
        yield return StartCoroutine(LoadPlayerDataCoroutine());
    
        // 최소 로딩 시간 보장
        float elapsedTime = Time.time - _initStartTime;
        if (elapsedTime < _minLoadingTime)
        {
            yield return new WaitForSeconds(_minLoadingTime - elapsedTime);
        }
    
        // 초기화 완료
        UpdateProgress(1.0f, "초기화 완료. 게임을 시작합니다...");
        yield return new WaitForSeconds(1.0f);
    
        // 다음 씬으로 이동
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    /// <summary>
    /// 세션 토큰을 사용한 자동 로그인 코루틴
    /// </summary>
    private IEnumerator SignInWithSessionTokenCoroutine()
    {
        Debug.Log("세션 토큰으로 자동 로그인 시도");
    
        // 로그인 작업 시작
        Task signInTask = SignInWithSessionToken();
    
        // Task 완료 대기 (별도 헬퍼 함수 사용)
        yield return StartCoroutine(WaitForTask(signInTask));
    
        // 로그인 결과 확인
        if (!_hasError && _isLoggedIn)
        {
            Debug.Log($"자동 로그인 성공. 플레이어 ID: {PlayerId}");
        }
        else
        {
            Debug.LogWarning("자동 로그인 실패");
            _isLoggedIn = false;
        }
    }
    
    /// <summary>
    /// 플레이어 데이터 로드 코루틴
    /// </summary>
    private IEnumerator LoadPlayerDataCoroutine()
    {
        Debug.Log("플레이어 데이터 로드 시작");
    
        // 데이터 로드 작업 시작
        Task loadTask = LoadPlayerData();
    
        // Task 완료 대기 (별도 헬퍼 함수 사용)
        yield return StartCoroutine(WaitForTask(loadTask));
    
        // 로드 결과 확인
        if (!_hasError)
        {
            Debug.Log($"플레이어 데이터 로드 완료. 닉네임: {PlayerNickname}, MMR: {PlayerMMR}");
        }
        else
        {
            Debug.LogError("플레이어 데이터 로드 실패");
        }
    }
    
    /// <summary>
    /// 코루틴 내에서 Task를 안전하게 기다리기 위한 헬퍼 함수
    /// </summary>
    private IEnumerator WaitForTask(Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
    
        // Task가 실패한 경우 예외 정보 전파
        if (task.IsFaulted)
        {
            _hasError = true;
            _lastErrorMessage = task.Exception?.InnerException?.Message ?? "알 수 없는 오류";
            Debug.LogError($"작업 실패: {_lastErrorMessage}");
        }
    }
    
    /// <summary>
    /// Unity 서비스 초기화 코루틴
    /// </summary>
    private IEnumerator InitializeUnityServicesCoroutine()
    {
        _statusText.text = "Unity 서비스 초기화 중...";
        Debug.Log("Unity 서비스 초기화 시작");

        // 초기화 작업 시작
        Task initTask = InitializeUnityServices();
    
        // Task 완료 대기 (별도 헬퍼 함수 사용)
        yield return StartCoroutine(WaitForTask(initTask));
    
        // 성공 시에만 실행
        if (!_hasError)
        {
            _isInitialized = true;
            Debug.Log("Unity 서비스 초기화 완료");
        }
    }


    /// <summary>
    /// Unity 서비스 초기화
    /// </summary>
    private async Task InitializeUnityServices()
    {
        _statusText.text = "Unity 서비스 초기화 중...";
        Debug.Log("Unity 서비스 초기화 시작");

        try
        {
            // 초기화 옵션 설정 (환경 설정 등)
            InitializationOptions options = new InitializationOptions();
            options.SetEnvironmentName(_environmentName);
            
            // 서비스 초기화
            await UnityServices.InitializeAsync(options);
            
            _isInitialized = true;
            Debug.Log("Unity 서비스 초기화 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity 서비스 초기화 오류: {e.Message}");
            throw; // 상위 함수에서 처리하도록 예외 전파
        }
    }

    /// <summary>
    /// 세션 토큰을 사용한 자동 로그인
    /// </summary>
    private async Task SignInWithSessionToken()
    {
        try
        {
            Debug.Log("세션 토큰으로 자동 로그인 시도");
            
            // 익명 인증으로 로그인 
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            PlayerId = AuthenticationService.Instance.PlayerId;
            _isLoggedIn = true;
            
            Debug.Log($"자동 로그인 성공. 플레이어 ID: {PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"자동 로그인 실패: {e.Message}");
            _isLoggedIn = false;
        }
    }

    /// <summary>
    /// 닉네임을 사용한 로그인 (익명 인증 후 닉네임 설정)
    /// </summary>
    private async void LoginWithNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            ShowLoginError("닉네임을 입력해주세요.");
            return;
        }

        // 닉네임 유효성 검사 (길이, 금지어 등)
        if (nickname.Length < 2 || nickname.Length > 12)
        {
            ShowLoginError("닉네임은 2~12자 사이여야 합니다.");
            return;
        }

        _loginPanel.SetActive(false);
        _loadingPanel.SetActive(true);
        UpdateProgress(0.3f, "로그인 중...");
        _loginErrorText.gameObject.SetActive(false);

        try
        {
            // 익명 인증으로 로그인
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            PlayerId = AuthenticationService.Instance.PlayerId;
            PlayerNickname = nickname;
            _isLoggedIn = true;
            
            // 닉네임 저장 (다음 로그인에 사용)
            PlayerPrefs.SetString("PlayerNickname", nickname);
            PlayerPrefs.Save();
            
            Debug.Log($"로그인 성공. 플레이어 ID: {PlayerId}, 닉네임: {nickname}");
            
            // 초기화 과정 재개 (플레이어 데이터 로드, 씬 전환 등)
            StartCoroutine(ContinueAfterLogin());
        }
        catch (Exception e)
        {
            // 로그인 오류 처리
            Debug.LogError($"로그인 오류: {e.Message}");
            _hasError = true;
            _lastErrorMessage = e.Message;
            
            _loadingPanel.SetActive(false);
            _loginPanel.SetActive(true);
            ShowLoginError($"로그인 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 플레이어 데이터 로드
    /// </summary>
    private async Task LoadPlayerData()
    {
        try
        {
            // 닉네임 로드
            if (string.IsNullOrEmpty(PlayerNickname) && PlayerPrefs.HasKey("PlayerNickname"))
            {
                PlayerNickname = PlayerPrefs.GetString("PlayerNickname");
            }
            
            // MMR 로드
            if (PlayerPrefs.HasKey("PlayerMMR"))
            {
                PlayerMMR = PlayerPrefs.GetInt("PlayerMMR");
            }
            else
            {
                // 기본 MMR 설정 및 저장
                PlayerMMR = 1000;
                PlayerPrefs.SetInt("PlayerMMR", PlayerMMR);
                PlayerPrefs.Save();
            }
            
            // 추가 데이터 로드 (예: 캐릭터 정보, 설정 등)
            // 필요에 따라 구현
            
            Debug.Log($"플레이어 데이터 로드 완료. 닉네임: {PlayerNickname}, MMR: {PlayerMMR}");
            
            // 비동기 함수의 형식을 맞추기 위한 완료된 태스크 반환
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            Debug.LogError($"플레이어 데이터 로드 오류: {e.Message}");
            throw; // 상위 함수에서 처리하도록 예외 전파
        }
    }

    /// <summary>
    /// 로그인 버튼 클릭 이벤트 처리
    /// </summary>
    private void OnLoginButtonClicked()
    {
        string nickname = _nicknameInput.text.Trim();
        LoginWithNickname(nickname);
    }

    /// <summary>
    /// 초기화 재시도
    /// </summary>
    public void RetryInitialization()
    {
        _retryButton.gameObject.SetActive(false);
        _loadingErrorText.gameObject.SetActive(false);
        _hasError = false;
        StartCoroutine(InitializeGame());
    }

    /// <summary>
    /// MMR 설정 (다른 씬에서 호출 가능)
    /// </summary>
    public void SetPlayerMMR(int mmr)
    {
        PlayerMMR = mmr;
        PlayerPrefs.SetInt("PlayerMMR", PlayerMMR);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 현재 플레이어 데이터를 PlayerPrefs에 저장
    /// </summary>
    public void SavePlayerData()
    {
        PlayerPrefs.SetString("PlayerNickname", PlayerNickname);
        PlayerPrefs.SetInt("PlayerMMR", PlayerMMR);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 정적 메서드: 현재 인스턴스 (없으면 null)
    /// </summary>
    public static GameInitManager GetInstance()
    {
        return _instance;
    }
    
    /// <summary>
    /// 현재 플레이어 데이터 반환
    /// </summary>
    public PlayerData GetPlayerData()
    {
        return new PlayerData(PlayerId, PlayerNickname, PlayerMMR);
    }
    
    
}