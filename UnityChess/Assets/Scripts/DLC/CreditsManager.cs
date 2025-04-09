using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreditsManager : MonoBehaviour
{
    public static CreditsManager Instance;

    [SerializeField] private TMP_Text creditsText;

    public int credits = 4000;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        credits = PlayerPrefs.GetInt("Credits", 4000);

        //Remove after testing
        PlayerPrefs.SetInt("Credits", 4000);
        PlayerPrefs.Save();
        credits = 4000;

    }

    void Update()
    {
        creditsText.text = credits.ToString();
    }

    public bool SpendingCredits(int amount)
    {
        if (credits >= amount)
        {
            credits -= amount;
            Debug.Log($"[CreditsManager] Spent {amount} coins. Remaining: {credits}");

            // Save after spending
            PlayerPrefs.SetInt("Credits", credits);
            PlayerPrefs.Save();
            return true;
        }
        else
        {
            Debug.LogWarning("[CreditsManager] Not enough credits!");
            return false;
        }
    }
}
