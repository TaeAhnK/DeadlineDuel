using UnityEngine;

public class TestDummyTarget : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    [SerializeField] private bool _regenerateHealth = true;
    [SerializeField] private float _regenerateDelay = 5f;
    
    private Renderer _renderer;
    private Color _originalColor;
    private float _lastDamageTime;
    
    private void Start()
    {
        _currentHealth = _maxHealth;
        _renderer = GetComponent<Renderer>();
        
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
    }
    
    private void Update()
    {
        // 체력 재생성
        if (_regenerateHealth && _currentHealth < _maxHealth && Time.time - _lastDamageTime > _regenerateDelay)
        {
            _currentHealth = _maxHealth;
            UpdateVisuals();
        }
    }
    
    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _lastDamageTime = Time.time;
        
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Debug.Log($"{gameObject.name} 처치됨");
        }
        
        Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받음. 남은 체력: {_currentHealth}/{_maxHealth}");
        
        // 시각적 피드백
        UpdateVisuals();
    }
    
    /// <summary>
    /// 더미 타겟의 시각적 상태 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        if (_renderer != null)
        {
            // 체력에 따른 색상 변화
            float healthPercent = _currentHealth / _maxHealth;
            
            if (healthPercent <= 0)
            {
                // 처치됨 - 회색
                _renderer.material.color = Color.gray;
            }
            else
            {
                // 빨간색(데미지)에서 원래 색상으로 변화
                _renderer.material.color = Color.Lerp(Color.red, _originalColor, healthPercent);
            }
        }
    }
    
    /// <summary>
    /// 테스트용 공격 시뮬레이션
    /// </summary>
    public void SimulateAttack(GameObject target, float damage)
    {
        Debug.Log($"{gameObject.name}이(가) {target.name}을(를) 공격합니다. 데미지: {damage}");
        
        TestPlayerController player = target.GetComponent<TestPlayerController>();
        if (player != null)
        {
            // 플레이어에게 데미지 적용 시도
            TestSwordCharacterSkills skills = target.GetComponent<TestSwordCharacterSkills>();
            if (skills != null && skills.ProcessCounter(gameObject, damage))
            {
                // 반격 성공
                Debug.Log($"{target.name}이(가) 공격을 반격했습니다!");
            }
            else
            {
                // 플레이어가 데미지를 받음
                player.TakeDamage(damage);
            }
        }
    }
}
