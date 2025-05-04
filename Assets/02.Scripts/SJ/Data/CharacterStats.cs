using Unity.Netcode;
using UnityEngine;

// 캐릭터 스탯만 관리하는 별도 클래스
public class CharacterStats : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    
    [Header("스탯 설정")]
    [SerializeField] private float attack = 10f;
    [SerializeField] private float defense = 5f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float coolTime = 1.0f;
    
    // 이벤트 정의
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDeath;
    
    // 프로퍼티
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public float Attack => attack;
    public float Defense => defense;
    public float AttackSpeed => attackSpeed;
    public float MoveSpeed => moveSpeed;
    public float CoolTime => coolTime;
    
    // 데미지 적용 메서드
    public void TakeDamage(float damage)
    {
        float actualDamage = CalculateDamage(damage);
        currentHP = Mathf.Max(0, currentHP - actualDamage);
        
        // 이벤트 발생
        OnHealthChanged?.Invoke(currentHP, maxHP);
        
        if (currentHP <= 0)
        {
            
            OnDeath?.Invoke();
        }
    }
    
    // 데미지 계산 메서드
    private float CalculateDamage(float damage)
    {
        return damage * (100f / (100f + defense));
    }
    
    // 스탯 수정 메서드
    public void ModifyAttack(float value)
    {
        attack = Mathf.Max(0, attack + value);
    }
    
    public void ModifyDefense(float value)
    {
        defense = Mathf.Max(0, defense + value);
    }
    
    public void ModifyAttackSpeed(float value)
    {
        attackSpeed = Mathf.Max(0, attackSpeed + value);
    }
    
    public void ModifyMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(0, moveSpeed + value);
    }
    
    public void ModifyCoolTime(float value)
    {
        coolTime = Mathf.Max(0, coolTime + value);
    }
    
    // 체력 회복 메서드
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
    
    // 스탯 데이터 설정
    public void SetStatsFromData(CharacterStatsData data)
    {
        if (data == null) return;
        
        maxHP = data.maxHealth;
        currentHP = maxHP;
        attack = data.attackPower;
        defense = data.defense;
        moveSpeed = data.moveSpeed;
        
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
}