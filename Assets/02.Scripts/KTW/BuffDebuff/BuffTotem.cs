using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BuffTotem : BuffableEntity
{
    [Header("Buff Data")]
    public string buffName;
    public BuffTargetEnum targetType;
    public BuffTypeEnum buffType;
    public float buffDuration = 30f;
    public float buffValue;

    [Header("Network")]
    public int owner;   // TODO ��Ʈ��ũ ���� ���� ������ �������� �ְ� �� ����� ������ �� �ֵ��� �ؾ��� ��

    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private float maxHP = 100.0f;
    [SerializeField] private float currentHP = 100.0f;
    private const float UIHPBarAnimationDuration = 0.3f;

    [Header("Spawn")]
    public int spawnIndex;


    private BuffableEntity target;
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
    /// ���ۿ� ������ ���� �� HP Bar�� �ݿ�. �ܿ� HP �˻��ϰ� �ı�
    /// </summary>
    /// <param name="damage">�ִ� ������</param>
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
            .Any(buff => buff.buffName == buffName);    // ���� ������ �����ϴ��� üũ

        // �ش� ������ �������� ���� �� ������Ʈ �߰�
        if (!isApplied) {
            appliedBuff = target.gameObject.AddComponent<BuffDebuff>();
            appliedBuff.buffName = buffName;
            appliedBuff.buffType = buffType;
            appliedBuff.value = buffValue;
        }
    }

    private BuffableEntity FindTarget() {
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

    private BuffableEntity FindPlayer() {
        return GamePlayManager_t.Instance.GetPlayer();
    }

    private BuffableEntity FindEnemy() {
        return GamePlayManager_t.Instance.GetEnemy();
    }

    private BuffableEntity FindBoss() {
        return GamePlayManager_t.Instance.GetEnemyBoss();
    }

    private void OnDestroy() {
        RemoveBuff();
        TotemManager.Instance.RemoveTotem(this);
    }
}
