using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage;
    public GameObject owner;
    public float lifespan = 5f;

    private TabsAI ownerAI;

    void Start()
    {
        Destroy(gameObject, lifespan);

        if (owner != null)
            ownerAI = owner.GetComponent<TabsAI>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        if (other.gameObject == owner) return;

        TabsAI targetAI = other.GetComponent<TabsAI>();
        if (targetAI != null && !targetAI.isDead)
        {
            if (ownerAI != null && targetAI.myTeam != ownerAI.myTeam)
            {
                targetAI.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}