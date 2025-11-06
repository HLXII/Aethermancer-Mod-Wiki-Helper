

using UnityEngine;
using UnityEngine.InputSystem;

namespace WikiHelper;

public class InputHook : MonoBehaviour
{
    private UIController controller;

    public void Init(UIController controller)
    {
        this.controller = controller;
    }

    void Update()
    {
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            // Grabbing Cherufe
            var monster = MonsterManager.Instance.GetMonster(718);
            SkillScraper.RunScrape(monster);

            // TODO: Scrape Monsters
            // MonsterManager.Instance.AllMonsters

            // Scraping Equipment
            EquipmentScraper.RunScrape(monster);
        }
    }
}