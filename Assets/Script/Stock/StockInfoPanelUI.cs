using UnityEngine;
using TMPro;

public class StockInfoPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stockTitleText;
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI weaknessText;
    [SerializeField] private TextMeshProUGUI descText;

    /// <summary>อัปเดตกล่องข้อมูลด้านขวาบนตามชื่อหุ้นที่เลือก</summary>
    public void ShowInfo(string stockName)
    {
        switch (stockName)
        {
            case "PTT":
                stockTitleText.text = "PTT — พลังงาน / น้ำมันและก๊าซ (Energy)";
                strengthText.text = "จุดแข็ง: ราคามีแนวโน้มดีช่วงเศรษฐกิจโลกฟื้นหรือน้ำมันแพง";
                weaknessText.text = "จุดอ่อน: แพ้ภาวะตลาดขาลงและช่วงสงครามที่ต้นทุนน้ำมันผันผวนสูง";
                descText.text = "ผู้นำธุรกิจพลังงานของไทย ครอบคลุมน้ำมัน ก๊าซ และปิโตรเคมี";
                break;

            case "KBANK":
                stockTitleText.text = "KBANK — การเงิน / ธนาคาร (Banking)";
                strengthText.text = "จุดแข็ง: ได้ประโยชน์ช่วงดอกเบี้ยสูงหรือเศรษฐกิจเติบโต";
                weaknessText.text = "จุดอ่อน: ช่วงตลาดขาลงหรือรัฐลดดอกเบี้ย จะทำกำไรลดลง";
                descText.text = "ธนาคารขนาดใหญ่ของไทย มีธุรกิจสินเชื่อ การลงทุน และดิจิทัลแบงก์กิ้ง";
                break;

            case "AOT":
                stockTitleText.text = "AOT — ท่องเที่ยว / สนามบิน (Tourism)";
                strengthText.text = "จุดแข็ง: ราคาพุ่งในช่วงเศรษฐกิจโลกดีขึ้น หรือหลังโรคระบาด";
                weaknessText.text = "จุดอ่อน: ราคาตกแรงเมื่อเกิดโรคระบาดหรือสงครามระหว่างประเทศ";
                descText.text = "ผู้บริหารสนามบินหลักของประเทศ ได้รายได้จากนักท่องเที่ยวทั่วโลก";
                break;

            case "BDMS":
                stockTitleText.text = "BDMS — สุขภาพ / โรงพยาบาล (Healthcare)";
                strengthText.text = "จุดแข็ง: ไม่กระทบมากในภาวะเศรษฐกิจไม่ดี เป็นหุ้นปลอดภัย";
                weaknessText.text = "จุดอ่อน: โตช้ากว่าหุ้นเทคฯ หรือพลังงานในตลาดขาขึ้น";
                descText.text = "เครือโรงพยาบาลเอกชนขนาดใหญ่ มีฐานลูกค้าทั้งไทยและต่างชาติ";
                break;

            case "DELTA":
                stockTitleText.text = "DELTA — เทคโนโลยี / อิเล็กทรอนิกส์ (Technology)";
                strengthText.text = "จุดแข็ง: ราคาพุ่งแรงในตลาดขาขึ้นและช่วงเทคโนโลยีบูม";
                weaknessText.text = "จุดอ่อน: ราคาผันผวนสูงมาก ตกแรงเมื่อมีข่าวแบนเทคจีนหรือสงคราม";
                descText.text = "ผู้ผลิตอุปกรณ์ไฟฟ้าและพลังงานระดับโลก รายได้ผูกกับตลาดเทคโนโลยี";
                break;

            case "CPNREIT":
                stockTitleText.text = "CPNREIT — อสังหาริมทรัพย์ / กองทรัสต์ (Real Estate)";
                strengthText.text = "จุดแข็ง: สร้างรายได้ต่อเนื่องจากค่าเช่า เหมาะกับช่วงตลาดนิ่ง";
                weaknessText.text = "จุดอ่อน: ช่วงตลาดขาลงหรือฟองสบู่อสังหาฯ จะขายออกยาก";
                descText.text = "กองทรัสต์ลงทุนในศูนย์การค้า มีรายได้สม่ำเสมอจากผู้เช่า";
                break;

            default:
                stockTitleText.text = stockName;
                strengthText.text = "จุดแข็ง: -";
                weaknessText.text = "จุดอ่อน: -";
                descText.text = "ไม่มีข้อมูลเพิ่มเติม";
                break;
        }
    }
}