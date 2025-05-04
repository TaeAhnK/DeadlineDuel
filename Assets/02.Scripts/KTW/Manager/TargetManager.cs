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
        // �̱��� ���� ����
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
    /// ������ �Ҵ�
    /// </summary>
    public void SetBossDataOnManager(ulong userId, NetworkObject b) {
        bossObjectDict[userId] = b;
    }

    /// <summary>
    /// �ܺο��� �� Ŭ���̾�Ʈ�� ���� �޾ư���
    /// </summary>
    public NetworkObject GetEnemyBoss(ulong userId) {
        return bossObjectDict[userIdDict[userId]];
    }

    public void SetPlayerDataOnManager(ulong userId, NetworkObject p) {
        playerObjectDict[userId] = p;
    }

    /// <summary>
    /// �ܺο��� �÷��̾� ������Ʈ �޾ư���
    /// </summary>
    public NetworkObject GetPlayer(ulong userId) {
        return playerObjectDict[userId];
    }

    /// <summary>
    /// �ܺο��� �� ������Ʈ �޾ư���. GetPlayer()�� �ݴ� ����
    /// </summary>
    public NetworkObject GetEnemy(ulong userId) {
        return playerObjectDict[userIdDict[userId]];
    }

    // GamePlayManager.cs�� �Ʒ��� �ڵ� �ۼ�
    /*
     
    OnClientConnected �޼��忡
    // �� ��° �÷��̾ ����Ǹ� ��ȣ ����
    if (NetworkManager.ConnectedClients.Count == 1) return;
    var clients = NetworkManager.ConnectedClients.Keys;

    if (clients
    foreach (var existingClient in clients)
    {
        if (existingClient == clientId) continue;
        
        // ��ȣ opponent ����
        _clientOpponentMap[existingClient] = clientId;
        _clientOpponentMap[clientId] = existingClient;
        
        // Ŭ���̾�Ʈ�� ID ����
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

    SpawnPlayerServerRpc()�޼��� �Ʒ��� �Ʒ� �߰�
    TargetManager.Instance.SetPlayerDataOnManager(clientId, networkObject);
     */
}
