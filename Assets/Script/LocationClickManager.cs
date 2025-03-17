using UnityEngine;

public class LocationClickManager : MonoBehaviour
{
    [SerializeField] private HeroController heroController;

    [Header("ลาก Waypoint ของสถานที่ลงมาใน Inspector")]
    [SerializeField] private GameObject finansiaWaypoint;
    [SerializeField] private GameObject goldShopWaypoint;
    [SerializeField] private GameObject bankWaypoint;
    [SerializeField] private GameObject realEstateWaypoint;
    [SerializeField] private GameObject lotteryWaypoint;
    [SerializeField] private GameObject taxOfficeWaypoint;
    [SerializeField] private GameObject policeWaypoint;
    [SerializeField] private GameObject casinoWaypoint;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // คลิกซ้าย
        {
            DetectLocationClick();
        }
    }

    private void DetectLocationClick()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;
            MoveToLocation(clickedObject);
        }
    }

    private void MoveToLocation(GameObject clickedObject)
    {
        switch (clickedObject.tag)
        {
            case "Finansia":
                heroController.SetDestinationByClick(finansiaWaypoint);
                break;
            case "GoldShop":
                heroController.SetDestinationByClick(goldShopWaypoint);
                break;
            case "Bank":
                heroController.SetDestinationByClick(bankWaypoint);
                break;
            case "RealEstate":
                heroController.SetDestinationByClick(realEstateWaypoint);
                break;
            case "Lottery":
                heroController.SetDestinationByClick(lotteryWaypoint);
                break;
            case "TaxOffice":
                heroController.SetDestinationByClick(taxOfficeWaypoint);
                break;
            case "Police":
                heroController.SetDestinationByClick(policeWaypoint);
                break;
            case "Casino":
                heroController.SetDestinationByClick(casinoWaypoint);
                break;
            default:
                Debug.Log("ไม่ได้คลิกที่สถานที่ที่รองรับ");
                break;
        }
    }
}