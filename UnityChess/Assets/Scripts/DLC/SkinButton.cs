using UnityChess;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinButton : MonoBehaviour
{
    [SerializeField] private string skinFileName; // e.g. "gold.png"
    [SerializeField] private int skinPrice = 500;

    private Button button;
    private TextMeshProUGUI buttonText; // Changed to TMP

    void Start()
    {
        button = GetComponent<Button>();
        // Find the TextMeshProUGUI component in the button's child
        buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText == null)
        {
            Debug.LogWarning("No TextMeshProUGUI component found in button's children!");
        }

        button.onClick.AddListener(OnClickBuySkin);
    }

    void OnClickBuySkin()
    {
        if (CreditsManager.Instance.SpendingCredits(skinPrice))
        {
            Debug.Log("Purchased " + skinFileName);

            // Get the player's side (White or Black)
            Side playerSide = GameManager.Instance.SideToMove;  // Assuming GameManager has the current player's side

            // Now pass both the skin file name and the player side
            SkinLoader.Instance.ApplySkinFromFirebase(skinFileName, playerSide);
            NetworkPlayer.LocalInstance.SetEquippedSkinServerRpc(skinFileName);

            // Disable the button and change the text to "Purchased"
            button.interactable = false;

            if (buttonText != null)
            {
                buttonText.text = "Owned";
            }
        }
        else
        {
            Debug.Log("Not enough credits!");
        }
    }
}





