using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  모든 생명체를 상속시킬 스크립트, 스탯 등 정보 포함
/// </summary>
public class Object_Base : MonoBehaviour
{
    public float attack = 10f;
    public float defense = 5f;
    public float attackSpeed = 1f;
    public float moveSpeed = 5f;
    public float coolTime = 1.0f;

    // 버프/디버프 적용
    public void ApplyBuffDebuff(BuffTypeEnum buffType, float value) {
        switch (buffType) {
            case BuffTypeEnum.Attack:
                attack = Mathf.Max(0, attack + value);  // 음수 방지    // 리롤 할거 생각하면 필요 없는 기능일지도..
                Debug.Log($"플레이어 공격력 {value} 변경 → 현재: {attack}");
                break;

            case BuffTypeEnum.Defense:
                defense += value;
                Debug.Log($"플레이어 방어력 {value} 변경 → 현재: {defense}");
                break;

            case BuffTypeEnum.AttackSpeed:
                attackSpeed = Mathf.Max(0, attackSpeed + value);    // 음수 방지
                Debug.Log($"플레이어 공격속도 {value} 변경 → 현재: {attackSpeed}");
                break;

            case BuffTypeEnum.MoveSpeed:
                moveSpeed = Mathf.Max(0, moveSpeed + value); // 음수 방지
                Debug.Log($"플레이어 이동속도 {value} 변경 → 현재: {moveSpeed}");
                break;

            case BuffTypeEnum.Cooltime:
                coolTime = Mathf.Max(0, coolTime + value); // 음수 방지
                Debug.Log($"플레이어 쿨타임 {value} 변경 → 현재: {coolTime}");
                break;
        }
    }

    public virtual void UpdateHP(float damage) {

    }
}
