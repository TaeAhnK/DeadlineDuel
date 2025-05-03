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
    [SerializeField] private Transform spawnPointsParent; // SpawnPoints ������Ʈ
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private List<int> player1AvailableSpawnPoints = new List<int>();
    [SerializeField] private List<int> player2AvailableSpawnPoints = new List<int>();
    [SerializeField] private Transform totemsParent;

    [SerializeField] private GameObject[] vbxPrefab; // ����Ʈ

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
    /// ���� UI���� ������������ �޾� ������ ��ȯ
    /// </summary>
    public void SummonTotem(BuffDebuffItem item) {
        // �÷��̾� �ĺ� (ȣ��Ʈ=Player1, Ŭ���̾�Ʈ=Player2)
        // TODO ȣ��Ʈ���� �Ǻ��Ϸ��� ��Ƽ�� �����ؾ�?
        //bool isPlayer1 = NetworkManager.Singleton.IsHost;
        bool isPlayer1 = true;

        List<int> availableSpawnPoints = isPlayer1 ? player1AvailableSpawnPoints : player2AvailableSpawnPoints;

        if (availableSpawnPoints.Count == 0) {
            Debug.Log("TotemManager.cs | SummonTotem ������ �� �ִ� ����Ʈ�� ����");
            return;
        }

        // ���� ���� ����Ʈ ����
        int randomIdx = Random.Range(0, availableSpawnPoints.Count);
        int spawnIndex = availableSpawnPoints[randomIdx];
        availableSpawnPoints.RemoveAt(randomIdx);

        // ���� ����
        GameObject newTotem = Instantiate(
            totemPrefab,
            spawnPoints[spawnIndex].position,
            Quaternion.identity,
            totemsParent
        );
        BuffTotem totem = newTotem.GetComponent<BuffTotem>();

        // ���� ������ ����
        totem.buffName = item.buffName;
        totem.targetType = item.target;
        totem.buffType = item.type;
        totem.buffValue = item.value;
        totem.spawnIndex = spawnIndex;

        GameObject vbxObj = Instantiate(
            vbxPrefab[0],
            totem.transform // �θ� �������� ����
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
