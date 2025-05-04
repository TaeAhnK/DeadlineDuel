using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 테스트용 플레이어 컨트롤러 - 네트워크 기능 없이 로컬에서 테스트하기 위한 클래스
/// </summary>
public class TestPlayerController : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Camera _playerCamera;
    
    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private GameObject _moveIndicatorPrefab;
    
    [Header("체력 설정")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    [SerializeField] private UnityEngine.UI.Image _healthBarFill;
    
    // 컴포넌트 참조
    private Animator _animator;
    private NavMeshAgent _navAgent;
    private GameObject _moveIndicator;
    
    // 상태 변수
    private bool _isMoving = false;
    
    private void Start()
    {
        Debug.Log("TestPlayerController 초기화 시작");
        
        // 컴포넌트 참조 가져오기
        _animator = GetComponentInChildren<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        
        // 체력 초기화
        _currentHealth = _maxHealth;
        UpdateHealthBar();
        
        // NavMeshAgent 설정
        if (_navAgent != null)
        {
            _navAgent.speed = _moveSpeed;
            _navAgent.angularSpeed = _rotationSpeed * 100;
            _navAgent.acceleration = 8f;
            
            // NavMesh에 있는지 확인
            if (!_navAgent.isOnNavMesh)
            {
                Debug.LogError("캐릭터가 NavMesh 위에 있지 않습니다!");
            }
        }
        else
        {
            Debug.LogError("NavMeshAgent 컴포넌트가 없습니다!");
        }
        
        // 카메라 참조 가져오기
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
            if (_playerCamera == null)
            {
                Debug.LogError("씬에 메인 카메라가 없습니다!");
            }
        }
        
        // 이동 표시자 생성
        CreateMoveIndicator();
        
        Debug.Log("TestPlayerController 초기화 완료");
    }
    
    private void CreateMoveIndicator()
    {
        if (_moveIndicatorPrefab != null)
        {
            _moveIndicator = Instantiate(_moveIndicatorPrefab);
            _moveIndicator.SetActive(false);
        }
        else
        {
            Debug.LogWarning("이동 표시자 프리팹이 지정되지 않았습니다.");
        }
    }
    
    private void Update()
    {
        // 이동 입력 처리
        HandleMovementInput();
        
        // 이동 상태 업데이트
        UpdateMovementState();
    }
    
    private void HandleMovementInput()
    {
        // 마우스 우클릭 감지
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("마우스 우클릭 감지됨");
            
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
                Debug.Log($"카메라 참조 결과: {(_playerCamera != null ? "성공" : "실패")}");
                
                if (_playerCamera == null)
                {
                    Debug.LogError("카메라를 찾을 수 없습니다. 이동 불가.");
                    return;
                }
            }

            Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            Debug.Log($"레이캐스트 시도 - 레이어 마스크: {_groundLayer.value}");
            
            if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
            {
                Debug.Log($"레이캐스트 성공: {hit.point}");
                
                // 이동 표시자 표시
                if (_moveIndicator != null)
                {
                    _moveIndicator.transform.position = hit.point + Vector3.up * 0.3f;
                    _moveIndicator.SetActive(true);
                    StartCoroutine(HideMoveIndicator(0.5f));
                }

                // NavMeshAgent로 이동 경로 설정
                if (_navAgent != null && _navAgent.isOnNavMesh && _navAgent.enabled)
                {
                    _navAgent.SetDestination(hit.point);
                    Debug.Log($"이동 목적지 설정: {hit.point}");
                }
                else
                {
                    Debug.LogWarning("NavMeshAgent가 사용 불가능하여 대체 이동 로직 사용");
                    StartCoroutine(SimpleMove(hit.point));
                }
            }
            else
            {
                Debug.LogError($"레이캐스트 실패 - 바닥을 찾지 못했습니다");
            }
        }
    }
    
    private IEnumerator HideMoveIndicator(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_moveIndicator != null)
        {
            _moveIndicator.SetActive(false);
        }
    }
    
    private IEnumerator SimpleMove(Vector3 destination)
    {
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", true);
        }
        
        // 방향 계산
        Vector3 direction = destination - transform.position;
        direction.y = 0; // y축 회전 제거
        
        // 방향을 향해 회전
        if (direction.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        float stoppingDistance = 0.1f;
        
        // 목적지에 도달할 때까지 이동
        while (Vector3.Distance(transform.position, destination) > stoppingDistance)
        {
            // 방향을 향해 이동
            Vector3 moveDirection = (destination - transform.position).normalized;
            moveDirection.y = 0;
            transform.position += moveDirection * _moveSpeed * Time.deltaTime;
            
            yield return null;
        }
        
        // 애니메이션 중지
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
        }
        
        Debug.Log("목적지에 도착");
    }
    
    private void UpdateMovementState()
    {
        if (_navAgent != null && _navAgent.enabled)
        {
            bool isCurrentlyMoving = _navAgent.velocity.magnitude > 0.1f;
            
            // 이동 상태가 변경된 경우에만 애니메이션 업데이트
            if (isCurrentlyMoving != _isMoving)
            {
                _isMoving = isCurrentlyMoving;
                
                // 애니메이션 상태 업데이트
                if (_animator != null)
                {
                    _animator.SetBool("IsMoving", _isMoving);
                }
            }
        }
    }
    
    /// <summary>
    /// NavMeshAgent 비활성화 (스킬 사용 시 직접 이동을 위해)
    /// </summary>
    public void DisableNavAgent()
    {
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;
            _navAgent.enabled = false;
            Debug.Log("NavMeshAgent 비활성화됨");
        }
    }
    
    /// <summary>
    /// NavMeshAgent 다시 활성화
    /// </summary>
    public void EnableNavAgent()
    {
        if (_navAgent != null && !_navAgent.enabled)
        {
            _navAgent.enabled = true;
            _navAgent.isStopped = false;
            Debug.Log("NavMeshAgent 다시 활성화됨");
        }
    }
    
    /// <summary>
    /// 캐릭터가 데미지를 받음
    /// </summary>
    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        
        Debug.Log($"플레이어가 {amount} 데미지를 받음. 현재 체력: {_currentHealth}/{_maxHealth}");
        
        // 체력바 업데이트
        UpdateHealthBar();
        
        // 애니메이션 재생 (있다면)
        if (_animator != null)
        {
            _animator.SetTrigger("Hit");
        }
        
        // 사망 처리
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 체력바 업데이트
    /// </summary>
    private void UpdateHealthBar()
    {
        if (_healthBarFill != null)
        {
            _healthBarFill.fillAmount = _currentHealth / _maxHealth;
        }
    }
    
    /// <summary>
    /// 캐릭터 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log("플레이어 사망");
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("Die");
            _animator.SetBool("IsDead", true);
        }
        
        // 이동 비활성화
        if (_navAgent != null)
        {
            _navAgent.isStopped = true;
            _navAgent.enabled = false;
        }
        
        // 컴포넌트 비활성화 (선택사항)
        // enabled = false;
    }
    
    /// <summary>
    /// 캐릭터 부활/체력 회복
    /// </summary>
    public void Revive(float healthPercent = 1f)
    {
        _currentHealth = _maxHealth * healthPercent;
        
        // 애니메이션 상태 초기화
        if (_animator != null)
        {
            _animator.SetBool("IsDead", false);
        }
        
        // NavMeshAgent 다시 활성화
        EnableNavAgent();
        
        // 체력바 업데이트
        UpdateHealthBar();
        
        Debug.Log($"플레이어 부활. 체력: {_currentHealth}/{_maxHealth}");
    }
    
    /// <summary>
    /// 현재 체력 백분율 반환
    /// </summary>
    public float GetHealthPercent()
    {
        return _currentHealth / _maxHealth;
    }
    
    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        _moveSpeed = speed;
        
        if (_navAgent != null)
        {
            _navAgent.speed = speed;
        }
    }
    
    /// <summary>
    /// 이동 속도 반환
    /// </summary>
    public float GetMoveSpeed()
    {
        return _moveSpeed;
    }
}