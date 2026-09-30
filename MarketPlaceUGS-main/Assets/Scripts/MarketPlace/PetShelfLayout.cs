using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps pets at a readable size and grows one horizontal shelf.</summary>
[RequireComponent(typeof(GridLayoutGroup))]
public sealed class PetShelfLayout : MonoBehaviour
{
    private ScrollRect scroll;
    private RectTransform rect;
    private GridLayoutGroup grid;
    private float lastWidth = -1, lastHeight = -1;
    private int lastCount = -1;

    public void Refresh()
    {
        if (rect == null) rect = (RectTransform)transform;
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        if (scroll == null) scroll = GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.viewport == null) return;
        float width = scroll.viewport.rect.width;
        float height = scroll.viewport.rect.height;
        if (width <= 0 || height <= 0) return;
        int count = 0;
        foreach (Transform child in transform)
            if (child.gameObject.activeSelf && child.GetComponent<PetDisplayUI>() != null) count++;
        if (count == lastCount && Mathf.Approximately(width, lastWidth) && Mathf.Approximately(height, lastHeight)) return;
        lastCount = count; lastWidth = width; lastHeight = height;
        float oldOffset = rect.anchoredPosition.x;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, .5f);
        grid.cellSize = new Vector2(280, 280);
        grid.spacing = new Vector2(24, 0);
        grid.padding = new RectOffset(16, 16, 0, 0);
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = 1;
        grid.childAlignment = TextAnchor.MiddleCenter;
        float needed = count * 280 + Mathf.Max(0, count - 1) * 24 + 32;
        float contentWidth = Mathf.Max(width, needed);
        rect.sizeDelta = new Vector2(contentWidth, 0);
        rect.anchoredPosition = new Vector2(Mathf.Clamp(oldOffset, width - contentWidth, 0), 0);
        scroll.horizontal = needed > width + .5f;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;
        if (scroll.horizontalScrollbar != null)
        {
            var bar = scroll.horizontalScrollbar;
            // ScrollRect only subscribes to scrollbar changes while horizontal is true.
            // Rebind after ConfigurePetArea (or a re-enable) has turned it off.
            scroll.horizontalScrollbar = bar;
            bar.gameObject.SetActive(scroll.horizontal);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void LateUpdate() => Refresh();
    private void OnEnable() { lastCount = -1; }
}
