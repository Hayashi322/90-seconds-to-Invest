using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;
using UnityEngine.Rendering;

// ===== ชนิดที่ใช้ร่วม =====
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

[Serializable]
public struct HoldingNet : INetworkSerializable, IEquatable<HoldingNet>
{
    public FixedString64Bytes stockName;
    public int quantity;

    // ⭐ ต้นทุนเฉลี่ยต่อหุ้นของตัวนี้
    public double averageCost;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref stockName);
        serializer.SerializeValue(ref quantity);
        serializer.SerializeValue(ref averageCost);   // ⭐ sync ไปให้ client ด้วย
    }

    public bool Equals(HoldingNet other)
        => stockName.Equals(other.stockName) && quantity == other.quantity;
}



// ===== InventoryManager =====
public class InventoryManager : NetworkBehaviour
{
    public static InventoryManager Instance;

    // Server เขียน / Client อ่าน
    public NetworkVariable<double> cash = new(
        10_000_000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> goldAmount = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ✅ อินิตตั้งแต่ประกาศ (ห้ามเป็น null ตอนสปอน)
    public NetworkList<HoldingNet> stockHoldings { get; private set; } = new NetworkList<HoldingNet>();

    // อีเวนต์แจ้งผลคาสิโนให้ UI (ถูกยิงเฉพาะ client เจ้าของ)
    public event Action<CasinoResult> CasinoResultReceived;

    // แจ้ง UI เรื่องเงินเปลี่ยน (previous, current) เฉพาะ local owner
    public event Action<double, double> CashChanged;

    public NetworkVariable<int> bonus = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);



