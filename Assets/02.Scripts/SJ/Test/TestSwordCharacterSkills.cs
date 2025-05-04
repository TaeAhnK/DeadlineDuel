using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

/// <summary>
/// 검을 사용하는 캐릭터의 스킬 테스트용 클래스
/// </summary>
public class TestSwordCharacterSkills : MonoBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] private TestPlayerController _playerController;
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
    
    [Header("디버그")]
    [SerializeField] private bool _showHitboxes = false;
    
    // 스킬 쿨다운 타이머
    private float[] _skillCooldowns = new float[4];
    
    // 스킬 상태
    private bool _isCounterActive = false;
    private bool _isDashing = false;
    private bool _isUltimateCasting = false;
    
    // 디버그 정보
    [SerializeField, ReadOnly] private float[] _debugCooldowns = new float[4];
    [SerializeField, ReadOnly] private string[] _skillStateText = new string[4];
    
    /// <summary>
    /// 스킬별 최대 쿨다운 값 배열
    /// </summary>
    private float[] _maxCooldowns;
    
    private void Start()
    {
        // 컴포넌트 자동 참조
        if (_playerController == null)
            _playerController = GetComponent<TestPlayerController>();
            
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
            
        // 쿨다운 초기화
        _maxCooldowns = new float[]
        {
            _slashCooldown,
            _counterCooldown,
            _dashCooldown,
            _ultimateCooldown
        };
        
        // 스킬 오리진이 없으면 생성
        if (_skillOrigin == null)
        {
            GameObject originObj = new GameObject("SkillOrigin");
            originObj.transform.SetParent(transform);
            originObj.transform.localPosition = new Vector3(0, 1.5f, 1f); // 기본 위치 설정
            _skillOrigin = originObj.transform;
            
            Debug.Log("스킬 오리진 자동 생성됨");
        }
        
        Debug.Log("테스트용 검 캐릭터 스킬 컴포넌트 초기화됨");
    }
    
    private void Update()
    {
        // 쿨다운 업데이트
        UpdateCooldowns();
        
        // 스킬 입력 처리
        HandleSkillInput();
        
        // 디버그 정보 업데이트
        UpdateDebugInfo();
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
        
        Debug.Log("Q 스킬 - 강한 베기 사용");
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("SlashAttack");
        }
        
        // 이펙트 생성
        if (_slashEffectPrefab != null)
        {
            Vector3 spawnPos = _skillOrigin != null ? _skillOrigin.position : transform.position + Vector3.up;
            GameObject effectObj = Instantiate(_slashEffectPrefab, spawnPos, Quaternion.LookRotation(transform.forward));
            Destroy(effectObj, 1f);
        }
        
        // 부채꼴 범위 내 적 검출 및 데미지 적용
        ApplySlashDamage(transform.position, transform.forward, _slashRange, _slashWidth, _slashDamage);
    }
    
    /// <summary>
    /// 부채꼴 범위에 데미지 적용
    /// </summary>
    private void ApplySlashDamage(Vector3 center, Vector3 forward, float range, float angle, float damage)
    {
        // 범위 내 모든 콜라이더 검출
        Collider[] hitColliders = Physics.OverlapSphere(center, range);
        int hitCount = 0;
        
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
                // 테스트용 더미 타겟에 데미지 적용
                TestDummyTarget target = hitCollider.GetComponent<TestDummyTarget>();
                if (target != null)
                {
                    target.TakeDamage(damage);
                    hitCount++;
                    
                    // 타격 효과
                    StartCoroutine(HighlightObject(hitCollider.gameObject));
                }
            }
        }
        
        Debug.Log($"Q 스킬 - 강한 베기: {hitCount}개 대상 적중, 각각 {damage} 데미지");
        
        // 디버그 시각화
        if (_showHitboxes)
        {
            StartCoroutine(VisualizeSlash(center, forward, range, angle));
        }
    }
    
    /// <summary>
    /// 부채꼴 범위 시각화 (디버그용)
    /// </summary>
    private IEnumerator VisualizeSlash(Vector3 center, Vector3 forward, float range, float angle)
    {
        float duration = 0.5f;
        float startTime = Time.time;
        
        while (Time.time - startTime < duration)
        {
            // 부채꼴 그리기
            int segments = 20;
            float halfAngle = angle * 0.5f;
            Vector3 previousPoint = center + Quaternion.Euler(0, -halfAngle, 0) * forward * range;
            
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = -halfAngle + (angle * i / segments);
                Vector3 currentPoint = center + Quaternion.Euler(0, currentAngle, 0) * forward * range;
                
                // 선 그리기
                Debug.DrawLine(center, currentPoint, Color.red);
                
                if (i > 0)
                {
                    Debug.DrawLine(previousPoint, currentPoint, Color.red);
                }
                
                previousPoint = currentPoint;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// W 스킬 - 반격기
    /// </summary>
    private void UseCounterSkill()
    {
        _skillCooldowns[1] = _counterCooldown;
        
        Debug.Log("W 스킬 - 반격기 준비");
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("CounterStance");
            _animator.SetBool("IsCountering", true);
        }
        
        // 반격 상태 활성화
        _isCounterActive = true;
        
        // 이펙트 생성
        if (_counterEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(_counterEffectPrefab, transform.position, transform.rotation);
            effectObj.transform.SetParent(transform);
            Destroy(effectObj, _counterDuration);
        }
        
        // 일정 시간 후 반격 상태 해제
        StartCoroutine(DeactivateCounterAfterDelay(_counterDuration));
    }
    
    private IEnumerator DeactivateCounterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 반격 상태 해제
        _isCounterActive = false;
        
        // 애니메이션 업데이트
        if (_animator != null)
        {
            _animator.SetBool("IsCountering", false);
        }
        
        Debug.Log("W 스킬 - 반격기 종료");
    }
    
    /// <summary>
    /// 데미지를 받았을 때 반격 처리
    /// </summary>
    public bool ProcessCounter(GameObject attacker, float incomingDamage)
    {
        if (!_isCounterActive) return false;
        
        Debug.Log($"반격 성공! 대상: {attacker.name}, 받은 데미지: {incomingDamage}, 반격 데미지: {_counterDamage}");
        
        // 반격 성공 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("CounterSuccess");
            _animator.SetBool("IsCountering", false);
        }
        
        // 공격자에게 반격 데미지 적용
        TestDummyTarget attackerTarget = attacker.GetComponent<TestDummyTarget>();
        if (attackerTarget != null)
        {
            attackerTarget.TakeDamage(_counterDamage);
        }
        
        // 반격 상태 해제
        _isCounterActive = false;
        
        return true;
    }
    
    /// <summary>
    /// E 스킬 - 이동기
    /// </summary>
    private void UseDashSkill()
    {
        _skillCooldowns[2] = _dashCooldown;
        
        Debug.Log("E 스킬 - 대시 사용");
        
        // 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("Dash");
        }
        
        // 대시 방향 계산 (마우스 위치 또는 현재 바라보는 방향)
        Vector3 dashDirection = transform.forward;
        
        // 대시 실행
        StartCoroutine(PerformDash(transform.position, dashDirection));
    }
    
    private IEnumerator PerformDash(Vector3 startPosition, Vector3 direction)
    {
        _isDashing = true;
        
        // 대시 시작 이펙트
        if (_dashEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(_dashEffectPrefab, transform.position, Quaternion.LookRotation(direction));
            effectObj.transform.SetParent(transform);
            Destroy(effectObj, 1f);
        }
        
        // 대시 실행
        float distanceTraveled = 0f;
        Vector3 dashVector = direction.normalized * _dashDistance;
        
        // 충돌 검사를 위한 레이캐스트
        RaycastHit hit;
        bool willHitObstacle = Physics.Raycast(startPosition, direction, out hit, _dashDistance, LayerMask.GetMask("Default"));
        float adjustedDistance = willHitObstacle ? hit.distance - 0.5f : _dashDistance;
        
        // NavMeshAgent 비활성화 (직접 이동 사용)
        TestPlayerController controller = GetComponent<TestPlayerController>();
        if (controller != null)
        {
            controller.DisableNavAgent();
        }
        
        // 대시 경로에 있는 적들에게 데미지를 줄 대상 목록
        HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        
        // 시작 및 종료 위치 저장
        Vector3 dashEndPosition = startPosition + direction.normalized * adjustedDistance;
        
        // 디버그 라인
        if (_showHitboxes)
        {
            Debug.DrawLine(startPosition, dashEndPosition, Color.blue, 1f);
        }
        
        while (distanceTraveled < adjustedDistance)
        {
            float dashStep = _dashSpeed * Time.deltaTime;
            distanceTraveled += dashStep;
            
            if (distanceTraveled > adjustedDistance)
            {
                dashStep -= (distanceTraveled - adjustedDistance);
                distanceTraveled = adjustedDistance;
            }
            
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
                TestDummyTarget target = hitCollider.GetComponent<TestDummyTarget>();
                if (target != null)
                {
                    target.TakeDamage(_dashDamage);
                    hitTargets.Add(hitCollider.gameObject);
                    
                    // 타격 효과
                    StartCoroutine(HighlightObject(hitCollider.gameObject));
                    
                    Debug.Log($"대시 중 {hitCollider.name}에게 {_dashDamage} 데미지");
                }
            }
            
            yield return null;
        }
        
        // 최종 위치 보정
        transform.position = dashEndPosition;
        
        _isDashing = false;
        
        // NavMeshAgent 다시 활성화
        if (controller != null)
        {
            controller.EnableNavAgent();
        }
        
        // 대시 종료 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("DashComplete");
        }
        
        Debug.Log($"대시 완료, 타격한 대상 수: {hitTargets.Count}");
    }
    
    /// <summary>
    /// R 스킬 - 궁극기
    /// </summary>
    private IEnumerator CastUltimateSkill()
    {
        _skillCooldowns[3] = _ultimateCooldown;
        _isUltimateCasting = true;
        
        Debug.Log($"R 스킬 - 궁극기 시전 시작 (캐스팅 시간: {_ultimateCastTime}초)");
        
        // 캐스팅 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("UltimateCast");
        }
        
        // 캐스팅 이펙트 (필요시)
        GameObject castingEffect = null;
        if (_ultimateEffectPrefab != null)
        {
            castingEffect = Instantiate(_ultimateEffectPrefab, transform.position, Quaternion.identity);
            castingEffect.transform.localScale = Vector3.one * 0.5f; // 캐스팅 중에는 작게
        }
        
        // 캐스팅 시간 대기
        yield return new WaitForSeconds(_ultimateCastTime);
        
        // 캐스팅 완료, 스킬 발동
        _isUltimateCasting = false;
        
        // 캐스팅 이펙트 제거
        if (castingEffect != null)
        {
            Destroy(castingEffect);
        }
        
        // 스킬 사용 애니메이션
        if (_animator != null)
        {
            _animator.SetTrigger("UltimateRelease");
        }
        
        // 이펙트 생성
        Vector3 targetPos = transform.position + transform.forward * _ultimateRange;
        if (_ultimateEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(_ultimateEffectPrefab, targetPos, Quaternion.identity);
            effectObj.transform.localScale = Vector3.one * 2f; // 발동 시에는 크게
            Destroy(effectObj, 3f);
        }
        
        // 범위 내 적에게 데미지 적용
        ApplyUltimateDamage(targetPos, _ultimateRadius, _ultimateDamage);
        
        Debug.Log($"R 스킬 - 궁극기 발동 완료 (대상 위치: {targetPos}, 범위: {_ultimateRadius})");
        
        // 디버그 시각화
        if (_showHitboxes)
        {
            StartCoroutine(VisualizeCircle(targetPos, _ultimateRadius, 1f));
        }
    }
    
    private void ApplyUltimateDamage(Vector3 center, float radius, float damage)
    {
        // 범위 내 모든 콜라이더 검출
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        int hitCount = 0;
        
        foreach (var hitCollider in hitColliders)
        {
            // 자기 자신은 제외
            if (hitCollider.gameObject == gameObject) continue;
            
            // 적에게 데미지 적용
            TestDummyTarget target = hitCollider.GetComponent<TestDummyTarget>();
            if (target != null)
            {
                target.TakeDamage(damage);
                hitCount++;
                
                // 타격 효과
                StartCoroutine(HighlightObject(hitCollider.gameObject));
            }
        }
        
        Debug.Log($"궁극기 데미지 적용: {hitCount}개 대상 적중, 각각 {damage} 데미지");
    }
    
    /// <summary>
    /// 원형 범위 시각화 (디버그용)
    /// </summary>
    private IEnumerator VisualizeCircle(Vector3 center, float radius, float duration)
    {
        float startTime = Time.time;
        
        while (Time.time - startTime < duration)
        {
            // 원 그리기
            int segments = 30;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * 2 * Mathf.PI / segments;
                Vector3 currentPoint = center + new Vector3(radius * Mathf.Cos(angle), 0, radius * Mathf.Sin(angle));
                Debug.DrawLine(previousPoint, currentPoint, Color.yellow);
                previousPoint = currentPoint;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 오브젝트 하이라이트 효과 (데미지 적용 시각화)
    /// </summary>
    private IEnumerator HighlightObject(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            
            yield return new WaitForSeconds(0.2f);
            
            renderer.material.color = originalColor;
        }
    }
    
    /// <summary>
    /// 디버그 정보 업데이트
    /// </summary>
    private void UpdateDebugInfo()
    {
        // 쿨다운 시간 복사
        System.Array.Copy(_skillCooldowns, _debugCooldowns, _skillCooldowns.Length);
        
        // 스킬 상태 텍스트
        _skillStateText[0] = _skillCooldowns[0] > 0 ? $"쿨다운: {_skillCooldowns[0]:F1}s" : "사용 가능";
        _skillStateText[1] = _isCounterActive ? "반격 대기중" : (_skillCooldowns[1] > 0 ? $"쿨다운: {_skillCooldowns[1]:F1}s" : "사용 가능");
        _skillStateText[2] = _isDashing ? "대시 중" : (_skillCooldowns[2] > 0 ? $"쿨다운: {_skillCooldowns[2]:F1}s" : "사용 가능");
        _skillStateText[3] = _isUltimateCasting ? "시전 중" : (_skillCooldowns[3] > 0 ? $"쿨다운: {_skillCooldowns[3]:F1}s" : "사용 가능");
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
        if (index < 0 || index >= _maxCooldowns.Length)
            return 0f;
            
        return _maxCooldowns[index];
    }
    
    /// <summary>
    /// 스킬 테스트용 버튼 메서드 (Inspector에서 호출)
    /// </summary>
    public void TestSkill(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0:
                if (_skillCooldowns[0] <= 0) UseSlashSkill();
                break;
            case 1:
                if (_skillCooldowns[1] <= 0) UseCounterSkill();
                break;
            case 2:
                if (_skillCooldowns[2] <= 0) UseDashSkill();
                break;
            case 3:
                if (_skillCooldowns[3] <= 0) StartCoroutine(CastUltimateSkill());
                break;
        }
    }
    
    /// <summary>
    /// Gizmos로 범위 표시 (에디터에서만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Q 스킬 범위 (부채꼴)
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        DrawArcGizmo(transform.position, transform.forward, _slashRange, _slashWidth);
        
        // R 스킬 범위 (원)
        Vector3 ultimatePos = transform.position + transform.forward * _ultimateRange;
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawSphere(ultimatePos, _ultimateRadius);
    }
    
    /// <summary>
    /// 부채꼴 기즈모 그리기
    /// </summary>
    private void DrawArcGizmo(Vector3 center, Vector3 forward, float radius, float angle)
    {
        int segments = 20;
        float halfAngle = angle * 0.5f;
        Vector3 previousPoint = center + Quaternion.Euler(0, -halfAngle, 0) * forward * radius;
        
        // 부채꼴 경계선 그리기
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -halfAngle + (angle * i / segments);
            Vector3 currentPoint = center + Quaternion.Euler(0, currentAngle, 0) * forward * radius;
            
            Gizmos.DrawLine(center, currentPoint);
            
            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, currentPoint);
            }
            
            previousPoint = currentPoint;
        }
    }
}

