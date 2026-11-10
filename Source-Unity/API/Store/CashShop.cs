using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity.Store
{

    public class CashShop : MonoBehaviour
    {

        [SerializeField]
        protected string storeId;
        public virtual string StoreId => storeId;

        [SerializeField]
        protected List<CashShopProduct> products = new List<CashShopProduct>(); 
        public virtual int ProductCount => products == null ? 0 : products.Count;

        public virtual CashShopProduct GetProduct(int index)
        {
            if (products == null || index < 0 || index >= products.Count) return null;
            return products[index];
        }

        [SerializeField]
        protected UnityEvent OnStoreFetch = new UnityEvent();

        [SerializeField]
        protected UnityEvent OnStoreRefreshGUI = new UnityEvent();

        public virtual int Balance => SwoleUserAccount.ActiveAccount == null ? 0 : SwoleUserAccount.ActiveAccount.SwoleBucks;

        protected readonly Dictionary<string, CashShopProductInfo> productInfo = new Dictionary<string, CashShopProductInfo>();

        /// <summary>Whether the server currently sells the product. Only valid after a fetch.</summary>
        public virtual bool IsAvailable(CashShopProduct product) => product != null && productInfo.ContainsKey(product.ProductID);
        public virtual bool TryGetProductInfo(CashShopProduct product, out CashShopProductInfo info)
        {
            info = default;
            return product != null && productInfo.TryGetValue(product.ProductID, out info);
        }

        public virtual void FetchStoreBase()
        {
            FetchStore();
        }

        public virtual void FetchStore()
        {
            var userAccount = SwoleUserAccount.ActiveAccount;
            if (userAccount == null || !userAccount.HasUser) return;

            userAccount.FetchCashShop(StoreId, FetchStoreCallback, (error) => Debug.LogError($"Failed to fetch cashshop '{StoreId}': {error}"));
        }
        protected virtual void FetchStoreCallback(string storeJson)
        {
            try
            {
                var storeInfo = swole.FromJson<CashShopInfo>(storeJson);
                if (storeInfo.storeId != StoreId)
                {
                    Debug.LogError($"Returned storeId '{storeInfo.storeId}' did not match cashshop '{StoreId}'");
                    return;
                }

                productInfo.Clear();
                if (storeInfo.products != null)
                {
                    foreach (var info in storeInfo.products) productInfo[info.productId] = info;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse cashshop info for store '{StoreId}'");
                Debug.LogException(ex);

                return;
            }

            OnStoreFetch?.Invoke();
            RefreshGUIBase();
        }

        private void RefreshGUIBase()
        {
            OnStoreRefreshGUI?.Invoke();

            RefreshGUI();
        }

        public virtual void RefreshGUI()
        {
        }
    }

    /// <summary>Response of GET /cashshop. <see cref="ownedProductIds"/> is only filled when logged in.</summary>
    [Serializable]
    public struct CashShopInfo
    {
        public string storeId;
        public CashShopProductInfo[] products;
        public string[] ownedProductIds;
    }

    [Serializable]
    public struct CashShopProductInfo
    {
        /// <summary>'storeId.itemId', matching <see cref="CashShopProduct.ProductID"/></summary>
        public string productId;
        public int price;
        public string currencyId;
    }

    [Serializable]
    public struct CashShopPurchaseRequest
    {
        public string productId;
        public int price;
        public string currencyId;
    }

    [Serializable]
    public struct CashShopPurchaseResponse
    {
        public bool wasSuccessful;
        public string info;
        /// <summary>New balance of the purchase currency, or -1 if unknown</summary>
        public int balance;
    }

}
