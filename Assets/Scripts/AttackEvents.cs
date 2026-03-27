using UnityEngine;

public class AttackEvents : MonoBehaviour
{
    private TabsAI tabs;
    [Header("Damage Settings")]
    [SerializeField] private float damageValue;

    [Header("Ranged Attack Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("Summon Settings")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private Transform[] summonPoints;

    [Header("AOE Settings")]
    [SerializeField] private GameObject[] aoeCol;

    void Awake()
    {
        tabs = GetComponent<TabsAI>();
    }

    public void CloseRangeAttack()
    {
        if (tabs == null || tabs.target == null) return;

        TabsAI enemy = tabs.target.GetComponent<TabsAI>();
        if (enemy == null || enemy.isDead) return;

        enemy.TakeDamage(damageValue);
    }

    public void ShootProjectile()
    {
        if (tabs == null || tabs.target == null) return;
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        Vector3 dir = (tabs.target.position - projectileSpawnPoint.position).normalized;
        dir.y = 0;

        Quaternion spawnRotation = Quaternion.LookRotation(dir);
        GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, spawnRotation);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir * projectileSpeed;
        }

        Projectile projScript = proj.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.damage = damageValue;
            projScript.owner = tabs.gameObject;
        }
    }
    public void SummonNPC()
    {
        if (npcPrefab == null || summonPoints == null || summonPoints.Length == 0) return;

        foreach (Transform point in summonPoints)
        {
            if (point == null) continue;

            GameObject newNPC = Instantiate(npcPrefab, point.position, point.rotation);

            TabsAI newTabs = newNPC.GetComponent<TabsAI>();
            if (newTabs != null)
            {
                newTabs.myTeam = tabs.myTeam;
                newTabs.patrolPoints = tabs.patrolPoints;
            }
        }
    }

    public void StartAOE()
    {
        if (aoeCol == null || aoeCol.Length == 0) return;

        foreach (GameObject aoe in aoeCol)
        {
            if (aoe == null) continue;

            AOE script = aoe.GetComponent<AOE>();
            if (script != null)
            {
                script.damage = damageValue; 
                script.owner = tabs.gameObject;   
            }
            Debug.Log(script.damage);
        }
    }

    public void EndAOE()
    {
        if (aoeCol == null || aoeCol.Length == 0) return;

        foreach (GameObject aoe in aoeCol)
        {
            if (aoe == null) continue;

            AOE script = aoe.GetComponent<AOE>();
            if (script != null)
            {
                script.damage = 0;                
            }
            Debug.Log(script.damage);
        }
    }
}