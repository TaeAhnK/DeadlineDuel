using UnityEngine;

public class MoveIndicatorController : MonoBehaviour
{
    [Header("이동 표시자 설정")]
    [SerializeField] private float _rotationSpeed = 90f; // 회전 속도 (도/초)
    [SerializeField] private float _pulseSpeed = 1f; // 크기 변화 속도
    [SerializeField] private float _minScale = 0.8f; // 최소 크기 비율
    [SerializeField] private float _maxScale = 1.2f; // 최대 크기 비율
    
    // 초기 스케일
    private Vector3 _originalScale;
    
    private void Awake()
    {
        // 초기 스케일 저장
        _originalScale = transform.localScale;
    }
    
    private void OnEnable()
    {
        // 활성화될 때마다 기본 스케일로 초기화
        transform.localScale = _originalScale;
    }
    
    private void Update()
    {
        // Y축 회전
        transform.Rotate(0, _rotationSpeed * Time.deltaTime, 0);
        
        // 크기 변화 (펄스 효과)
        float scale = Mathf.Lerp(_minScale, _maxScale, (Mathf.Sin(Time.time * _pulseSpeed) + 1) * 0.5f);
        transform.localScale = _originalScale * scale;
    }
}