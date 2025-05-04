using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TargetManager : NetworkBehaviour
{
    private static TargetManager _instance;
    public static TargetManager Instance => _instance;

    [Header("Object")]
    private Dictionary<ulong, ulong> userIdDict = new();
    private Dictionary<ulong, NetworkObject> playerObjectDict = new();
    private Dictionary<ulong, NetworkObject> bossObjectDict = new();

    private void Awake() {
        // 싱글톤 패턴 구현
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }
    }

    public void SetUserId(ulong id1, ulong id2) {
        userIdDict[id1] = id2;
        userIdDict[id2] = id1;
    }

    /// <summary>
    /// 보스를 할당
    /// </summary>
    public void SetBossDataOnManager(ulong userId, NetworkObject b) {
        bossObjectDict[userId] = b;
    }

    /// <summary>
    /// 외부에서 적 클라이언트의 보스 받아가기
    /// </summary>
    public NetworkObject GetEnemyBoss(ulong userId) {
        return bossObjectDict[userIdDict[userId]];
    }

    public void SetPlayerDataOnManager(ulong userId, NetworkObject p) {
        playerObjectDict[userId] = p;
    }

    /// <summary>
    /// 외부에서 플레이어 오브젝트 받아가기
    /// </summary>
    public NetworkObject GetPlayer(ulong userId) {
        return playerObjectDict[userId];
    }

    /// <summary>
    /// 외부에서 적 오브젝트 받아가기. GetPlayer()와 반대 조건
    /// </summary>
    public NetworkObject GetEnemy(ulong userId) {
        return playerObjectDict[userIdDict[userId]];
    }

    // GamePlayManager.cs에 아래의 코드 작성
    /*
     
    OnClientConnected 메서드에
    // 두 번째 플레이어가 연결되면 상호 매핑
    if (NetworkManager.ConnectedClients.Count == 1) return;
    var clients = NetworkManager.ConnectedClients.Keys;

    if (clients
    foreach (var existingClient in clients)
    {
        if (existingClient == clientId) continue;
        
        // 상호 opponent 설정
        _clientOpponentMap[existingClient] = clientId;
        _clientOpponentMap[clientId] = existingClient;
        
        // 클라이언트에 ID 전송
        SetClientIDRpc(existingClient, clientId);
        break;
    }

    [ClientRpc]
    private void SetClientIDRpc(ulong yourId, ulong opponentId)
    {
        if (NetworkManager.Singleton.LocalClientId == yourId){
            TargetManager.Instance.SetUserId(yourId, opponentId);
        }
    }

    SpawnPlayerServerRpc()메서드 아래에 아래 추가
    TargetManager.Instance.SetPlayerDataOnManager(clientId, networkObject);
     */
}
