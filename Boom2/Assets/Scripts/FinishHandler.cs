using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyPeasyFirstPersonController
{
    public class FinishHandler : MonoBehaviour
    {
        [Header("Collision Settings")]
        [Tooltip("Tag des Spielers (z.B. 'Player')")]
        public string playerTag = "Player";

        [Header("Death Settings")]
        [Tooltip("Verzögerung bevor das Level neustartet (in Sekunden)")]
        public float restartDelay = 2f;

        [Tooltip("Soll die Kamera beim Tod noch bewegbar sein?")]
        public bool freezeCameraOnDeath = true;

        [Header("Audio (Optional)")]
        [Tooltip("Sound der beim Aufprall abgespielt wird")]
        public AudioClip deathSound;

        private bool hasCollided = false;

        private void OnCollisionEnter(Collision collision)
        {
            // Prüfe ob das Objekt der Spieler ist
            if (!hasCollided && collision.gameObject.CompareTag(playerTag))
            {
                hasCollided = true;
                HandlePlayerDeath(collision.gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Alternative für Trigger-Collider
            if (!hasCollided && other.CompareTag(playerTag) && !CompareTag("Finish"))
            {
                hasCollided = true;
                HandlePlayerDeath(other.gameObject);
            }
            else if (!hasCollided && CompareTag("Finish") && !other.CompareTag("Dynamite"))
            {
                Debug.Log("Level Complete!");
                hasCollided = true;
                HandlePlayerDeath(other.gameObject);
            }
        }

        private void HandlePlayerDeath(GameObject player)
        {
            // Hole den FirstPersonController
            FirstPersonController fpsController = player.GetComponent<FirstPersonController>();

            if (fpsController != null)
            {
                // Deaktiviere den Controller
                fpsController.enabled = false;

                // Freezze den CharacterController
                if (fpsController.characterController != null)
                {
                    fpsController.characterController.enabled = false;
                }

                // Deaktiviere das Input
                if (fpsController.input != null)
                {
                    fpsController.input.enabled = false;
                }

                // Optional: Freezze die Kamera
                if (freezeCameraOnDeath && fpsController.playerCamera != null)
                {
                    // Deaktiviere die Kamera-Rotation
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            // Spiele Death Sound ab (falls vorhanden)
            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, player.transform.position);
            }

            // Starte den Restart-Timer
            Invoke(nameof(RestartLevel), restartDelay);

            // Optional: Debug-Nachricht
            Debug.Log("Player hit obstacle! Restarting in " + restartDelay + " seconds...");
        }

        private void RestartLevel()
        {
            // Lade die aktuelle Scene neu
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}