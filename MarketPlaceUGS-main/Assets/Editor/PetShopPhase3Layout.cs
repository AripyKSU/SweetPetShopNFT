#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PetShopPhase3Layout
{
    private const string ScenePath = "Assets/Scene/1.unity";
    private const string BackgroundPath = "Assets/Art/PetShop/Backgrounds/bg_petshop_main.png";
    private const string FencePath = "Assets/Art/PetShop/Environment/fence_auction.png";
    private const string RugPath = "Assets/Art/PetShop/Environment/rug_inventory.png";
    [MenuItem("Tools/Pet Shop/Apply Phase 3 Layout")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvas = scene.GetRootGameObjects().SelectMany(Flatten)
            .FirstOrDefault(go => go.GetComponent<Canvas>() != null);
        if (canvas == null) throw new InvalidOperationException("Canvas was not found in Scene 1.");

        ConfigureCanvas(canvas);

        var background = GetOrCreateUI("PetShopBackground", canvas.transform, 0);
        Stretch(background.GetComponent<RectTransform>());
        var backgroundImage = GetOrAdd<Image>(background);
        backgroundImage.sprite = LoadSprite(BackgroundPath);
        backgroundImage.color = Color.white;
        backgroundImage.preserveAspect = false;
        backgroundImage.raycastTarget = false;

        var mainArea = GetOrCreateUI("MainPetArea", canvas.transform, 1);
        SetAnchors(mainArea.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0.72f, 1),
            new Vector2(20, 24), new Vector2(-12, -24));

        var auction = GetOrCreateUI("AuctionFenceArea", mainArea.transform, 0);
        SetAnchors(auction.GetComponent<RectTransform>(), new Vector2(0, 0.48f), Vector2.one,
            new Vector2(16, 12), new Vector2(-16, -18));
        ConfigureArea(auction, LoadSprite(FencePath), new Color(1, 1, 1, 0.97f), "경매장 펫", 34);

        var inventory = GetOrCreateUI("InventoryArea", mainArea.transform, 1);
        SetAnchors(inventory.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1, 0.46f),
            new Vector2(16, 16), new Vector2(-16, -8));
        ConfigureArea(inventory, LoadSprite(RugPath), new Color(1, 1, 1, 0.96f), "내 펫 보관소", 32);

        var transferGuide = GetOrCreateUI("TransferGuide", mainArea.transform, 2);
        SetAnchors(transferGuide.GetComponent<RectTransform>(), new Vector2(0.18f, 0.445f), new Vector2(0.82f, 0.515f), Vector2.zero, Vector2.zero);
        var guideText = GetOrAdd<TextMeshProUGUI>(transferGuide);
        guideText.text = "↑ 판매 등록    ·    ↓ 구매 / 판매 취소";
        guideText.alignment = TextAlignmentOptions.Center;
        guideText.fontSize = 24;
        guideText.color = new Color32(88, 58, 37, 255);
        guideText.raycastTarget = false;

        MoveExistingPanel(canvas.transform, "MarketPanel", auction.transform, "MarketPetViewport");
        MoveExistingPanel(canvas.transform, "InventoryPanel", inventory.transform, "InventoryPetViewport");

        var rightPanel = GetOrCreateUI("RightServicePanel", canvas.transform, 2);
        SetAnchors(rightPanel.GetComponent<RectTransform>(), new Vector2(0.72f, 0), Vector2.one,
            new Vector2(8, 24), new Vector2(-20, -24));
        var rightImage = GetOrAdd<Image>(rightPanel);
        rightImage.color = new Color32(244, 236, 218, 235);
        rightImage.raycastTarget = false;
        AdoptServiceObjects(canvas.transform, rightPanel.transform);

        var dialogLayer = GetOrCreateUI("DialogLayer", canvas.transform, canvas.transform.childCount);
        Stretch(dialogLayer.GetComponent<RectTransform>());
        dialogLayer.transform.SetAsLastSibling();
        var dragLayer = GetOrCreateUI("DragLayer", canvas.transform, canvas.transform.childCount);
        Stretch(dragLayer.GetComponent<RectTransform>());
        dragLayer.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[PetShopPhase3] Scene 1 pet-shop layout applied and saved.");
    }

    public static void ApplyBatch()
    {
        Apply();
        EditorApplication.Exit(0);
    }

    private static void ConfigureCanvas(GameObject canvas)
    {
        var scaler = GetOrAdd<CanvasScaler>(canvas);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100;
    }

    private static void ConfigureArea(GameObject area, Sprite sprite, Color color, string title, float fontSize)
    {
        var image = GetOrAdd<Image>(area);
        image.sprite = sprite;
        image.type = sprite != null && sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;

        var header = GetOrCreateUI(area.name == "AuctionFenceArea" ? "AuctionHeader" : "InventoryHeader", area.transform, 0);
        SetAnchors(header.GetComponent<RectTransform>(), new Vector2(0.04f, 0.84f), new Vector2(0.55f, 0.98f), Vector2.zero, Vector2.zero);
        var text = GetOrAdd<TextMeshProUGUI>(header);
        text.text = title;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = new Color32(80, 48, 29, 255);
        text.raycastTarget = false;
    }

    private static void MoveExistingPanel(Transform canvas, string oldName, Transform parent, string newName)
    {
        var panel = canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == oldName);
        if (panel == null)
        {
            panel = GetOrCreateUI(newName, parent, parent.childCount).transform;
        }
        else
        {
            panel.name = newName;
            panel.SetParent(parent, false);
        }
        panel.gameObject.SetActive(true);

        var rect = panel as RectTransform;
        if (rect != null) SetAnchors(rect, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.83f), Vector2.zero, Vector2.zero);
        ConfigureOpenPetPanel(panel.gameObject);
    }

    private static void ConfigureOpenPetPanel(GameObject panel)
    {
        foreach (var row in panel.GetComponentsInChildren<InventoryRowUI>(true))
            UnityEngine.Object.DestroyImmediate(row.gameObject);
        foreach (var row in panel.GetComponentsInChildren<MarketRowUI>(true))
            UnityEngine.Object.DestroyImmediate(row.gameObject);

        MakeTransparent(panel.GetComponent<Image>());
        var outerMask = panel.GetComponent<Mask>();
        if (outerMask != null) outerMask.enabled = false;
        var scrollForViewport = panel.GetComponent<ScrollRect>();
        if (scrollForViewport != null && scrollForViewport.viewport != null)
        {
            var viewportImage = scrollForViewport.viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                Color viewportColor = viewportImage.color;
                viewportColor.a = 1f;
                viewportImage.color = viewportColor;
                viewportImage.raycastTarget = false;
            }
            var viewportMask = scrollForViewport.viewport.GetComponent<Mask>();
            if (viewportMask != null) viewportMask.showMaskGraphic = false;
        }

        foreach (var scrollbar in panel.GetComponentsInChildren<Scrollbar>(true))
            scrollbar.gameObject.SetActive(false);

        var scroll = panel.GetComponent<ScrollRect>();
        if (scroll != null)
        {
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.enabled = false;
        }

        var content = panel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Content") as RectTransform;
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (child.GetComponent<InventoryRowUI>() != null || child.GetComponent<MarketRowUI>() != null || child.name == "EmptyState")
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        SetAnchors(content, Vector2.zero, Vector2.one, new Vector2(18, 12), new Vector2(-18, -12));
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        var grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
        grid.cellSize = new Vector2(220, 245);
        grid.spacing = new Vector2(12, 8);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
    }

    private static void MakeTransparent(Image image)
    {
        if (image == null) return;
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void AdoptServiceObjects(Transform canvas, Transform rightPanel)
    {
        string[] serviceNames = { "MythicNftPanel", "WalletPanel" };
        foreach (var name in serviceNames)
        {
            var item = canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && t.parent != rightPanel);
            if (item != null && !item.IsChildOf(rightPanel)) item.SetParent(rightPanel, true);
        }

        // Authentication controls are wired to a component rather than a consistently named panel.
        var auth = canvas.GetComponentsInChildren<UserNamePw>(true).FirstOrDefault();
        if (auth != null)
        {
            var root = FindDirectCanvasChild(auth.transform, canvas);
            if (root != null && root != rightPanel && !root.IsChildOf(rightPanel)) root.SetParent(rightPanel, true);
        }
    }

    private static Transform FindDirectCanvasChild(Transform child, Transform canvas)
    {
        var current = child;
        while (current != null && current.parent != canvas) current = current.parent;
        return current;
    }

    private static GameObject GetOrCreateUI(string name, Transform parent, int siblingIndex)
    {
        var existing = parent.Cast<Transform>().FirstOrDefault(t => t.name == name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning($"[PetShopPhase3] Sprite not found: {path}");
        return sprite;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static GameObject[] Flatten(GameObject root)
    {
        return root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
    }
}
#endif
