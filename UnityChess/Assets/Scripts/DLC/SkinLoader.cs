using Firebase.Storage;
using System.IO;
using System.Threading.Tasks;
using UnityChess;
using UnityEngine;

public class SkinLoader : MonoBehaviour
{
    public static SkinLoader Instance;

    public Material CurrentMaterial { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(this);
    }

    public async Task ApplySkinFromFirebase(string fileName, Side playerSide)
    {
        string path = await DownloadSkinTexture(fileName);
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));  // Load the texture

            // Create a new material using the Standard shader for 3D models
            Material newMat = new Material(Shader.Find("Standard"));
            newMat.mainTexture = tex;  // Assign the downloaded texture to the material

            CurrentMaterial = newMat;  // Set the material globally
            PlayerPrefs.SetString("EquippedSkin", fileName);

            Debug.Log("Loaded new skin: " + newMat.name);  // Debug line to confirm material loading

            // Apply material to only the player's pieces
            ApplyMaterialToPlayerPieces(newMat, playerSide);
        }
    }


    public void ApplyMaterialToPlayerPieces(Material material, Side playerSide)
    {
        VisualPiece[] pieces = FindObjectsOfType<VisualPiece>(true); // Get all pieces
        foreach (var piece in pieces)
        {
            // Debug to check if the PieceColor matches the player's side
            Debug.Log($"Checking piece {piece.name} color: {piece.PieceColor}, Player's side: {playerSide}");

            if (piece.PieceColor == playerSide)  // Check if the piece belongs to the player
            {
                MeshRenderer mr = piece.GetComponent<MeshRenderer>();  // Get MeshRenderer for 3D pieces
                if (mr != null)
                {
                    Debug.Log($"Applying material to {piece.name} (Player's Piece)");  // Debug log
                    mr.material = material;  // Apply the material to the MeshRenderer
                }
            }
        }
    }



    private async Task<string> DownloadSkinTexture(string fileName)
    {
        string localPath = Path.Combine(Application.persistentDataPath, fileName);
        var storage = FirebaseStorage.DefaultInstance;
        var storageRef = storage.GetReference($"skins/{fileName}");

        try
        {
            await storageRef.GetFileAsync(localPath);
            Debug.Log($"Downloaded {fileName} to: {localPath}");

            // Use FileStream with a 'using' statement to ensure it gets properly released
            using (FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] fileBytes = new byte[fs.Length];
                await fs.ReadAsync(fileBytes, 0, fileBytes.Length);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileBytes);  // Load the texture
                return localPath;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to download skin: " + e);
            return null;
        }
    }


    public async void LoadEquippedSkin()
    {
        string fileName = PlayerPrefs.GetString("EquippedSkin", "");
        if (!string.IsNullOrEmpty(fileName))
        {
            // Get the player's side (White or Black)
            Side playerSide = GameManager.Instance.SideToMove;

            // Debug log to check if player side is correct
            Debug.Log("Player's side: " + playerSide);

            // Now call ApplySkinFromFirebase with both the fileName and playerSide
            await ApplySkinFromFirebase(fileName, playerSide);
        }
    }



}

