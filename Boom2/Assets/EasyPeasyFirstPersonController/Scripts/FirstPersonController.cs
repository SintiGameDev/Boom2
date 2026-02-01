namespace EasyPeasyFirstPersonController
{
    using UnityEngine;

    public partial class FirstPersonController : MonoBehaviour
    {
        [Header("Settings")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 5f;
        public float crouchSpeed = 1.5f;
        public float jumpSpeed = 4f;
        public float gravity = 9.81f;
        public float slideDuration = 0.7f;
        public float slideSpeed = 6f;
        public float mouseSensitivity = 2f;
        public float strafeTiltAmount = 2f;

        [Header("Slow Fall Settings")]
        [Tooltip("Reduzierte Schwerkraft beim Halten der Leertaste während des Falls")]
        public float slowFallGravity = 3f;
        [Tooltip("Wie schnell die Schwerkraft zwischen normal und slow wechselt")]
        public float gravityTransitionSpeed = 5f;
        [Tooltip("Minimale Fall-Geschwindigkeit um Slow-Fall zu aktivieren")]
        public float minFallSpeedForSlowFall = -2f;

        [Header("Time Slow Settings")]
        [Tooltip("Globale Zeit-Skalierung, während Leertaste gehalten wird")]
        public float timeSlowScale = 0.5f;
        [Tooltip("Maximale Gesamtzeit in Sekunden, die Slow-Time genutzt werden darf (z.B. 300 = 5min)")]
        public float maxSlowTimeSeconds = 300f;

        [Header("References")]
        public Transform playerCamera;
        public Transform cameraParent;
        public Transform groundCheck;
        public LayerMask groundMask;

        [HideInInspector] public CharacterController characterController;
        [HideInInspector] public InputManagerOld input;
        [HideInInspector] public Vector3 moveDirection;
        [HideInInspector] public bool isGrounded;

        private PlayerBaseState currentState;
        private PlayerStateFactory states;
        private float xRotation = 0f;
        private float currentTilt;
        private float tiltVelocity;
        private float currentGravity;

        public PlayerBaseState CurrentState { get => currentState; set => currentState = value; }

        [Header("Visual Settings")]
        public float normalFov = 60f;
        public float sprintFov = 75f;
        public float slideFovBoost = 5f;
        public float fovChangeSpeed = 8f;
        public float bobAmount = 0.001f;
        public float bobSpeed = 10f;
        public float recoilReturnSpeed = 5f;

        [HideInInspector] public Camera cam;
        [HideInInspector] public float targetFov;
        [HideInInspector] public float currentBobIntensity;
        [HideInInspector] public float currentBobSpeed;
        [HideInInspector] public float targetTilt;

        private float bobTimer;
        private float fovVelocity;
        private float originalCamY;

        [Header("Height Settings")]
        public float standingCameraHeight = 1.75f;
        public float crouchingCameraHeight = 1f;
        public float crouchingCharacterControllerHeight = 1f;
        [HideInInspector] public float standingCharacterControllerHeight = 1.8f;
        [HideInInspector] public Vector3 standingCharacterControllerCenter = new Vector3(0, 0.9f, 0);
        [HideInInspector] public float targetCameraY;

        [Header("Ledge Settings")]
        public LayerMask ledgeLayer;
        public float ledgeDetectionDistance = 1f;
        private float landingMomentum;

        [Header("Swimming Settings")]
        public float swimSpeed = 4f;
        public float swimSprintSpeed = 6f;
        public float waterDrag = 2f;
        public LayerMask waterMask;
        [HideInInspector] public bool isInWater;

        [Header("Debug")]
        public bool currentStateDebug = true;
        public bool showSlowFallDebug = false;

        // Time slow runtime fields
        [HideInInspector] public float remainingSlowTimeSeconds;
        private bool timeSlowActive;
        private float originalFixedDeltaTime;

        void OnGUI()
        {
            if (currentState != null && Application.isEditor && currentStateDebug)
                GUILayout.Label("Current State: " + currentState.GetType().Name);

            if (showSlowFallDebug && Application.isEditor)
            {
                GUILayout.Label("Current Gravity: " + currentGravity.ToString("F2"));
                GUILayout.Label("Vertical Speed: " + moveDirection.y.ToString("F2"));
                GUILayout.Label("Is Grounded: " + isGrounded);
                GUILayout.Label("SlowTime Remaining (s): " + remainingSlowTimeSeconds.ToString("F1"));
                GUILayout.Label("TimeSlow Active: " + timeSlowActive);
            }
        }

        private void Awake()
        {
            cam = playerCamera.GetComponent<Camera>();
            targetFov = normalFov;
            targetCameraY = standingCameraHeight;
            originalCamY = standingCameraHeight;
            currentGravity = gravity;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            characterController = GetComponent<CharacterController>();
            standingCharacterControllerHeight = characterController.height;
            standingCharacterControllerCenter = characterController.center;
            input = GetComponent<InputManagerOld>();
            states = new PlayerStateFactory(this);

            currentState = states.Grounded();
            currentState.EnterState();

            // Time slow initialization
            remainingSlowTimeSeconds = maxSlowTimeSeconds;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            timeSlowActive = false;
        }

        private void Update()
        {
            // Time slow handling first so behavior in this frame uses the intended timescale
            HandleTimeSlow();

            isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask, QueryTriggerInteraction.Ignore);

            HandleSlowFall();
            currentState.UpdateState();
            HandleRotation();
            UpdateVisuals();
        }

        private void HandleTimeSlow()
        {
            // Wenn Leertaste gehalten wird und noch Fuel vorhanden ist -> Zeit verlangsamen
            bool wantSlow = input != null && input.jumpInput && remainingSlowTimeSeconds > 0f;

            if (wantSlow)
            {
                timeSlowActive = true;
                // Zeitverbrauch in Realzeit (unscaled), damit Time.timeScale die Verbrauchsrate nicht beeinflusst
                remainingSlowTimeSeconds -= Time.unscaledDeltaTime;
                if (remainingSlowTimeSeconds <= 0f)
                {
                    remainingSlowTimeSeconds = 0f;
                    timeSlowActive = false;
                }
            }
            else
            {
                timeSlowActive = false;
            }

            // Smoothes Lerp für angenehmere Übergänge
            float targetScale = timeSlowActive ? timeSlowScale : 1f;
            // Lerp basierend auf unscaledDeltaTime damit Übergang unabhängig von aktuellen timescale ist
            Time.timeScale = Mathf.Lerp(Time.timeScale, targetScale, Time.unscaledDeltaTime * 8f);

            // FixedDeltaTime anpassen um Physik stabil zu halten
            Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
        }

        private void HandleSlowFall()
        {
            // Prüfe ob der Spieler fällt und nicht am Boden ist
            bool isFalling = !isGrounded && moveDirection.y < minFallSpeedForSlowFall;

            // Prüfe ob die Leertaste gehalten wird
            bool slowFallActive = isFalling && input.jumpInput;

            // Ziel-Schwerkraft basierend auf Slow-Fall Status
            float targetGravity = slowFallActive ? slowFallGravity : gravity;

            // Smooth Übergang zwischen normaler und reduzierter Schwerkraft
            currentGravity = Mathf.Lerp(currentGravity, targetGravity, Time.deltaTime * gravityTransitionSpeed);
        }

        // Diese Methode kann von deinen States verwendet werden
        public float GetCurrentGravity()
        {
            return currentGravity;
        }

        private void HandleRotation()
        {
            float mouseX = input.lookInput.x * mouseSensitivity;
            float mouseY = input.lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            float strafeTilt = -input.moveInput.x * strafeTiltAmount;
            float combinedTargetTilt = targetTilt + strafeTilt;

            currentTilt = Mathf.SmoothDamp(currentTilt, combinedTargetTilt, ref tiltVelocity, 0.1f);

            playerCamera.localRotation = Quaternion.Euler(xRotation, 0, currentTilt);
        }

        public void UpdateVisuals()
        {
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, 1f / fovChangeSpeed);

            landingMomentum = Mathf.Lerp(landingMomentum, 0, Time.deltaTime * 10f);
            float newY = Mathf.Lerp(cameraParent.localPosition.y, targetCameraY, Time.deltaTime * 8f);

            if (characterController.velocity.magnitude > 0.1f && isGrounded)
            {
                bobTimer += Time.deltaTime * currentBobSpeed;
                float bobOffset = Mathf.Sin(bobTimer) * currentBobIntensity;
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, newY + bobOffset, cameraParent.localPosition.z);
            }
            else
            {
                bobTimer = 0;
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, newY, cameraParent.localPosition.z);
            }
        }

        public bool HasCeiling()
        {
            float radius = characterController.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (characterController.height - radius);
            float checkDistance = standingCharacterControllerHeight - characterController.height + 0.1f;

            return Physics.SphereCast(origin, radius, Vector3.up, out _, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        public bool CheckLedge(out Vector3 climbPosition)
        {
            climbPosition = Vector3.zero;
            RaycastHit wallHit;
            Vector3 wallOrigin = transform.position + Vector3.up * 1.5f;

            if (Physics.Raycast(wallOrigin, transform.forward, out wallHit, ledgeDetectionDistance, ledgeLayer, QueryTriggerInteraction.Ignore))
            {
                // NEUE PRÜFUNG: Ignoriere Objekte mit Tag "Dynamite"
                if (wallHit.collider.CompareTag("Dynamite"))
                {
                    return false; // Kein Ledge wenn es Dynamit ist
                }

                Vector3 ledgeOrigin = wallOrigin + Vector3.up * 0.6f + transform.forward * 0.2f;
                RaycastHit ledgeHit;

                if (!Physics.Raycast(ledgeOrigin, transform.forward, 0.5f, groundMask))
                {
                    if (Physics.Raycast(ledgeOrigin + transform.forward * 0.4f, Vector3.down, out ledgeHit, 1f, groundMask))
                    {
                        // ZUSÄTZLICHE PRÜFUNG: Auch hier Dynamit ignorieren
                        if (ledgeHit.collider.CompareTag("Dynamite"))
                        {
                            return false;
                        }

                        climbPosition = ledgeHit.point + Vector3.up * 1f;
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterMask) != 0)
            {
                isInWater = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterMask) != 0)
            {
                isInWater = false;
            }
        }

        private void OnDisable()
        {
            // Stelle sicher, dass beim Deaktivieren das TimeScale zurückgesetzt wird
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }
}