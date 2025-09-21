using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // The target object to orbit around
    public Vector3 offset = Vector3.zero; // Offset from the target's position
    public float distance = 20.0f; // Initial distance from the target
    public float xSpeed = 250.0f; // Speed of the camera's horizontal rotation
    public float ySpeed = 120.0f; // Speed of the camera's vertical rotation
    public float yMinLimit = -20f; // Minimum vertical angle
    public float yMaxLimit = 80f; // Maximum vertical angle
    public float zoomSpeed = 10.0f;
    public float distanceMin = 3f; // Minimum zoom distance
    public float distanceMax = 30f; // Maximum zoom distance
    public float panSpeed = 0.3f; // Speed of the panning movement

    private float x = 0.0f; // Current horizontal angle
    private float y = 0.0f; // Current vertical angle
    private Vector3 previousPosition; // Previous mouse position for panning

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Prevent the Rigidbody from preventing the camera movement
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().freezeRotation = true;
        }
        distanceMax = 100f;
    }

    void LateUpdate()
    {
        if (target)
        {
            // Orbiting
            if (Input.GetMouseButton(1)) // Right mouse button for orbiting
            {
                x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            // Zooming
            distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            distance = Mathf.Clamp(distance, distanceMin, distanceMax);

            // Panning
            if (Input.GetMouseButton(0)) // Left mouse button for panning
            {
                Vector3 direction = previousPosition - Input.mousePosition;
                direction = new Vector3(direction.x * panSpeed, direction.y * panSpeed, 0);
                transform.Translate(direction);
            }
            previousPosition = Input.mousePosition;

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position + offset;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    // Utility function to clamp angles between a minimum and maximum value
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
