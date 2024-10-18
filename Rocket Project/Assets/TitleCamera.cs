using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleCamera : MonoBehaviour
{
    public float speed = 1.0f;
    public enum CameraDirection
    {
        Left,
        Right
    }
    public CameraDirection direction = CameraDirection.Left;
    Transform cameraTransform;

    // Start is called before the first frame update
    void Start()
    {
        cameraTransform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the camera
        if (direction == CameraDirection.Left)
        {
            cameraTransform.Rotate(Vector3.up, -speed * Time.deltaTime);
        }
        else
        {
            cameraTransform.Rotate(Vector3.up, speed * Time.deltaTime);
        }
    }
}
