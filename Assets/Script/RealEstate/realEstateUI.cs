using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RealEstateUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RealEstateManager manager;
    [SerializeField] private TextMeshProUGUI houseNameText;
    [SerializeField] private TextMeshProUGUI housePriceText;
    [SerializeField] private TextMeshProUGUI ownerHouseText;

    [Header("Auto Refresh")]
    [SerializeField] private float uiRefreshEvery = 0.5f;

    private int currentIndex = -1;

    private void Start()
    {
        if (!manager) manager = FindObjectOfType<RealEstateManager>();
        InvokeRepeating(nameof(Refresh), 0.2f, uiRefreshEvery);

        if (manager && manager.Houses != null)
            manager.Houses.OnListChanged += _ => Refresh();
    }

    private void OnDestroy()
    {
        if (manager && manager.Houses != null)
            manager.Houses.OnListChanged -= _ => Refresh();
    }

    public void House1() => SetIndex(0);
    public void House2() => SetIndex(1);
    public void House3() => SetIndex(2);
    public void House4() => SetIndex(3);

    private void SetIndex(int idx)
    {
        currentIndex = idx;
        Refresh();
    }

    public void Buy()
    {
        if (!manager || currentIndex < 0) return;
        manager.BuyHouseServerRpc(currentIndex);
    }

    public void Sell()
    {
        if (!manager || currentIndex < 0) return;
        manager.SellHouseServerRpc(currentIndex);
    }

    // ✅ ปุ่มปล่อยเช่า/ยกเลิกปล่อยเช่า
    public void ForRentOn()
    {
        if (!manager || currentIndex < 0) return;
        manager.ForRentServerRpc(currentIndex, true);
    }
    public void ForRentOff()
    {
        if (!manager || currentIndex < 0) return;
        manager.ForRentServerRpc(currentIndex, false);
    }

    private void Refresh()
    {
        if (!houseNameText || !housePriceText || !ownerHouseText) return;

        if (!manager || manager.Houses == null || manager.Houses.Count == 0 ||
            currentIndex < 0 || currentIndex >= manager.Houses.Count)
        {
            houseNameText.text = "";
            housePriceText.text = "";
            ownerHouseText.text = "";
            return;
        }

        var rec = manager.Houses[currentIndex];

        houseNameText.text = $"House {currentIndex + 1}";
        housePriceText.text = $"ราคา: {rec.price:N0}";

        string ownerLabel = "ไม่มีเจ้าของ";

        if (rec.ownerClientId != ulong.MaxValue)
        {
            ownerLabel = GetOwnerLabel(rec.ownerClientId);
        }

        string rentFlag = rec.forRent ? "ใช่" : "ไม่";
        ownerHouseText.text = $"เจ้าของ : {ownerLabel}  |  ปล่อยเช่า : {rentFlag}";
    }

    /// <summary>
    /// คืนชื่อเจ้าของบ้าน จาก clientId
    /// - ถ้าเป็นเราเอง → ใช้ชื่อเรา
    ///   (ดึงจาก LobbyManager ก่อน, ถ้าไม่มีใช้จาก PlayerData / PlayerPrefs)
    /// - ถ้าเป็นคนอื่น → ถ้าเจอใน LobbyManager ใช้ playerName
    /// - ถ้าไม่เจอ → fallback เป็น "P{clientId}"
    /// </summary>
    private string GetOwnerLabel(ulong ownerClientId)
    {
        var nm = NetworkManager.Singleton;

        // เตรียมตัวแปรไว้เก็บชื่อจาก LobbyManager (ใช้ได้ทั้งของเราและของคนอื่น)
        string nameFromLobby = null;

        var lobby = LobbyManager.Instance;
        if (lobby != null && lobby.players != null)
        {
            for (int i = 0; i < lobby.players.Count; i++)
            {
                var p = lobby.players[i];
                if (p.clientId == ownerClientId)
                {
                    string n = p.playerName.ToString();
                    if (!string.IsNullOrWhiteSpace(n))
                    {
                        nameFromLobby = n;
                    }
                    break;
                }
            }
        }

        // ถ้าเป็นบ้านของเราเอง (LocalClient)
        if (nm != null && nm.LocalClientId == ownerClientId)
        {
            // 1) ถ้าใน LobbyManager มีชื่อเราอยู่แล้ว → ใช้อันนี้ก่อน
            if (!string.IsNullOrWhiteSpace(nameFromLobby))
                return nameFromLobby + " (ฉัน)";

            // 2) ลองดึงจาก PlayerData (ดาต้ากลางของเรา)
            if (PlayerData.Instance != null &&
                !string.IsNullOrWhiteSpace(PlayerData.Instance.playerName))
            {
                return PlayerData.Instance.playerName + " (ฉัน)";
            }

            // 3) fallback จาก PlayerPrefs ("player_name")
            string prefsName = PlayerPrefs.GetString("player_name", "");
            if (!string.IsNullOrWhiteSpace(prefsName))
                return prefsName + " (ฉัน)";

            // 4) fallback สุดท้าย
            return $"P{ownerClientId}";
        }

        // ถ้าเป็นคนอื่น → ใช้ชื่อจาก LobbyManager ถ้ามี
        if (!string.IsNullOrWhiteSpace(nameFromLobby))
            return nameFromLobby;

        // หาไม่เจอเลย → ใช้รูปแบบเดิม
        return $"P{ownerClientId}";
    }


}
