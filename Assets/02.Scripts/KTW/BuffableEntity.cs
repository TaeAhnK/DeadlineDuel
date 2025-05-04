using Stats.Boss;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
///  모든 생명체를 상속시킬 스크립트, 스탯 등 정보 포함. 일단 자세한건 나중에
/// </summary>
public class BuffableEntity : NetworkBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private CharacterStats _characterStats; // 캐릭터 스탯 참조
    [SerializeField] private Stats.Boss.BossStats _bossStats; // 보스 스탯 참조


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

    private NetworkList<NetworkBuff> _activeBuffs;  // 이 오브젝트에 활성화된 버프/디버프 리스트 

    // 실제 스탯 계산 (원본 + 버프 효과)
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

    // 유니티 에디터에서만 값 확인할 수 있도록 추가
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
    /// BuffDebuff.cs에서 호출하여 버프 추가
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyBuffDebuffServerRpc(BuffTypeEnum buffType, float value) {
        if (!IsServer) return;

        // 중복방지는 TotemManager에서 처리하였으므로 여기서는 안함

        _activeBuffs.Add(new NetworkBuff {
            buffType = buffType,
            value = value
        });
    }

    /// <summary>
    /// BuffDebuff.cs에서 호출하여 걸려있는 버프 제거
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
    /// 공격력 증가버프 감소디버프가 동시에 존재하여도 적용되도록 Modifier 배치
    /// </summary>
    private float GetFinalStat(float baseValue, BuffTypeEnum targetType) {
        float totalModifier = 0f;
        //foreach (BuffDebuff buff in activeBuffs) {
        //    if (buff.buffType == targetType) {
        //        totalModifier += buff.value;
        //    }
        //}
        //return Mathf.Max(0, baseValue + totalModifier); // 음수 방지
        return 1.0f;
    }


    public virtual void UpdateHP(float damage) {

    }
}
