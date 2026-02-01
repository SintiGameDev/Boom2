using UnityEngine;
using UnityEngine.UI;

namespace EasyPeasyFirstPersonController
{
    public class FallVignetteEffect : MonoBehaviour
    {
        [Header("References")]
        public FirstPersonController fpsController;

        [Header("Vignette Settings")]
        public float minFallSpeedForEffect = 5f;
        public float maxFallSpeedForEffect = 30f;
        public float maxVignetteIntensity = 0.7f;

        private Canvas vignetteCanvas;
        private Image vignetteImage;

        void Start()
        {
            CreateVignetteOverlay();
        }

        void CreateVignetteOverlay()
        {
            // Canvas erstellen
            GameObject canvasObj = new GameObject("VignetteCanvas");
            canvasObj.transform.SetParent(transform);

            vignetteCanvas = canvasObj.AddComponent<Canvas>();
            vignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            vignetteCanvas.sortingOrder = 100;

            // Image erstellen
            GameObject imageObj = new GameObject("VignetteImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            vignetteImage = imageObj.AddComponent<Image>();

            // WICHTIG: Nutze ein Vignette Sprite oder erstelle eins
            // Für jetzt: Einfacher radialer Gradient von transparent zu schwarz
            vignetteImage.color = new Color(0, 0, 0, 0);

            // Fullscreen
            RectTransform rect = vignetteImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        void Update()
        {
            if (fpsController == null || vignetteImage == null)
                return;

            float fallSpeed = Mathf.Abs(fpsController.moveDirection.y);

            if (fallSpeed > minFallSpeedForEffect)
            {
                float normalized = Mathf.Clamp01(
                    (fallSpeed - minFallSpeedForEffect) / (maxFallSpeedForEffect - minFallSpeedForEffect)
                );

                float targetAlpha = normalized * maxVignetteIntensity;
                float currentAlpha = vignetteImage.color.a;

                // Smooth transition
                float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 5f);
                vignetteImage.color = new Color(0, 0, 0, newAlpha);
            }
            else
            {
                // Fade out
                float currentAlpha = vignetteImage.color.a;
                float newAlpha = Mathf.Lerp(currentAlpha, 0, Time.deltaTime * 5f);
                vignetteImage.color = new Color(0, 0, 0, newAlpha);
            }
        }
    }
}