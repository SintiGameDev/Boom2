using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasyPeasyFirstPersonController
{
    public class ObstacleCollision : MonoBehaviour
    {
        [Header("Collision Settings")]
        [Tooltip("Tag des Spielers (z.B. 'Player')")]
        public string playerTag = "Player";

        [Header("Death Settings")]
        [Tooltip("Verzögerung bevor das Level neustartet (in Sekunden)")]
        public float restartDelay = 0f;

        [Tooltip("Soll die Kamera beim Tod noch bewegbar sein?")]
        public bool freezeCameraOnDeath = true;

        [Header("Fade Settings")]
        [Tooltip("Dauer des Fade-to-Black Effekts (in Sekunden)")]
        public float fadeDuration = 0.01f;

        [Tooltip("Farbe des Fade-Overlays")]
        public Color fadeColor = Color.black;

        [Header("Audio (Optional)")]
        [Tooltip("Sound der beim Aufprall abgespielt wird")]
        public AudioClip deathSound;

        private bool hasCollided = false;
        private Canvas fadeCanvas;
        private Image fadeImage;

        private void Awake()
        {
            // Erstelle Fade Canvas beim Start
            CreateFadeCanvas();
        }

        private void CreateFadeCanvas()
        {
            // Erstelle Canvas GameObject
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);

            // Füge Canvas Komponente hinzu
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // Sehr hohe Sorting Order damit es über allem ist

            // Füge Canvas Scaler hinzu für bessere Skalierung
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Füge Graphic Raycaster hinzu (optional)
            canvasObj.AddComponent<GraphicRaycaster>();

            // Erstelle Image für Fade
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0); // Start transparent

            // Setze RectTransform auf Fullscreen
            RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // Deaktiviere Canvas initial
            fadeCanvas.gameObject.SetActive(false);
        }

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
            if (!hasCollided && other.CompareTag(playerTag))
            {
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

            // Starte Fade-to-Black
            StartCoroutine(FadeAndRestart());

            // Optional: Debug-Nachricht
            Debug.Log("Player hit obstacle! Fading to black and restarting in " + restartDelay + " seconds...");
        }

        private System.Collections.IEnumerator FadeAndRestart()
        {
            // Aktiviere Canvas
            if (fadeCanvas != null)
            {
                fadeCanvas.gameObject.SetActive(true);
            }

            // Fade to Black
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // unscaledDeltaTime funktioniert auch bei Time.timeScale änderungen
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);

                if (fadeImage != null)
                {
                    fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                }

                yield return null;
            }

            // Stelle sicher, dass komplett schwarz
            if (fadeImage != null)
            {
                fadeImage.color = fadeColor;
            }

            // Warte restliche Zeit bis Restart
            float remainingTime = restartDelay - fadeDuration;
            if (remainingTime > 0)
            {
                yield return new WaitForSecondsRealtime(remainingTime);
            }

            // Lade Scene neu
            RestartLevel();
        }

        private void RestartLevel()
        {
            // Stelle Time.timeScale zurück (falls durch Slow-Motion verändert)
            Time.timeScale = 1f;

            // Lade die aktuelle Scene neu
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}