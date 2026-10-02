using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PortfolioMarketDemo : MonoBehaviour
{
    [Header("Cloud Save Market")]
    [SerializeField] private string currencyId = "COIN";
    [SerializeField] private int defaultPrice = 100;

    [Header("Pet Adoption (Resource IDs)")]

    [Header("Top UI")]
    [SerializeField] private TextMeshProUGUI debugLine;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private Button giveEquipmentBtn;
    [SerializeField] private Button addCoinBtn;
    [SerializeField] private Button claimBtn;

    [Header("Inventory UI")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private InventoryRowUI inventoryRowPrefab;

    [Header("Market UI")]
    [SerializeField] private Transform marketContent;
    [SerializeField] private MarketRowUI marketRowPrefab;
    [SerializeField] private Button marketRefreshBtn;

    [Header("Market Options")]
    [SerializeField] private int marketLimit = 30;
    [SerializeField] private string marketSort = "NEWEST"; // NEWEST / PRICE_ASC / PRICE_DESC

    [Header("Visuals")]
    [SerializeField] private ItemVisualData itemVisuals;

    private Task refreshTask;
    private int inventoryRefreshVersion;
    private int marketRefreshVersion;
    private MarketLiveUpdates live;
    private bool refreshAfterDrag;
    private bool transactionInProgress;
    private PetTradeDialogUI tradeDialog;
    private bool wasSignedIn;

    private void Start()
    {
        ConfigurePetArea(inventoryContent);
        ConfigurePetArea(marketContent);
        RemoveLegacyRows();
        ClearChildren(inventoryContent);
        ClearChildren(marketContent);
        tradeDialog = PetTradeDialogUI.GetOrCreate(inventoryContent);
        live = gameObject.AddComponent<MarketLiveUpdates>();
        live.Changed += OnMarketChanged;
        if (refreshBtn != null) refreshBtn.onClick.AddListener(() => _ = RefreshAllAsync());
        if (giveEquipmentBtn != null) giveEquipmentBtn.onClick.AddListener(() => _ = GiveRandomItemAsync());
        if (addCoinBtn != null) addCoinBtn.onClick.AddListener(() => _ = AddCoinAsync(100));
        if (claimBtn != null) claimBtn.onClick.AddListener(() => _ = ClaimEarningsAsync());
        if (marketRefreshBtn != null) marketRefreshBtn.onClick.AddListener(() => _ = RefreshMarketAsync());
        wasSignedIn = AuthenticationService.Instance.IsSignedIn;
        if (wasSignedIn) refreshTask = RefreshDataAsync();
        else CreateDragPreviews();
    }

    private void Update()
    {
        bool isSignedIn = AuthenticationService.Instance.IsSignedIn;
        if (!isSignedIn)
        {
            EnsurePetAreaActive(inventoryContent);
            EnsurePetAreaActive(marketContent);
        }
        if (isSignedIn != wasSignedIn)
        {
            wasSignedIn = isSignedIn;
            if (isSignedIn && (refreshTask == null || refreshTask.IsCompleted))
                refreshTask = RefreshDataAsync();
            else if (!isSignedIn)
            {
                ResetLoggedOutState();
            }
        }

        if (!refreshAfterDrag || PetDisplayUI.IsAnyPetDragging ||
            (tradeDialog != null && tradeDialog.gameObject.activeSelf) ||
            (refreshTask != null && !refreshTask.IsCompleted)) return;

        refreshAfterDrag = false;
        refreshTask = RefreshDataAsync();
    }

    public void ResetLoggedOutState()
    {
        wasSignedIn = false;
        inventoryRefreshVersion++;
        marketRefreshVersion++;
        refreshAfterDrag = false;
        if (tradeDialog != null) tradeDialog.Hide();
        if (coinText != null) coinText.text = "0";
        ClearChildren(inventoryContent);
        ClearChildren(marketContent);
        CreateDragPreviews();
    }

    private void OnMarketChanged()
    {
        if (!isActiveAndEnabled) return;
        if (PetDisplayUI.IsAnyPetDragging)
        {
            refreshAfterDrag = true;
            return;
        }
        if (refreshTask == null || refreshTask.IsCompleted) refreshTask = RefreshDataAsync();
    }
    private void OnDestroy() { if (live != null) live.Changed -= OnMarketChanged; }
    public Task RefreshAllAsync()
    {
        if (refreshTask != null && !refreshTask.IsCompleted) return refreshTask;
        return refreshTask = RefreshDataAsync(true);
    }
    private async Task RefreshDataAsync(bool syncNft = false)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        await RefreshCoinsAsync();
        if (syncNft) { var nft = FindFirstObjectByType<SimpleMarket.MythicNftPanel>(); if (nft) await nft.SyncInventoryAsync(); }
        await RefreshInventoryAsync();
        await RefreshMarketAsync();
    }

    private void SetMessage(string message)
    {
        if (debugLine != null) debugLine.text = message;
        Debug.Log(message);
    }

    private static string TradeErrorMessage(Exception error, string action)
    {
        string detail = error.ToString();
        if (detail.Contains("INSUFFICIENT_GOLD") || detail.Contains("INSUFFICIENT_COIN"))
            return action + "에 실패했어요. 코인 잔액이 부족합니다.";
        if (detail.Contains("LISTING_NOT_ACTIVE"))
            return action + "에 실패했어요. 이미 판매되거나 취소된 매물입니다. 경매장을 새로고침해 주세요.";
        if (detail.Contains("NOT_OWNER") || detail.Contains("FORBIDDEN"))
            return action + "에 실패했어요. 이 매물을 처리할 권한이 없습니다.";
        return action + "에 실패했어요. Cloud Code 연결 또는 로그인 상태를 확인해 주세요. 자세한 오류는 Console에 기록했습니다.";
    }

    private int GetIntemPriceFromData(string resourceId)
    {
        if(itemVisuals != null) {
            var mapping = itemVisuals.GetMapping(resourceId);
            if(mapping != null && mapping.price > 0) {
                return mapping.price;
            }
        }
        return defaultPrice;
    }

    // -------------------------
    // Cloud Save: Coin
    // -------------------------
    public async Task RefreshCoinsAsync()
    {
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            var p = await MarketCloudClient.GetPlayer();
            if (!AuthenticationService.Instance.IsSignedIn || AuthenticationService.Instance.PlayerId != playerId) return;
            if (coinText != null) coinText.text = p.balance.ToString();
        }
        catch (Exception e) { Debug.LogWarning(e); SetMessage("코인 잔액을 불러오지 못했어요. Cloud Save / Cloud Code 설정을 확인해 주세요."); }
    }
    private async Task AddCoinAsync(long amount)
    {
        try
        {
            await MarketCloudClient.Mutate<object>("Mkt_GrantDemo", new() { { "kind", "coin" } });
            await RefreshCoinsAsync(); SetMessage("수업용 코인 100개를 받았어요.");
        }
        catch (Exception e) { Debug.LogWarning(e); SetMessage("코인을 받지 못했어요. demoEnabled와 수업 지급 한도를 확인해 주세요."); }
    }

    // -------------------------
    // Cloud Save: Inventory
    // -------------------------
    public async Task RefreshInventoryAsync()
    {
        if (DeferRefreshWhileDragging()) return;
        int version = ++inventoryRefreshVersion;
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            var player = await MarketCloudClient.GetPlayer();
            if (this == null || version != inventoryRefreshVersion ||
                !AuthenticationService.Instance.IsSignedIn || AuthenticationService.Instance.PlayerId != playerId) return;
            if (DeferRefreshWhileDragging()) return;
            ClearChildren(inventoryContent);
            List<MarketCloudClient.Item> items = player.items ?? new List<MarketCloudClient.Item>();

            if (items.Count == 0)
                CreateEmptyState(inventoryContent, "아직 함께하는 펫이 없어요.\n펫 받기로 첫 친구를 만나보세요.");

            foreach (var item in items)
            {
                if (inventoryRowPrefab == null || inventoryContent == null)
                {
                    Debug.Log($"Inventory: {item.InventoryItemId} / {item.PlayersInventoryItemId}");
                    continue;
                }

                InventoryRowUI row = Instantiate(inventoryRowPrefab, inventoryContent);

                Sprite icon = null;
                string displayName = item.InventoryItemId;
                int price = defaultPrice;

                if(itemVisuals != null) {
                    var mapping = itemVisuals.GetMapping(item.InventoryItemId);
                    if(mapping != null) {
                        icon = mapping.icon;
                        if (!string.IsNullOrEmpty(mapping.itemName)) displayName = mapping.itemName;

                        if (mapping.price > 0) price = mapping.price;
                    }
                }

                row.Bind(item, displayName, icon, price, CreateListingFromButtonAsync, OnInventoryPetDropped);
            }

            ArrangePetGrid(inventoryContent, items.Count);

            SetMessage($"함께하는 펫 {items.Count}마리를 불러왔어요.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetMessage("펫 보관소를 불러오지 못했어요. 로그인 상태와 Cloud Code 배포를 확인해 주세요.");
        }
    }

    // -------------------------
    // Demo pet adoption: resource IDs remain unchanged for server compatibility.
    // -------------------------
    private async Task GiveRandomItemAsync()
    {
        try
        {
            await MarketCloudClient.Mutate<object>("Mkt_GrantDemo", new() { { "kind", "item" } });
            await RefreshInventoryAsync();
            SetMessage("새 펫을 영입했어요. 펫 보관소를 확인해 주세요.");
        }
        catch (Exception e) { Debug.LogWarning(e); SetMessage("펫 영입에 실패했어요. demoEnabled와 수업 지급 한도를 확인해 주세요."); }
    }

    // -------------------------
    // Cloud Code: Marketplace
    // -------------------------
    private Task CreateListingAsync(string playersInventoryItemId, int price) =>
        RunTransactionAsync(() => CreateListingCoreAsync(playersInventoryItemId, price));

    private Task CreateListingFromButtonAsync(string playersInventoryItemId, int price)
    {
        tradeDialog.ShowPrice("판매 가격 정하기", "경매장에 등록할 가격을 입력해 주세요.",
            price, confirmedPrice => _ = CreateListingAsync(playersInventoryItemId, confirmedPrice));
        return Task.CompletedTask;
    }

    private async Task CreateListingCoreAsync(string playersInventoryItemId, int price)
    {
        try
        {
            // The caller has already selected the price source; never overwrite it here.
            if (price < 1 || price > 1000000)
            {
                SetMessage("가격은 1~1,000,000 정수로 입력하세요.");
                return;
            }
            var args = new Dictionary<string, object>
            {
                { "players_inventory_item_id", playersInventoryItemId },
                { "price", price },
                { "currency_id", currencyId }
            };

            CreateListingResult res = await MarketCloudClient.Mutate<CreateListingResult>(
                "Mkt_CreateListing",
                args
            );

            SetMessage("펫을 경매장에 등록했어요.");
            await RefreshAllAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetMessage("판매 등록에 실패했어요. Cloud Code 권한과 배포 환경을 확인해 주세요.");
        }
    }

    private async Task RefreshMarketAsync()
    {
        if (DeferRefreshWhileDragging()) return;
        int version = ++marketRefreshVersion;
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            var args = new Dictionary<string, object>
            {
                { "limit", marketLimit },
                { "sort", marketSort }
            };

            MarketListResult res = await CloudCodeService.Instance.CallEndpointAsync<MarketListResult>(
                "Mkt_GetActiveListings",
                args
            );
            if (this == null || version != marketRefreshVersion ||
                !AuthenticationService.Instance.IsSignedIn || AuthenticationService.Instance.PlayerId != playerId) return;
            if (DeferRefreshWhileDragging()) return;
            ClearChildren(marketContent);

            if (res.listings == null)
            {
                CreateEmptyState(marketContent, "현재 입양 가능한 펫이 없어요.\n잠시 후 다시 확인해 주세요.");
                SetMessage("현재 경매장에 등록된 펫이 없어요.");
                return;
            }

            if (res.listings.Length == 0)
                CreateEmptyState(marketContent, "현재 입양 가능한 펫이 없어요.\n잠시 후 다시 확인해 주세요.");

            foreach (var listing in res.listings)
            {
                if (marketRowPrefab == null || marketContent == null)
                {
                    Debug.Log($"Listing: {listing.listingId} price={listing.price}");
                    continue;
                }

                MarketRowUI row = Instantiate(marketRowPrefab, marketContent);

                Sprite icon = null;
                string displayName = listing.inventoryItemId;

                if(itemVisuals != null) {
                    var mapping = itemVisuals.GetMapping(listing.inventoryItemId);
                    if(mapping != null) {
                        icon = mapping.icon;
                        if (!string.IsNullOrEmpty(mapping.itemName)) displayName = mapping.itemName;
                    }
                }


                row.Bind(listing, displayName, icon, BuyListingAsync, CancelListingAsync, OnMarketPetDropped);
            }

            ArrangePetGrid(marketContent, res.listings.Length);

            SetMessage($"경매장 펫 {res.listings.Length}마리를 불러왔어요.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetMessage("경매장을 불러오지 못했어요. Cloud Code 배포를 확인해 주세요.");
        }
    }

    private Task BuyListingAsync(string listingId) =>
        RunTransactionAsync(() => BuyListingCoreAsync(listingId));

    private async Task BuyListingCoreAsync(string listingId)
    {
        try
        {
            // ★ snake_case로 변경
            var args = new Dictionary<string, object> { { "listing_id", listingId } };

            BuyResult res = await MarketCloudClient.Mutate<BuyResult>(
                "Mkt_BuyListing",
                args
            );

            SetMessage("펫 구매를 완료했어요. 펫 보관소를 확인해 주세요.");
            await RefreshAllAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetMessage(TradeErrorMessage(e, "구매"));
        }
    }

    private Task CancelListingAsync(string listingId) =>
        RunTransactionAsync(() => CancelListingCoreAsync(listingId));

    private async Task CancelListingCoreAsync(string listingId)
    {
        try
        {
            // ★ snake_case로 변경
            var args = new Dictionary<string, object> { { "listing_id", listingId } };

            CancelResult res = await MarketCloudClient.Mutate<CancelResult>(
                "Mkt_CancelListing",
                args
            );

            SetMessage("판매를 취소했어요. 펫이 보관소로 돌아왔어요.");
            await RefreshAllAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetMessage(TradeErrorMessage(e, "판매 취소"));
        }
    }

    private async Task ClaimEarningsAsync()
    {
        try
        {
            // ★ snake_case로 변경
            var args = new Dictionary<string, object> { { "currency_id", currencyId } };

            ClaimResult res = await MarketCloudClient.Mutate<ClaimResult>(
                "Mkt_ClaimEarnings",
                args
            );

            SetMessage($"판매대금 {res.claimed:N0}코인을 받았어요.");
            await RefreshAllAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetMessage(TradeErrorMessage(e, "판매대금 정산"));
        }
    }

    private void OnInventoryPetDropped(string instanceId, string itemId, int suggestedPrice, PointerEventData eventData)
    {
        if (!IsDroppedInside(marketContent, eventData)) return;
        if (itemId == "MYTHIC_SWORD_NFT")
        {
            tradeDialog.ShowConfirm("판매할 수 없는 펫", "NFT 희귀 펫은 일반 경매장에 등록할 수 없어요.", null);
            return;
        }

        string displayName = GetDisplayName(itemId);
        tradeDialog.ShowPrice("판매 가격 정하기", $"{displayName}을(를) 경매장에 등록할 가격을 입력해 주세요.",
            suggestedPrice, price => _ = CreateListingAsync(instanceId, price));
    }

    private void OnMarketPetDropped(string listingId, bool isMine, string itemId, int price, PointerEventData eventData)
    {
        if (!IsDroppedInside(inventoryContent, eventData)) return;
        string displayName = GetDisplayName(itemId);
        if (isMine)
            tradeDialog.ShowConfirm("판매 취소", $"{displayName} 판매를 취소하고 인벤토리로 돌려보낼까요?",
                () => _ = CancelListingAsync(listingId));
        else
            tradeDialog.ShowConfirm("펫 구매 확인", $"{displayName}을(를) {price} {currencyId}에 구매할까요?",
                () => _ = BuyListingAsync(listingId));
    }

    private string GetDisplayName(string itemId)
    {
        if (itemVisuals != null)
        {
            var mapping = itemVisuals.GetMapping(itemId);
            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.itemName)) return mapping.itemName;
        }
        return itemId;
    }

    private static bool IsDroppedInside(Transform content, PointerEventData eventData)
    {
        if (content == null) return false;
        Transform area = content;
        while (area.parent != null && area.name != "AuctionFenceArea" && area.name != "InventoryArea")
            area = area.parent;
        RectTransform target = area as RectTransform;
        Camera camera = eventData.pressEventCamera;
        return target != null && RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, camera);
    }

    private async Task RunTransactionAsync(Func<Task> operation)
    {
        if (transactionInProgress)
        {
            SetMessage("이미 거래를 처리하고 있어요. 잠시만 기다려 주세요.");
            return;
        }

        transactionInProgress = true;
        try
        {
            Task request = operation();
            if (await Task.WhenAny(request, Task.Delay(TimeSpan.FromSeconds(15))) != request)
                SetMessage("서버 응답이 지연되고 있어요. 중복 요청하지 말고 결과를 기다려 주세요.");
            await request;
        }
        finally
        {
            transactionInProgress = false;
        }
    }

    private static void ConfigurePetArea(Transform content)
    {
        if (!(content is RectTransform rect)) return;

        EnsurePetAreaActive(content);

        ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            if (scroll.horizontalScrollbar != null) scroll.horizontalScrollbar.gameObject.SetActive(false);
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(false);
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.enabled = true;
            MakeTransparent(scroll.GetComponent<Image>());
            Mask outerMask = scroll.GetComponent<Mask>();
            if (outerMask != null) outerMask.enabled = false;
            if (scroll.viewport != null)
            {
                Image viewportImage = scroll.viewport.GetComponent<Image>();
                if (viewportImage != null)
                {
                    Color color = viewportImage.color;
                    color.a = 1f;
                    viewportImage.color = color;
                    viewportImage.raycastTarget = true;
                }
                Mask viewportMask = scroll.viewport.GetComponent<Mask>();
                if (viewportMask != null) viewportMask.showMaskGraphic = false;
            }
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(18, 12);
        rect.offsetMax = new Vector2(-18, -12);
        var layout = content.GetComponent<GridLayoutGroup>();
        if (layout == null)
        {
            Debug.LogError($"{content.name}에 GridLayoutGroup이 필요합니다. Scene의 Content 레이아웃을 확인하세요.", content);
            return;
        }
        layout.cellSize = new Vector2(220, 245);
        layout.spacing = new Vector2(12, 8);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 1;
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        if (content.GetComponent<PetShelfLayout>() == null) content.gameObject.AddComponent<PetShelfLayout>();
        LayoutRebuilder.MarkLayoutForRebuild(rect);
    }

    private static void ArrangePetGrid(Transform content, int itemCount)
    {
        if (content == null) return;
        content.GetComponent<PetShelfLayout>()?.Refresh();
    }

    private static void MakeTransparent(Image image)
    {
        if (image == null) return;
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void EnsurePetAreaActive(Transform content)
    {
        Transform areaRoot = content;
        while (areaRoot != null && areaRoot.name != "InventoryPetViewport" && areaRoot.name != "MarketPetViewport")
            areaRoot = areaRoot.parent;
        if (areaRoot != null && !areaRoot.gameObject.activeSelf) areaRoot.gameObject.SetActive(true);
    }

    private bool DeferRefreshWhileDragging()
    {
        if (!PetDisplayUI.IsAnyPetDragging && !(tradeDialog != null && tradeDialog.gameObject.activeSelf)) return false;
        refreshAfterDrag = true;
        return true;
    }

    private static void CreateEmptyState(Transform parent, string message)
    {
        if (parent == null) return;
        GameObject empty = new GameObject("EmptyState", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        empty.transform.SetParent(parent, false);

        LayoutElement element = empty.GetComponent<LayoutElement>();
        element.ignoreLayout = true;
        element.preferredWidth = 540;
        element.preferredHeight = 150;

        TextMeshProUGUI text = empty.GetComponent<TextMeshProUGUI>();
        PetShopTheme theme = parent.GetComponentInParent<PetShopTheme>();
        if (theme != null) text.font = theme.font;
        text.text = message;
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(88, 58, 37, 220);
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = 24;
        text.overflowMode = TextOverflowModes.Truncate;
        RectTransform emptyRect = empty.GetComponent<RectTransform>();
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = new Vector2(24, 24);
        emptyRect.offsetMax = new Vector2(-24, -24);
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            parent.GetChild(i).gameObject.SetActive(false);
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void RemoveLegacyRows()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        foreach (InventoryRowUI row in canvas.GetComponentsInChildren<InventoryRowUI>(true))
            if (inventoryRowPrefab == null || row.gameObject != inventoryRowPrefab.gameObject)
                Destroy(row.gameObject);

        foreach (MarketRowUI row in canvas.GetComponentsInChildren<MarketRowUI>(true))
            if (marketRowPrefab == null || row.gameObject != marketRowPrefab.gameObject)
                Destroy(row.gameObject);
    }

    private void CreateDragPreviews()
    {
        CreateEmptyState(inventoryContent, "로그인하고 나만의 펫을 만나보세요.");
        CreateEmptyState(marketContent, "로그인 후 입양 가능한 펫을 확인하세요.");
    }
    // -------------------------
    // DTOs
    // -------------------------
    [Serializable]
    public class CreateListingResult { public string listingId; }

    [Serializable]
    public class MarketListResult { public long revision; public ListingDto[] listings; }

    [Serializable]
    public class ListingDto
    {
        public string listingId;
        public string status;
        public string sellerPlayerId;
        public string inventoryItemId;
        public Dictionary<string, object> instanceData;
        public string currencyId;
        public int price;
        public long createdAt;
    }

    [Serializable]
    public class BuyResult { public bool ok; public string newPlayersInventoryItemId; }

    [Serializable]
    public class CancelResult { public bool ok; public string returnedPlayersInventoryItemId; }

    [Serializable]
    public class ClaimResult { public bool ok; public long claimed; }
}
