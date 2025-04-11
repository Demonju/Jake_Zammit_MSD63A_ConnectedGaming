using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityChess;
using UnityEngine;
using System.IO;

public class CreditsManager : MonoBehaviour
{
    public static CreditsManager Instance;

    [SerializeField] private TMP_Text creditsText;

    public int credits = 2000;

    // Default materials for white and black pieces
    [SerializeField] private Material whitePieceMaterial;
    [SerializeField] private Material blackPieceMaterial;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        credits = PlayerPrefs.GetInt("Credits", 2000);

        //Remove after testing
        PlayerPrefs.SetInt("Credits", 2000);
        PlayerPrefs.Save();
        credits = 2000;

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

    //Cleaning

    public void ResetPiecesToDefaultSkin()
    {
        // Ensure both default materials are assigned
        if (whitePieceMaterial == null || blackPieceMaterial == null)
        {
            Debug.LogError("Default materials are not assigned!");
            return;
        }

        VisualPiece[] pieces = FindObjectsOfType<VisualPiece>(); // Get all the chess pieces

        foreach (var piece in pieces)
        {
            MeshRenderer mr = piece.GetComponent<MeshRenderer>();

            if (mr != null)
            {
                // Check the piece's color and apply the appropriate material
                if (piece.PieceColor == Side.White)
                {
                    mr.material = whitePieceMaterial; // Apply white material
                }
                else if (piece.PieceColor == Side.Black)
                {
                    mr.material = blackPieceMaterial; // Apply black material
                }
            }
        }

        Debug.Log("Reset all pieces to their default skin.");
    }

    public void ClearEquippedSkin()
    {
        PlayerPrefs.DeleteKey("EquippedSkin");
        PlayerPrefs.Save();
        Debug.Log("Cleared saved equipped skin.");
    }

    public void ClearDownloadedSkins()
    {
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.png");

        foreach (string file in files)
        {
            File.Delete(file);
            Debug.Log("Deleted skin file: " + file);
        }
    }

    public void OnClearSkinsButtonClicked()
    {
        ClearDownloadedSkins();
        ClearEquippedSkin();
        ResetPiecesToDefaultSkin(); // Reset pieces to default skin when button is clicked
    }

}
