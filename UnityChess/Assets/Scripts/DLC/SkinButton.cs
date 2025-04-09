using UnityChess;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : MonoBehaviour
{
    [SerializeField] private string skinFileName; // e.g. "gold.png"
    [SerializeField] private int skinPrice = 500;

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
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
        }
    }
}


