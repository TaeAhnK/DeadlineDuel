using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  ��� ����ü�� ��ӽ�ų ��ũ��Ʈ, ���� �� ���� ����
/// </summary>
public class Object_Base : MonoBehaviour
{
    public float attack = 10f;
    public float defense = 5f;
    public float attackSpeed = 1f;
    public float moveSpeed = 5f;
    public float coolTime = 1.0f;

    // ����/����� ����
    public void ApplyBuffDebuff(BuffTypeEnum buffType, float value) {
        switch (buffType) {
            case BuffTypeEnum.Attack:
                attack = Mathf.Max(0, attack + value);  // ���� ����    // ���� �Ұ� �����ϸ� �ʿ� ���� ���������..
                Debug.Log($"�÷��̾� ���ݷ� {value} ���� �� ����: {attack}");
                break;

            case BuffTypeEnum.Defense:
                defense += value;
                Debug.Log($"�÷��̾� ���� {value} ���� �� ����: {defense}");
                break;

            case BuffTypeEnum.AttackSpeed:
                attackSpeed = Mathf.Max(0, attackSpeed + value);    // ���� ����
                Debug.Log($"�÷��̾� ���ݼӵ� {value} ���� �� ����: {attackSpeed}");
                break;

            case BuffTypeEnum.MoveSpeed:
                moveSpeed = Mathf.Max(0, moveSpeed + value); // ���� ����
                Debug.Log($"�÷��̾� �̵��ӵ� {value} ���� �� ����: {moveSpeed}");
                break;

            case BuffTypeEnum.Cooltime:
                coolTime = Mathf.Max(0, coolTime + value); // ���� ����
                Debug.Log($"�÷��̾� ��Ÿ�� {value} ���� �� ����: {coolTime}");
                break;
        }
    }

    public virtual void UpdateHP(float damage) {

    }
}
