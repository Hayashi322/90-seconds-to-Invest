using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

// ===== ถ้าเธอแยกไปไฟล์ CasinoTypes.cs แล้ว ให้ลบสองชนิดนี้ออก และใส่ using <namespace> แทน =====
public enum BetChoice
{
    HighEven,  // ผลรวม >= 6 และเป็นคู่
    HighOdd,   // ผลรวม >= 6 และเป็นคี่
    LowEven,   // ผลรวม < 6 และเป็นคู่
    LowOdd     // ผลรวม < 6 และเป็นคี่
}

public struct CasinoResult
{
    public bool win;
    public int dice1;
    public int dice2;
    public string message;
}
// =================================================================================================

[Serializable]
public struct HoldingNet : INetworkSerializable, IEquatable<HoldingNet>
{
    public FixedString64Bytes stockName;
    public int quantity;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref stockName);
        serializer.SerializeValue(ref quantity);
    }

    public bool Equals(HoldingNet other)
        => stockName.Equals(other.stockName) && quantity == other.quantity;
}

public class InventoryManager : NetworkBehaviour
{
    public static InventoryManager Instance;

    // Server เขียน / Client อ่าน
    public NetworkVariable<float> cash = new(
        10_000_000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> goldAmount = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<HoldingNet> stockHoldings;

    // อีเวนต์แจ้งผลคาสิโนให้ UI (ถูกยิงเฉพาะ client เจ้าของ)
    public event Action<CasinoResult> CasinoResultReceived;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (stockHoldings == null)
            stockHoldings = new NetworkList<HoldingNet>();

        if (IsServer && cash.Value <= 0)
            cash.Value = 10_000_000f;

        if (IsOwner) Instance = this;
    }

    // ============================
    //       Casino (Server)
    // ============================
    [ServerRpc(RequireOwnership = false)]
    public void PlaceBetServerRpc(int cost, BetChoice choice, ServerRpcParams rpcParams = default)
    {
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
        };

        if (cost <= 0)
        {
            SendCasinoResultClientRpc(false, 0, 0, "Invalid cost.", target);
            return;
        }

        if (cash.Value < cost)
        {
            SendCasinoResultClientRpc(false, 0, 0, "Not enough cash.", target);
            return;
        }

        // หักเงินก่อน
        cash.Value -= cost;

        // ทอยเต๋า
        int d1 = UnityEngine.Random.Range(1, 7);
        int d2 = UnityEngine.Random.Range(1, 7);
        int sum = d1 + d2;

        bool isEven = (sum % 2) == 0;
        bool isHigh = sum >= 6;

        bool win = choice switch
        {
            BetChoice.HighEven => isHigh && isEven,
            BetChoice.HighOdd => isHigh && !isEven,
            BetChoice.LowEven => !isHigh && isEven,
            BetChoice.LowOdd => !isHigh && !isEven,
            _ => false
        };

        if (win)
        {
            float reward = cost * 1.5f;
            cash.Value += reward;
        }

        SendCasinoResultClientRpc(win, d1, d2, "", target);
    }

    [ClientRpc]
    private void SendCasinoResultClientRpc(bool win, int d1, int d2, string message, ClientRpcParams rpcParams = default)
    {
        CasinoResultReceived?.Invoke(new CasinoResult
        {
            win = win,
            dice1 = d1,
            dice2 = d2,
            message = message
        });
    }

    // ============================
    //   Public Read Helpers (UI)
    // ============================
    public int GetStockQuantity(string stockName)
    {
        if (stockHoldings == null) return 0;
        var key = (FixedString64Bytes)stockName;   // แปลงก่อนเทียบ
        for (int i = 0; i < stockHoldings.Count; i++)
            if (stockHoldings[i].stockName.Equals(key))
                return stockHoldings[i].quantity;
        return 0;
    }

    // ============================
    //       Server-side Ops
    // ============================
    [ServerRpc(RequireOwnership = false)]
    public void BuyGoldServerRpc(int qty, float price, ServerRpcParams rpcParams = default)
    {
        if (qty <= 0 || price <= 0) return;

        float cost = qty * price;
        if (cash.Value >= cost)
        {
            cash.Value -= cost;
            goldAmount.Value += qty;
            Debug.Log($"[Inventory][Server] Buy GOLD x{qty} | gold={goldAmount.Value} | cash={cash.Value:N2}");
        }
        else
        {
            Debug.Log($"[Inventory][Server] Buy GOLD failed (need {cost:N2}, has {cash.Value:N2})");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyStockServerRpc(string stockName, int qty, float price, ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(stockName) || qty <= 0 || price <= 0) return;

        float cost = qty * price;
        if (cash.Value < cost)
        {
            Debug.Log($"[Inventory][Server] Buy STOCK failed (need {cost:N2}, has {cash.Value:N2})");
            return;
        }

        cash.Value -= cost;
        AddOrIncreaseStock(stockName, qty);
        Debug.Log($"[Inventory][Server] Buy {stockName} x{qty} @ {price:N2} | cash={cash.Value:N2}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SellStockServerRpc(string stockName, int qty, float price, ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(stockName) || qty <= 0 || price <= 0) return;

        int current = GetStockQuantity(stockName);
        if (current < qty)
        {
            Debug.Log($"[Inventory][Server] Sell STOCK failed (have {current}, want {qty})");
            return;
        }

        RemoveOrDecreaseStock(stockName, qty);
        float revenue = qty * price;
        cash.Value += revenue;
        Debug.Log($"[Inventory][Server] Sell {stockName} x{qty} @ {price:N2} | cash={cash.Value:N2}");
    }

    // ============================
    //   Internal (Server only)
    // ============================
    private void AddOrIncreaseStock(string stockName, int qty)
    {
        var key = (FixedString64Bytes)stockName;   // แปลงก่อน
        for (int i = 0; i < stockHoldings.Count; i++)
        {
            if (stockHoldings[i].stockName.Equals(key))
            {
                var h = stockHoldings[i];
                h.quantity += qty;
                stockHoldings[i] = h; // ต้องเขียนกลับเพื่อ trigger sync
                return;
            }
        }
        stockHoldings.Add(new HoldingNet { stockName = key, quantity = qty });
    }

    private void RemoveOrDecreaseStock(string stockName, int qty)
    {
        var key = (FixedString64Bytes)stockName;   // แปลงก่อน
        for (int i = 0; i < stockHoldings.Count; i++)
        {
            if (stockHoldings[i].stockName.Equals(key))
            {
                var h = stockHoldings[i];
                h.quantity -= qty;
                if (h.quantity <= 0)
                    stockHoldings.RemoveAt(i);
                else
                    stockHoldings[i] = h;
                return;
            }
        }
    }
}
