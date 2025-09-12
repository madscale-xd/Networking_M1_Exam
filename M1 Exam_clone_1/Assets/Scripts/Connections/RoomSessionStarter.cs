// RoomSessionStarter.cs
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class RoomSessionStarter : MonoBehaviourPunCallbacks
{
    [Header("Session")]
    [SerializeField] private string sessionSceneName = "SessionScene";

    [Tooltip("Reference for validation. Must also be placed under a Resources folder at runtime.")]
    [SerializeField] private GameObject playerPrefab;

    [Header("UI")]
    [SerializeField] private Button startSessionButton; // optional: only interactable for MasterClient

    void Start()
    {
        if (startSessionButton != null)
        {
            startSessionButton.onClick.AddListener(OnStartSessionClicked);
            startSessionButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    void OnDestroy()
    {
        if (startSessionButton != null) startSessionButton.onClick.RemoveListener(OnStartSessionClicked);
    }

    // Keep button interactability correct if master client changes
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (startSessionButton != null) startSessionButton.interactable = PhotonNetwork.IsMasterClient;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (startSessionButton != null) startSessionButton.interactable = PhotonNetwork.IsMasterClient;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (startSessionButton != null) startSessionButton.interactable = PhotonNetwork.IsMasterClient;
    }

    // Called by the UI button (or call programmatically)
    public void OnStartSessionClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the MasterClient can start the session.");
            return;
        }

        // Optional: set a room custom property so others can know which scene was requested
        Hashtable props = new Hashtable { { "scene", sessionSceneName } };
        PhotonNetwork.CurrentRoom?.SetCustomProperties(props);

        // This will cause all clients to load the scene because AutomaticallySyncScene = true
        PhotonNetwork.LoadLevel(sessionSceneName);
    }

#if UNITY_EDITOR
    // Editor-time warning to help you remember to put the prefab in Resources
    private void OnValidate()
    {
        if (playerPrefab != null)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(playerPrefab);
            if (!path.Contains("/Resources/"))
                Debug.LogWarning($"RoomSessionStarter: '{playerPrefab.name}' is not under a Resources folder. PhotonNetwork.Instantiate requires the prefab to be in Resources at runtime.");
        }
    }
#endif
}
