using UnityEngine;
using Unity.Netcode;

public class QuadViewController : MonoBehaviour
{
    [Header("대상 설정")]
    [SerializeField] private Transform _target; // 따라갈 타겟 (플레이어)
    
    [Header("카메라 설정")]
    [SerializeField] private float _distance = 10f; // 타겟으로부터의 거리
    [SerializeField] private float _height = 8f; // 타겟으로부터의 높이
    [SerializeField] private float _angle = 45f; // 카메라 기울기 각도
    
    [Header("카메라 이동 설정")]
    [SerializeField] private float _smoothSpeed = 5f; // 카메라 이동 부드러움 정도
    [SerializeField] private Vector3 _offset = Vector3.zero; // 추가 오프셋

    // 플레이어 참조를 설정하는 메서드
    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        // 타겟 위치 계산
        Vector3 targetPosition = _target.position + _offset;
        
        // 카메라 위치 계산
        Vector3 desiredPosition = CalculateCameraPosition(targetPosition);
        
        // 부드러운 이동 적용
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // 카메라가 타겟을 바라보도록 설정
        transform.LookAt(targetPosition);
    }
    
    private Vector3 CalculateCameraPosition(Vector3 targetPosition)
    {
        // 각도를 라디안으로 변환
        float angleRad = _angle * Mathf.Deg2Rad;
        
        // X, Z 평면에서의 방향 벡터 계산 (쿼드뷰는 주로 X, Z 평면에서 북동쪽)
        float dirX = Mathf.Sin(angleRad) * _distance;
        float dirZ = Mathf.Cos(angleRad) * _distance;
        
        // 카메라 최종 위치 계산
        return new Vector3(
            targetPosition.x - dirX,  // X 좌표
            targetPosition.y + _height, // Y 좌표 (높이)
            targetPosition.z - dirZ   // Z 좌표
        );
    }
}