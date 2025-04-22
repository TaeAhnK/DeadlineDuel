using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    [SerializeField] private Transform _fillImage;  // 체력바 Fill 이미지
    [SerializeField] private Transform _background; // 체력바 배경
    
    private Camera _mainCamera;
    private float _initialScale;
    
    private void Start()
    {
        // 메인 카메라 찾기
        _mainCamera = Camera.main;
        
        // 초기 스케일 저장
        if (_fillImage != null)
        {
            _initialScale = _fillImage.localScale.x;
        }
    }
    
    private void LateUpdate()
    {
        // 항상 카메라를 향하도록 회전
        if (_mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
        }
    }
    
    // 체력바 업데이트 (0.0f ~ 1.0f 값)
    public void UpdateHealthBar(float fillAmount)
    {
        if (_fillImage != null)
        {
            // X 스케일만 조정하여 체력바 표시
            Vector3 newScale = _fillImage.localScale;
            newScale.x = _initialScale * Mathf.Clamp01(fillAmount);
            _fillImage.localScale = newScale;
        }
    }
    
    // 체력바 표시/숨김
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}