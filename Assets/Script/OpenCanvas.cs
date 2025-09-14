using UnityEngine;

public class OpenCanvas : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] canvases;
    [SerializeField] private GameObject blockRaycast;   // ลาก BlockRaycast มาวาง
    [SerializeField] private HeroControllerNet controllerNet;

    private void Awake()
    {
        if (canvases == null || canvases.Length == 0)
            canvases = GetComponentsInChildren<CanvasGroup>(true);
        CloseAll();
        if (blockRaycast) blockRaycast.SetActive(false);
    }

    public void openCanvas(int number)
    {
        if (number < 0 || number >= canvases.Length) return;
        CloseAll();
        canvases[number].alpha = 1; canvases[number].blocksRaycasts = true; canvases[number].interactable = true;
        if (blockRaycast) blockRaycast.SetActive(true);
        controllerNet?.SetUIOpen(true);
    }

    public void closeCanvas()
    {
        CloseAll();
        if (blockRaycast) blockRaycast.SetActive(false);
        controllerNet?.SetUIOpen(false);
    }

    private void CloseAll()
    {
        foreach (var c in canvases) { c.alpha = 0; c.blocksRaycasts = false; c.interactable = false; }
    }
}
