using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Authentication;
using UnityEngine.EventSystems;

public class MarketRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Image iconImage;

    private string listingId;
    private Func<string, System.Threading.Tasks.Task> buyAsync;
    private Func<string, System.Threading.Tasks.Task> cancelAsync;

    public void Bind(
        PortfolioMarketDemo.ListingDto listing,
        string displayName,
        Sprite iconSprite,
        Func<string, System.Threading.Tasks.Task> buyFunc,
        Func<string, System.Threading.Tasks.Task> cancelFunc,
        Action<string, bool, string, int, PointerEventData> droppedFunc = null)
    {
        listingId = listing.listingId;
        buyAsync = buyFunc;
        cancelAsync = cancelFunc;

        if (titleText != null) titleText.gameObject.SetActive(false);
        if (priceText != null) priceText.text = $"{listing.price:N0} {listing.currencyId}";

        if (iconImage != null) {
            iconImage.sprite = iconSprite;
            iconImage.gameObject.SetActive(iconImage != null);
        }

        if (buyBtn != null)
        {
            buyBtn.onClick.RemoveAllListeners();
            buyBtn.gameObject.SetActive(false);
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.gameObject.SetActive(false);
        }

        bool isMine = AuthenticationService.Instance.IsSignedIn &&
                      listing.sellerPlayerId == AuthenticationService.Instance.PlayerId;
        PetDisplayUI display = GetComponent<PetDisplayUI>();
        if (display != null) display.Bind(listing.inventoryItemId, iconSprite, isMine, false,
            e => droppedFunc?.Invoke(listing.listingId, isMine, listing.inventoryItemId, listing.price, e));
    }

    private async System.Threading.Tasks.Task BuyAsync()
    {
        if (buyAsync == null) return;
        await buyAsync.Invoke(listingId);
    }

    private async System.Threading.Tasks.Task CancelAsync()
    {
        if (cancelAsync == null) return;
        await cancelAsync.Invoke(listingId);
    }
}
