using Stats.Boss;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
///  모든 생명체를 상속시킬 스크립트, 스탯 등 정보 포함. 일단 자세한건 나중에
/// </summary>
public class BuffableEntity : NetworkBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private CharacterStats _characterStats; // 캐릭터 스탯 참조
    [SerializeField] private Stats.Boss.BossStats _bossStats; // 보스 스탯 참조

    private void Awake() {
        if (!_bossStats) _bossStats = GetComponent<BossStats>();
    }

    /// <summary>
    /// BuffDebuff.cs에서 호출하여 버프 추가
    /// </summary>
    public void ApplyBuffDebuff(BuffTypeEnum buffType, float value) {
        switch (buffType) {
            case BuffTypeEnum.Attack:
                _bossStats.Atk.Value += value;
                Debug.Log($"플레이어 공격력 {value} 변경 → 현재: {_bossStats.Atk.Value}");
                break;

            case BuffTypeEnum.Defense:
                _bossStats.Def.Value += value;
                Debug.Log($"플레이어 방어력 {value} 변경 → 현재: {_bossStats.Def.Value}");
                break;
        }
    }
}
