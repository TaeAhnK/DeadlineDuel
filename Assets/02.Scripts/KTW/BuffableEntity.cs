using Stats.Boss;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
///  ��� ����ü�� ��ӽ�ų ��ũ��Ʈ, ���� �� ���� ����. �ϴ� �ڼ��Ѱ� ���߿�
/// </summary>
public class BuffableEntity : NetworkBehaviour
{
    [Header("���� ������Ʈ")]
    [SerializeField] private CharacterStats _characterStats; // ĳ���� ���� ����
    [SerializeField] private Stats.Boss.BossStats _bossStats; // ���� ���� ����

    private void Awake() {
        if (!_bossStats) _bossStats = GetComponent<BossStats>();
    }

    /// <summary>
    /// BuffDebuff.cs���� ȣ���Ͽ� ���� �߰�
    /// </summary>
    public void ApplyBuffDebuff(BuffTypeEnum buffType, float value) {
        switch (buffType) {
            case BuffTypeEnum.Attack:
                _bossStats.Atk.Value += value;
                Debug.Log($"�÷��̾� ���ݷ� {value} ���� �� ����: {_bossStats.Atk.Value}");
                break;

            case BuffTypeEnum.Defense:
                _bossStats.Def.Value += value;
                Debug.Log($"�÷��̾� ���� {value} ���� �� ����: {_bossStats.Def.Value}");
                break;
        }
    }
}
