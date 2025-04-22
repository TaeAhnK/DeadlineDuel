using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 검을 사용하는 캐릭터의 스킬 구현 클래스
/// </summary>
public class SwordCharacterSkills : NetworkBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Animator _animator;
    
    [Header("스킬 설정")]
    [SerializeField] private Transform _skillOrigin; // 스킬 발사 위치 (검 끝)
    
    [Header("Q 스킬 - 강한 베기")]
    [SerializeField] private GameObject _slashEffectPrefab;
    [SerializeField] private float _slashDamage = 25f;
    [SerializeField] private float _slashRange = 3f;
    [SerializeField] private float _slashWidth = 120f; // 부채꼴 각도
    [SerializeField] private float _slashCooldown = 5f;
    
    [Header("W 스킬 - 반격기")]
    [SerializeField] private GameObject _counterEffectPrefab;
    [SerializeField] private float _counterDuration = 1.5f; // 반격 가능 시간
    [SerializeField] private float _counterDamage = 40f; // 반격 시 데미지
    [SerializeField] private float _counterCooldown = 12f;
    
    [Header("E 스킬 - 이동기")]
    [SerializeField] private GameObject _dashEffectPrefab;
    [SerializeField] private float _dashDistance = 5f; // 돌진 거리
    [SerializeField] private float _dashDamage = 15f; // 돌진 데미지
    [SerializeField] private float _dashSpeed = 20f; // 돌진 속도
    [SerializeField] private float _dashCooldown = 8f;
    
    [Header("R 스킬 - 궁극기")]
    [SerializeField] private GameObject _ultimateEffectPrefab;
    [SerializeField] private float _ultimateDamage = 80f;
    [SerializeField] private float _ultimateRange = 6f;
    [SerializeField] private float _ultimateRadius = 4f; // AOE 반경
    [SerializeField] private float _ultimateCastTime = 1f; // 시전 시간
    [SerializeField] private float _ultimateCooldown = 60f;
    
    // 스킬 쿨다운 타이머
    private float[] _skillCooldowns = new float[4];
    
    // 스킬 상태
    private bool _isCounterActive = false;
    private bool _isDashing = false;
    private bool _isUltimateCasting = false;
    
    // 네트워크 변수
    private NetworkVariable<bool> _isCounterActiveNet = new NetworkVariable<bool>(false);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 컴포넌트 자동 참조
        if (_playerController == null)
            _playerController = GetComponent<PlayerController>();
            
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
            
        // 네트워크 변수 콜백
        _isCounterActiveNet.OnValueChanged += OnCounterStateChanged;
    }
    
    private void Update()
    {
        if (!IsOwner) return;
        
        // 쿨다운 업데이트
        UpdateCooldowns();
        
        // 스킬 입력 처리
        HandleSkillInput();
    }
    
    /// <summary>
    /// 쿨다운 업데이트
    /// </summary>
    private void UpdateCooldowns()
    {
        for (int i = 0; i < _skillCooldowns.Length; i++)
        {
            if (_skillCooldowns[i] > 0)
            {
                _skillCooldowns[i] -= Time.deltaTime;
                if (_skillCooldowns[i] < 0) _skillCooldowns[i] = 0;
            }
        }
    }
    
    /// <summary>
    /// 스킬 입력 처리
    /// </summary>
    private void HandleSkillInput()
    {
        // 캐스팅 중이거나 대시 중에는 다른 스킬 사용 불가
        if (_isUltimateCasting || _isDashing) return;
        
        // Q 스킬 - 강한 베기
        if (Input.GetKeyDown(KeyCode.Q) && _skillCooldowns[0] <= 0)
        {
            UseSlashSkill();
        }
        
        // W 스킬 - 반격기
        if (Input.GetKeyDown(KeyCode.W) && _skillCooldowns[1] <= 0)
        {
            UseCounterSkill();
        }
        
        // E 스킬 - 이동기
        if (Input.GetKeyDown(KeyCode.E) && _skillCooldowns[2] <= 0)
        {
            UseDashSkill();
        }
        
        // R 스킬 - 궁극기
        if (Input.GetKeyDown(KeyCode.R) && _skillCooldowns[3] <= 0)
        {
            StartCoroutine(CastUltimateSkill());
        }
    }
    
    /// <summary>
    /// Q 스킬 - 강한 베기
    /// </summary>
    private void UseSlashSkill()
    {
        _skillCooldowns[0] = _slashCooldown;
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("SlashAttack");
        }
        
        // 서버에 스킬 사용 요청
        UseSlashSkillServerRpc(transform.position, transform.forward);
    }
    
    [ServerRpc]
    private void UseSlashSkillServerRpc(Vector3 position, Vector3 direction)
    {
        // 이펙트 생성
        if (_slashEffectPrefab != null)
        {
            Vector3 spawnPos = _skillOrigin != null ? _skillOrigin.position : position + Vector3.up;
            GameObject effectObj = Instantiate(_slashEffectPrefab, spawnPos, Quaternion.LookRotation(direction));
            
            // 네트워크에 스폰
            NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn();
                StartCoroutine(DestroyAfterDelay(networkObj, 1f));
            }
            else
            {
                Destroy(effectObj, 1f);
            }
        }
        
        // 부채꼴 범위 내 적 검출 및 데미지 적용
        ApplySlashDamage(position, direction, _slashRange, _slashWidth, _slashDamage);
        
        // 모든 클라이언트에서 이펙트 표시
        ShowSlashEffectClientRpc(position, direction);
    }
    
    [ClientRpc]
    private void ShowSlashEffectClientRpc(Vector3 position, Vector3 direction)
    {
        // 로컬 이펙트 추가 (필요시)
        // 여기서는 애니메이션과 서버에서 생성한 이펙트로 충분하므로 생략
    }
    
    /// <summary>
    /// 부채꼴 범위에 데미지 적용
    /// </summary>
    private void ApplySlashDamage(Vector3 center, Vector3 forward, float range, float angle, float damage)
    {
        // 범위 내 모든 콜라이더 검출
        Collider[] hitColliders = Physics.OverlapSphere(center, range);
        
        foreach (var hitCollider in hitColliders)
        {
            // 자기 자신은 제외
            if (hitCollider.gameObject == gameObject) continue;
            
            // 대상 위치
            Vector3 directionToTarget = hitCollider.transform.position - center;
            directionToTarget.y = 0; // Y축 무시 (수평면에서만 계산)
            
            // 부채꼴 범위 내에 있는지 확인
            float angleToTarget = Vector3.Angle(forward, directionToTarget);
            if (angleToTarget <= angle * 0.5f && directionToTarget.magnitude <= range)
            {
                // 더미 타겟이나 적 플레이어에게 데미지 적용
                DummyTarget target = hitCollider.GetComponent<DummyTarget>();
                if (target != null)
                {
                    target.TakeDamageServerRpc(damage);
                }
                
                // 추후 플레이어 데미지 로직 추가 가능
            }
        }
    }
    
    /// <summary>
    /// W 스킬 - 반격기
    /// </summary>
    private void UseCounterSkill()
    {
        _skillCooldowns[1] = _counterCooldown;
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("CounterStance");
        }
        
        // 서버에 스킬 사용 요청
        UseCounterSkillServerRpc();
        
        // 일정 시간 후 반격 상태 해제 예약
        StartCoroutine(DeactivateCounterAfterDelay(_counterDuration));
    }
    
    [ServerRpc]
    private void UseCounterSkillServerRpc()
    {
        // 반격 상태 활성화
        _isCounterActiveNet.Value = true;
        
        // 이펙트 생성
        if (_counterEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(_counterEffectPrefab, transform.position, transform.rotation);
            effectObj.transform.SetParent(transform);
            
            // 네트워크에 스폰
            NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn();
                StartCoroutine(DestroyAfterDelay(networkObj, _counterDuration));
            }
            else
            {
                Destroy(effectObj, _counterDuration);
            }
        }
    }
    
    private IEnumerator DeactivateCounterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 서버에 반격 상태 해제 요청
        DeactivateCounterServerRpc();
    }
    
    [ServerRpc]
    private void DeactivateCounterServerRpc()
    {
        _isCounterActiveNet.Value = false;
    }
    
    /// <summary>
    /// 반격 상태 변경 콜백
    /// </summary>
    private void OnCounterStateChanged(bool oldValue, bool newValue)
    {
        _isCounterActive = newValue;
        
        // UI나 시각적 효과 업데이트
        if (_animator != null)
        {
            _animator.SetBool("IsCountering", newValue);
        }
    }
    
    /// <summary>
    /// 데미지를 받았을 때 반격 처리 (DummyTarget이나 적 플레이어로부터 호출)
    /// </summary>
    public bool ProcessCounter(GameObject attacker, float incomingDamage)
    {
        if (!_isCounterActive) return false;
        
        // 반격 성공 시 반격 효과 발동
        if (IsServer)
        {
            // 공격자에게 반격 데미지 적용
            DummyTarget attackerTarget = attacker.GetComponent<DummyTarget>();
            if (attackerTarget != null)
            {
                attackerTarget.TakeDamageServerRpc(_counterDamage);
            }
            
            // 반격 성공 애니메이션 재생
            TriggerCounterSuccessClientRpc();
            
            // 반격 상태 해제
            _isCounterActiveNet.Value = false;
        }
        
        return true;
    }
    
    [ClientRpc]
    private void TriggerCounterSuccessClientRpc()
    {
        // 반격 성공 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("CounterSuccess");
        }
        
        // 추가 이펙트 재생 가능
    }
    
    /// <summary>
    /// E 스킬 - 이동기
    /// </summary>
    private void UseDashSkill()
    {
        _skillCooldowns[2] = _dashCooldown;
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("Dash");
        }
        
        // 대시 방향 계산 (마우스 위치 또는 현재 바라보는 방향)
        Vector3 dashDirection = transform.forward;
        
        // 서버에 스킬 사용 요청
        UseDashSkillServerRpc(transform.position, dashDirection);
    }
    
    [ServerRpc]
    private void UseDashSkillServerRpc(Vector3 startPosition, Vector3 direction)
    {
        StartCoroutine(PerformDash(startPosition, direction));
    }
    
    private IEnumerator PerformDash(Vector3 startPosition, Vector3 direction)
    {
        _isDashing = true;
        
        // 대시 시작 이펙트
        if (_dashEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(_dashEffectPrefab, transform.position, Quaternion.LookRotation(direction));
            effectObj.transform.SetParent(transform);
            
            NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn();
                StartCoroutine(DestroyAfterDelay(networkObj, 1f));
            }
            else
            {
                Destroy(effectObj, 1f);
            }
        }
        
        // 대시 실행
        float distanceTraveled = 0f;
        Vector3 dashVector = direction.normalized * _dashDistance;
        
        // 대시 경로에 있는 적들에게 데미지를 줄 대상 목록
        HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        
        while (distanceTraveled < _dashDistance)
        {
            float dashStep = _dashSpeed * Time.deltaTime;
            distanceTraveled += dashStep;
            
            // 이동 적용
            transform.position += direction.normalized * dashStep;
            
            // 현재 위치에서 적 검출
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1f);
            foreach (var hitCollider in hitColliders)
            {
                // 자기 자신은 제외
                if (hitCollider.gameObject == gameObject) continue;
                
                // 이미 타격한 대상은 제외
                if (hitTargets.Contains(hitCollider.gameObject)) continue;
                
                // 적에게 데미지 적용
                DummyTarget target = hitCollider.GetComponent<DummyTarget>();
                if (target != null)
                {
                    target.TakeDamageServerRpc(_dashDamage);
                    hitTargets.Add(hitCollider.gameObject);
                }
                
                // 추후 플레이어 데미지 로직 추가 가능
            }
            
            yield return null;
        }
        
        _isDashing = false;
        
        // 대시 종료 이펙트 또는 애니메이션
        DashCompleteClientRpc();
    }
    
    [ClientRpc]
    private void DashCompleteClientRpc()
    {
        // 대시 종료 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("DashComplete");
        }
    }
    
    /// <summary>
    /// R 스킬 - 궁극기
    /// </summary>
    private IEnumerator CastUltimateSkill()
    {
        _skillCooldowns[3] = _ultimateCooldown;
        _isUltimateCasting = true;
        
        // 캐스팅 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("UltimateCast");
        }
        
        // 서버에 캐스팅 시작 알림
        StartUltimateCastingServerRpc();
        
        // 캐스팅 시간 대기
        yield return new WaitForSeconds(_ultimateCastTime);
        
        // 캐스팅 완료, 스킬 발동
        _isUltimateCasting = false;
        
        // 서버에 스킬 사용 요청
        UseUltimateSkillServerRpc(transform.position, transform.forward);
    }
    
    [ServerRpc]
    private void StartUltimateCastingServerRpc()
    {
        // 캐스팅 시작 이펙트
        StartUltimateCastingClientRpc();
    }
    
    [ClientRpc]
    private void StartUltimateCastingClientRpc()
    {
        // 캐스팅 이펙트 시작
        // 예: 캐릭터 주변에 에너지 축적 효과
    }
    
    [ServerRpc]
    private void UseUltimateSkillServerRpc(Vector3 position, Vector3 direction)
    {
        // 이펙트 생성
        if (_ultimateEffectPrefab != null)
        {
            Vector3 targetPos = position + direction * _ultimateRange;
            GameObject effectObj = Instantiate(_ultimateEffectPrefab, targetPos, Quaternion.identity);
            
            NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn();
                StartCoroutine(DestroyAfterDelay(networkObj, 3f));
            }
            else
            {
                Destroy(effectObj, 3f);
            }
        }
        
        // 범위 내 적에게 데미지 적용
        Vector3 impactPos = position + direction * _ultimateRange;
        ApplyUltimateDamage(impactPos, _ultimateRadius, _ultimateDamage);
        
        // 궁극기 발동 효과 (모든 클라이언트)
        UltimateActivatedClientRpc(impactPos);
    }
    
    private void ApplyUltimateDamage(Vector3 center, float radius, float damage)
    {
        // 범위 내 모든 콜라이더 검출
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        
        foreach (var hitCollider in hitColliders)
        {
            // 자기 자신은 제외
            if (hitCollider.gameObject == gameObject) continue;
            
            // 적에게 데미지 적용
            DummyTarget target = hitCollider.GetComponent<DummyTarget>();
            if (target != null)
            {
                target.TakeDamageServerRpc(damage);
            }
            
            // 추후 플레이어 데미지 로직 추가 가능
        }
    }
    
    [ClientRpc]
    private void UltimateActivatedClientRpc(Vector3 impactPos)
    {
        // 궁극기 발동 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("UltimateRelease");
        }
        
        // 카메라 효과나 화면 효과 추가 가능
    }
    
    /// <summary>
    /// 네트워크 객체 지연 제거
    /// </summary>
    private IEnumerator DestroyAfterDelay(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null && obj.IsSpawned)
        {
            obj.Despawn();
        }
    }
    
    /// <summary>
    /// 스킬 쿨다운 정보 제공
    /// </summary>
    public float GetSkillCooldown(int index)
    {
        if (index < 0 || index >= _skillCooldowns.Length)
            return 0f;
            
        return _skillCooldowns[index];
    }
    
    /// <summary>
    /// 스킬 최대 쿨다운 정보 제공
    /// </summary>
    public float GetSkillMaxCooldown(int index)
    {
        switch (index)
        {
            case 0: return _slashCooldown;
            case 1: return _counterCooldown;
            case 2: return _dashCooldown;
            case 3: return _ultimateCooldown;
            default: return 0f;
        }
    }
}