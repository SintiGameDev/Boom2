namespace EasyPeasyFirstPersonController
{
    using UnityEngine;

    public class PlayerFallState : PlayerBaseState
    {
        public PlayerFallState(FirstPersonController currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) { }

        public override void EnterState()
        {
            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0;
            ctx.targetTilt = 0;
        }

        public override void UpdateState()
        {
            ApplyGravity();
            HandleAirMovement();
            UpdateFallingFOV();  // NEUE METHODE
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Setze FOV zurück auf normal beim Verlassen
            ctx.targetFov = ctx.normalFov;
        }

        public override void CheckSwitchStates()
        {
            if (ctx.isGrounded && ctx.moveDirection.y <= 0)
            {
                SwitchState(factory.Grounded());
            }
            else if (ctx.CheckLedge(out _))
            {
                SwitchState(factory.LedgeGrab());
            }
            else if (ctx.isInWater)
            {
                SwitchState(factory.Swimming());
            }
        }

        private void ApplyGravity()
        {
            // Verwende die dynamische Schwerkraft vom Controller (berücksichtigt Slow-Fall)
            ctx.moveDirection.y -= ctx.GetCurrentGravity() * Time.deltaTime;
            ctx.characterController.Move(new Vector3(0, ctx.moveDirection.y, 0) * Time.deltaTime);
        }

        private void HandleAirMovement()
        {
            Vector2 input = ctx.input.moveInput;
            Vector3 move = ctx.transform.right * input.x + ctx.transform.forward * input.y;
            ctx.characterController.Move(move * ctx.walkSpeed * 0.8f * Time.deltaTime);
        }

        private void UpdateFallingFOV()
        {
            // Berechne FOV basierend auf Fallgeschwindigkeit
            float fallSpeed = Mathf.Abs(ctx.moveDirection.y);

            // Minimale Fallgeschwindigkeit bevor FOV sich ändert
            float minFallSpeed = 5f;

            // Maximale Fallgeschwindigkeit für maximales FOV
            float maxFallSpeed = 30f;

            // Maximales FOV beim Fallen (erhöht um 20 Grad vom normalFov)
            float maxFallFov = ctx.normalFov + 20f;

            if (fallSpeed > minFallSpeed)
            {
                // Normalisiere die Fallgeschwindigkeit zwischen 0 und 1
                float normalizedSpeed = Mathf.Clamp01((fallSpeed - minFallSpeed) / (maxFallSpeed - minFallSpeed));

                // Interpoliere FOV basierend auf Geschwindigkeit
                ctx.targetFov = Mathf.Lerp(ctx.normalFov, maxFallFov, normalizedSpeed);
            }
            else
            {
                // Langsamer Fall = normales FOV
                ctx.targetFov = ctx.normalFov;
            }
        }
    }
}