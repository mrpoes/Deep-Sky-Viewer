using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;
    public Camera mainCamera;
    public float sensitivity = 200f;

    float xRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // vertical: rotate this object (camera pivot) up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        mainCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // horizontal: rotate the player body left/right
        playerBody.Rotate(Vector3.up * mouseX);
    }
}