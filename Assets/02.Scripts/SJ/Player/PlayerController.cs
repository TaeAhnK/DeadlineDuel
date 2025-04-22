using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private GameObject _cameraPrefab; // 시네머신 카메라 프리팹
    
    private QuadViewCinemachine _cameraController;
    
    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private LayerMask _groundLayer; // 지형 레이어
    [SerializeField] private GameObject _moveIndicatorPrefab; // 이동 표시자 프리팹
    
    [Header("스킬 설정")]
    [SerializeField] private Skill[] _skills = new Skill[4]; // Q, W, E, R 스킬
    [SerializeField] private Transform _skillOrigin; // 스킬 발사 위치
    
    // 컴포넌트 참조
    private Animator _animator;
    private NavMeshAgent _navAgent;
    private GameObject _moveIndicator;
    
    // 네트워크 변수
    private NetworkVariable<bool> _isMoving = new NetworkVariable<bool>(false);
    private NetworkVariable<int> _currentSkillIndex = new NetworkVariable<int>(-1);
    
    // 스킬 쿨다운 타이머
    private float[] _skillCooldowns = new float[4];
    
    // 선택된 캐릭터 정보
    private int _characterIndex;
    private Dictionary<int, float> _skillMaxCooldowns = new Dictionary<int, float>();
    private Dictionary<int, float> _skillDamages = new Dictionary<int, float>();
    
    [System.Serializable]
    public class Skill
    {
        public string name;
        public float cooldown;
        public GameObject effectPrefab;
        public float damage;
        public float range;
        public float aoeRadius;
        public bool isTargeted; // 타겟팅 스킬인지 여부
        public string animationTrigger;
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 컴포넌트 참조 가져오기
        _animator = GetComponentInChildren<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        
        // NavMeshAgent 설정
        if (_navAgent != null)
        {
            _navAgent.speed = _moveSpeed;
            _navAgent.angularSpeed = _rotationSpeed * 100;
            _navAgent.acceleration = 8f;
        }
        
        // 자신의 플레이어일 경우에만 카메라 생성
        if (IsOwner)
        {
            SetupCamera();
            CreateMoveIndicator();
        }
        
        // 네트워크 변수 콜백
        _isMoving.OnValueChanged += OnMovingChanged;
        _currentSkillIndex.OnValueChanged += OnSkillIndexChanged;
    }
    
    /// <summary>
    /// 선택된 캐릭터 데이터 로드
    /// </summary>
    private void LoadCharacterData()
    {
        // CharacterManager에서 데이터 가져오기
        CharacterManager characterManager = CharacterManager.Instance;
    
        if (characterManager != null)
        {
            // 선택된 캐릭터 인덱스
            _characterIndex = characterManager.GetSelectedCharacterIndex();
        
            // 캐릭터 스탯 데이터
            CharacterStatsData statsData = characterManager.GetCharacterStats(_characterIndex);
        
            // 스탯 설정
            CharacterStats stats = GetComponent<CharacterStats>();
            if (stats != null && statsData != null && IsServer)
            {
                stats.SetStatsData(statsData);
            }
        }
    }
    
    /// <summary>
    /// CharacterManager의 데이터로 스킬 정보 업데이트
    /// </summary>
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
    }
    
    /// <summary>
    /// 스킬 UI 초기화 (클라이언트 쪽에서만 호출)
    /// </summary>
    private void InitializeSkillUI()
    {
        // SkillUIManager 찾기
        SkillUIManager skillUIManager = FindObjectOfType<SkillUIManager>();
        
        if (skillUIManager != null)
        {
            // 스킬 정보 전달
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i] != null)
                {
                    skillUIManager.UpdateSkillCooldown(i, 0, _skillMaxCooldowns[i]);
                }
            }
        }
    }
    
    private void SetupCamera()
    {
        // 카메라 프리팹 생성
        GameObject cameraObj = Instantiate(_cameraPrefab);
        
        // 쿼드뷰 시네머신 컨트롤러 가져오기
        _cameraController = cameraObj.GetComponent<QuadViewCinemachine>();
        
        // 카메라 타겟 설정
        if (_cameraController != null)
        {
            _cameraController.SetTarget(transform);
        }
        
        // 씬 전환 시에도 카메라 유지
        DontDestroyOnLoad(cameraObj);
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
        // 자신의 플레이어만 입력 처리
        if (!IsOwner) return;
        
        // 이동 입력 처리
        HandleMovementInput();
        
        // 스킬 입력 처리
        HandleSkillInput();
        
        // 스킬 쿨다운 업데이트
        UpdateSkillCooldowns();
        
        // 이동 상태 업데이트
        UpdateMovementState();
    }
    
    private void HandleMovementInput()
    {
        // 마우스 우클릭 감지
        if (Input.GetMouseButtonDown(1))
        {
            // 지형에 레이캐스트하여 이동 위치 결정
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
            {
                // 이동 표시자 표시
                if (_moveIndicator != null)
                {
                    _moveIndicator.transform.position = hit.point + Vector3.up * 0.1f;
                    _moveIndicator.SetActive(true);
                    StartCoroutine(HideMoveIndicator(0.5f));
                }
                
                // NavMeshAgent로 이동 경로 설정
                if (_navAgent != null)
                {
                    _navAgent.SetDestination(hit.point);
                    
                    // 서버에 이동 요청
                    SetMoveTargetServerRpc(hit.point);
                }
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
            // 마우스 위치에 타겟팅 표시자 표시 (여기서는 구현하지 않음)
            // 예: StartCoroutine(ShowTargetingIndicator(index));
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
        // 스킬 쿨다운 시작
        _skillCooldowns[index] = _skills[index].cooldown;
        
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
    
    [ServerRpc]
    private void SetMoveTargetServerRpc(Vector3 position)
    {
        // 서버에서 NavMeshAgent 설정
        if (_navAgent != null)
        {
            _navAgent.SetDestination(position);
        }
    }
    
    [ServerRpc]
    private void SetMovingStateServerRpc(bool isMoving)
    {
        _isMoving.Value = isMoving;
    }
    
    [ServerRpc]
    private void UseSkillServerRpc(int skillIndex, Vector3 targetPosition)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Length)
            return;
            
        // 스킬 사용 상태 설정
        _currentSkillIndex.Value = skillIndex;
        
        // 스킬 이펙트 및 데미지 적용
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
                // 직선형 스킬인 경우, 발사체 방향 설정 등 추가 로직 필요
                // 여기서는 간단하게 구현
                
                // 레이캐스트로 타겟 탐색
                RaycastHit hit;
                if (Physics.Raycast(spawnPosition, direction.normalized, out hit, skill.range))
                {
                    // 타겟에 데미지 적용
                    DummyTarget target = hit.collider.GetComponent<DummyTarget>();
                    if (target != null)
                    {
                        target.TakeDamageServerRpc(skill.damage);
                    }
                }
            }
            
            // 일정 시간 후 이펙트 제거
            NetworkObject networkObj = effectObj.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn(); // 네트워크에 스폰
                
                // 5초 후 제거
                StartCoroutine(DestroyEffectAfterDelay(networkObj, 5f));
            }
            else
            {
                // 네트워크 오브젝트가 아닌 경우 일반 Destroy 사용
                Destroy(effectObj, 5f);
            }
        }
        
        // 일정 시간 후 스킬 상태 초기화
        StartCoroutine(ResetSkillState(0.5f));
    }
    
    private IEnumerator DestroyEffectAfterDelay(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
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
            // 더미 타겟 또는 적 플레이어 확인
            DummyTarget target = hitCollider.GetComponent<DummyTarget>();
            if (target != null)
            {
                target.TakeDamageServerRpc(damage);
            }
            
            // 추후 플레이어 데미지 로직 추가 가능
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
        if (newValue >= 0 && newValue < _skills.Length)
        {
            // 스킬 애니메이션 재생
            if (_animator != null && !string.IsNullOrEmpty(_skills[newValue].animationTrigger))
            {
                _animator.SetTrigger(_skills[newValue].animationTrigger);
            }
        }
    }
    
    // 디버깅용 - 스킬 범위 시각화
    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < _skills.Length; i++)
        {
            if (_skills[i] != null)
            {
                if (_skills[i].aoeRadius > 0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position + transform.forward * _skills[i].range, _skills[i].aoeRadius);
                }
                else
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(transform.position, transform.forward * _skills[i].range);
                }
            }
        }
    }
}