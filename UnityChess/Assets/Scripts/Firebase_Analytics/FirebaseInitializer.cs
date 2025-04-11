using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseDatabase Database;

    void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Database = FirebaseDatabase.DefaultInstance;
                Debug.Log("Firebase ready.");
            }
            else
            {
                Debug.LogError("Firebase init failed.");
            }
        });
    }
}

