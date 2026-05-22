using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Swole.UI;

using TMPro;

namespace Swole.API.Unity.Store
{

    public class MarketplaceGUI : MonoBehaviour
    {

        [Header("BUY")]
        [SerializeField]
        protected RectTransform buyWindow;

        [SerializeField]
        protected UIRecyclingList buyWindow_listingsList; 

        [SerializeField]
        protected RectTransform auctionInfo_root;
        [SerializeField]
        protected Image auctionInfo_thumbnail;
        [SerializeField]
        protected TMP_Text auctionInfo_displayName;
        [SerializeField]
        protected TMP_Text auctionInfo_sellerName;
        [SerializeField]
        protected TMP_Text auctionInfo_price;

        [SerializeField]
        protected TMP_Text auctionInfo_balanceText;


        [Header("SELL")]
        [SerializeField]
        protected RectTransform sellWindow; 


        protected virtual void OnEnable()
        {

            if (buyWindow != null)
            {
                if (buyWindow_listingsList == null)
                {
                    var listingsRoot = buyWindow.FindDeepChildLiberal("listings");
                    if (listingsRoot == null) listingsRoot = buyWindow;

                    buyWindow_listingsList = listingsRoot.GetComponentInChildren<UIRecyclingList>(true);
                }

                auctionInfo_root = buyWindow.FindDeepChildLiberal("auctionInfo") as RectTransform;
                var auctionInfoRoot = auctionInfo_root;
                if (auctionInfoRoot == null) auctionInfoRoot = buyWindow;

                if (auctionInfo_thumbnail == null) auctionInfo_thumbnail = auctionInfoRoot.gameObject.FindFirstComponentUnderChild<Image>("thumbnail", true);
                if (auctionInfo_displayName == null) auctionInfo_displayName = auctionInfoRoot.gameObject.FindFirstComponentUnderChild<TMP_Text>("displayName", true);
                if (auctionInfo_sellerName == null) auctionInfo_sellerName = auctionInfoRoot.gameObject.FindFirstComponentUnderChild<TMP_Text>("sellerName", true);
                if (auctionInfo_price == null) auctionInfo_price = auctionInfoRoot.gameObject.FindFirstComponentUnderChild<TMP_Text>("price", true);
                if (auctionInfo_balanceText == null) auctionInfo_balanceText = auctionInfoRoot.gameObject.FindFirstComponentUnderChild<TMP_Text>("balance", true); 
            }

            Marketplace.ClearCatalogue();

            var rootObjects = Utils.GetAllRootObjects();
            foreach (var rootObject in rootObjects)
            {
                var provider = rootObject.GetComponent<IMarketableCatalogueProvider>();
                if (provider != null)
                {
                    Marketplace.RegisterCatalogue(provider); 
                }
            }
        }

        protected MarketplaceListing[] allListings;
        protected int selectedListing = -1;

        protected virtual void OnRefreshListingsListMember(UIRecyclingList.MemberData memberData, GameObject instance)
        {
            if (allListings == null) return;

            if (memberData.storage is int listingIndex && listingIndex > 0 && listingIndex <= allListings.Length)
            {
                var listing = allListings[listingIndex];
                Marketplace.TryGetProduct(listing.productId, out var product);
                Marketplace.TryGetCurrency(listing.currencyId, out var currency);

                var thumbnail = instance.FindFirstComponentUnderChild<Image>("thumbnail", true);
                if (thumbnail != null) thumbnail.sprite = product == null ? null : product.PreviewSmall;

                var sellerName = instance.FindFirstComponentUnderChild<TMP_Text>("sellerName", true);
                if (sellerName != null) sellerName.SetText(listing.sellerName);

                var price = instance.FindFirstComponentUnderChild<TMP_Text>("price", true);
                if (price != null) sellerName.SetText(currency == null ? listing.price.ToString() : currency.AsStringWithSuffix(listing.price)); 
            }
        }
        protected virtual void OnClickListingsListMember(UIRecyclingList.MemberData memberData)
        {
            if (memberData.storage is int listingIndex) SetSelectedListing(listingIndex);
        }
        protected virtual void RedrawListings()
        {
            if (buyWindow_listingsList != null)
            {
                buyWindow_listingsList.Clear();
                if (allListings != null)
                {
                    for (int i = 0; i < allListings.Length; i++)
                    {
                        var listing = allListings[i];
                        if (!Marketplace.TryGetProduct(listing.productId, out var product)) continue;

                        buyWindow_listingsList.AddNewMemberWithClickData(product.DisplayName, OnClickListingsListMember, false, OnRefreshListingsListMember, i);
                    }
                }

                buyWindow_listingsList.Refresh();
            }
        }

        public void SetListings(MarketplaceListing[] listings)
        {
            allListings = listings;
            selectedListing = -1;

            RedrawListings();
        }

        public void SetSelectedListing(int listingIndex)
        {
            bool isValid = listingIndex >= 0 && allListings != null && listingIndex < allListings.Length;
            var listing = allListings[listingIndex];
            if (!Marketplace.TryGetProduct(listing.productId, out var product)) isValid = false;
            if (!Marketplace.TryGetCurrency(listing.currencyId, out var currency)) isValid = false;

            var user = SwoleUserAccount.ActiveAccount;
            if (user == null) isValid = false;

            if (auctionInfo_root != null)
            {
                auctionInfo_root.gameObject.SetActive(isValid);
            }

            if (isValid)
            {
                if (auctionInfo_thumbnail != null) auctionInfo_thumbnail.sprite = product.PreviewSmall;
                if (auctionInfo_displayName != null) auctionInfo_displayName.SetText(product.DisplayName);
                if (auctionInfo_sellerName != null) auctionInfo_sellerName.SetText(listing.sellerName);
                if (auctionInfo_price != null) auctionInfo_price.SetText(currency.AsStringWithSuffix(listing.price));
                if (auctionInfo_balanceText != null)
                {
                    user.FetchCurrencyBalance(currency.ID, (float bal) => auctionInfo_balanceText.SetText(currency.AsStringWithSuffix(bal)), null, true);                     
                }
            }
        }

    }

}
