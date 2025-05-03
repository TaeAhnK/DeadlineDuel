using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TotemManager : MonoBehaviour
{
    private static TotemManager _instance;
    public static TotemManager Instance => _instance;

    [SerializeField] private UI_BuffDebuff ui_buffDebuff;

    [SerializeField] private GameObject totemPrefab;
    [SerializeField] private List<BuffTotem> spawnedTotems = new List<BuffTotem>();

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointsParent; // SpawnPoints 오브젝트
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private List<int> player1AvailableSpawnPoints = new List<int>();
    [SerializeField] private List<int> player2AvailableSpawnPoints = new List<int>();
    [SerializeField] private Transform totemsParent;

    [SerializeField] private GameObject[] vbxPrefab; // 이펙트

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

        InitializeSpawnPoints();
    }

    private void InitializeSpawnPoints() {
        foreach (Transform child in spawnPointsParent)
            spawnPoints.Add(child);

        // Player1: 0,2,4 / Player2: 1,3,5
        for (int i = 0; i < spawnPoints.Count; i++) {
            if (i % 2 == 0) player1AvailableSpawnPoints.Add(i);
            else player2AvailableSpawnPoints.Add(i);
        }

        if (totemsParent == null) totemsParent = this.transform;
    }

    /// <summary>
    /// 토템 UI에게 아이템정보를 받아 토템을 소환
    /// </summary>
    public void SummonTotem(BuffDebuffItem item) {
        // 플레이어 식별 (호스트=Player1, 클라이언트=Player2)
        // TODO 호스트인지 판별하려면 멀티로 실행해야?
        //bool isPlayer1 = NetworkManager.Singleton.IsHost;
        bool isPlayer1 = true;

        List<int> availableSpawnPoints = isPlayer1 ? player1AvailableSpawnPoints : player2AvailableSpawnPoints;

        if (availableSpawnPoints.Count == 0) {
            Debug.Log("TotemManager.cs | SummonTotem 스폰할 수 있는 포인트가 없음");
            return;
        }

        // 랜덤 스폰 포인트 선택
        int randomIdx = Random.Range(0, availableSpawnPoints.Count);
        int spawnIndex = availableSpawnPoints[randomIdx];
        availableSpawnPoints.RemoveAt(randomIdx);

        // 토템 생성
        GameObject newTotem = Instantiate(
            totemPrefab,
            spawnPoints[spawnIndex].position,
            Quaternion.identity,
            totemsParent
        );
        BuffTotem totem = newTotem.GetComponent<BuffTotem>();

        // 토템 데이터 설정
        totem.buffName = item.buffName;
        totem.targetType = item.target;
        totem.buffType = item.type;
        totem.buffValue = item.value;
        totem.spawnIndex = spawnIndex;

        GameObject vbxObj = Instantiate(
            vbxPrefab[0],
            totem.transform // 부모를 토템으로 지정
        );

        spawnedTotems.Add(totem);
    }


    public void RemoveTotem(BuffTotem totem) {
        if (spawnedTotems.Contains(totem)) {
            int index = totem.spawnIndex;
            if (index % 2 == 0) player1AvailableSpawnPoints.Add(index);
            else player2AvailableSpawnPoints.Add(index);

            spawnedTotems.Remove(totem);
        }
    }
}
