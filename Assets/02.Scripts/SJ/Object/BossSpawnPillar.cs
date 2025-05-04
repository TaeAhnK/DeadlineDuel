using Unity.Netcode;
using UnityEngine;

public class BossSpawnPillar : NetworkBehaviour
{
    [SerializeField] private int pillarIndex;
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Material activatedMaterial;
    [SerializeField] private Renderer pillarRenderer;
    [SerializeField] private float interactionDistance = 3f; // 상호작용 가능 거리
    
    private NetworkVariable<bool> isActivated = new NetworkVariable<bool>(false);
    private bool playerInRange = false;
    private GameObject nearestPlayerObject;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 활성화 상태 변경 시 콜백 등록
        isActivated.OnValueChanged += OnActivationChanged;
        UpdatePillarAppearance(isActivated.Value);
        
        // GamePlayManager에 이벤트 등록 - OnBossSpawned 이벤트 추가 필요
        if (GamePlayManager.Instance != null)
        {
            // 보스 스폰 이벤트 등록 (GamePlayManager에 이 이벤트 추가 필요)
            GamePlayManager.Instance.OnBossSpawned += OnBossSpawned;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        isActivated.OnValueChanged -= OnActivationChanged;
        
        // 이벤트 해제
        if (GamePlayManager.Instance != null)
        {
            GamePlayManager.Instance.OnBossSpawned -= OnBossSpawned;
        }
        
        base.OnNetworkDespawn();
    }
    
    private void Update()
    {
        // 현재 접속한 로컬 플레이어 기준으로 처리
        if (!IsClient) 
            return;
        
        // 플레이어와의 거리 체크
        CheckNearbyPlayers();
        
        // G 키 입력 감지 및 처리
        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        {
            // 아직 활성화되지 않은 경우에만 상호작용
            if (!isActivated.Value)
            {
                InteractWithPillar();
            }
        }
    }
    
    private void CheckNearbyPlayers()
    {
        // 로컬 플레이어 찾기
        PlayerController localPlayer = null;
        
        // 모든 플레이어 컨트롤러 검색
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            // IsOwner 또는 IsLocalPlayer로 로컬 플레이어 찾기
            if (player.IsOwner)
            {
                localPlayer = player;
                break;
            }
        }
        
        if (localPlayer != null)
        {
            // 로컬 플레이어와 기둥 사이의 거리 계산
            float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
            
            // 상호작용 범위 내에 있는지 확인
            playerInRange = (distance <= interactionDistance);
            
            if (playerInRange)
            {
                nearestPlayerObject = localPlayer.gameObject;
                
                // 상호작용 가능 UI 힌트 표시 (별도 구현 필요)
                // ShowInteractionHint();
            }
            else
            {
                nearestPlayerObject = null;
                
                // 상호작용 불가 UI 힌트 숨기기 (별도 구현 필요)
                // HideInteractionHint();
            }
        }
    }
    
    private void InteractWithPillar()
    {
        // 플레이어가 범위 내에 있는지 확인
        if (!playerInRange || nearestPlayerObject == null || isActivated.Value)
            return;
        
        // 플레이어의 Object_Base 컴포넌트 가져오기
        Object_Base playerBase = nearestPlayerObject.GetComponent<Object_Base>();
        
        if (playerBase != null)
        {
            // 플레이어 ID 가져오기
            string playerId = playerBase.GetPlayerId();
            
            if (!string.IsNullOrEmpty(playerId))
            {
                // 서버에 기둥 활성화 요청
                ActivatePillarServerRpc(playerId);
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ActivatePillarServerRpc(string playerId)
    {
        if (isActivated.Value) return;
        
        Debug.Log($"플레이어 {playerId}가 기둥 {pillarIndex}를 활성화했습니다");
        
        // 기둥을 활성화 상태로 설정
        isActivated.Value = true;
        
        // 기둥 상호작용을 게임 매니저에 알림
        GamePlayManager.Instance.PillarInteractionServerRpc(playerId);
    }
    
    private void OnActivationChanged(bool previousValue, bool newValue)
    {
        // 기둥 외관 업데이트
        UpdatePillarAppearance(newValue);
    }
    
    private void UpdatePillarAppearance(bool activated)
    {
        if (pillarRenderer != null)
        {
            // 활성화 상태에 따라 재질 변경
            pillarRenderer.material = activated ? activatedMaterial : inactiveMaterial;
        }
    }
    
    // 보스 스폰 이벤트 콜백
    private void OnBossSpawned()
    {
        // 서버에서만 처리
        if (IsServer)
        {
            // 기둥 오브젝트 제거
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
        }
    }
    
    // 에디터에서 상호작용 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}