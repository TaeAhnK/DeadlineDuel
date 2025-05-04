using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  ��� ����ü�� ��ӽ�ų ��ũ��Ʈ, ���� �� ���� ����
/// </summary>
public class Object_Base : MonoBehaviour
{
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float baseAttackSpeed = 1f;
    public float baseMoveSpeed = 5f;
    public float baseCoolTime = 1.0f;

    [Header("Current Stats (Read Only At Editor)")]
    [SerializeField] private float currentAttack;
    [SerializeField] private float currentDefense;
    [SerializeField] private float currentAttackSpeed;
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private float currentCoolTime;

    private List<BuffDebuff> activeBuffs = new List<BuffDebuff>();  // �� ������Ʈ�� Ȱ��ȭ�� ����/����� ����Ʈ

    // ���� ���� ��� (���� + ���� ȿ��)
    public float Attack => GetFinalStat(baseAttack, BuffTypeEnum.Attack);
    public float Defense => GetFinalStat(baseDefense, BuffTypeEnum.Defense);
    public float AttackSpeed => GetFinalStat(baseAttackSpeed, BuffTypeEnum.AttackSpeed);
    public float MoveSpeed => GetFinalStat(baseMoveSpeed, BuffTypeEnum.MoveSpeed);
    public float CoolTime => GetFinalStat(baseCoolTime, BuffTypeEnum.Cooltime);

    private void Update() {
    #if UNITY_EDITOR
        currentAttack = Attack;
        currentDefense = Defense;
        currentAttackSpeed = AttackSpeed;
        currentMoveSpeed = MoveSpeed;
        currentCoolTime = CoolTime;
    #endif
    }

    /// <summary>
    /// BuffDebuff.cs���� ȣ���Ͽ� Object_Base�� ���� �߰�
    /// </summary>
    public void ApplyBuffDebuff(BuffDebuff buff) {
        activeBuffs.Add(buff);
    }

    /// <summary>
    /// BuffDebuff.cs���� ȣ���Ͽ� Object_Base�� �ɷ��ִ� ���� ����
    /// </summary>
    public void RemoveBuffDebuff(BuffDebuff buff) {
        if (activeBuffs.Contains(buff)) {
            activeBuffs.Remove(buff);
        }
    }

    /// <summary>
    /// ���ݷ� �������� ���ҵ������ ���ÿ� �����Ͽ��� ����ǵ��� Modifier ��ġ
    /// </summary>
    private float GetFinalStat(float baseValue, BuffTypeEnum targetType) {
        float totalModifier = 0f;
        foreach (BuffDebuff buff in activeBuffs) {
            if (buff.buffType == targetType) {
                totalModifier += buff.value;
            }
        }
        return Mathf.Max(0, baseValue + totalModifier); // ���� ����
    }


    public virtual void UpdateHP(float damage) {

    }
}
