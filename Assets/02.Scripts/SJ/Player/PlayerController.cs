using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Netcode.Components;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    private QuadViewCinemachine _cameraController;
    private Camera _playerCamera;
    
    [Header("이동 설정")]
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private GameObject _moveIndicatorPrefab;
    
    [Header("스킬 설정")]
    [SerializeField] private Skill[] _skills = new Skill[4]; // Q, W, E, R 스킬
    [SerializeField] private Transform _skillOrigin;
    
    [Header("체력바 설정")]
    [SerializeField] private RectTransform _healthBarBackground;
    [SerializeField] private Image _healthBarFill;
    
    [Header("일반 공격 설정")]
    [SerializeField] private int _maxCombo = 4;
    [SerializeField] private float _comboWindow = 1f; // 콤보 유지 시간
    [SerializeField] private float _comboDistance = 2f; // 일반공격 거리
    [SerializeField] private float _comboDamage = 10f; // 일반공격 데미지
    
    [Header("일반 공격 이펙트")]
    [SerializeField] private GameObject[] _normalAttackEffects; // 콤보별 이펙트
    [SerializeField] private GameObject _dashAttackEffect; // 대시 공격 이펙트
    
    // 이펙트 회전 옵션 추가
    [Header("이펙트 회전 설정")]
    [SerializeField] private bool _useEffectCustomRotation = true;
    [SerializeField] private Vector3[] _normalAttackRotationOffsets; // 콤보별 회전값
    [SerializeField] private Vector3 _dashAttackRotationOffset; // 대시 공격 회전값
    
    // 이펙트 스폰 위치
    [SerializeField] private Transform _attackEffectOrigin;
    
    // 컴포넌트 참조
    private Animator _animator;
    private NavMeshAgent _navAgent;
    private GameObject _moveIndicator;
    private Object_Base _objectBase;
    private CharacterStats _characterStats;
    
    //일반공격콤보변수
    private int _currentCombo = 0;
    private float _lastAttackTime = 0f;
    
    // 네트워크 변수
    private NetworkVariable<bool> _isMoving = new NetworkVariable<bool>(false);
    private NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(-1);
    private NetworkVariable<bool> _isDead = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> _isAttacking = new NetworkVariable<bool>(false);
    
    // 스킬 쿨다운 타이머
    private float[] _skillCooldowns = new float[4];
    
    // 선택된 캐릭터 정보
    private int _characterIndex;
    private Dictionary<int, float> _skillMaxCooldowns = new Dictionary<int, float>();
    private Dictionary<int, float> _skillDamages = new Dictionary<int, float>();
    
    //캐릭터 움직임 동기화 부분
    // 마지막 위치 및 회전 업데이트 시간 추적
    private float _lastPositionUpdateTime;
    private Vector3 _lastSentPosition;
    private Quaternion _lastSentRotation;
    
    // 위치 업데이트 간격 (초) - 값이 작을수록 더 자주 업데이트, 더 부드러움
    [SerializeField] private float _positionUpdateInterval = 0.05f; // 초당 20회 업데이트
    
    // 위치 변경 임계값 - 이 값 이상 변경되었을 때만 업데이트
    [SerializeField] private float _positionThreshold = 0.01f;
    [SerializeField] private float _rotationThreshold = 1.0f;
    
    [System.Serializable]
    public class Skill
    {
        public string name;
        public float cooldown;
        public GameObject effectPrefab;
        public float damage;
        public float range;
        public float aoeRadius;
        public bool isTargeted;
        public string animationTrigger;
    }

    public override void OnNetworkSpawn()
    {

        Debug.Log($"[OnNetworkSpawn] IsOwner: {IsOwner}, OwnerClientId: {OwnerClientId}");

        base.OnNetworkSpawn();
        
        // 컴포넌트 참조 가져오기
        _animator = GetComponentInChildren<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        _objectBase = GetComponent<Object_Base>();
        _characterStats = GetComponent<CharacterStats>();
        
        CreateMoveIndicator();
        InitializeSkills();
        LoadCharacterData();

        // NavMeshAgent 설정
        if (_navAgent != null && _characterStats != null)
        {
            _navAgent.speed = _objectBase.GetMoveSpeed();
            _navAgent.angularSpeed = _rotationSpeed * 100;
            _navAgent.acceleration = 8f;
        }

       
        if(IsOwner)
        {
            SetupCamera();
            // 자신의 플레이어인 경우 초기화
            _lastSentPosition = transform.position;
            _lastSentRotation = transform.rotation;
            _lastPositionUpdateTime = Time.time;

            // NavMeshAgent 설정
            if (_navAgent != null && _characterStats != null)
            {
                _navAgent.speed = _objectBase.GetMoveSpeed();
                _navAgent.angularSpeed = _rotationSpeed * 100;
                _navAgent.acceleration = 8f;
            }
        }

        // 체력바 초기화
        InitializeHealthBar();
        
        // 네트워크 변수 콜백
        _isMoving.OnValueChanged += OnMovingChanged;
        _currentSkillIndex.OnValueChanged += OnSkillIndexChanged;
        _isDead.OnValueChanged += OnDeathStateChanged;
        
        // CharacterStats의 OnDeath 이벤트 구독
        if (_characterStats != null)
        {
            _characterStats.OnDeath += HandleLocalDeath;
        }
    }

    private void LoadCharacterData()
    {
        // CharacterManager가 없는 경우 기본값 사용
        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null) return;
    
        // 선택된 캐릭터 인덱스
        _characterIndex = characterManager.GetSelectedCharacterIndex();
    
        // 캐릭터 스탯 데이터
        CharacterStatsData statsData = characterManager.GetCharacterStats(_characterIndex);
    
        // 스탯 설정 - Object_Base를 통해 설정
        if (_objectBase != null && statsData != null && IsServer)
        {
            _objectBase.SetCharacterStats(statsData);
        }
    
        // 스킬 정보 업데이트
        UpdateSkillsFromCharacterData();
    }
    
    private void UpdateSkillsFromCharacterData()
    {
        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null) return;
        
        CharacterData characterData = characterManager.GetCharacterData(_characterIndex);
        if (characterData == null) return;
        
        // 스킬 정보 업데이트
        for (int i = 0; i < _skills.Length && i < characterData.skills.Length; i++)
        {
            if (characterData.skills[i] != null)
            {
                // 기존 스킬 객체가 없으면 생성
                if (_skills[i] == null)
                    _skills[i] = new Skill();
                
                // 스킬 정보 업데이트
                _skills[i].name = characterData.skills[i].name;
                _skills[i].cooldown = characterData.skills[i].cooldown;
                _skills[i].damage = characterData.skills[i].damage;
                _skills[i].effectPrefab = characterData.skills[i].skillEffectPrefab;
                
                // 쿨다운 및 데미지 정보 저장
                _skillMaxCooldowns[i] = _skills[i].cooldown;
                _skillDamages[i] = _skills[i].damage;
            }
        }
        
        // 로컬 플레이어일 경우 UI에 초기 스킬 정보 전달
        if (IsOwner)
        {
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i] != null)
                {
                    GamePlayManager.Instance.StartSkillCooldownFromServer(i, 0);
                }
            }
        }
    }

    private void InitializeSkills()
    {
        // 기본 스킬 쿨다운 정보 설정
        for (int i = 0; i < _skills.Length; i++)
        {
            if (_skills[i] != null)
            {
                _skillMaxCooldowns[i] = _skills[i].cooldown;
                _skillDamages[i] = _skills[i].damage;
            }
        }
    }

    private void InitializeHealthBar()
    {
        // Object_Base를 통해 체력 정보 접근 (구성 패턴 사용)
        if (_objectBase != null && _healthBarFill != null)
        {
            // 초기 체력바 상태 설정
            UpdateHealthBar(_objectBase.GetCurrentHP(), _objectBase.GetMaxHP());
    
            // 데미지 이벤트 구독
            _objectBase.OnDamageTaken += UpdateHealthBar;
        }
        else
        {
            // null 체크를 위한 로그 추가
            Debug.LogWarning($"InitializeHealthBar: _objectBase = {_objectBase}, _healthBarFill = {_healthBarFill}");
        }
    }
    
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (_healthBarFill != null)
        {
            // 체력 비율 계산
            float fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
            _healthBarFill.fillAmount = fillAmount;
        
            // 로컬 플레이어일 경우 GamePlayManager에 HP 상태 업데이트
            if (IsOwner)
            {
                GamePlayManager.Instance.UpdatePlayerHPFromServer(fillAmount);
            }
        }
        else
        {
            Debug.LogWarning($"UpdateHealthBar: _healthBarFill is null. CurrentHealth = {currentHealth}, MaxHealth = {maxHealth}");
        }
    }
    
    private void SetupCamera()
    {
        
        if (!IsOwner) return; // 로컬 플레이어가 아니면 실행 안함
        Debug.Log("=== SetupCamera 호출 ===");
    
        // 자식에서 이미 있는 QuadViewCinemachine 찾기
        _cameraController = GetComponentInChildren<QuadViewCinemachine>();
    
        if (_cameraController != null)
        {
            _cameraController.SetTarget(transform);
            _playerCamera = _cameraController.GetCamera();
        
            if (_playerCamera != null)
            {
                Debug.Log($"카메라 설정 성공: {_playerCamera.name}");
            }
        }
    }
    
    private IEnumerator GetQuadViewCameraWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 카메라 컨트롤러에서 카메라 참조 가져오기
        if (_cameraController != null)
        {
            _playerCamera = _cameraController.GetCamera();
        }
        
        // 카메라를 찾지 못한 경우 대체 방법
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
    }
    
    private void CreateMoveIndicator()
    {
        if (_moveIndicatorPrefab != null)
        {
            _moveIndicator = Instantiate(_moveIndicatorPrefab);
            _moveIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 이동 입력 처리
        HandleMovementInput();

        // 스킬 입력 처리
        HandleSkillInput();

        UpdateMovementAnimation();
        
        // 일반 공격 입력 처리
        HandleNormalAttack();
        
        // 콤보 초기화 체크
        CheckComboReset();

        // 스킬 쿨다운 업데이트
        UpdateSkillCooldowns();

        // 이동 상태 업데이트
        UpdateMovementState();

        // 위치 동기화 로직 추가
        UpdatePositionSynchronization();

        
    }


    private void HandleMovementInput()
    {
        // 마우스 우클릭 감지
        if (Input.GetMouseButtonDown(1))
        {
            if (_playerCamera == null)
            {
                Debug.LogError("카메라를 찾을 수 없습니다. 이동 불가.");
                return;
            }

            Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
            {
                // 이동 표시자 표시
                if (_moveIndicator != null)
                {
                    _moveIndicator.transform.position = hit.point + Vector3.up * 0.5f;
                    _moveIndicator.SetActive(true);
                    StartCoroutine(HideMoveIndicator(0.5f));
                }

                // NavMeshAgent로 이동 경로 설정
                if (_navAgent != null)
                {
                    _navAgent.SetDestination(hit.point);
                }
            }
        }
    }
    
    private void UpdateMovementAnimation()
    {
        if(!IsOwner) return;
        
        if (_navAgent == null || _animator == null) return;
    
        // NavMesh Agent의 속도 확인
        float speed = _navAgent.velocity.magnitude;
        bool isRunning = speed > 0.1f;
    
        // 애니메이션 파라미터 설정
        _animator.SetBool("IsRunning", isRunning);
    }

    private IEnumerator HideMoveIndicator(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_moveIndicator != null)
        {
            _moveIndicator.SetActive(false);
        }
    }
    
    private void HandleNormalAttack()
    {
        if (Input.GetMouseButtonDown(0)) // 좌클릭
        {
            
            // 이동 중인지 확인
            bool isMoving = _navAgent.velocity.magnitude > 0.1f;
            
            // 콤보 윈도우 확인
            if (Time.time - _lastAttackTime <= _comboWindow && _currentCombo < _maxCombo)
            {
                _currentCombo++;
            }
            else
            {
                _currentCombo = 1; // 콤보 초기화
            }
            
            _lastAttackTime = Time.time;
            
            // 이동 중이면 대시공격, 아니면 일반공격
            if (isMoving)
            {
                PerformDashAttack();
            }
            else
            {
                PerformNormalAttack();
            }
        }
    }
    
    private void PerformDashAttack()
    {
        // 로컬에서 즉시 대시공격 애니메이션 재생
        if (_animator != null)
        {
            // 콤보 상관없이 항상 DashAttack1 사용
            _animator.SetTrigger("DashAttack1");
        }
        
        // 로컬 이펙트 즉시 실행
        SpawnEffect(_dashAttackEffect, true);
    
        // 서버에 대시공격 요청
        PerformDashAttackServerRpc(_currentCombo);
    }
    
    private void PerformNormalAttack()
    {
        // 로컬에서 즉시 애니메이션 재생
        if (_animator != null)
        {
            string comboTrigger = $"NormalAttack{_currentCombo}";
            _animator.SetTrigger(comboTrigger);
            
            // 현재 콤보 번호 설정
            _animator.SetInteger("ComboCount", _currentCombo);
        }
        
        // 로컬 이펙트 즉시 실행
        if (_normalAttackEffects != null && _currentCombo > 0 && _currentCombo <= _normalAttackEffects.Length)
        {
            SpawnEffect(_normalAttackEffects[_currentCombo - 1], false, _currentCombo);
        }
        
        // 서버에 공격 요청
        PerformNormalAttackServerRpc(_currentCombo);
    }
    
    private IEnumerator ResetAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isAttacking.Value = false;
    }
    
    private void CheckComboReset()
    {
        // 콤보 타임아웃
        if (Time.time - _lastAttackTime > _comboWindow && _currentCombo > 0)
        {
            ResetCombo();
        }
    }
    
    private void ResetCombo()
    {
        // 콤보 종료 애니메이션 트리거
        if (_animator != null)
        {
            _animator.SetTrigger("ComboReset");
        }
        
        _currentCombo = 0;
        Debug.Log("콤보 리셋");
    }
    
    [ServerRpc]
    private void PerformDashAttackServerRpc(int comboIndex)
    {
        // 대시공격도 일반공격과 동일한 범위 체크 로직 사용
        PerformAttackLogic(comboIndex, isDashAttack: true);
        
        // 모든 클라이언트에 이펙트 표시 명령
        SpawnAttackEffectClientRpc(comboIndex, true);
    }
    
    [ServerRpc]
    private void PerformNormalAttackServerRpc(int comboIndex)
    {
        PerformAttackLogic(comboIndex, isDashAttack: false);
        
        // 모든 클라이언트에 이펙트 표시 명령
        SpawnAttackEffectClientRpc(comboIndex, false);
    }
    
    private void PerformAttackLogic(int comboIndex, bool isDashAttack)
    {
        // 공격 애니메이션 상태 설정
        _isAttacking.Value = true;
        
        // 전방 부채꼴 범위 체크 (기존 로직과 동일)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _comboDistance);
        
        foreach (var collider in hitColliders)
        {
            Vector3 direction = collider.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, direction);
            
            if (angle < 45f)
            {
                PlayerController targetPlayer = collider.GetComponent<PlayerController>();
                if (targetPlayer != null && targetPlayer != this)
                {
                    targetPlayer.TakeDamageServerRpc(_comboDamage);
                }
            }
        }
        
        StartCoroutine(ResetAttackState(0.3f));
    }
    
    [ClientRpc]
    private void SpawnAttackEffectClientRpc(int comboIndex, bool isDashAttack)
    {
        // 로컬 클라이언트는 이미 이펙트를 실행했으므로 건너뛴다
        if (IsOwner) return;
        
        if (isDashAttack)
        {
            SpawnEffect(_dashAttackEffect, true);
        }
        else
        {
            if (_normalAttackEffects != null && comboIndex > 0 && comboIndex <= _normalAttackEffects.Length)
            {
                SpawnEffect(_normalAttackEffects[comboIndex - 1], false, comboIndex);
            }
        }
    }
    
    private void SpawnEffect(GameObject effectPrefab, bool isDashAttack = false, int comboIndex = 0)
    {
        if (effectPrefab == null) return;
        
        Vector3 spawnPosition = _attackEffectOrigin != null ? 
            _attackEffectOrigin.position : 
            transform.position + transform.forward * 0.5f + Vector3.up;
        
        // 회전 계산
        Quaternion effectRotation;
        if (_useEffectCustomRotation)
        {
            if (isDashAttack)
            {
                // 대시 공격 회전
                effectRotation = transform.rotation * Quaternion.Euler(_dashAttackRotationOffset);
            }
            else if (_normalAttackRotationOffsets != null && comboIndex > 0 && comboIndex <= _normalAttackRotationOffsets.Length)
            {
                // 콤보별 회전
                effectRotation = transform.rotation * Quaternion.Euler(_normalAttackRotationOffsets[comboIndex - 1]);
            }
            else
            {
                // 기본 회전
                effectRotation = transform.rotation;
            }
        }
        else
        {
            // 기본 회전 사용
            effectRotation = transform.rotation;
        }
            
        GameObject effect = Instantiate(effectPrefab, spawnPosition, effectRotation);
        
        // 이펙트 자동 제거
        Destroy(effect, 2f);
    }
    
    
    private void HandleSkillInput()
    {
        // Q, W, E, R 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Q) && _skillCooldowns[0] <= 0)
        {
            UseSkill(0);
        }
        else if (Input.GetKeyDown(KeyCode.W) && _skillCooldowns[1] <= 0)
        {
            UseSkill(1);
        }
        else if (Input.GetKeyDown(KeyCode.E) && _skillCooldowns[2] <= 0)
        {
            UseSkill(2);
        }
        else if (Input.GetKeyDown(KeyCode.R) && _skillCooldowns[3] <= 0)
        {
            UseSkill(3);
        }
    }
    
    private void UseSkill(int index)
    {
        if (index < 0 || index >= _skills.Length || _skills[index] == null)
            return;
            
        Skill skill = _skills[index];
        
        // 스킬이 타겟팅이 필요한 경우
        if (skill.isTargeted)
        {
            Debug.Log($"타겟팅 스킬 준비: {skill.name}");
        }
        else
        {
            // 타겟팅이 필요 없는 스킬은 즉시 사용
            UseSkillAtPosition(index, transform.position + transform.forward * skill.range);
        }
    }
    
    private void UseSkillAtPosition(int index, Vector3 targetPosition)
    {
        if (index < 0 || index >= _skills.Length || _skills[index] == null)
            return;
            
        // 로컬에서 즉시 애니메이션 재생
        if (_animator != null && !string.IsNullOrEmpty(_skills[index].animationTrigger))
        {
            _animator.SetTrigger(_skills[index].animationTrigger);
        }

        // 스킬 쿨다운 시작
        _skillCooldowns[index] = _skills[index].cooldown;
        
        // UI 업데이트 - GamePlayManager를 통해 스킬 쿨다운 시작
        GamePlayManager.Instance.StartSkillCooldownFromServer(index, (int)_skills[index].cooldown);
        
        // 서버에 스킬 사용 요청
        UseSkillServerRpc(index, targetPosition);
    }
    
    private void UpdateSkillCooldowns()
    {
        for (int i = 0; i < _skillCooldowns.Length; i++)
        {
            if (_skillCooldowns[i] > 0)
            {
                _skillCooldowns[i] -= Time.deltaTime;
            }
        }
    }
    
    private void UpdateMovementState()
    {
        if (_navAgent != null && IsOwner)
        {
            bool isCurrentlyMoving = _navAgent.velocity.magnitude > 0.1f;
            
            // 이동 상태가 변경된 경우에만 서버에 알림
            if (isCurrentlyMoving != _isMoving.Value)
            {
                SetMovingStateServerRpc(isCurrentlyMoving);
            }
        }
    }
    
    // 위치 동기화 처리
    private void UpdatePositionSynchronization()
    {
        if(!IsOwner) return;
        
        // 현재 시간이 마지막 업데이트 시간 + 업데이트 간격보다 크면 업데이트
        if (Time.time - _lastPositionUpdateTime >= _positionUpdateInterval)
        {
            // 현재 위치와 회전
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;
            
            // 위치나 회전이 임계값 이상 변경되었는지 확인
            bool positionChanged = Vector3.Distance(_lastSentPosition, currentPosition) >= _positionThreshold;
            bool rotationChanged = Quaternion.Angle(_lastSentRotation, currentRotation) >= _rotationThreshold;
            
            // 변경되었으면 업데이트 
            if (positionChanged || rotationChanged)
            {
                // 서버에 업데이트 요청
                SyncTransformServerRpc(currentPosition, currentRotation, _navAgent.velocity);
                
                // 마지막 전송 값 업데이트
                _lastSentPosition = currentPosition;
                _lastSentRotation = currentRotation;
                _lastPositionUpdateTime = Time.time;
            }
        }
    }
    
    [ServerRpc]
    private void SyncTransformServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        // 추가 유효성 검사 (필요 시)
        // 예: 속도 제한, 위치 제한 등
        
        // 1. 서버에서 트랜스폼 업데이트
        transform.position = position;
        transform.rotation = rotation;
        
        // 2. 변경 사항을 다른 모든 클라이언트에게 전달
        SyncTransformClientRpc(position, rotation, velocity);
    }
    
    [ClientRpc]
    private void SyncTransformClientRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        // 소유자는 이미 자신의 위치를 업데이트했으므로 무시
        if (IsOwner) return;
        
        // 다른 클라이언트에서 트랜스폼 업데이트
        transform.position = position;
        transform.rotation = rotation;
        
        //부드러운 움직임을 위한 보간 로직 추가
        StartCoroutine(SmoothMovement(position, rotation, velocity));
    }
    
    //부드러운 움직임을 위한 코루틴
    private IEnumerator SmoothMovement(Vector3 targetPosition, Quaternion targetRotation, Vector3 velocity)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float startTime = Time.time;
        
        // 거리에 따라 이동 시간 계산 (속도를 사용하여 예측)
        float moveTime = Mathf.Max(0.1f, journeyLength / velocity.magnitude);
        
        while (Time.time - startTime < moveTime)
        {
            float journeyFraction = (Time.time - startTime) / moveTime;
            transform.position = Vector3.Lerp(startPosition, targetPosition, journeyFraction);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, journeyFraction);
            yield return null;
        }
        
        // 최종 위치로 설정
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
    
    [ServerRpc]
    private void SetMovingStateServerRpc(bool isMoving)
    {
        _isMoving.Value = isMoving;
    }
    
    [ServerRpc]
    private void UseSkillServerRpc(int skillIndex, Vector3 targetPosition)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Length || _skills[skillIndex] == null)
            return;
            
        // 스킬 사용 상태 설정
        _currentSkillIndex.Value = skillIndex;
        
        Skill skill = _skills[skillIndex];
        
        // 타겟 방향으로 회전
        Vector3 direction = targetPosition - transform.position;
        if (direction.magnitude > 0.1f)
        {
            direction.y = 0; // y축 회전 제거
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // 스킬 이펙트 생성
        if (skill.effectPrefab != null)
        {
            // 스킬 발사 위치 결정
            Vector3 spawnPosition = _skillOrigin != null ? _skillOrigin.position : transform.position + Vector3.up;
            
            // 이펙트 생성 및 방향 설정
            GameObject effectObj = Instantiate(skill.effectPrefab, spawnPosition, transform.rotation);
            
            // 범위형 스킬인 경우
            if (skill.aoeRadius > 0)
            {
                // 타겟 위치에 생성
                effectObj.transform.position = targetPosition;
                
                // 범위 내 적 타겟 탐색 및 데미지 적용
                ApplyAoeDamage(targetPosition, skill.aoeRadius, skill.damage);
            }
            else
            {
                // 직선형 스킬
                RaycastHit hit;
                if (Physics.Raycast(spawnPosition, direction.normalized, out hit, skill.range))
                {
                    PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
                    if (targetPlayer != null && targetPlayer != this)
                    {
                        targetPlayer.TakeDamageServerRpc(skill.damage);
                    }
                }
            }
            
            // 네트워크에 이펙트 스폰
            NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn();
                StartCoroutine(DestroyEffectAfterDelay(networkObj, 5f));
            }
            else
            {
                Destroy(effectObj, 5f);
            }
        }
        
        // 일정 시간 후 스킬 상태 초기화
        StartCoroutine(ResetSkillState(0.5f));
    }
    
    private IEnumerator DestroyEffectAfterDelay(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && obj.IsSpawned)
        {
            obj.Despawn();
        }
    }
    
    private void ApplyAoeDamage(Vector3 center, float radius, float damage)
    {
        // 범위 내 모든 콜라이더 탐색
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        
        foreach (var hitCollider in hitColliders)
        {
            // Player만 데미지 받도록 처리
            PlayerController targetPlayer = hitCollider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this)
            {
                targetPlayer.TakeDamageServerRpc(damage);
            }
        }
    }
    
    private IEnumerator ResetSkillState(float delay)
    {
        yield return new WaitForSeconds(delay);
        _currentSkillIndex.Value = -1;
    }
    
    // 이동 상태 변경 콜백
    private void OnMovingChanged(bool oldValue, bool newValue)
    {
        // 애니메이션 상태 업데이트
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", newValue);
        }
    }
    
    // 스킬 인덱스 변경 콜백
    private void OnSkillIndexChanged(int oldValue, int newValue)
    {
        // 소유자가 아닌 경우에만 애니메이션 실행 (리모트 플레이어용)
        if (!IsOwner && newValue >= 0 && newValue < _skills.Length && _skills[newValue] != null)
        {
            // 스킬 애니메이션 재생
            if (_animator != null && !string.IsNullOrEmpty(_skills[newValue].animationTrigger))
            {
                _animator.SetTrigger(_skills[newValue].animationTrigger);
            }
        }
    }
    
    private void HandleLocalDeath()
    {
        // 서버에 죽음 알림
        NotifyDeathServerRpc();
    }
    
    [ServerRpc]
    private void NotifyDeathServerRpc()
    {
        // 서버에서 죽음 상태 설정
        _isDead.Value = true;
    }
    
    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (newValue) // 죽었을 때
        {
            // 죽음 애니메이션 재생
            if (_animator != null)
            {
                _animator.SetTrigger("Death");
            }
            
            // 네비게이션 비활성화
            if (_navAgent != null)
            {
                _navAgent.enabled = false;
            }
            
            // 입력 비활성화 (로컬 플레이어인 경우)
            if (IsOwner)
            {
                // 입력 처리를 막는 플래그를 추가할 수 있음
                // 예: enabled = false;
            }
            
            Debug.Log($"플레이어 {OwnerClientId} 사망");
        }
    }
    
    // 데미지 받는 메서드 추가
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        if (_isDead.Value) return; // 이미 죽었다면 리턴
        
        if (_characterStats != null)
        {
            _characterStats.TakeDamage(damage);
        }
    }
    
    private void OnDestroy()
    {
        if (_characterStats != null)
        {
            _characterStats.OnDeath -= HandleLocalDeath;
        }
    }
}