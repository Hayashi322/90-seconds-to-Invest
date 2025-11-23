using System;
using Unity.Netcode;

[Serializable]
public struct LotterySlotNet : INetworkSerializable, IEquatable<LotterySlotNet>
{
    public int ticketNumber;      // เลขหวย
    public bool isSold;           // ถูกซื้อแล้วหรือยัง
    public ulong ownerClientId;   // คนซื้อ
    public bool isWinning;        // ✅ ใบนี้คือใบถูกรางวัลหรือไม่

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ticketNumber);
        serializer.SerializeValue(ref isSold);
        serializer.SerializeValue(ref ownerClientId);
        serializer.SerializeValue(ref isWinning); // ✅ Serialize ค่าใหม่
    }

    public bool Equals(LotterySlotNet other)
    {
        return ticketNumber == other.ticketNumber
            && isSold == other.isSold
            && ownerClientId == other.ownerClientId
            && isWinning == other.isWinning;
    }
}
