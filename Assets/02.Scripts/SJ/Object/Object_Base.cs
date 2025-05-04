using Unity.Netcode;
using UnityEngine;

// NetworkString 구조체 추가
public struct NetworkString : INetworkSerializable
{
    public string Value;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // 초기화되지 않은 string을 위한 안전한 처리
        if (Value == null)
        {
            Value = string.Empty;
        }
        serializer.SerializeValue(ref Value);
    }
    
    // 기본 생성자 추가
    public NetworkString(string value)
    {
        Value = value ?? string.Empty;
    }
}

public class Object_Base : NetworkBehaviour
{
    // CharacterStats 참조 - 직렬화 문제 없음
    private CharacterStats _stats;
    
    // 네트워크 변수 - NetworkString 타입으로 변경
    private NetworkVariable<NetworkString> playerId = new NetworkVariable<NetworkString>(new NetworkString(""));
    
    // 이벤트
    public System.Action<float, float> OnDamageTaken;
    public System.Action<Object_Base> OnObjectDestroyed;
    
    protected virtual void Awake()
    {
        // CharacterStats 컴포넌트 가져오기 또는 생성
        _stats = GetComponent<CharacterStats>();
        if (_stats == null)
        {
            _stats = gameObject.AddComponent<CharacterStats>();
        }
        
        // CharacterStats 이벤트 연결
        _stats.OnHealthChanged += (current, max) => {
            OnDamageTaken?.Invoke(current, max);
        };
        
        _stats.OnDeath += () => {
            OnObjectDestroyed?.Invoke(this);
        };
    }
    
    // 서버에서 플레이어 ID 설정
    [ServerRpc]
    public void SetPlayerIdServerRpc(string id)
    {
        playerId.Value = new NetworkString { Value = id };
    }
    
    // 플레이어 ID 가져오기
    public string GetPlayerId()
    {
        return playerId.Value.Value;
    }
    
    // 스탯 접근 메서드들 (getter)
    public float GetMaxHP() => _stats.MaxHP;
    public float GetCurrentHP() => _stats.CurrentHP;
    public float GetAttack() => _stats.Attack;
    public float GetDefense() => _stats.Defense;
    public float GetAttackSpeed() => _stats.AttackSpeed;
    public float GetMoveSpeed() => _stats.MoveSpeed;
    public float GetCoolTime() => _stats.CoolTime;
    
    // 버프/디버프 적용
    public void ApplyBuffDebuff(BuffTypeEnum buffType, float value) 
    {
        switch (buffType) 
        {
            case BuffTypeEnum.Attack:
                _stats.ModifyAttack(value);
                Debug.Log($"플레이어 공격력 {value} 변경 → 현재: {_stats.Attack}");
                break;

            case BuffTypeEnum.Defense:
                _stats.ModifyDefense(value);
                Debug.Log($"플레이어 방어력 {value} 변경 → 현재: {_stats.Defense}");
                break;

            case BuffTypeEnum.AttackSpeed:
                _stats.ModifyAttackSpeed(value);
                Debug.Log($"플레이어 공격속도 {value} 변경 → 현재: {_stats.AttackSpeed}");
                break;

            case BuffTypeEnum.MoveSpeed:
                _stats.ModifyMoveSpeed(value);
                Debug.Log($"플레이어 이동속도 {value} 변경 → 현재: {_stats.MoveSpeed}");
                break;

            case BuffTypeEnum.Cooltime:
                _stats.ModifyCoolTime(value);
                Debug.Log($"플레이어 쿨타임 {value} 변경 → 현재: {_stats.CoolTime}");
                break;
        }
        
        // 서버에서만 스탯 업데이트를 UI에 알림
        if (IsServer && !string.IsNullOrEmpty(playerId.Value.Value))
        {
            // 게임 매니저를 통해 UI 업데이트
            GamePlayManager.Instance.UpdatePlayerStatsFromServer(
                _stats.Attack, _stats.Defense, _stats.AttackSpeed, (int)_stats.CoolTime);
        }
    }

    public virtual void UpdateHP(float damage) 
    {
        if (IsServer)
        {
            // CharacterStats를 통해 데미지 처리
            _stats.TakeDamage(damage);
            
            // 플레이어인 경우 UI 업데이트
            if (!string.IsNullOrEmpty(playerId.Value.Value))
            {
                GamePlayManager.Instance.UpdatePlayerHPFromServer(_stats.CurrentHP / _stats.MaxHP);
            }
        }
    }
    
    // 캐릭터 스탯 데이터 설정
    public void SetCharacterStats(CharacterStatsData statsData)
    {
        if (IsServer && _stats != null)
        {
            _stats.SetStatsFromData(statsData);
        }
    }
}