using UnityEngine;

public class KartController : MonoBehaviour
{
    [Header("Movement")]
    public float topSpeed = 10f;
    public float acceleration = 5f;
    public float reverseSpeed = 5f;
    public float braking = 10f;
    public float coastingDrag = 4f;
    
    [Header("Steering")]
    public float steerStrength = 5f;
    public float grip = 0.95f;
    
    [Header("Wheels")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;
    
    [Header("Suspension")]
    public float suspensionHeight = 0.1f;
    public float suspensionSpring = 3000f;
    public float suspensionDamper = 1500f;
    
    [Header("Stability")]
    public float downforce = 100f;
    
    public LayerMask groundLayers = Physics.DefaultRaycastLayers;
    
    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private bool brakeInput;
    
    private float groundPercent;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Rigidbody 설정
        rb.mass = 500f;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.5f;
        
        // Rigidbody 제약 조건 - X, Z축 회전 잠금 (뒤집힘 방지)
        rb.constraints = RigidbodyConstraints.None;
        
        // WheelCollider 설정
        SetupWheels();
    }
    
    void SetupWheels()
    {
        WheelCollider[] wheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };
        
        foreach (var wheel in wheels)
        {
            if (wheel == null) continue;
            
            // Suspension 설정
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = suspensionSpring;
            spring.damper = suspensionDamper;
            wheel.suspensionSpring = spring;
            
            wheel.suspensionDistance = suspensionHeight;
            
            // 마찰력 설정
            WheelFrictionCurve forwardFriction = wheel.forwardFriction;
            forwardFriction.stiffness = 1f;
            wheel.forwardFriction = forwardFriction;
            
            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.stiffness = 1f;
            wheel.sidewaysFriction = sidewaysFriction;
        }
    }
    
    void Update()
    {
        // 입력
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space);
    }
    
    void FixedUpdate()
    {
        UpdateGroundedPercent();
        
        // 다운포스 적용
        if (groundPercent > 0f)
        {
            rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
        }
        
        // 속도 계산
        Vector3 localVel = transform.InverseTransformVector(rb.linearVelocity);
        float currentSpeed = localVel.z;
        
        bool movingForward = currentSpeed > 0.01f;
        bool acceleratingForward = moveInput > 0.01f;
        
        // 가속/감속
        float maxSpeed = acceleratingForward ? topSpeed : reverseSpeed;
        float accelPower = acceleratingForward ? acceleration : reverseSpeed;
        
        bool isBraking = (movingForward && brakeInput) || (movingForward && moveInput < -0.01f) || 
                         (!movingForward && moveInput > 0.01f);
        
        float finalAccel = isBraking ? braking : accelPower;
        
        // 이동
        if (groundPercent > 0f)
        {
            Vector3 movement = transform.forward * moveInput * finalAccel;
            
            // 최대 속도 제한
            bool overMaxSpeed = Mathf.Abs(currentSpeed) >= maxSpeed;
            if (overMaxSpeed && !isBraking)
                movement = Vector3.zero;
            
            Vector3 newVelocity = rb.linearVelocity + movement * Time.fixedDeltaTime;
            newVelocity.y = rb.linearVelocity.y;
            
            if (groundPercent > 0f && !overMaxSpeed)
            {
                newVelocity = Vector3.ClampMagnitude(newVelocity, maxSpeed);
            }
            
            // 코스팅 (입력 없을 때 자동 감속)
            if (Mathf.Abs(moveInput) < 0.01f)
            {
                newVelocity = Vector3.MoveTowards(newVelocity, 
                    new Vector3(0, rb.linearVelocity.y, 0), 
                    Time.fixedDeltaTime * coastingDrag);
            }
            
            rb.linearVelocity = newVelocity;
        }
        
        // 조향
        if (groundPercent > 0f && Mathf.Abs(currentSpeed) > 0.5f)
        {
            float turningPower = turnInput * steerStrength;
            
            // Angular velocity로 회전
            var angularVel = rb.angularVelocity;
            angularVel.y = Mathf.MoveTowards(angularVel.y, turningPower * 0.4f, Time.fixedDeltaTime * 20f);
            
            // 후진 시 반대로
            if (!movingForward && !acceleratingForward)
                angularVel.y *= -1f;
            
            rb.angularVelocity = angularVel;
            
            // Velocity도 회전
            rb.linearVelocity = Quaternion.AngleAxis(
                turningPower * Mathf.Sign(localVel.z) * 25f * grip * Time.fixedDeltaTime, 
                transform.up
            ) * rb.linearVelocity;
        }
        
        // 앞바퀴 조향각 설정
        if (frontLeftWheel) frontLeftWheel.steerAngle = turnInput * 30f;
        if (frontRightWheel) frontRightWheel.steerAngle = turnInput * 30f;
    }
    
    void UpdateGroundedPercent()
    {
        int groundedCount = 0;
        WheelCollider[] wheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };
        
        foreach (var wheel in wheels)
        {
            if (wheel == null) continue;
            
            if (wheel.GetGroundHit(out WheelHit hit))
            {
                groundedCount++;
            }
        }
        
        groundPercent = (float)groundedCount / 4f;
    }
    
    void OnGUI()
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
        
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = Color.white;
        textStyle.fontSize = 14;
        
        Vector3 localVel = transform.InverseTransformVector(rb.linearVelocity);
        
        GUI.Box(new Rect(5, 5, 350, 100), "", boxStyle);
        GUI.Label(new Rect(10, 10, 330, 30), $"속도: {localVel.z:F1} / {topSpeed}", textStyle);
        GUI.Label(new Rect(10, 40, 330, 30), $"바닥 접촉: {groundPercent * 100f:F0}%", textStyle);
        GUI.Label(new Rect(10, 70, 330, 30), $"브레이크: {(brakeInput ? "ON" : "OFF")}", textStyle);
    }
    
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
}