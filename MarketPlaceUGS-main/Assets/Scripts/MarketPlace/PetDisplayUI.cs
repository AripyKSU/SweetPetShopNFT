using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PetDisplayUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static int activeDragCount;

    public static bool IsAnyPetDragging => activeDragCount > 0;

    [SerializeField] private Image petImage;
    [SerializeField] private Image cushionImage;
    [SerializeField] private Sprite specialCushion;
    [SerializeField] private Sprite nftCushion;
    [SerializeField] private Image outlineImage;
    [SerializeField] private Image footMarker;
    [SerializeField] private GameObject nftBadge;
    [SerializeField] private GameObject ownedBadge;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI stateText;

    private bool locked;
    private bool dragging;
    private Action<PointerEventData> dropped;
    private RectTransform dragGhost;
    private Canvas rootCanvas;
    private ScrollRect shelfScroll;
    private bool scrolling;

    public void Bind(string itemId, Sprite petSprite, bool isOwnedListing, bool isLocked,
        Action<PointerEventData> onDropped = null)
    {
        bool isNft = itemId == "MYTHIC_SWORD_NFT";
        bool isSpecial = itemId == "LEGENDARY_SWORD";
        locked = isLocked;
        dropped = onDropped;
        if (actionPanel != null) actionPanel.SetActive(false);
        // Decorative graphics must never change the pointer's hit target on hover.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = graphic.gameObject == gameObject || graphic.GetComponent<Button>() != null;
        Image hitArea = GetComponent<Image>();
        if (hitArea != null) { hitArea.color = Color.clear; hitArea.canvasRenderer.cullTransparentMesh = false; }

        if (petImage != null)
        {
            petImage.sprite = petSprite;
            petImage.gameObject.SetActive(true);
            petImage.enabled = true;
            petImage.color = Color.white;
        }
        if (cushionImage != null)
        {
            cushionImage.sprite = isNft ? nftCushion : specialCushion;
            cushionImage.gameObject.SetActive(isSpecial || isNft);
        }
        if (nftBadge != null) nftBadge.SetActive(isNft);
        if (ownedBadge != null) ownedBadge.SetActive(isOwnedListing);
        RefreshState();
    }

    public void OnPointerEnter(PointerEventData eventData) => SetOutline(true);
    public void OnPointerExit(PointerEventData eventData) { if (!dragging) SetOutline(false); }
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        shelfScroll = GetComponentInParent<ScrollRect>();
        shelfScroll?.OnInitializePotentialDrag(eventData);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - eventData.pressPosition;
        if (shelfScroll != null && shelfScroll.horizontal && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            scrolling = true;
            activeDragCount++;
            eventData.eligibleForClick = false;
            shelfScroll.OnBeginDrag(eventData);
            return;
        }
        if (locked || dropped == null) return;
        if (!dragging) activeDragCount++;
        dragging = true;
        eventData.eligibleForClick = false;
        CreateGhost(eventData);
        RefreshState();
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (scrolling) { shelfScroll.OnDrag(eventData); return; }
        if (!dragging || dragGhost == null) return;
        PositionGhost(eventData);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrolling)
        {
            shelfScroll.OnEndDrag(eventData);
            ReleaseDrag();
            return;
        }
        if (!dragging) return;
        Action<PointerEventData> callback = dropped;
        ReleaseDrag();
        RefreshState();
        callback?.Invoke(eventData);
    }

    private void OnDisable()
    {
        ReleaseDrag();
    }

    private void ReleaseDrag()
    {
        if (scrolling) activeDragCount = Mathf.Max(0, activeDragCount - 1);
        scrolling = false;
        if (dragging) activeDragCount = Mathf.Max(0, activeDragCount - 1);
        dragging = false;
        if (dragGhost != null)
        {
            if (Application.isPlaying) Destroy(dragGhost.gameObject);
            else DestroyImmediate(dragGhost.gameObject);
        }
        dragGhost = null;
    }

    private void CreateGhost(PointerEventData eventData)
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;
        Transform layer = rootCanvas.transform.Find("DragLayer");
        if (layer == null) layer = rootCanvas.transform;
        GameObject ghost = new GameObject("PetDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragGhost = ghost.GetComponent<RectTransform>();
        dragGhost.SetParent(layer, false);
        dragGhost.SetAsLastSibling();
        RectTransform sourceRect = transform as RectTransform;
        dragGhost.sizeDelta = sourceRect != null ? sourceRect.rect.size : new Vector2(220f, 245f);
        Image image = ghost.GetComponent<Image>();
        image.sprite = petImage != null ? petImage.sprite : null;
        image.color = image.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.9f);
        image.preserveAspect = true;
        image.raycastTarget = false;
        CanvasGroup group = ghost.GetComponent<CanvasGroup>();
        group.alpha = 0.72f;
        group.blocksRaycasts = false;
        group.interactable = false;
        PositionGhost(eventData);
    }

    private void PositionGhost(PointerEventData eventData)
    {
        RectTransform canvasRect = dragGhost.parent as RectTransform;
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, camera, out Vector2 point))
            dragGhost.anchoredPosition = point;
    }

    private void RefreshState()
    {
        if (stateText != null) stateText.gameObject.SetActive(false);
        if (footMarker != null)
            footMarker.color = locked ? new Color32(126, 126, 126, 210) :
                dragging ? new Color32(84, 181, 167, 235) :
                new Color32(101, 139, 91, 120);
        SetOutline(locked || dragging);
        if (actionPanel != null && actionPanel.activeSelf) actionPanel.SetActive(false);
    }

    private void SetOutline(bool visible)
    {
        // The old "outline" was a filled sign sprite covering the pet.
        if (outlineImage != null) outlineImage.enabled = false;
    }
}
