using UnityEngine;

public class KartController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float acceleration = 8f; // 가속도
    public float deceleration = 10f; // 감속도
    public float maxSpeed = 40f; // 최고 속도
    public float brakeForce = 20f; // 브레이크 힘
    
    [Header("Turn Settings")]
    public float baseTurnSpeed = 100f; // 기본 회전 속도
    public float minTurnSpeed = 40f; // 고속일 때 최소 회전 속도
    public AnimationCurve turnSpeedCurve; // 속도에 따른 회전력 커브
    
    [Header("Physics Settings")]
    public float groundDrag = 2f; // 바닥 마찰력
    public float airDrag = 0.5f; // 공중 마찰력
    public float slopeForce = 5f; // 경사로 힘
    public float maxSlopeAngle = 45f; // 최대 등반 각도
    public LayerMask groundLayer; // 바닥 레이어
    public float groundCheckDistance = 2.0f; // 바닥 체크 거리 (서스펜션 거리 0.56 참고)
    public bool lockYPosition = false; // Y축 위치 고정 (진동 완전 제거)
    public bool useHoverHeight = true; // 바닥 위 일정 높이 유지 (추천)
    public float hoverHeight = 0.56f; // 바닥에서 떠있을 높이 (Wheel Collider Target Position 참고)
    public float hoverForce = 80f; // 높이 유지 힘 (Wheel Spring 20000 참고 - 스케일 조정)
    public float hoverDamping = 15f; // 높이 조절 감쇠력 (Wheel Damper 500 참고 - 스케일 조정)
    public bool forceUpright = true; // 카트를 강제로 수평 유지 (캡슐 콜라이더 사용 시 필요)
    
    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private bool brakeInput;
    private float currentSpeed; // 현재 속도
    private bool isGrounded;
    private RaycastHit slopeHit;
    private float fixedYPosition; // 고정할 Y 위치
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // 무게중심 낮춤
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임
        
        // 🔧 Collider 타입 체크
        Collider col = GetComponent<Collider>();
        if (col is CapsuleCollider)
        {
            Debug.LogWarning("⚠️ 캡슐 콜라이더는 기울어질 수 있습니다! Box Collider 사용을 권장합니다.");
        }
        
        // 추가 안정성 설정
        rb.linearDamping = 1f; // 진동 방지를 위해 증가
        rb.angularDamping = 3f; // 회전 저항 증가
        
        // 🔧 흔들림 방지: 회전 제한
        if (useHoverHeight)
        {
            // Hover 사용 시: X, Z 회전만 고정 (Y축 이동은 Hover가 제어)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else if (lockYPosition)
        {
            // Y 위치 고정 사용 시: X, Z 회전 + Y 위치 고정
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        }
        else
        {
            // 기본: X, Z 회전만 고정
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        // 🔧 충돌 감지 모드 변경
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // 🔧 질량 설정 (가벼우면 흔들림이 심함)
        if (rb.mass < 10f)
        {
            rb.mass = 15f; // 적절한 무게 설정
        }
        
        // Y 위치 저장
        fixedYPosition = transform.position.y;
        
        // 기본 턴 스피드 커브 설정 (Inspector에서 수정 가능)
        if (turnSpeedCurve == null || turnSpeedCurve.keys.Length == 0)
        {
            turnSpeedCurve = AnimationCurve.Linear(0, 1, 1, 0.4f);
            // 0% 속도에서 100% 회전력, 100% 속도에서 40% 회전력
        }
    }
    
    void Update()
    {
        // 입력 받기
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space); // 브레이크
        
        // 바닥 체크
        CheckGround();
    }
    
    void FixedUpdate()
    {
        // 강제 수평 유지 (캡슐 콜라이더 사용 시)
        if (forceUpright)
        {
            ForceUprightRotation();
        }
        
        // 호버 시스템 (바닥 위 일정 높이 유지)
        if (useHoverHeight)
        {
            ApplyHoverForce();
        }
        
        // 마찰력 적용
        ApplyDrag();
        
        // 경사로 처리
        HandleSlope();
        
        // 이동 처리
        HandleMovement();
        
        // 회전 처리
        HandleRotation();
    }
    
    void CheckGround()
    {
        // 카트 아래로 Ray를 쏴서 바닥 체크
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
        
        // 디버그: 레이어 마스크 확인
        if (groundLayer == 0)
        {
            Debug.LogWarning("Ground Layer가 설정되지 않았습니다! Inspector에서 Ground Layer를 설정하세요.");
        }
        
        // 디버그: 바닥 감지 실패 시
        if (!isGrounded)
        {
            // 레이어 상관없이 뭐라도 맞았는지 체크
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
            {
                Debug.LogWarning($"바닥은 감지되었으나 레이어가 다릅니다! 감지된 레이어: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
        }
    }
    
    void ApplyHoverForce()
    {
        // 바닥까지의 거리 측정
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            // 현재 바닥으로부터의 높이
            float currentHeight = hit.distance;
            
            // 목표 높이와의 차이
            float heightDifference = hoverHeight - currentHeight;
            
            // 스프링-댐퍼 시스템으로 부드럽게 높이 유지
            float upwardForce = heightDifference * hoverForce;
            float dampingForce = -rb.linearVelocity.y * hoverDamping;
            
            // Y축 방향으로만 힘 적용
            rb.AddForce(Vector3.up * (upwardForce + dampingForce), ForceMode.Force);
        }
    }
    
    void ForceUprightRotation()
    {
        // 현재 회전을 Y축만 남기고 강제로 수평으로
        Vector3 currentRotation = transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(0, currentRotation.y, 0);
        
        // 부드럽게 보정
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
    }
    
    void ApplyDrag()
    {
        // 바닥에 있을 때와 공중일 때 다른 마찰력 적용
        rb.linearDamping = isGrounded ? groundDrag : airDrag;
    }
    
    void HandleSlope()
    {
        // 경사로에 있는지 체크
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, groundCheckDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            
            // 경사각이 너무 크면 미끄러짐
            if (slopeAngle > maxSlopeAngle)
            {
                Vector3 slideDirection = new Vector3(slopeHit.normal.x, -slopeHit.normal.y, slopeHit.normal.z);
                rb.AddForce(slideDirection * slopeForce, ForceMode.Force);
            }
        }
    }
    
    void HandleMovement()
    {
        // 브레이크 처리
        if (brakeInput)
        {
            // 급제동
            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeForce * Time.fixedDeltaTime);
            }
            else
            {
                currentSpeed = 0;
            }
        }
        // 가속/감속 처리
        else if (Mathf.Abs(moveInput) > 0.1f)
        {
            // 입력이 있으면 가속
            currentSpeed += moveInput * acceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed * 0.5f, maxSpeed);
        }
        else
        {
            // 입력 없으면 자연 감속
            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                currentSpeed = 0;
            }
        }
        
        // 경사로에 있을 때 이동 방향 조정
        Vector3 moveDirection;
        if (OnSlope())
        {
            // 경사면을 따라 이동
            moveDirection = GetSlopeMoveDirection();
        }
        else
        {
            // 평지 이동
            moveDirection = transform.forward;
        }
        
        // Rigidbody로 물리 기반 이동
        Vector3 movement = moveDirection * currentSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = rb.position + movement;
        
        // Y축 위치 고정 (진동 완전 제거 옵션)
        if (lockYPosition)
        {
            newPosition.y = fixedYPosition;
        }
        
        rb.MovePosition(newPosition);
    }
    
    void HandleRotation()
    {
        // 움직일 때만 회전 (제자리 회전 방지)
        if (Mathf.Abs(currentSpeed) > 1f)
        {
            // 속도에 따른 회전력 계산
            float speedRatio = Mathf.Abs(currentSpeed) / maxSpeed; // 0~1
            float turnMultiplier = turnSpeedCurve.Evaluate(speedRatio);
            float adjustedTurnSpeed = Mathf.Lerp(baseTurnSpeed, minTurnSpeed, speedRatio);
            adjustedTurnSpeed *= turnMultiplier;
            
            // 후진 시 핸들 반대로
            float direction = currentSpeed > 0 ? 1f : -1f;
            float turn = turnInput * direction * adjustedTurnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0, turn, 0);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
    
    bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, groundCheckDistance, groundLayer))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }
    
    Vector3 GetSlopeMoveDirection()
    {
        // 경사면과 평행한 이동 방향 계산
        return Vector3.ProjectOnPlane(transform.forward, slopeHit.normal).normalized;
    }
    
    // 디버그용 속도 및 상태 표시
    void OnGUI()
    {
        // 배경 박스 스타일
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f)); // 반투명 검은색
        
        // 텍스트 스타일
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = Color.white;
        textStyle.fontSize = 14;
        textStyle.fontStyle = FontStyle.Bold;
        
        // 배경 박스
        GUI.Box(new Rect(5, 5, 420, 130), "", boxStyle);
        
        // 정보 표시
        GUI.Label(new Rect(10, 10, 400, 30), $"현재 속도: {currentSpeed:F2} / {maxSpeed}", textStyle);
        GUI.Label(new Rect(10, 40, 400, 30), $"바닥 접촉: {(isGrounded ? "예" : "아니오")}", textStyle);
        GUI.Label(new Rect(10, 70, 400, 30), $"브레이크: {(brakeInput ? "ON" : "OFF")}", textStyle);
        
        if (OnSlope())
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            GUI.Label(new Rect(10, 100, 400, 30), $"경사각: {angle:F1}°", textStyle);
        }
    }
    
    // 텍스처 생성 헬퍼 함수
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    
    // Gizmos로 디버깅
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // 바닥 체크 레이
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        Gizmos.DrawWireSphere(transform.position + Vector3.down * groundCheckDistance, 0.1f);
        
        // 경사면 노멀
        if (OnSlope())
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(slopeHit.point, slopeHit.point + slopeHit.normal * 2f);
        }
    }
}
