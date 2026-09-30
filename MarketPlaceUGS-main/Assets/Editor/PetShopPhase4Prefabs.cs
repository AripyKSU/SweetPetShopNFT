#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PetShopPhase4Prefabs
{
    private const string Output = "Assets/Prefabs/PetShop";
    private const string SignPath = "Assets/Art/PetShop/Props/prop_price_sign.png";
    private const string SpecialCushionPath = "Assets/Art/PetShop/Props/cushion_special.png";
    private const string NftCushionPath = "Assets/Art/PetShop/Props/cushion_nft.png";

    [MenuItem("Tools/Pet Shop/Create Phase 4 Display Prefabs")]
    public static void Create()
    {
        EnsureFolder("Assets/Prefabs", "PetShop");
        CreateInventory();
        CreateMarket();
        WireSceneReferences();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PetShopPhase4] Spatial pet display prefabs created and validated.");
    }

    public static void CreateBatch()
    {
        Create();
        EditorApplication.Exit(0);
    }

    private static void CreateInventory()
    {
        GameObject root = BuildBase("PetInventoryDisplay", out DisplayParts p);
        InventoryRowUI row = root.AddComponent<InventoryRowUI>();
        Button sell = AddActionButton(p.actionPanel.transform, "SellButton", "판매하기", new Color32(91, 137, 83, 255));
        Wire(row, ("titleText", p.title), ("instanceText", p.detail), ("optionText", p.state),
            ("priceText", p.price), ("iconImage", p.pet), ("sellBtn", sell));
        Save(root, "PetInventoryDisplay.prefab");
    }

    private static void CreateMarket()
    {
        GameObject root = BuildBase("PetMarketDisplay", out DisplayParts p);
        MarketRowUI row = root.AddComponent<MarketRowUI>();
        Button buy = AddActionButton(p.actionPanel.transform, "BuyButton", "입양하기", new Color32(91, 137, 83, 255));
        Button cancel = AddActionButton(p.actionPanel.transform, "CancelButton", "판매 취소", new Color32(156, 101, 77, 255));
        Wire(row, ("titleText", p.title), ("priceText", p.price), ("buyBtn", buy),
            ("cancelBtn", cancel), ("iconImage", p.pet));
        Save(root, "PetMarketDisplay.prefab");
    }

    private static GameObject BuildBase(string name, out DisplayParts p)
    {
        GameObject root = UI(name, null);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260, 300);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = 260; layout.preferredHeight = 300;

        Image hitArea = root.AddComponent<Image>();
        // Keep the full draggable footprint visible while the final pet art is being verified.
        hitArea.color = Color.clear;
        hitArea.canvasRenderer.cullTransparentMesh = false;
        hitArea.raycastTarget = true;

        Image cushion = AddImage(root.transform, "Cushion", new Vector2(0.5f, 0.30f), new Vector2(210, 100));
        Image foot = AddImage(root.transform, "FootMarker", new Vector2(0.5f, 0.19f), new Vector2(150, 24));
        foot.sprite = LoadSprite(SignPath); foot.type = Image.Type.Sliced;
        Image outline = AddImage(root.transform, "SelectionOutline", new Vector2(0.5f, 0.56f), new Vector2(230, 230));
        outline.sprite = LoadSprite(SignPath); outline.type = Image.Type.Sliced; outline.color = Color.clear;
        Image pet = AddImage(root.transform, "PetImage", new Vector2(0.5f, 0.58f), new Vector2(210, 210));
        pet.preserveAspect = true;

        TextMeshProUGUI price = name == "PetMarketDisplay"
            ? AddText(root.transform, "Price", new Vector2(0.67f, 0.08f), new Vector2(150, 28), 19, FontStyles.Bold)
            : null;

        GameObject nft = Badge(root.transform, "NftBadge", "NFT", new Vector2(0.5f, 0.08f), new Color32(102, 73, 151, 240));
        GameObject owned = Badge(root.transform, "OwnedBadge", "내 매물", new Vector2(0.19f, 0.08f), new Color32(72, 121, 105, 240));

        GameObject actions = UI("ActionPanel", root.transform);
        SetRect(actions.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(210, 42));
        HorizontalLayoutGroup group = actions.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 8; group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true; group.childForceExpandWidth = true;
        actions.SetActive(false);

        PetDisplayUI display = root.AddComponent<PetDisplayUI>();
        Wire(display, ("petImage", pet), ("cushionImage", cushion),
            ("specialCushion", LoadSprite(SpecialCushionPath)), ("nftCushion", LoadSprite(NftCushionPath)),
            ("outlineImage", outline), ("footMarker", foot), ("nftBadge", nft),
            ("ownedBadge", owned), ("actionPanel", actions), ("stateText", null));

        nft.SetActive(false); owned.SetActive(false); cushion.gameObject.SetActive(false);
        p = new DisplayParts { pet = pet, price = price, actionPanel = actions };
        return root;
    }

    private static Button AddActionButton(Transform parent, string name, string label, Color color)
    {
        GameObject go = UI(name, parent);
        Image image = go.AddComponent<Image>(); image.color = color;
        Button button = go.AddComponent<Button>();
        LayoutElement layout = go.AddComponent<LayoutElement>(); layout.preferredHeight = 38;
        TextMeshProUGUI text = AddText(go.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(100, 34), 17, FontStyles.Bold);
        text.text = label; text.color = Color.white;
        return button;
    }

    private static GameObject Badge(Transform parent, string name, string label, Vector2 anchor, Color color)
    {
        GameObject go = UI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), anchor, new Vector2(74, 30));
        Image image = go.AddComponent<Image>(); image.color = color;
        TextMeshProUGUI text = AddText(go.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(70, 28), 15, FontStyles.Bold);
        text.text = label; text.color = Color.white;
        return go;
    }

    private static Image AddImage(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject go = UI(name, parent); SetRect(go.GetComponent<RectTransform>(), anchor, size);
        Image image = go.AddComponent<Image>(); image.raycastTarget = false; return image;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, Vector2 anchor, Vector2 size, float fontSize, FontStyles style = FontStyles.Normal)
    {
        GameObject go = UI(name, parent); SetRect(go.GetComponent<RectTransform>(), anchor, size);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize; text.fontStyle = style; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(76, 51, 34, 255); text.raycastTarget = false; return text;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
    }

    private static void Wire(Object target, params (string name, Object value)[] values)
    {
        SerializedObject so = new SerializedObject(target);
        foreach (var value in values)
        {
            SerializedProperty property = so.FindProperty(value.name);
            if (property == null) throw new System.InvalidOperationException($"Missing field {target.GetType().Name}.{value.name}");
            property.objectReferenceValue = value.value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Save(GameObject root, string file)
    {
        string path = $"{Output}/{file}";
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        Object.DestroyImmediate(root);
        if (!success) throw new System.InvalidOperationException($"Failed to save {path}");
    }

    private static void WireSceneReferences()
    {
        const string scenePath = "Assets/Scene/1.unity";
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        PortfolioMarketDemo market = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<PortfolioMarketDemo>(true)).FirstOrDefault();
        if (market == null) throw new System.InvalidOperationException("PortfolioMarketDemo was not found in Scene 1.");

        InventoryRowUI inventoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Output}/PetInventoryDisplay.prefab")
            .GetComponent<InventoryRowUI>();
        MarketRowUI marketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Output}/PetMarketDisplay.prefab")
            .GetComponent<MarketRowUI>();
        Wire(market, ("inventoryRowPrefab", inventoryPrefab), ("marketRowPrefab", marketPrefab));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.InvalidOperationException($"Sprite not found: {path}");
        return sprite;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private sealed class DisplayParts
    {
        public Image pet;
        public TextMeshProUGUI title;
        public TextMeshProUGUI price;
        public TextMeshProUGUI detail;
        public TextMeshProUGUI state;
        public GameObject actionPanel;
    }
}
#endif
