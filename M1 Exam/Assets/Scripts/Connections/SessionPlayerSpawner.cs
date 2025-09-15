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

    [Header("Assignable prefabs (index -> prefab)")]
    [Tooltip("Assign one prefab per character. The selector sets an index; this array maps that index to the prefab to spawn.")]
    [SerializeField] private GameObject[] prefabPrefabs = new GameObject[0];

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

        // --- 1) Determine chosen index (Photon player prop preferred, PlayerPrefs fallback) ---
        int chosenIndex = -1;

        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties != null)
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(SessionPlayerSpawnerCharacterKeys.PROP_CHARACTER_INDEX, out object objIndex))
            {
                if (objIndex is int) chosenIndex = (int)objIndex;
                else int.TryParse(objIndex?.ToString() ?? "-1", out chosenIndex);
            }
        }

        if (chosenIndex < 0 && PlayerPrefs.HasKey(SessionPlayerSpawnerCharacterKeys.PROP_CHARACTER_INDEX))
        {
            chosenIndex = PlayerPrefs.GetInt(SessionPlayerSpawnerCharacterKeys.PROP_CHARACTER_INDEX, -1);
        }

        // --- 2) Pick the prefab by index (preferred) ---
        GameObject selectedPrefab = null;
        string prefabNameToUse = null;

        if (chosenIndex >= 0 && prefabPrefabs != null && chosenIndex < prefabPrefabs.Length)
        {
            selectedPrefab = prefabPrefabs[chosenIndex];
            if (selectedPrefab != null) prefabNameToUse = selectedPrefab.name;
            else Debug.LogWarning($"SessionPlayerSpawner: prefabPrefabs[{chosenIndex}] is null. Falling back to inspector defaults.");
        }
        else if (chosenIndex >= 0)
        {
            Debug.LogWarning($"SessionPlayerSpawner: chosen index {chosenIndex} out of range (prefabPrefabs length {prefabPrefabs?.Length}). Falling back to inspector defaults.");
        }

        // --- 3) Fallback to inspector-assigned prefab or string name ---
        if (selectedPrefab == null)
        {
            if (playerPrefab != null)
            {
                selectedPrefab = playerPrefab;
                prefabNameToUse = playerPrefab.name;
            }
            else
            {
                prefabNameToUse = playerPrefabName;
            }
        }

        if (string.IsNullOrEmpty(prefabNameToUse))
        {
            Debug.LogError("SessionPlayerSpawner: No player prefab assigned and no selection found to spawn.");
            return;
        }

        // --- 4) Optional: quick Resources existence check to warn about Photon requirements ---
        var resCheck = Resources.Load<GameObject>(prefabNameToUse);
        if (resCheck == null)
        {
            Debug.LogWarning($"SessionPlayerSpawner: Prefab named '{prefabNameToUse}' was NOT found under any Resources folder. PhotonNetwork.Instantiate will fail unless you register a PrefabPool that can provide this prefab.");
        }

        // --- 5) Compute spawn position/rotation ---
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        if (spawnPoints != null && spawnPoints.Length > 0 && PhotonNetwork.LocalPlayer != null)
        {
            int idx = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            spawnPos = spawnPoints[idx].position;
            spawnRot = spawnPoints[idx].rotation;
        }

        // --- 6) Instantiate via Photon and pass the chosenIndex as instantiationData ---
        GameObject player = null;
        try
        {
            object[] instantiationData = new object[] { chosenIndex };
            player = PhotonNetwork.Instantiate(prefabNameToUse, spawnPos, spawnRot, 0, instantiationData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SessionPlayerSpawner: PhotonNetwork.Instantiate failed for '{prefabNameToUse}'. Exception: {ex.Message}\nEnsure the prefab is available to Photon (Resources or PrefabPool).");
        }

        if (player != null)
        {
            hasSpawned = true;
            Debug.Log($"SessionPlayerSpawner: Spawned local player '{prefabNameToUse}' with chosenIndex={chosenIndex}.");
        }
        else
        {
            Debug.LogError($"SessionPlayerSpawner: Failed to instantiate '{prefabNameToUse}'. Ensure the prefab is available to Photon (Resources or PrefabPool).");
        }
    }

#if UNITY_EDITOR
    // Editor-time checks to warn you if assigned prefabs are not in a Resources folder
    private void OnValidate()
    {
        if (prefabPrefabs != null)
        {
            for (int i = 0; i < prefabPrefabs.Length; i++)
            {
                var p = prefabPrefabs[i];
                if (p == null) continue;
                string path = AssetDatabase.GetAssetPath(p);
                if (!string.IsNullOrEmpty(path) && !path.Contains("/Resources/"))
                {
                    Debug.LogWarning($"SessionPlayerSpawner: prefabPrefabs[{i}] '{p.name}' is not inside a Resources folder. PhotonNetwork.Instantiate will fail at runtime unless you use a custom PrefabPool.");
                }
            }
        }

        if (playerPrefab != null)
        {
            string path = AssetDatabase.GetAssetPath(playerPrefab);
            if (!path.Contains("/Resources/"))
                Debug.LogWarning($"SessionPlayerSpawner: assigned playerPrefab '{playerPrefab.name}' is not under a Resources folder. PhotonNetwork.Instantiate will not find it at runtime unless you use a PrefabPool.");
            playerPrefabName = playerPrefab.name;
        }
    }
#endif
}
