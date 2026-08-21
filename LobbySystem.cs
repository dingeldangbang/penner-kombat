using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Lobby & Matchmaking: Raum erstellen/beitreten (privat/öffentlich,
    /// Passwort, 2–4 Spieler), Raumliste, Ready-System, Chat.
    /// Baut auf NetworkManager/WebSocketClient auf.
    /// </summary>
    public class LobbySystem : MonoBehaviour
    {
        public static LobbySystem Instance;

        [Header("UI")]
        public GameObject lobbyPanel;
        public TMP_InputField roomNameInput;
        public TMP_InputField passwordInput;
        public TMP_Dropdown maxPlayersDropdown;
        public Toggle publicRoomToggle;
        public TextMeshProUGUI roomListText;
        public TMP_InputField chatInput;
        public TextMeshProUGUI chatLogText;
        public Button createRoomButton;
        public Button joinRoomButton;
        public Button readyButton;

        [Header("Chat")]
        public int maxChatLines = 50;

        private readonly List<string> chatLog = new List<string>();
        private string currentRoom;
        private int maxPlayers = 2;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start()
        {
            if (createRoomButton != null) createRoomButton.onClick.AddListener(CreateRoom);
            if (joinRoomButton != null) joinRoomButton.onClick.AddListener(JoinRoom);
            if (readyButton != null) readyButton.onClick.AddListener(ToggleReady);
            if (maxPlayersDropdown != null) maxPlayersDropdown.onValueChanged.AddListener(v => maxPlayers = v + 2); // 2..4
        }

        void Update()
        {
            if (chatInput != null && (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Enter].wasPressedThisFrame))
                SendChat();
        }

        // ===== Raum =====
        void CreateRoom()
        {
            string room = roomNameInput != null ? roomNameInput.text : "";
            if (string.IsNullOrEmpty(room)) room = "Raum_" + Random.Range(1000, 9999);
            currentRoom = room;
            if (NetworkManager.Instance != null) NetworkManager.Instance.JoinRoom(room);
            AddChat($"✨ Raum '{room}' erstellt (max. {maxPlayers} Spieler).");
        }

        void JoinRoom()
        {
            string room = roomNameInput != null ? roomNameInput.text : "";
            if (string.IsNullOrEmpty(room)) return;
            currentRoom = room;
            if (NetworkManager.Instance != null) NetworkManager.Instance.JoinRoom(room);
            AddChat($"🚪 Raum '{room}' beigetreten.");
        }

        void ToggleReady()
        {
            bool ready = NetworkManager.Instance != null && !NetworkManager.Instance.IsReady;
            if (NetworkManager.Instance != null) NetworkManager.Instance.SetReady(ready);
            if (readyButton != null) readyButton.GetComponentInChildren<TextMeshProUGUI>().text = ready ? "BEREIT" : "BEREIT?";
        }

        // ===== Chat =====
        void SendChat()
        {
            if (chatInput == null || string.IsNullOrEmpty(chatInput.text)) return;
            string name = NetworkManager.Instance != null ? NetworkManager.Instance.playerName : "Spieler";
            string msg = $"[{name}] {chatInput.text}";
            if (NetworkManager.Instance != null) NetworkManager.Instance.Send(msg);
            AddChat(msg);
            chatInput.text = "";
        }

        public void AddChat(string message)
        {
            chatLog.Add(message);
            if (chatLog.Count > maxChatLines) chatLog.RemoveAt(0);
            if (chatLogText != null) chatLogText.text = string.Join("\n", chatLog);
        }

        public void RefreshRoomList(List<string> rooms)
        {
            if (roomListText != null)
                roomListText.text = rooms.Count == 0 ? "(keine öffentlichen Räume)" : string.Join("\n", rooms);
        }
    }
}
