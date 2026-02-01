using UnityEngine;

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
            return; // Beende die Methode, keine Explosion
        }

        // Markiere als explodiert SOFORT (vor allen anderen Aktionen)
        hasExploded = true;

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
        Destroy(gameObject);
    }
}