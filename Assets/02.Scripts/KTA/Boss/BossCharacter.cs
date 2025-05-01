using Unity.Netcode;
using UnityEngine;

namespace Boss
{
    public class BossCharacter : NetworkBehaviour
    {
        /// <summary>
        ///  This Class is for Network Init
        ///  You need to Set Target Player Here
        ///  This code is yet test code
        /// </summary>
        [field: SerializeField] public NetworkVariable<ulong> AssignedPlayerId = new();
        [field: SerializeField] private Transform targetPlayer;
        
        [field: SerializeField] private Material OpaqueMaterial;
        [field: SerializeField] private Material TransparentMaterial;

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