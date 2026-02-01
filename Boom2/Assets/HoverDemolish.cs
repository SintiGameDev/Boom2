using EasyPeasyFirstPersonController;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{
    // Wir brauchen einen Collider, damit die Maus erkannt wird
    [RequireComponent(typeof(Collider))]
    public class HoverDemolish : MonoBehaviour
    {
        [Header("Demolition Settings")]
        [Tooltip("Container für die Bruchpunkte (Transform mit leeren GameObjects)")]
        [SerializeField] private Transform breakPointsParent;

        [Tooltip("Material für die Schnittflächen")]
        [SerializeField] private Material interiorMaterial;

        [Tooltip("Hier werden die Bruchstücke hineingespeichert")]
        [SerializeField] private Transform resultParent;

        // Instanz der Logik-Klasse
        private MeshDemolisher meshDemolisher;
        private bool hasRequiredScript = false;
        private MeshRenderer myRenderer;

        private void Start()
        {
            meshDemolisher = new MeshDemolisher();
            myRenderer = GetComponent<MeshRenderer>();

            // Prüfen, ob das spezifische Skript vorhanden ist
            // Wir gehen davon aus, dass du eine Klasse namens 'ObstacleCollision' hast
            if (GetComponent<ObstacleCollision>() != null)
            {
                hasRequiredScript = true;
            }
            else
            {
                // Optional: Warnung ausgeben
                // Debug.LogWarning($"GameObject {name} hat kein ObstacleCollision Skript und wird ignoriert.");
            }

            // Sicherheitscheck für resultParent
            if (resultParent == null)
            {
                Debug.LogError("Bitte weise dem HoverDemolish Skript einen 'Result Parent' (leeres GameObject) zu!");
                hasRequiredScript = false; // Skript deaktivieren um Fehler zu vermeiden
            }
        }

        private void OnMouseEnter()
        {
            // Abbruch, wenn das Collision-Skript fehlt oder wir bereits im Demolish-Modus sind (Sicherheit)
            if (!hasRequiredScript || !myRenderer.enabled) return;

            PerformDemolish();
        }

        private void OnMouseExit()
        {
            if (!hasRequiredScript) return;

            ResetToNormal();
        }

        private void PerformDemolish()
        {
            // 1. Sicherstellen, dass der Container leer ist
            ClearResultParent();

            // 2. Breakpoints sammeln
            List<Transform> breakPoints = new List<Transform>();
            if (breakPointsParent != null)
            {
                breakPoints = Enumerable.Range(0, breakPointsParent.childCount)
                                        .Select(x => breakPointsParent.GetChild(x))
                                        .ToList();
            }

            // 3. Demolish ausführen (nutzt deine MeshDemolisher Logik)
            // Wir übergeben 'gameObject', da wir dieses Objekt zerlegen wollen
            List<GameObject> shards = meshDemolisher.Demolish(gameObject, breakPoints, interiorMaterial);

            // 4. Bruchstücke konfigurieren
            foreach (GameObject shard in shards)
            {
                // In den Container verschieben
                shard.transform.SetParent(resultParent, true);

                // HIER: Skalierung auf 50% setzen wie gewünscht
                shard.transform.localScale = Vector3.one * 0.5f;
            }

            // 5. Das originale Mesh unsichtbar machen (NICHT SetActive(false), sonst geht OnMouseExit nicht mehr!)
            if (myRenderer != null) myRenderer.enabled = false;
        }

        private void ResetToNormal()
        {
            // 1. Alle Bruchstücke löschen
            ClearResultParent();

            // 2. Das originale Mesh wieder sichtbar machen
            if (myRenderer != null) myRenderer.enabled = true;
        }

        private void ClearResultParent()
        {
            if (resultParent == null) return;

            // Alle Kinder löschen
            foreach (Transform child in resultParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}