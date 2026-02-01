using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Explosion")]
    public GameObject explosionEffect;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;

    private void OnCollisionEnter(Collision collision)
    {
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

        // Zerstöre Projektil
        Destroy(gameObject);
    }
}