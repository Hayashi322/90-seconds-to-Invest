// Assets/Scripts/Shared/CasinoTypes.cs
namespace Game.Economy
{
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
}
