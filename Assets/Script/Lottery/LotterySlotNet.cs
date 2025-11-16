using System;
using Unity.Netcode;

/// <summary>
/// ข้อมูลหวย 1 ใบที่แสดงในร้านขายหวย
/// </summary>
[Serializable]
public struct LotterySlotNet : INetworkSerializable, IEquatable<LotterySlotNet>
{
    // เลขหวย 6 หลัก เช่น 123456
    public int ticketNumber;

    // true = ใบนี้ถูกซื้อไปแล้ว
    public bool isSold;

    // clientId ของคนที่ซื้อ (0 = ยังไม่มีเจ้าของ)
    public ulong ownerClientId;

    // --- INetworkSerializable ---
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ticketNumber);
        serializer.SerializeValue(ref isSold);
        serializer.SerializeValue(ref ownerClientId);
    }

    // --- IEquatable<LotterySlotNet> ---
    public bool Equals(LotterySlotNet other)
    {
        return ticketNumber == other.ticketNumber
               && isSold == other.isSold
               && ownerClientId == other.ownerClientId;
    }

    public override bool Equals(object obj)
    {
        return obj is LotterySlotNet other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ticketNumber;
            hash = (hash * 397) ^ isSold.GetHashCode();
            hash = (hash * 397) ^ ownerClientId.GetHashCode();
            return hash;
        }
    }
}
