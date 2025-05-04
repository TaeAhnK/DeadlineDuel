using Stats.Boss;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
///  ��� ����ü�� ��ӽ�ų ��ũ��Ʈ, ���� �� ���� ����. �ϴ� �ڼ��Ѱ� ���߿�
/// </summary>
public class BuffableEntity : NetworkBehaviour
{
    [Header("���� ������Ʈ")]
    [SerializeField] private CharacterStats _characterStats; // ĳ���� ���� ����
    [SerializeField] private Stats.Boss.BossStats _bossStats; // ���� ���� ����


    public float currentHp = 100.0f;
    public float maxHp = 100.0f;
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float baseAttackSpeed = 1f;
    public float baseMoveSpeed = 5f;
    public float baseCoolTime = 1.0f;

    [Header("Current Stats (Read Only At Editor)")]
    [SerializeField] private float eAttack;
    [SerializeField] private float eDefense;
    [SerializeField] private float eAttackSpeed;
    [SerializeField] private float eMoveSpeed;
    [SerializeField] private float eCoolTime;

    private NetworkList<NetworkBuff> _activeBuffs;  // �� ������Ʈ�� Ȱ��ȭ�� ����/����� ����Ʈ 

    // ���� ���� ��� (���� + ���� ȿ��)
    public float HP => currentHp;
    public float Attack => GetFinalStat(baseAttack, BuffTypeEnum.Attack);
    public float Defense => GetFinalStat(baseDefense, BuffTypeEnum.Defense);
    public float AttackSpeed => GetFinalStat(baseAttackSpeed, BuffTypeEnum.AttackSpeed);
    public float MoveSpeed => GetFinalStat(baseMoveSpeed, BuffTypeEnum.MoveSpeed);
    public float CoolTime => GetFinalStat(baseCoolTime, BuffTypeEnum.Cooltime);

    private void Awake() {
        _activeBuffs = new();

        if (!_characterStats) _characterStats = GetComponent<CharacterStats>();
        if (!_bossStats) _bossStats = GetComponent<BossStats>();
    }

    // ����Ƽ �����Ϳ����� �� Ȯ���� �� �ֵ��� �߰�
    private void Update() {
#if UNITY_EDITOR
        // UpdateDebugValues();
#endif
    }

    private void UpdateDebugValues() {
        eAttack = Attack;
        eDefense = Defense;
        eAttackSpeed = AttackSpeed;
        eMoveSpeed = MoveSpeed;
        eCoolTime = CoolTime;
    }

    /// <summary>
    /// BuffDebuff.cs���� ȣ���Ͽ� ���� �߰�
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyBuffDebuffServerRpc(BuffTypeEnum buffType, float value) {
        if (!IsServer) return;

        // �ߺ������� TotemManager���� ó���Ͽ����Ƿ� ���⼭�� ����

        _activeBuffs.Add(new NetworkBuff {
            buffType = buffType,
            value = value
        });
    }

    /// <summary>
    /// BuffDebuff.cs���� ȣ���Ͽ� �ɷ��ִ� ���� ����
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RemoveBuffDebuffServerRpc(BuffTypeEnum buffType, float value) { 
        if (!IsServer) return;

        for (int i = _activeBuffs.Count - 1; i >= 0; i--) {
            if (_activeBuffs[i].buffType == buffType && Mathf.Approximately(_activeBuffs[i].value, value)) {
                _activeBuffs.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// ���ݷ� �������� ���ҵ������ ���ÿ� �����Ͽ��� ����ǵ��� Modifier ��ġ
    /// </summary>
    private float GetFinalStat(float baseValue, BuffTypeEnum targetType) {
        float totalModifier = 0f;
        //foreach (BuffDebuff buff in activeBuffs) {
        //    if (buff.buffType == targetType) {
        //        totalModifier += buff.value;
        //    }
        //}
        //return Mathf.Max(0, baseValue + totalModifier); // ���� ����
        return 1.0f;
    }


    public virtual void UpdateHP(float damage) {

    }
}
