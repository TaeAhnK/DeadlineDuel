using UnityEngine;
using Unity.Netcode;

public class DummyTarget : NetworkBehaviour
{
    [Header("더미 설정")]
    [SerializeField] private float _health = 100f;
    [SerializeField] private GameObject _hitEffect;
    
    // 네트워크 변수 - 체력
    private NetworkVariable<float> _networkHealth = new NetworkVariable<float>(100f);
    
    // UI 표시 요소
    [SerializeField] private GameObject _healthBarPrefab;
    private GameObject _healthBarInstance;
    private Transform _healthBarFill;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 초기 체력 설정
        _networkHealth.Value = _health;
        
        // 네트워크 변수 변경 콜백
        _networkHealth.OnValueChanged += OnHealthChanged;
        
        // 체력바 생성
        CreateHealthBar();
    }
    
    private void CreateHealthBar()
    {
        if (_healthBarPrefab != null)
        {
            // 월드 공간에 UI 생성 (허수아비 위에 떠있게)
            _healthBarInstance = Instantiate(_healthBarPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            _healthBarInstance.transform.SetParent(transform);
            
            // Fill 이미지 찾기 (체력바 UI에 "Fill" 이름의 자식이 있다고 가정)
            _healthBarFill = _healthBarInstance.transform.Find("Fill");
        }
    }
    
    // 서버에서만 호출 - 데미지 처리
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, ServerRpcParams serverRpcParams = default)
    {
        // 체력 감소
        float newHealth = Mathf.Max(0, _networkHealth.Value - damage);
        _networkHealth.Value = newHealth;
        
        // 히트 이펙트 생성 (모든 클라이언트에 표시)
        if (_hitEffect != null)
        {
            ShowHitEffectClientRpc();
        }
    }
    
    // 모든 클라이언트에서 호출 - 히트 이펙트 표시
    [ClientRpc]
    private void ShowHitEffectClientRpc()
    {
        if (_hitEffect != null)
        {
            GameObject effect = Instantiate(_hitEffect, transform.position + Vector3.up, Quaternion.identity);
            Destroy(effect, 2f); // 2초 후 이펙트 제거
        }
    }
    
    // 체력 변경 콜백
    private void OnHealthChanged(float oldValue, float newValue)
    {
        // 체력바 업데이트
        UpdateHealthBar(newValue / _health);
    }
    
    // 체력바 업데이트
    private void UpdateHealthBar(float fillAmount)
    {
        if (_healthBarFill != null)
        {
            // 스케일 조정으로 체력바 표시
            Vector3 scale = _healthBarFill.localScale;
            scale.x = fillAmount;
            _healthBarFill.localScale = scale;
        }
    }
}