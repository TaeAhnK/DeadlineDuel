using UnityEngine;
using Unity.Netcode;

public class SkillEffectController : NetworkBehaviour
{
    [Header("이펙트 설정")]
    [SerializeField] private float _moveSpeed = 20f; // 발사체 이동 속도
    [SerializeField] private float _lifetime = 5f; // 이펙트 수명
    [SerializeField] private float _damage = 10f; // 기본 데미지 (스킬 설정으로 덮어씀)
    [SerializeField] private bool _isProjectile = true; // 발사체인지 여부 (false면 고정 이펙트)
    [SerializeField] private bool _isAoe = false; // 범위 공격인지 여부
    [SerializeField] private float _aoeRadius = 0f; // 범위 공격 반경
    
    [Header("시각 효과")]
    [SerializeField] private ParticleSystem _impactEffect; // 충돌 효과
    [SerializeField] private TrailRenderer _trailRenderer; // 이동 궤적
    
    // 이동 방향
    private Vector3 _direction;
    
    // 네트워크 변수
    private NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<bool> _hasImpacted = new NetworkVariable<bool>(false);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 이펙트 초기화
        if (IsServer)
        {
            // 기본 방향은 전방
            _direction = transform.forward;
        }
        
        // 일정 시간 후 자동 제거 (서버만)
        if (IsServer)
        {
            Invoke("DestroyEffect", _lifetime);
        }
        
        // 네트워크 변수 콜백
        _hasImpacted.OnValueChanged += OnImpactStateChanged;
    }
    
    // 이펙트 초기화 (서버에서 호출)
    public void Initialize(Vector3 direction, float damage, float speed = 0)
    {
        if (!IsServer) return;
        
        _direction = direction.normalized;
        _damage = damage;
        
        if (speed > 0)
        {
            _moveSpeed = speed;
        }
    }
    
    // AOE 설정 (서버에서 호출)
    public void SetAoe(bool isAoe, float radius)
    {
        if (!IsServer) return;
        
        _isAoe = isAoe;
        _aoeRadius = radius;
        
        // AOE인 경우 이동하지 않음
        if (_isAoe)
        {
            _isProjectile = false;
        }
    }
    
    private void Update()
    {
        if (!IsServer) return;
        
        // 이미 충돌했으면 이동하지 않음
        if (_hasImpacted.Value) return;
        
        // 발사체인 경우 이동
        if (_isProjectile)
        {
            MoveProjectile();
        }
        // 고정 이펙트이고 AOE인 경우, 주변 대상에 지속적으로 데미지
        else if (_isAoe)
        {
            ApplyAoeDamage();
        }
    }
    
    private void MoveProjectile()
    {
        // 발사체 이동
        transform.position += _direction * _moveSpeed * Time.deltaTime;
        
        // 네트워크 위치 업데이트
        _netPosition.Value = transform.position;
        
        // 레이캐스트로 충돌 감지
        RaycastHit hit;
        if (Physics.Raycast(transform.position, _direction, out hit, _moveSpeed * Time.deltaTime))
        {
            // 충돌 처리
            HandleImpact(hit.point, hit.collider);
        }
    }
    
    private void HandleImpact(Vector3 impactPoint, Collider hitCollider)
    {
        // 이미 충돌 처리됐으면 무시
        if (_hasImpacted.Value) return;
        
        // 충돌 위치로 이동
        transform.position = impactPoint;
        
        // 충돌 상태 설정
        _hasImpacted.Value = true;
        
        // 타겟에 데미지 적용
        DummyTarget target = hitCollider.GetComponent<DummyTarget>();
        if (target != null)
        {
            target.TakeDamageServerRpc(_damage);
        }
        
        // AOE 데미지 적용
        if (_isAoe)
        {
            ApplyAoeDamage();
        }
        
        // 일정 시간 후 제거 (충돌 이펙트 재생 시간 고려)
        CancelInvoke("DestroyEffect");
        Invoke("DestroyEffect", 2f);
    }
    
    private void ApplyAoeDamage()
    {
        // 범위 내 모든 콜라이더 탐색
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _aoeRadius);
        
        foreach (var hitCollider in hitColliders)
        {
            // 허수아비 타겟에 데미지 적용
            DummyTarget target = hitCollider.GetComponent<DummyTarget>();
            if (target != null)
            {
                target.TakeDamageServerRpc(_damage * Time.deltaTime); // 지속 데미지는 초당 데미지로 적용
            }
            
            // 추후 플레이어 데미지 로직 추가 가능
        }
    }
    
    // 충돌 상태 변경 콜백 (클라이언트에서 시각 효과 처리)
    private void OnImpactStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            // 이동 궤적 비활성화
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = false;
            }
            
            // 충돌 이펙트 재생
            if (_impactEffect != null)
            {
                _impactEffect.Play();
            }
        }
    }
    
    // 이펙트 제거 (서버에서 호출)
    private void DestroyEffect()
    {
        if (IsServer)
        {
            // 네트워크에서 제거
            NetworkObject.Despawn();
        }
    }
    
    // 디버깅용 - AOE 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (_isAoe && _aoeRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _aoeRadius);
        }
    }
}