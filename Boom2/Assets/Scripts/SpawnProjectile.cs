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

        [Header("Schuss Einstellungen")]
        [Tooltip("Verz�gerung zwischen Sch�ssen in Sekunden")]
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

            // AudioSource f�r Sound-Effekte
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
            // Pr�fe ob geschossen werden soll
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

            // F�ge Rigidbody hinzu falls nicht vorhanden
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectile.AddComponent<Rigidbody>();
            }

            // Setze Geschwindigkeit in Blickrichtung
            rb.linearVelocity = playerCamera.forward * projectileSpeed;

            // Zerst�re das Projektil nach der Lebensdauer
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
            Debug.Log($"Projektil abgefeuert! Geschwindigkeit: {projectileSpeed}");
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
            }
        }
    }
}