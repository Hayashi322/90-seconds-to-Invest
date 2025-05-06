using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScalerSmooth : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 targetScale = new Vector3(1.1f, 1.1f, 1f);
    private Vector3 originalScale;
    private Vector3 currentTarget;
    public float scaleSpeed = 10f;

    void Start()
    {
        originalScale = transform.localScale;
        currentTarget = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, currentTarget, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentTarget = targetScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTarget = originalScale;
    }
}
