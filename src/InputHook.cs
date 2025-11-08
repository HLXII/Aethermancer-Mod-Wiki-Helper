

using UnityEngine;
using UnityEngine.InputSystem;

namespace WikiHelper;

public class InputHookManager : MonoBehaviour
{
    public static InputHookManager Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
        {
            // If an instance already exists
            Debug.LogWarning("Instance of InputHookManager exists, exiting early");
            return;
        }
        Debug.Log("Creating instance of InputHookManager");

        GameObject singletonObject = new GameObject(typeof(InputHookManager).Name);
        Instance = singletonObject.AddComponent<InputHookManager>();

        DontDestroyOnLoad(singletonObject);
    }

    public static void Cleanup()
    {
        if (Instance != null)
        {
            Debug.Log("Cleaning up instance of InputHookManager");
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            // Grabbing Cherufe
            var monster = MonsterManager.Instance.GetMonster(718);
            SkillScraper.RunScrape(monster);

            // Scraping Equipment
            EquipmentScraper.RunScrape(monster);
        }
    }
}