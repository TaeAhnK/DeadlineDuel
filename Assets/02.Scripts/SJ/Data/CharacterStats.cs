using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    // 기본 스탯 데이터
    [SerializeField] private CharacterStatsData _baseStats;
    
    // 네트워크 동기화 변수
    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>();
    
    // 이벤트
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDeath;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _currentHealth.OnValueChanged += OnHealthValueChanged;
        
        if (IsServer)
        {
            // 서버에서 초기 체력 설정
            if (_baseStats != null)
            {
                _currentHealth.Value = _baseStats.maxHealth;
            }
        }
    }
    
    // 스탯 데이터 설정
    public void SetStatsData(CharacterStatsData statsData)
    {
        if (!IsServer) return;
        
        _baseStats = statsData;
        _currentHealth.Value = statsData.maxHealth;
    }
    
    // 데미지 처리
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;
        
        // 방어력을 적용한 실제 데미지 계산
        float actualDamage = CalculateDamage(damage);
        
        // 체력 감소
        _currentHealth.Value = Mathf.Max(0, _currentHealth.Value - actualDamage);
        
        // 데미지 이펙트 표시
        ShowDamageEffectClientRpc(actualDamage);
    }
    
    // 데미지 계산 (방어력 적용)
    private float CalculateDamage(float damage)
    {
        if (_baseStats == null) return damage;
        
        // 방어력 공식: damage * (100 / (100 + defense))
        return damage * (100f / (100f + _baseStats.defense));
    }
    
    [ClientRpc]
    private void ShowDamageEffectClientRpc(float damage)
    {
        // 데미지 시각 효과 (애니메이션, 팝업 텍스트 등)
        Debug.Log($"Damage taken: {damage}");
    }
    
    // 체력 변경 콜백
    private void OnHealthValueChanged(float previousValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue, _baseStats != null ? _baseStats.maxHealth : 100f);
        
        if (newValue <= 0)
        {
            OnDeath?.Invoke();
        }
    }
    
    // 공격력 getter
    public float GetAttackPower()
    {
        return _baseStats != null ? _baseStats.attackPower : 10f;
    }
    
    // 방어력 getter
    public float GetDefense()
    {
        return _baseStats != null ? _baseStats.defense : 5f;
    }
    
    // 이동속도 getter
    public float GetMoveSpeed()
    {
        return _baseStats != null ? _baseStats.moveSpeed : 5f;
    }
    
    // 현재 체력 getter
    public float GetCurrentHealth()
    {
        return _currentHealth.Value;
    }
    
    // 최대 체력 getter
    public float GetMaxHealth()
    {
        return _baseStats != null ? _baseStats.maxHealth : 100f;
    }
    
    // 현재 체력 퍼센트 getter
    public float GetHealthPercent()
    {
        if (_baseStats == null) return 0f;
        return _currentHealth.Value / _baseStats.maxHealth;
    }
}