using UnityEngine;

public class AOE : MonoBehaviour
{
    public float damage;
    public GameObject owner;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        TabsAI targetAI = other.GetComponent<TabsAI>();
        if (targetAI != null && !targetAI.isDead)
        {
            TabsAI ownerAI = owner.GetComponent<TabsAI>();
            if (ownerAI != null && targetAI.myTeam != ownerAI.myTeam)
            {
                targetAI.TakeDamage(damage);
            }
        }
    }
}