    //========ค่าเฉลี่ยทอง========//
    public NetworkVariable<int> averageGoldPrice =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int totalGoldCost = 0;
    public NetworkList<int> goldList = new NetworkList<int>();



    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && cash.Value <= 0)
            cash.Value = 10_000_000f;

        // ให้ Instance ชี้มาที่อินเวนทอรีของ local player
        if (IsOwner)
        {
            Instance = this;
            // subscribe ฟังการเปลี่ยนค่า cash ฝั่ง owner
            cash.OnValueChanged += OnCashValueChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner)
        {
            cash.OnValueChanged -= OnCashValueChanged;
        }
    }

    private void OnCashValueChanged(double previous, double current)
    {
        if (IsOwner)
        {
            CashChanged?.Invoke(previous, current);
        }
    }

    // ============================
    //   Common Cash Helper (Server)
    // ============================
    /// <summary>
    /// หักเงินจาก player คนนี้บนฝั่ง Server
    /// คืนค่า true ถ้าหักสำเร็จ, false ถ้าเงินไม่พอหรือเรียกผิดที่
    /// </summary>
    public bool TrySpendCash(double amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[Inventory][Server] TrySpendCash should be called on Server only.");
            return false;
        }

        if (amount <= 0) return true;

        if (cash.Value < amount)
        {
            Debug.Log($"[Inventory][Server] Spend failed (need {amount:N2}, has {cash.Value:N2})");
            return false;
        }

        double before = cash.Value;
        cash.Value -= amount;
        Debug.Log($"[Inventory][Server] Spend {amount:N2} | {before:N2} -> {cash.Value:N2}");
        return true;
    }

    // ============================
    //       GOLD (Server)
    // ============================
    [ServerRpc(RequireOwnership = false)]
    public void BuyGoldServerRpc(int qty)
    {
        if (qty <= 0) return;

        var shop = GoldShopManager.Instance;
        int unitPrice = shop ? shop.BuyGoldPrice.Value : 50_000; // fallback

        double cost = qty * unitPrice;
        if (cash.Value < cost) return;

        cash.Value -= cost;
        if (cash.Value < 0) cash.Value = 0;
        goldAmount.Value += qty;

        for (int i = 0; i < qty; i++) // เพิ่มราคาทองลงใน list ตามจำนวนที่ซื้อ
        {
            if (goldList.Count >= goldAmount.Value)
            {
                return;
            }
            goldList.Add(shop.BuyGoldPrice.Value);
        }

        averageGoldPrice.Value = CalculateAverageGold();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SellGoldServerRpc(int qty)
    {
        if (qty <= 0) return;
        if (goldAmount.Value < qty) return;

        var shop = GoldShopManager.Instance;
        int unitPrice = shop ? shop.SellGoldPrice.Value : 48_000; // fallback

        goldAmount.Value -= qty;
        cash.Value += qty * unitPrice;

        for (int i = 0; i < qty; i++) // ลดราคาทองลงใน list จากอันใหม่สุด
        {
            if (goldList.Count > 0)
            {
                goldList.RemoveAt(goldList.Count - 1);
            }
        }

        averageGoldPrice.Value = CalculateAverageGold();
    }

    private int CalculateAverageGold() //หาค่าเฉลี่ยทอง
    {
        if (goldList.Count == 0)
            return 0;

        int sum = 0;
        foreach (int price in goldList)
        {
            sum += price;
        }

        return sum / goldList.Count;
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
            SendCasinoResultClientRpc(false, 0, 0, "วางเงินเดิมพันก่อน", target);
            return;
        }

        if (cash.Value < cost)
        {
            SendCasinoResultClientRpc(false, 0, 0, "เงินไม่พอสำหรับการเดิมพัน", target);
            return;
        }

        // หักเงินก่อน
        cash.Value -= cost;

        // ทอยเต๋า
        int d1 = UnityEngine.Random.Range(1, 101);
        int d2 = UnityEngine.Random.Range(1, 7);
        int sum = d1;

        bool isEven = (sum % 2) == 0;
        bool isHigh = sum >= 6;

        if (sum <= 5) bonus.Value = 10;      //5%
        else if (sum <= 15) bonus.Value = 5; //10%
        else if (sum <= 30) bonus.Value = 3; //15%
        else if (sum <= 60) bonus.Value = 2; //30%
        else bonus.Value = 0;                //40%

        bool win = true;

        double reward = cost * bonus.Value;
        cash.Value += reward;

        SendCasinoResultClientRpc(win, d1, d2, "", target);
        Debug.Log("Bonus: " + bonus.Value);
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
        var key = (FixedString64Bytes)stockName;
        for (int i = 0; i < stockHoldings.Count; i++)
            if (stockHoldings[i].stockName.Equals(key))
                return stockHoldings[i].quantity;
        return 0;
    }

    // ⭐ helper ดูต้นทุนเฉลี่ยของหุ้นแต่ละตัว
    public double GetStockAverageCost(string stockName)
    {
        var key = (FixedString64Bytes)stockName;
        for (int i = 0; i < stockHoldings.Count; i++)
        {
            if (stockHoldings[i].stockName.Equals(key))
                return stockHoldings[i].averageCost;
        }
        return 0;
    }



    // ============================
    //       Server-side Ops (เดิม)
    // ============================
    [ServerRpc(RequireOwnership = false)]
    public void BuyGoldServerRpc(int qty, double price, ServerRpcParams rpcParams = default)
    {
        if (qty <= 0 || price <= 0) return;

        double cost = qty * price;
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
    public void BuyStockServerRpc(string stockName, int qty, double price, ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(stockName) || qty <= 0 || price <= 0) return;

        double cost = qty * price;
        if (cash.Value < cost)
        {
            Debug.Log($"[Inventory][Server] Buy STOCK failed (need {cost:N2}, has {cash.Value:N2})");
            return;
        }

        cash.Value -= cost;
        AddOrIncreaseStock(stockName, qty, price);   // ⭐ ส่งราคาเข้าไปคำนวณต้นทุนเฉลี่ย
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
    private void AddOrIncreaseStock(string stockName, int qty, double buyPricePerShare)
    {
        var key = (FixedString64Bytes)stockName;

        for (int i = 0; i < stockHoldings.Count; i++)
        {
            if (stockHoldings[i].stockName.Equals(key))
            {
                // ถ้ามีหุ้นตัวนี้อยู่แล้ว → อัปเดตต้นทุนเฉลี่ย
                var h = stockHoldings[i];

                int oldQty = h.quantity;
                double oldAvg = h.averageCost;

                int newQty = oldQty + qty;
                if (newQty <= 0)
                {
                    return;
                }

                double totalCostBefore = oldAvg * oldQty;
                double totalCostAdded = buyPricePerShare * qty;
                double newAvg = (totalCostBefore + totalCostAdded) / newQty;

                h.quantity = newQty;
                h.averageCost = newAvg;

                stockHoldings[i] = h;   // trigger sync
                return;
            }
        }

        // ซื้อครั้งแรกของหุ้นตัวนี้
        stockHoldings.Add(new HoldingNet
        {
            stockName = key,
            quantity = qty,
            averageCost = buyPricePerShare
        });
    }

    private void RemoveOrDecreaseStock(string stockName, int qty)
    {
        var key = (FixedString64Bytes)stockName;
        for (int i = 0; i < stockHoldings.Count; i++)
        {
            if (stockHoldings[i].stockName.Equals(key))
            {
                var h = stockHoldings[i];
                h.quantity -= qty;
                if (h.quantity <= 0)
                {
                    stockHoldings.RemoveAt(i);
                }
                else
                {
                    // ขายออกบางส่วน → ต้นทุนเฉลี่ยของ "ก้อนที่เหลือ" ยังคงเดิม
                    stockHoldings[i] = h;
                }
                return;
            }
        }
    }
}
