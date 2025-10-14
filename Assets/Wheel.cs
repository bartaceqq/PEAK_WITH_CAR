using UnityEngine;

public class Wheel : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform wheelMesh;
    public bool wheelTurn;

    void Update()
    {
        UpdateWheel();
    }

    void UpdateWheel()
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);

        // Apply position and rotation to mesh
        wheelMesh.position = pos;

        // Adjust if your wheel meshes face the wrong direction
        // Try swapping axes if rotation looks wrong
        wheelMesh.rotation = rot * Quaternion.Euler(0, 0, 90);
        // ↑ You can try 90, -90, or 180 depending on your wheel orientation
    }
}