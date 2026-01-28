using UnityEngine;

public class KartCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // 카트를 드래그해서 넣으세요
    
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 4, -6); // 카메라 위치 오프셋
    public float smoothSpeed = 8f; // 카메라 따라가는 속도 (높을수록 빠름)
    public float lookAheadDistance = 2f; // 카트 앞쪽을 얼마나 볼지
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // 카트의 회전을 고려한 목표 위치 계산
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        
        // 부드럽게 카메라 이동 (더 부드럽게)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // 카트 앞쪽을 바라보기 (회전도 부드럽게)
        Vector3 lookAtPosition = target.position + target.forward * lookAheadDistance;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}