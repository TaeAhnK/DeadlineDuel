using UnityEngine;
using Unity.Netcode;

public class LobbySceneLoader : MonoBehaviour
{
    [Header("프리팹 참조")]
    [SerializeField] private GameObject _lobbyManagerPrefab;
    [SerializeField] private GameObject _networkManagerPrefab;
    
    [Header("UI 요소")]
    [SerializeField] private GameObject _loadingPanel;
    
    private void Start()
    {
        // 로딩 패널 표시 (있는 경우)
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(true);
        }
        
        // NetworkManager가 씬에 이미 존재하는지 확인
        NetworkManager existingNetworkManager = FindObjectOfType<NetworkManager>();
        if (existingNetworkManager == null && _networkManagerPrefab != null)
        {
            // NetworkManager 생성
            Instantiate(_networkManagerPrefab);
        }
        
        // 로비 매니저가 씬에 이미 존재하는지 확인
        GameLobbyManager existingLobbyManager = FindObjectOfType<GameLobbyManager>();
        if (existingLobbyManager == null && _lobbyManagerPrefab != null)
        {
            // 로비 매니저 생성
            Instantiate(_lobbyManagerPrefab);
        }
        
        // 지연 후 로딩 패널 숨기기
        if (_loadingPanel != null)
        {
            Invoke("HideLoadingPanel", 1.5f);
        }
    }
    
    private void HideLoadingPanel()
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(false);
        }
    }
    
    // 로비 종료 시 정리
    public void CleanupLobby()
    {
        // 뒤로 가기 또는 로비 나가기 시 필요한 정리 작업
        // 연결 종료 및 로비 정보 정리
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // 클라이언트 연결 종료
            NetworkManager.Singleton.Shutdown();
        }
        
        // 이전 씬으로 이동 또는 메인 메뉴로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("02.GameMain");
    }
}