using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    private Transform target;
    private float damage;

    public void Initialize(Transform targetBuilding, float attackDamage)
    {
        target = targetBuilding;
        damage = attackDamage;
    }

    private void Update()
    {
        // Si el edificio ya fue destruido por otra tropa mientras la flecha volaba, destruimos el proyectil
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetCenter = target.position + Vector3.up * 1f;
        transform.position = Vector3.MoveTowards(transform.position, targetCenter, speed * Time.deltaTime);
        transform.LookAt(targetCenter);

        if (Vector3.Distance(transform.position, targetCenter) < 0.25f)
        {
            BuildingHealth health = target.GetComponentInParent<BuildingHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}