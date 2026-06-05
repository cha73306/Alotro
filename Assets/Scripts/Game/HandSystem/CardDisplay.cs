using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    public Image artwork;

    public CardData cardData;
    private HandManager handManager;

    private RectTransform rectTransform;

    public float liftAmount = 30f;

    public bool isDragging = false;

    public void Init(CardData data, HandManager manager)
    {
        cardData = data;
        handManager = manager;

        rectTransform = GetComponent<RectTransform>();

        artwork.sprite = data.sprite;
        artwork.preserveAspect = true;

        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false; // reset for next click
            return;
        }
        if (!cardData.isSelected)
        {
            // block selection if already at 5
            if (handManager.GetSelectedCount() >= 5)
                return;
        }

        cardData.isSelected = !cardData.isSelected;
        UpdateVisual();
        if (handManager != null)
        {
            handManager.UpdateLiveHandPreview();
        }
    }

    public void UpdateVisual()
    {
        // lift effect
        if (cardData.isSelected)
        {
            rectTransform.anchoredPosition += new Vector2(0, liftAmount);
        }
        else
        {
            rectTransform.anchoredPosition -= new Vector2(0, liftAmount);
        }
    }

    public bool MovingResetVisual()
    {
        return cardData.isSelected = false; // reset selection state
    }

    public bool IsSelected()
    {
        return cardData.isSelected;
    }
}