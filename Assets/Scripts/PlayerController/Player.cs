using UnityEngine;

namespace PlayerController
{
    public class Player : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Gravity Settings")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedGravity = -2f;

        [Header("Camera Settings")]
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float lookSensitivity = 150f;
        [SerializeField] private float minPitch = -40f;
        [SerializeField] private float maxPitch = 70f;

        private CharacterController _characterController;
        private Animator _animator;

        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            _yaw = cameraTarget.eulerAngles.y;
            _pitch = cameraTarget.eulerAngles.x;
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

            _yaw += mouseX * lookSensitivity * Time.deltaTime;
            _pitch -= mouseY * lookSensitivity * Time.deltaTime;

            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            cameraTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
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

            // Animation
            float movementAmount = new Vector2(horizontal, vertical).magnitude;
            movementAmount = Mathf.Clamp01(movementAmount);

            _animator.SetFloat(
                "movement",
                movementAmount,
                0.1f,
                Time.deltaTime
            );

            // Rotation
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDirection);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            // Gravity
            if (_characterController.isGrounded)
            {
                if (_verticalVelocity < 0)
                    _verticalVelocity = groundedGravity;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            // Final Movement
            Vector3 finalMovement =
                moveDirection.normalized * movementSpeed;

            finalMovement.y = _verticalVelocity;

            _characterController.Move(
                finalMovement * Time.deltaTime
            );
        }
        
        private void OnDrawGizmos()
        {
            CharacterController cc = GetComponent<CharacterController>();

            if (cc == null)
                return;

            Vector3 center = transform.TransformPoint(cc.center);

            float radius = cc.radius;
            float height = Mathf.Max(cc.height, radius * 2f);

            Vector3 bottom = center - Vector3.up * (height * 0.5f - radius);

            // Step Offset visualization
            Gizmos.color = Color.cyan;
            Vector3 stepCenter = bottom + Vector3.up * cc.stepOffset;
            Gizmos.DrawWireSphere(stepCenter, radius);

            // Slope Limit visualization
            Gizmos.color = Color.magenta;

            float slopeLength = 5f;
            Vector3 slopeDirection =
                Quaternion.AngleAxis(cc.slopeLimit, transform.right) *
                transform.forward;

            Gizmos.DrawRay(bottom, slopeDirection * slopeLength);
        }
    }
}