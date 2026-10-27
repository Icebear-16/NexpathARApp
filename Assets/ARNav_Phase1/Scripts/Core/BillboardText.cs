using UnityEngine;

/// <summary>
/// Makes a TextMeshPro object always face the camera (billboard effect).
/// Attach to any GameObject with TextMeshPro to keep it readable from any angle.
/// </summary>
public class BillboardText : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            // Look at camera position, but keep upright
            transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                           targetCamera.transform.rotation * Vector3.up);
        }
    }
}