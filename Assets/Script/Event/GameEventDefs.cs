using System;
using UnityEngine;

public enum GameEventId
{
    None = 0,

    Pandemic,           // เกิดโรคระบาดใหญ่!
    WarStarted,         // เกิดสงครามระหว่างประเทศ!
    GlobalEconomyUp,    // เศรษฐกิจโลกดีขึ้น!
    BanChineseTech,     // ข่าวลือว่าสหรัฐฯ จะแบนเทคจีน!

    BullMarket,         // ข่าวด่วน! ตลาดขาขึ้น
    BearMarket,         // ข่าวด่วน! ตลาดขาลง
    MarketCrash,        // ตลาดหุ้นเกิด Crash จริง ๆ!

    GoldCrash,          // ทองคำราคาตกหนัก!
    GoldRally,          // ทองคำราคาพุ่ง!

    HousingBubbleBurst, // ฟองสบู่แตก!
    HousingBubbleForm   // (เผื่อใช้ในอนาคต ฟองสบู่อสังหาฯ ราคาพุ่ง)
}


// ตลาดประเภทต่าง ๆ
public enum MarketTarget
{
    StocksAll,
    StocksTech,
    StocksTourism,
    RealEstate,
    Gold,
    Everything
}

// ผลของข่าวหนึ่งตัว (เช่น หุ้น -30%, ทอง +50%)
[Serializable]
public struct EventEffectDef
{
    public MarketTarget target;
    public float multiplier; // 0.7 = ลด 30%, 1.5 = เพิ่ม 50%
}

// ข้อมูลข่าว 1 ใบ (ใช้กำหนดใน Inspector)
[Serializable]
public class EventConfig
{
    public GameEventId id;
    public string title;
    [TextArea] public string description;
    public Sprite image;               // รูปที่ใช้แสดงข่าว
    public EventEffectDef[] effects;   // ผลกระทบต่อราคา
}
