using Unity.Netcode;
using UnityEngine;

namespace Boss
{
    public class BossCharacter : NetworkBehaviour
    {
        [field: SerializeField] public NetworkVariable<ulong> AssignedPlayerId = new();
        [field: SerializeField] private Transform targetPlayer;
        [field: SerializeField] public TransparencyController transparencyController;
        
        public bool IsClientBoss => AssignedPlayerId.Value == NetworkManager.Singleton.LocalClientId;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[Boss] Boss {gameObject.name} AssignedPlayerId: {AssignedPlayerId.Value}, LocalClientId: {NetworkManager.Singleton.LocalClientId}");
            
            if (AssignedPlayerId.Value == NetworkManager.Singleton.LocalClientId)
            {
                transparencyController.SetToOpaque();
            }
            else
            {
                transparencyController.SetToTransparent();
            }
        }

        // From Here Need Fix
        [ServerRpc]
        public void AssignPlayerServerRpc(ulong clientId)
        {
            AssignedPlayerId.Value = clientId; // 네트워크 변수 직접 설정
            FindPlayerTransformByClientId(clientId);
        }
        
        private void FindPlayerTransformByClientId(ulong clientId)
        {
            if (IsServer)
            {
                foreach (NetworkObject netObj in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
                {
                    // ✅ OwnerClientId + 태그 + IsPlayerObject 3중 검증
                    if (netObj.OwnerClientId == clientId && 
                        netObj.CompareTag("Player") &&
                        netObj.IsPlayerObject)
                    {
                        targetPlayer = netObj.transform;
                        Debug.Log($"[Boss] {clientId} 플레이어 연결 성공: {targetPlayer.name}");
                        return;
                    }
                }
                Debug.LogError($"[Boss] {clientId} 플레이어 찾기 실패!");
            }
            else if (IsClient)
            {
                foreach (NetworkObject netObj in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
                {
                    // ✅ OwnerClientId + 태그 + IsPlayerObject 3중 검증
                    if (netObj.OwnerClientId == clientId && 
                        netObj.CompareTag("Player") &&
                        netObj.IsPlayerObject)
                    {
                        targetPlayer = netObj.transform;
                        Debug.Log($"[Boss] {clientId} 플레이어 연결 성공: {targetPlayer.name}");
                        return;
                    }
                }
                Debug.LogError($"[Boss] {clientId} 플레이어 찾기 실패!");
            }
        }
        
        public bool GetTargetPlayer(out Transform targetPlayer)
        {
            if (this.targetPlayer == null)
            {
                FindPlayerTransformByClientId(AssignedPlayerId.Value);
            }
            
            targetPlayer = this.targetPlayer;
            return targetPlayer != null;
        }
    }
}