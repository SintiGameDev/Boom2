using Hanzzz.MeshDemolisher;
using UnityEngine;
using System.Collections.Generic; // Wichtig für List<>

public class Projectile : MonoBehaviour
{
    [Header("Explosion")]
    public GameObject explosionEffect;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;

    [Header("Kollisions-Filter")]
    [Tooltip("Tag des Spielers (Standard: Player)")]
    public string playerTag = "Player";

    [Tooltip("Soll das Projektil den Spieler ignorieren?")]
    public bool ignorePlayer = true;

    // Verhindert mehrfache Explosion
    private bool hasExploded = false;

    // Material für die Innenseiten
    public Material interiorMaterial;

    private void OnCollisionEnter(Collision collision)
    {
        // Wenn bereits explodiert, ignoriere weitere Kollisionen
        if (hasExploded)
        {
            return;
        }

        // Ignoriere Kollision mit dem Spieler
        if (ignorePlayer && collision.gameObject.CompareTag(playerTag))
        {
            Debug.Log("TNT ignoriert Kollision mit Spieler");
            return;
        }

        // Markiere als explodiert
        hasExploded = true;

        MeshDemolisher meshDemolisher = new MeshDemolisher();

        // Alle Objekte mit dem Tag "Points" finden
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Points");

        // Erstelle eine neue Liste für die Transforms
        List<Transform> breakPoints = new List<Transform>();

        // Füge alle gefundenen Punkte der Liste hinzu
        if (objs.Length > 0)
        {
            foreach (GameObject obj in objs)
            {
                breakPoints.Add(obj.transform);
            }
        }
        else
        {
            // Fallback: Wenn keine Punkte gefunden wurden, nimm die eigene Position
            breakPoints.Add(transform);
        }

        // KORREKTER AUFRUF: Wir übergeben die Liste
        //meshDemolisher.Demolish(collision.gameObject, breakPoints, interiorMaterial);

        // HIER WAR DER FEHLER: Die alte, falsche Zeile wurde entfernt.

        // Deaktiviere Collider um weitere Kollisionen zu verhindern
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Spawne Explosion Effekt
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Füge Explosion Force hinzu
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        // Zerstöre Projektil sofort
        if(collision.gameObject.tag != "Wall"){
            Destroy(collision.gameObject);
        }
      
        Destroy(gameObject);
    }
}