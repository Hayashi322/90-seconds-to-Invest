using Unity.Netcode;
using Unity.Collections;
using System; // for IEquatable

[Serializable]
public struct StockDataNet : INetworkSerializable, IEquatable<StockDataNet>
{
    public FixedString32Bytes stockName; // แทน string
    public float currentPrice;
    public float lastPrice;
    public float volatility;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref stockName);
        serializer.SerializeValue(ref currentPrice);
        serializer.SerializeValue(ref lastPrice);
        serializer.SerializeValue(ref volatility);
    }

    public bool Equals(StockDataNet other)
        => stockName.Equals(other.stockName)
        && currentPrice.Equals(other.currentPrice)
        && lastPrice.Equals(other.lastPrice)
        && volatility.Equals(other.volatility);
}
