using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject _cameraPrefab; // 카메라 프리팹
    
    private Camera _playerCamera;
    private QuadViewController _cameraController;
    
    [Header("플레이어 이동")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    
    [Header("플레이어 공격")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private GameObject _attackEffectPrefab;
    
    // 공격 쿨다운 타이머
    private float _attackTimer = 0f;
    
    // 애니메이터 참조
    private Animator _animator;
    
    // 네트워크 변수 - 공격 상태
    private NetworkVariable<bool> _isAttacking = new NetworkVariable<bool>(false);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 애니메이터 참조 가져오기
        _animator = GetComponentInChildren<Animator>();
        
        // 자신의 플레이어일 경우에만 카메라 생성
        if (IsOwner)
        {
            SetupCamera();
        }
        
        // 네트워크 변수 콜백
        _isAttacking.OnValueChanged += OnAttackingChanged;
    }
    
    private void SetupCamera()
    {
        // 카메라 생성
        GameObject cameraObj = Instantiate(_cameraPrefab);
        _playerCamera = cameraObj.GetComponent<Camera>();
        
        // 쿼드뷰 컨트롤러 가져오기
        _cameraController = cameraObj.GetComponent<QuadViewController>();
        
        // 카메라 타겟 설정
        if (_cameraController != null)
        {
            _cameraController.SetTarget(transform);
        }
        
        // 씬 전환 시에도 카메라 유지
        DontDestroyOnLoad(cameraObj);
    }
    
    private void Update()
    {
        // 자신의 플레이어만 입력 처리
        if (!IsOwner) return;
        
        // 이동 처리
        HandleMovement();
        
        // 공격 쿨다운 업데이트
        if (_attackTimer > 0)
        {
            _attackTimer -= Time.deltaTime;
        }
        
        // 공격 입력 처리
        if (Input.GetMouseButtonDown(0) && _attackTimer <= 0)
        {
            HandleAttack();
        }
    }
    
    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (horizontal != 0 || vertical != 0)
        {
            // 이동 방향 계산 (카메라 기준)
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            
            // y축 성분 제거 (바닥 평면에서만 이동하도록)
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // 최종 이동 방향
            Vector3 moveDirection = cameraRight * horizontal + cameraForward * vertical;
            
            // 이동 적용
            if (moveDirection.magnitude > 0.1f)
            {
                // 네트워크로 이동 요청
                MovePlayerServerRpc(moveDirection);
                
                // 회전 처리 (즉시 클라이언트에서 처리하여 지연감 감소)
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
            
            // 애니메이션 처리
            if (_animator != null)
            {
                _animator.SetBool("IsMoving", true);
            }
        }
        else
        {
            // 정지 애니메이션
            if (_animator != null)
            {
                _animator.SetBool("IsMoving", false);
            }
        }
    }
    
    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 direction)
    {
        // 정규화
        if (direction.magnitude > 1f)
            direction.Normalize();
            
        // 서버에서 위치 업데이트
        transform.position += direction * _moveSpeed * Time.deltaTime;
    }
    
    private void HandleAttack()
    {
        // 공격 쿨다운 시작
        _attackTimer = _attackCooldown;
        
        // 서버에 공격 요청
        AttackServerRpc();
    }
    
    [ServerRpc]
    private void AttackServerRpc()
    {
        // 공격 상태 설정
        _isAttacking.Value = true;
        
        // 허수아비 검출 및 데미지 적용
        DetectAndDamageTargets();
        
        // 공격 상태 해제 (애니메이션에 맞춰 지연)
        StartCoroutine(ResetAttackState(0.5f));
    }
    
    private System.Collections.IEnumerator ResetAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isAttacking.Value = false;
    }
    
    private void OnAttackingChanged(bool oldValue, bool newValue)
    {
        // 공격 애니메이션 재생
        if (newValue && _animator != null)
        {
            _animator.SetTrigger("Attack");
        }
        
        // 공격 이펙트 생성
        if (newValue && _attackEffectPrefab != null)
        {
            GameObject attackEffect = Instantiate(_attackEffectPrefab, transform.position + transform.forward * 1f, transform.rotation);
            Destroy(attackEffect, 1f); // 1초 후 이펙트 제거
        }
    }
    
    private void DetectAndDamageTargets()
    {
        // 공격 범위 내 콜라이더 검출
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1f, _attackRange);
        
        foreach (var hitCollider in hitColliders)
        {
            // 더미 타겟인지 확인
            DummyTarget dummyTarget = hitCollider.GetComponent<DummyTarget>();
            if (dummyTarget != null)
            {
                // 데미지 적용
                dummyTarget.TakeDamageServerRpc(_attackDamage);
            }
        }
    }
    
    // 디버깅용 - 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1f, _attackRange);
    }
}