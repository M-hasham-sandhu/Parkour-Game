using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float lookSensitivity = 150f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 70f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        yaw = cameraTarget.eulerAngles.y;
        pitch = cameraTarget.eulerAngles.x;
    }

    private void Update()
    {
        CameraLook();
        PlayerMovement();
    }

    private void CameraLook()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * lookSensitivity * Time.deltaTime;
        pitch -= mouseY * lookSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void PlayerMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraForward = cameraTarget.forward;
        Vector3 cameraRight = cameraTarget.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraForward * vertical +
            cameraRight * horizontal;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            transform.position +=
                moveDirection.normalized * (movementSpeed * Time.deltaTime);

            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}