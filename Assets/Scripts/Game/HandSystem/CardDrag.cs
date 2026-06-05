using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private Vector2 startPos;
    private bool isDragging;
    private bool wasDragged;

    private float dragThreshold = 10f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
        isDragging = false;
        wasDragged = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GetComponent<CardDisplay>().isDragging = true;
        GetComponent<CardDisplay>().UpdateVisual();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if (Vector2.Distance(startPos, eventData.position) > dragThreshold)
            {
                GetComponent<CardDisplay>().isDragging = true;
                isDragging = true;

                transform.SetParent(canvas.transform);
            }
            else return;
        }

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            HandManager.Instance.HandleCardDrop(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            GetComponent<CardDisplay>().OnPointerClick(eventData);
        }
    }
}