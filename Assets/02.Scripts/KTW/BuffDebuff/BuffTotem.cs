using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BuffTotem : Object_Base
{
    [Header("Buff Data")]
    public string buffName;
    public BuffTargetEnum targetType;
    public BuffTypeEnum buffType;
    public float buffDuration = 30f;
    public float buffValue;

    [Header("Network")]
    public int owner;   // TODO 네트워크 값에 따라 누구의 토템인지 넣고 그 사람만 공격할 수 있도록 해야할 듯

    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private float maxHP = 100.0f;
    [SerializeField] private float currentHP = 100.0f;
    private const float UIHPBarAnimationDuration = 0.3f;

    [Header("Spawn")]
    public int spawnIndex;


    private Object_Base target;
    private BuffDebuff appliedBuff;

    private void Start() {
        InitializeHP();
        StartCoroutine(BuffDurationRoutine());
    }

    private void InitializeHP() {
        currentHP = maxHP;
        hpSlider.value = 1f;
    }

    /// <summary>
    /// 토템에 데미지 적용 후 HP Bar에 반영. 잔여 HP 검사하고 파괴
    /// </summary>
    /// <param name="damage">주는 데미지</param>
    public override void UpdateHP(float damage) {
        base.UpdateHP(damage);

        currentHP = Mathf.Max(currentHP - damage, 0);
        hpSlider.DOValue(currentHP / maxHP, UIHPBarAnimationDuration).SetEase(Ease.OutQuad);

        if (currentHP <= 0) {
            Destroy(gameObject);
        }
    }

    private IEnumerator BuffDurationRoutine() {
        ApplyBuffToTarget();
        yield return new WaitForSeconds(buffDuration);
        Destroy(gameObject);
    }

    private void ApplyBuffToTarget() {
        target = FindTarget();

        if (target == null) {
            Debug.LogError($"BuffTotem | ({targetType}) target not found");
            return;
        }

        bool isApplied = target.GetComponents<BuffDebuff>()
            .Any(buff => buff.buffName == buffName);    // 동일 버프가 존재하는지 체크

        // 해당 버프가 존재하지 않을 때 컴포넌트 추가
        if (!isApplied) {
            appliedBuff = target.gameObject.AddComponent<BuffDebuff>();
            appliedBuff.buffName = buffName;
            appliedBuff.buffType = buffType;
            appliedBuff.value = buffValue;
        }
    }

    private Object_Base FindTarget() {
        return targetType switch {
            BuffTargetEnum.Player => FindPlayer(),
            BuffTargetEnum.Enemy => FindEnemy(),
            BuffTargetEnum.Boss => FindBoss(),
            _ => null
        };
    }

    private void RemoveBuff() {
        if (appliedBuff != null) {
            Destroy(appliedBuff);
        }
    }

    private Object_Base FindPlayer() {
        return FindObjectOfType<Object_Base>();
    }

    private Object_Base FindEnemy() {
        return FindObjectOfType<Object_Base>();
    }

    private Object_Base FindBoss() {
        return FindObjectOfType<Object_Base>();
    }

    private void OnDestroy() {
        RemoveBuff();
        TotemManager.Instance.RemoveTotem(this);
    }
}
