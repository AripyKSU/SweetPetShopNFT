using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instanceText;
    [SerializeField] private TextMeshProUGUI optionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button sellBtn;

    private string playersInventoryItemId;
    private int sellPrice;
    private Func<string, int, Task> createListingAsync;

    public void Bind(
        MarketCloudClient.Item item,
        string displayName,
        Sprite icon,
        int price,
        Func<string, int, Task> createListingFunc,
        Action<string, string, int, PointerEventData> droppedFunc = null)
    {
        // ★ 디버그: 전달받은 값 확인
        Debug.Log($"[InventoryRowUI.Bind] InventoryItemId: {item.InventoryItemId}");
        Debug.Log($"[InventoryRowUI.Bind] PlayersInventoryItemId: {item.PlayersInventoryItemId}");

        playersInventoryItemId = item.PlayersInventoryItemId;
        sellPrice = price;
        bool isNft = item.InventoryItemId == "MYTHIC_SWORD_NFT";
        createListingAsync = isNft ? null : createListingFunc;

        if (titleText != null) titleText.gameObject.SetActive(false);

        if(iconImage != null) {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }

        if (priceText != null) priceText.gameObject.SetActive(false);
        if (instanceText != null) instanceText.gameObject.SetActive(false);
        if (optionText != null) optionText.gameObject.SetActive(false);

        if (sellBtn != null)
        {
            sellBtn.onClick.RemoveAllListeners();
            sellBtn.gameObject.SetActive(false);
        }

        PetDisplayUI display = GetComponent<PetDisplayUI>();
        if (display != null) display.Bind(item.InventoryItemId, icon, false, false,
            e => droppedFunc?.Invoke(item.PlayersInventoryItemId, item.InventoryItemId, price, e));
    }

    private async Task SellAsync()
    {
        // ★ 디버그: Sell 시점에 ID 확인
        Debug.Log($"[InventoryRowUI.SellAsync] playersInventoryItemId: '{playersInventoryItemId}'");

        if (string.IsNullOrEmpty(playersInventoryItemId))
        {
            Debug.LogError("[SellAsync] playersInventoryItemId is null or empty!");
            return;
        }

        if (createListingAsync == null)
        {
            Debug.LogError("[SellAsync] createListingAsync is null!");
            return;
        }

        
        Debug.Log($"[SellAsync] Calling createListingAsync with ID: {playersInventoryItemId}, Price: {sellPrice}");

        await createListingAsync.Invoke(playersInventoryItemId, sellPrice);
    }

    private static string TryGetString(Dictionary<string, object> data, string key, string defaultValue)
    {
        if (data == null) return defaultValue;
        if (!data.TryGetValue(key, out object value)) return defaultValue;
        return value != null ? value.ToString() : defaultValue;
    }

    private static int TryGetInt(Dictionary<string, object> data, string key, int defaultValue)
    {
        if (data == null) return defaultValue;
        if (!data.TryGetValue(key, out object value)) return defaultValue;

        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (int.TryParse(value.ToString(), out int parsed)) return parsed;

        return defaultValue;
    }
}
