using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TargetManager : NetworkBehaviour
{
    private static TargetManager _instance;
    public static TargetManager Instance => _instance;

    [Header("Object")]
    [SerializeField] private BuffableEntity boss1;
    [SerializeField] private BuffableEntity boss2;
    [SerializeField] private Object_Base player1;
    [SerializeField] private Object_Base player2;

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
    }

    /// <summary>
    /// 보스를 할당
    /// </summary>
    public void SetBossDataOnManager(NetworkObject b, ulong userId) {
        bossObjectDict[userId] = b;
    }

    /// <summary>
    /// 외부에서 적 클라이언트의 보스 받아가기
    /// </summary>
    public NetworkObject GetEnemyBoss(ulong userId) {
        return bossObjectDict[userIdDict[userId]];
    }

    public void SetPlayerDataOnManager(NetworkObject p, ulong userId) {
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
}
