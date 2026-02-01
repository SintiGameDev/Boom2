using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyPeasyFirstPersonController;
using Hanzzz.MeshDemolisher;
using UnityEngine;

public class HoverDemolish : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kamera die für das Raycast benutzt wird. Falls leer, wird Camera.main verwendet.")]
    public Camera rayCamera;

    [Tooltip("Material für die Innenflächen der demolierten Teile")]
    public Material interiorMaterial;

    [Header("Hover Settings")]
    [Range(0.01f, 2f)]
    public float highlightScale = 0.5f;
    [Tooltip("Maximale Raycast-Distanz")]
    public float maxDistance = 100f;

    private MeshDemolisher demolisher = new MeshDemolisher();
    private GameObject currentTarget;
    private List<GameObject> currentPieces;
    private MeshRenderer[] cachedRenderers;
    private int currentRequestId = 0;

    private void Update()
    {
        Camera cam = rayCamera != null ? rayCamera : Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            // Suche ObstacleCollision in getroffenen Objekt oder Parent
            ObstacleCollision obstacle = hit.collider.GetComponentInParent<ObstacleCollision>();
            if (obstacle != null)
            {
                GameObject hitObject = obstacle.gameObject;
                if (hitObject != currentTarget)
                {
                    // Neuer Hover-Target
                    ClearCurrentHighlight();
                    StartHoverHighlight(hitObject);
                }

                return; // early return - wir bleiben auf dem aktuellen Target
            }
        }

        // Kein gültiges Target getroffen -> ggf. aufräumen
        if (currentTarget != null)
        {
            ClearCurrentHighlight();
        }
    }

    private async void StartHoverHighlight(GameObject target)
    {
        if (target == null)
            return;

        currentTarget = target;
        currentRequestId++;
        int myRequestId = currentRequestId;

        // MeshRenderer(s) verstecken (sichtbar nach Ende wieder aktivieren)
        cachedRenderers = currentTarget.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in cachedRenderers)
        {
            r.enabled = false;
        }

        // Erzeuge temporäre Breakpoint-Transforms innerhalb des Mesh-Bounds
        GameObject tempParent = new GameObject($"_HoverBP_{target.GetInstanceID()}");
        List<Transform> breakPoints = CreateTemporaryBreakpoints(target, tempParent);

        // Start async Demolish (DemolishAsync liest die Positionen sofort vor dem await)
        Task<List<GameObject>> demolishTask = demolisher.DemolishAsync(target, breakPoints, interiorMaterial);

        // Temp-Objects können nun zerstört werden (DemolishAsync hat die Positionen bereits kopiert)
        Destroy(tempParent);

        List<GameObject> pieces = null;
        try
        {
            pieces = await demolishTask;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"HoverDemolish: DemolishAsync failed for '{target.name}': {ex.Message}");
        }

        // Falls während der asynchronen Arbeit das Target gewechselt wurde, verwerfe das Ergebnis
        if (myRequestId != currentRequestId)
        {
            if (pieces != null)
            {
                pieces.ForEach(p => { if (p != null) Object.Destroy(p); });
            }
            return;
        }

        if (pieces == null || pieces.Count == 0)
        {
            // Falls nichts erzeugt wurde, re-enable original renderer sofort
            foreach (var r in cachedRenderers)
                if (r != null) r.enabled = true;
            currentTarget = null;
            return;
        }

        // Parent die Teile an das Target und skaliere sie auf highlightScale
        currentPieces = pieces;
        foreach (var p in currentPieces)
        {
            if (p == null) continue;
            // Parent setzen (worldPositionStays = true um Lage beizubehalten), dann lokal skalieren
            p.transform.SetParent(currentTarget.transform, true);
            p.transform.localScale = Vector3.one * highlightScale;
        }
    }

    private void ClearCurrentHighlight()
    {
        currentRequestId++; // invalidiere laufende Anfragen

        // Zerstöre erzeugte Pieces
        if (currentPieces != null)
        {
            foreach (var p in currentPieces)
            {
                if (p != null)
                    Destroy(p);
            }
            currentPieces = null;
        }

        // Original-Renderer wieder aktivieren
        if (cachedRenderers != null)
        {
            foreach (var r in cachedRenderers)
            {
                if (r != null)
                    r.enabled = true;
            }
            cachedRenderers = null;
        }

        currentTarget = null;
    }

    private List<Transform> CreateTemporaryBreakpoints(GameObject target, GameObject parent)
    {
        List<Transform> res = new List<Transform>();
        if (target == null)
            return res;

        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return res;

        Mesh mesh = mf.sharedMesh;
        // Nutze Mesh-Bounds, erzeuge 4 Punkte: center + 3 kleine offsets
        Vector3 centerLocal = mesh.bounds.center;
        Vector3 ext = mesh.bounds.extents;
        Vector3[] localOffsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(ext.x * 0.5f, 0f, 0f),
            new Vector3(0f, ext.y * 0.5f, 0f),
            new Vector3(0f, 0f, ext.z * 0.5f)
        };

        for (int i = 0; i < localOffsets.Length; i++)
        {
            GameObject bp = new GameObject($"_bp_{i}");
            bp.transform.SetParent(parent.transform, false);
            Vector3 worldPos = target.transform.TransformPoint(centerLocal + localOffsets[i]);
            bp.transform.position = worldPos;
            res.Add(bp.transform);
        }

        return res;
    }

    private void OnDisable()
    {
        ClearCurrentHighlight();
    }
}