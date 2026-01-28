using UnityEngine;

public class KartController : MonoBehaviour
{
    public float speed = 10f;
    public float turnSpeed = 100f;
    public float acceleration = 8f; // 가속도 (낮춰서 천천히 가속)
    public float deceleration = 10f; // 감속도
    public float maxSpeed = 40f; // 최고 속도 (높여서 속도감 증가)
    
    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float currentSpeed; // 현재 속도
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // 무게중심 낮춤
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임
        
        // 추가 안정성 설정
        rb.linearDamping = 0.5f; // 약간의 저항
        rb.angularDamping = 2f; // 회전 저항
    }
    
    void Update()
    {
        // 입력만 받기
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }
    
    void FixedUpdate()
    {
        // 가속/감속 처리
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            // 입력이 있으면 가속
            currentSpeed += moveInput * acceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed);
        }
        else
        {
            // 입력 없으면 감속 (더 부드럽게)
            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                currentSpeed = 0;
            }
        }
        
        // Rigidbody로 물리 기반 이동
        Vector3 movement = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
        
        // 움직일 때만 회전 (제자리 회전 방지)
        if (Mathf.Abs(currentSpeed) > 1f)
        {
            // 후진 시 핸들 반대로
            float direction = currentSpeed > 0 ? 1f : -1f;
            float turn = turnInput * direction * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0, turn, 0);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
    
    // 디버그용 속도 표시
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), $"현재 속도: {currentSpeed:F2} / {maxSpeed}");
    }
}