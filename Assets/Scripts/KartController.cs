using UnityEngine;

public class KartController : MonoBehaviour
{
    public float speed = 10f;
    public float turnSpeed = 100f;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        float move = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;
        
        transform.Translate(0, 0, move);
        transform.Rotate(0, turn, 0);
    }
}