// SessionPlayerSpawner.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SessionPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Prefab (drag a prefab here or set name)")]
    [Tooltip("Optional: assign the player prefab directly in the inspector. It still needs to be available to Photon (i.e., inside a Resources folder) unless you set up a custom PrefabPool.")]
    [SerializeField] private GameObject playerPrefab; // optional inspector assignment

    [Tooltip("Name of the player prefab file located in Assets/Resources (without path). Used if no prefab is assigned.")]
    [SerializeField] private string playerPrefabName = "PlayerPrefab";

    [Header("Optional spawn points (order used by ActorNumber to distribute)")]
    [SerializeField] private Transform[] spawnPoints;

    // Instance guard so we don't spawn multiple times if Start + OnJoinedRoom both fire
    private bool hasSpawned = false;

    void Start()
    {
        TrySpawnPlayer();
    }

    public override void OnJoinedRoom()
    {
        TrySpawnPlayer();
    }

    private void TrySpawnPlayer()
    {
        if (hasSpawned) return;
        if (!PhotonNetwork.InRoom) return;

        // choose name (prefer explicit prefab if assigned)
        string prefabNameToUse = (playerPrefab != null) ? playerPrefab.name : playerPrefabName;
        if (string.IsNullOrEmpty(prefabNameToUse))
        {
            Debug.LogError("SessionPlayerSpawner: No player prefab assigned and playerPrefabName is empty.");
            return;
        }

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            spawnPos = spawnPoints[idx].position;
            spawnRot = spawnPoints[idx].rotation;
        }

        GameObject player = null;
        try
        {
            player = PhotonNetwork.Instantiate(prefabNameToUse, spawnPos, spawnRot);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SessionPlayerSpawner: PhotonNetwork.Instantiate failed for '{prefabNameToUse}'. Exception: {ex.Message}\nMake sure the prefab exists in a Resources folder or register it with a Photon PrefabPool.");
        }

        if (player != null)
        {
            hasSpawned = true;
        }
        else
        {
            Debug.LogError($"SessionPlayerSpawner: Failed to instantiate '{prefabNameToUse}'. Ensure the prefab is available to Photon (Resources or PrefabPool).");
        }
    }

#if UNITY_EDITOR
    // Editor-time checks and convenience: sync the name and warn if not in Resources
    private void OnValidate()
    {
        if (playerPrefab != null)
        {
            string path = AssetDatabase.GetAssetPath(playerPrefab);
            if (!path.Contains("/Resources/"))
                Debug.LogWarning($"SessionPlayerSpawner: Assigned prefab '{playerPrefab.name}' is not under a Resources folder. PhotonNetwork.Instantiate requires prefabs in Resources at runtime unless you use a custom PrefabPool.");

            // keep the string name in sync with the assigned prefab
            playerPrefabName = playerPrefab.name;
        }
    }
#endif
}
