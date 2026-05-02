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

        public virtual void FetchStoreBase()
        {
            FetchStore();

            OnStoreFetch?.Invoke();

            RefreshGUIBase();
        }

        public virtual void FetchStore()
        {
            var userAccount = SwoleUserAccount.ActiveAccount;
            if (userAccount == null || !userAccount.HasUser) return;

            if (products == null) products = new List<CashShopProduct>();
            products.Clear();

            var storejson = userAccount.FetchCashShop(storeId);
            CashShopInfo storeInfo = default;
            try
            {
                storeInfo = swole.FromJson<CashShopInfo>(storejson);
                if (string.IsNullOrWhiteSpace(storeInfo.storeId))
                {
                    Debug.LogError($"Returned storeId was invalid for cashshop '{storeId}'");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse cashshop info for store '{storeId}'");
                Debug.LogException(ex);

                return;
            }
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

    [Serializable]
    public struct CashShopInfo
    {

        public string storeId;

        public string[] productIds;

        public CashShopProductInfo[] dynamicProductInfo;

    }

    [Serializable]
    public struct CashShopProductInfo
    {

        public string productId;

        public CashShopProductAttribute[] attributes;

    }

    [Serializable]
    public struct CashShopProductAttribute
    {

        public string attribute;

        public string attributeValue;

    }

}
