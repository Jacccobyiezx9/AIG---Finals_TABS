using TMPro;
using UnityEngine;

public class UnitCounter : MonoBehaviour
{
    // Static allows any TabsAI to access these without a direct reference
    public static int humanCount;
    public static int monsterCount;

    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private GameObject humanPanel;
    [SerializeField] private GameObject monsterPanel;

    void Update()
    {
        counterText.text = $"Humans: {humanCount} | Monsters: {monsterCount}";

        if (humanCount <= 0) 
            monsterPanel.SetActive(true);
        if (monsterCount <= 0) 
            humanPanel.SetActive(true);
    }
}