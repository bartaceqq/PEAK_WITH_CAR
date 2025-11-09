using UnityEngine;

public class SimpleCar : MonoBehaviour
{
    public float acceleration = 20f;
    public float maxSpeed = 40f;
    public float turnSpeed = 100f;
    public float traction = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // lower center of mass for stability
    }

    void FixedUpdate()
    {
        // Get input
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // Calculate forward velocity (no WheelColliders)
        Vector3 forward = transform.forward * moveInput * acceleration;

        // Limit top speed
        if (rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(forward, ForceMode.Acceleration);

        // Turn based on speed
        float turnAmount = turnInput * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnOffset = Quaternion.Euler(0, turnAmount, 0);
        rb.MoveRotation(rb.rotation * turnOffset);

        // Add traction (reduces drifting)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x /= (1f + Time.fixedDeltaTime * traction); // dampen sideways velocity
        rb.linearVelocity = transform.TransformDirection(localVel);
    }
}