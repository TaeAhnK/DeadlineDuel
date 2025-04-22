using UnityEngine;
using Cinemachine;
using Unity.Netcode;

public class QuadViewCinemachine : MonoBehaviour
{
    [Header("시네머신 설정")]
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    
    [Header("카메라 거리 설정")]
    [SerializeField] private float _mouseScrollSpeed = 5f;
    [SerializeField] private float _minCameraDistance = 10f;
    [SerializeField] private float _maxCameraDistance = 30f;
    
    [Header("카메라 이동 설정")]
    [SerializeField] private float _mouseEdgeScrollSpeed = 15f;
    [SerializeField] private int _edgeThreshold = 20; // 화면 가장자리 픽셀
    [SerializeField] private Transform _cameraTarget; // 카메라가 따라갈 타겟
    
    // 시네머신 컴포넌트
    private CinemachineFramingTransposer _framingTransposer;
    
    // 로컬 플레이어 참조
    private Transform _playerTransform;
    private bool _isFollowingPlayer = true;
    
    private void Start()
    {
        // 시네머신 카메라 참조 가져오기
        if (_virtualCamera == null)
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            
        // 프레이밍 트랜스포저 참조 가져오기
        _framingTransposer = _virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        
        // 초기 카메라 타겟 설정
        if (_cameraTarget == null)
        {
            // 새로운 빈 게임오브젝트를 카메라 타겟으로 생성
            GameObject targetObj = new GameObject("CameraTarget");
            _cameraTarget = targetObj.transform;
            
            // 씬 전환 시에도 파괴되지 않도록 설정
            DontDestroyOnLoad(targetObj);
        }
        
        // 시네머신 카메라의 타겟 설정
        _virtualCamera.Follow = _cameraTarget;
        
        // 쿼드뷰 각도 설정 (X 회전)
        _virtualCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }
    
    private void Update()
    {
        // 로컬 플레이어만 카메라 제어 가능
        if (_playerTransform == null)
            return;
            
        // 마우스 휠로 줌인/줌아웃
        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0)
        {
            float newDistance = _framingTransposer.m_CameraDistance - scrollDelta * _mouseScrollSpeed;
            _framingTransposer.m_CameraDistance = Mathf.Clamp(newDistance, _minCameraDistance, _maxCameraDistance);
        }
        
        // 화면 가장자리에서 마우스로 카메라 이동 (프리룩 모드에서만)
        if (!_isFollowingPlayer)
        {
            Vector3 moveDirection = Vector3.zero;
            
            // 마우스 위치 확인
            Vector3 mousePos = Input.mousePosition;
            
            // 화면 가장자리 검사
            if (mousePos.x < _edgeThreshold)
                moveDirection.x = -1;
            else if (mousePos.x > Screen.width - _edgeThreshold)
                moveDirection.x = 1;
                
            if (mousePos.y < _edgeThreshold)
                moveDirection.z = -1;
            else if (mousePos.y > Screen.height - _edgeThreshold)
                moveDirection.z = 1;
                
            // 카메라 타겟 이동
            if (moveDirection != Vector3.zero)
            {
                moveDirection = Quaternion.Euler(0, _virtualCamera.transform.eulerAngles.y, 0) * moveDirection;
                _cameraTarget.position += moveDirection * _mouseEdgeScrollSpeed * Time.deltaTime;
            }
        }
        else
        {
            // 플레이어 팔로우 모드일 때는 카메라 타겟을 플레이어 위치로 설정
            _cameraTarget.position = _playerTransform.position;
        }
        
        // F 키를 누르면 프리룩 모드와 플레이어 팔로우 모드 토글
        if (Input.GetKeyDown(KeyCode.F))
        {
            _isFollowingPlayer = !_isFollowingPlayer;
        }
        
        // 스페이스바로 플레이어에 카메라 즉시 고정
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 플레이어 팔로우 모드로 전환
            _isFollowingPlayer = true;
            
            // 카메라 타겟을 플레이어 위치로 즉시 이동
            _cameraTarget.position = _playerTransform.position;
            
            // 워프 효과로 부드럽게 이동
            _framingTransposer.OnTargetObjectWarped(_cameraTarget, _cameraTarget.position - transform.position);
        }
    }
    
    // 플레이어 타겟 설정 메서드
    public void SetTarget(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        
        // 초기에 카메라 타겟을 플레이어 위치로 설정
        if (_cameraTarget != null && playerTransform != null)
        {
            _cameraTarget.position = playerTransform.position;
        }
    }
}