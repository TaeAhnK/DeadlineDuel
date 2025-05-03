using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  모든 생명체를 상속시킬 스크립트, 스탯 등 정보 포함
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

    private List<BuffDebuff> activeBuffs = new List<BuffDebuff>();  // 이 오브젝트에 활성화된 버프/디버프 리스트

    // 실제 스탯 계산 (원본 + 버프 효과)
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
    /// BuffDebuff.cs에서 호출하여 Object_Base에 버프 추가
    /// </summary>
    public void ApplyBuffDebuff(BuffDebuff buff) {
        activeBuffs.Add(buff);
    }

    /// <summary>
    /// BuffDebuff.cs에서 호출하여 Object_Base에 걸려있는 버프 제거
    /// </summary>
    public void RemoveBuffDebuff(BuffDebuff buff) {
        if (activeBuffs.Contains(buff)) {
            activeBuffs.Remove(buff);
        }
    }

    /// <summary>
    /// 공격력 증가버프 감소디버프가 동시에 존재하여도 적용되도록 Modifier 배치
    /// </summary>
    private float GetFinalStat(float baseValue, BuffTypeEnum targetType) {
        float totalModifier = 0f;
        foreach (BuffDebuff buff in activeBuffs) {
            if (buff.buffType == targetType) {
                totalModifier += buff.value;
            }
        }
        return Mathf.Max(0, baseValue + totalModifier); // 음수 방지
    }


    public virtual void UpdateHP(float damage) {

    }
}
