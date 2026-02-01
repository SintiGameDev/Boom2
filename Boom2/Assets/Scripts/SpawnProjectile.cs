using UnityEngine;

namespace EasyPeasyFirstPersonController
{
    public class SpawnProjectile : MonoBehaviour
    {
        [Header("Projektil Einstellungen")]
        [Tooltip("Das 3D Model Prefab das gespawned werden soll")]
        public GameObject projectilePrefab;

        [Tooltip("Geschwindigkeit des Projektils")]
        public float projectileSpeed = 20f;

        [Tooltip("Lebensdauer des Projektils in Sekunden (0 = unendlich)")]
        public float projectileLifetime = 5f;

        [Header("Spawn Position")]
        [Tooltip("Offset von der Kamera aus (x=rechts, y=hoch, z=vorne)")]
        public Vector3 spawnOffset = new Vector3(0, -0.2f, 0.5f);

        [Header("Fall-Abstoßung")]
        [Tooltip("Multiplizier für die Abstoßungskraft basierend auf Fallgeschwindigkeit")]
        public float fallRepulsionMultiplier = 0.5f;

        [Tooltip("Minimale Fallgeschwindigkeit bevor Abstoßung aktiv wird")]
        public float minFallSpeedForRepulsion = 5f;

        [Tooltip("Zufälligkeit der Abstoßrichtung (0 = gerade weg, 1 = sehr zufällig)")]
        [Range(0f, 1f)]
        public float repulsionRandomness = 0.3f;

        [Header("Schuss Einstellungen")]
        [Tooltip("Verzögerung zwischen Schüssen in Sekunden")]
        public float fireRate = 0.5f;

        [Tooltip("Kann der Spieler halten um zu feuern?")]
        public bool allowAutoFire = false;

        [Header("Audio (Optional)")]
        [Tooltip("Sound beim Abfeuern")]
        public AudioClip shootSound;

        [Header("Effekte (Optional)")]
        [Tooltip("Muzzle Flash Effekt beim Abfeuern")]
        public GameObject muzzleFlashEffect;

        [Tooltip("Dauer des Muzzle Flash in Sekunden")]
        public float muzzleFlashDuration = 0.1f;

        [Header("Referenzen")]
        [Tooltip("Die Kamera des Spielers (wird automatisch gefunden wenn leer)")]
        public Transform playerCamera;

        private FirstPersonController fpsController;
        private AudioSource audioSource;
        private float nextFireTime = 0f;

        void Start()
        {
            // Hole FPS Controller vom gleichen GameObject
            fpsController = GetComponent<FirstPersonController>();

            // Finde die Kamera automatisch wenn nicht zugewiesen
            if (playerCamera == null && fpsController != null)
            {
                playerCamera = fpsController.playerCamera;
            }

            // AudioSource für Sound-Effekte
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && shootSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Warnung wenn kein Prefab zugewiesen ist
            if (projectilePrefab == null)
            {
                Debug.LogWarning("SpawnProjectile: Kein Projektil-Prefab zugewiesen!");
            }
        }

        void Update()
        {
            // Prüfe ob geschossen werden soll
            bool wantsToShoot = allowAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

            if (wantsToShoot && Time.time >= nextFireTime && projectilePrefab != null)
            {
                ShootProjectile();
                nextFireTime = Time.time + fireRate;
            }
        }

        void ShootProjectile()
        {
            // Berechne Spawn Position (vor der Kamera mit Offset)
            Vector3 spawnPosition = playerCamera.position +
                                   playerCamera.right * spawnOffset.x +
                                   playerCamera.up * spawnOffset.y +
                                   playerCamera.forward * spawnOffset.z;

            // Spawne das Projektil
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, playerCamera.rotation);

            // Füge Rigidbody hinzu falls nicht vorhanden
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectile.AddComponent<Rigidbody>();
            }

            // WICHTIG: Ignoriere Kollision zwischen Projektil und Spieler
            Collider projectileCollider = projectile.GetComponent<Collider>();
            Collider playerCollider = GetComponent<Collider>();

            if (projectileCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, playerCollider, true);
            }

            // ZUSÄTZLICH: Ignoriere auch Character Controller
            if (projectileCollider != null && fpsController != null && fpsController.characterController != null)
            {
                Physics.IgnoreCollision(projectileCollider, fpsController.characterController, true);
            }

            // BERECHNE GESCHWINDIGKEIT MIT FALL-ABSTOSUNG
            Vector3 shootDirection = playerCamera.forward;
            float baseSpeed = projectileSpeed;

            // Hole Fallgeschwindigkeit vom FPS Controller
            if (fpsController != null)
            {
                float fallSpeed = Mathf.Abs(fpsController.moveDirection.y);

                // Nur wenn schnell genug gefallen wird
                if (fallSpeed > minFallSpeedForRepulsion && !fpsController.isGrounded)
                {
                    // Berechne Abstoßkraft basierend auf Fallgeschwindigkeit
                    float repulsionForce = fallSpeed * fallRepulsionMultiplier;

                    // Berechne Richtung von Spieler zum Projektil (radialer Vektor)
                    Vector3 playerToProjectile = (spawnPosition - transform.position).normalized;

                    // Füge Zufälligkeit hinzu für mehr Variation
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-repulsionRandomness, repulsionRandomness),
                        Random.Range(-repulsionRandomness, repulsionRandomness),
                        Random.Range(-repulsionRandomness, repulsionRandomness)
                    );

                    Vector3 repulsionDirection = (playerToProjectile + randomOffset).normalized;

                    // Kombiniere Schuss-Richtung mit Abstoßung
                    shootDirection = (shootDirection.normalized * baseSpeed + repulsionDirection * repulsionForce).normalized;

                    // Erhöhe Geschwindigkeit leicht durch die Abstoßung
                    baseSpeed += repulsionForce * 0.5f;

                    Debug.Log($"Fall-Abstoßung aktiv! Fallspeed: {fallSpeed:F2}, Repulsion: {repulsionForce:F2}");
                }
            }

            // Setze finale Geschwindigkeit
            rb.linearVelocity = shootDirection * baseSpeed;

            // Zerstöre das Projektil nach der Lebensdauer
            if (projectileLifetime > 0)
            {
                Destroy(projectile, projectileLifetime);
            }

            // Spiele Schuss-Sound ab
            if (shootSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shootSound);
            }

            // Zeige Muzzle Flash
            if (muzzleFlashEffect != null)
            {
                GameObject flash = Instantiate(muzzleFlashEffect, spawnPosition, playerCamera.rotation, playerCamera);
                Destroy(flash, muzzleFlashDuration);
            }

            // Debug Info
            Debug.Log($"Projektil abgefeuert! Geschwindigkeit: {baseSpeed}");
        }

        // Optional: Visualisierung im Editor
        void OnDrawGizmosSelected()
        {
            if (playerCamera != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 spawnPos = playerCamera.position +
                                  playerCamera.right * spawnOffset.x +
                                  playerCamera.up * spawnOffset.y +
                                  playerCamera.forward * spawnOffset.z;

                Gizmos.DrawWireSphere(spawnPos, 0.1f);
                Gizmos.DrawRay(spawnPos, playerCamera.forward * 2f);

                // Zeige Abstoßungsradius wenn gefallen wird
                if (fpsController != null && !fpsController.isGrounded)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, 2f);
                }
            }
        }
    }
}