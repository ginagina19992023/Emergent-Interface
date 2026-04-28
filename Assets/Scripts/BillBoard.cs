using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Cache the main camera so we don't have to find it every frame
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Option 1: Full Billboarding (Faces the camera perfectly, tilts up and down)
        // We set the sprite's forward direction to match the camera's forward direction.
        transform.forward = mainCamera.transform.forward;

        /* // Option 2: Y-Axis Only (Classic Doom style - doesn't tilt up/down)
        // Uncomment this block and comment out Option 1 to use this instead.
        
        Vector3 targetPosition = new Vector3(
            mainCamera.transform.position.x, 
            transform.position.y, 
            mainCamera.transform.position.z
        );
        transform.LookAt(targetPosition);
        */
    }
}