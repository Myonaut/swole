#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.User;
using Swole.Store;

namespace Swole.API.Unity.Store
{

    [CreateAssetMenu(fileName = "product", menuName = "Swole/Store", order = 0)]
    public class CashShopProduct : ScriptableObject, IRealWorldPurchasable
    {

        [SerializeField]
        protected string storeId;
        public string StoreID => storeId; 

        public string ID => name;

        public string ProductID => $"{StoreID}.{ID}";

        public bool IsOwnedByUser() => IsOwnedByUser(SwoleUserAccount.ActiveAccount);
        public bool IsOwnedByUser(ISwoleUser user)
        {
            if (user == null) return false;
            return user.DoesOwnCashShopProduct(ProductID);
        }

        [SerializeField]
        protected string displayName;
        public virtual string DisplayName => displayName;

        [SerializeField, TextArea]
        protected string description;
        public virtual string Description => description;

        [SerializeField]
        protected string category;
        public virtual string Category => category;

        [SerializeField]
        protected string group;
        public virtual string Group => group;

        public void FetchPrice(Action<float> callback, Action<string> errorCallback)
        {
            if (callback == null) return;

            var user = SwoleUserAccount.ActiveAccount;
            if (user == null)
            {
                errorCallback?.Invoke("User not logged in.");
                return;
            }

            user.FetchCashShopProductPrice(ProductID, callback, errorCallback);
        }

        public void FetchCurrencyID(Action<string> callback, Action<string> errorCallback)
        {
            if (callback == null) return;

            var user = SwoleUserAccount.ActiveAccount;
            if (user == null)
            {
                errorCallback?.Invoke("User not logged in.");
                return;
            }

            user.FetchCashShopProductCurrencyID(ProductID, callback, errorCallback);
        }

        public void FetchPriceString(Action<string> callback, Action<string> errorCallback)
        {
            if (callback == null) return;

            var user = SwoleUserAccount.ActiveAccount; 
            if (user == null)
            {
                errorCallback?.Invoke("User not logged in.");
                return;
            }

            user.FetchCashShopProductPriceString(ProductID, callback, errorCallback); 
        }

        [SerializeField]
        protected Sprite previewSmall;
        public virtual Sprite PreviewSmall => previewSmall == null ? previewLarge : previewSmall;

        [SerializeField]
        protected Sprite previewLarge;
        public virtual Sprite PreviewLarge => previewLarge == null ? previewSmall : previewLarge;

        [SerializeField]
        protected List<string> tags = new List<string>();
        public IReadOnlyList<string> Tags => tags;
        public virtual int TagCount => tags == null ? 0 : tags.Count;
        public virtual string GetTag(int index) => (tags == null || index < 0 || index >= tags.Count) ? null : tags[index];
        public virtual bool HasTag(string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < tags.Count; i++) if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        [SerializeField]
        protected List<string> keywords = new List<string>();
        public IReadOnlyList<string> Keywords => keywords;
        public virtual int KeywordCount => keywords == null ? 0 : keywords.Count;
        public virtual string GetKeyword(int index) => (keywords == null || index < 0 || index >= keywords.Count) ? null : keywords[index];
        public virtual bool MatchesKeyword(string query, bool exact = false)
        {
            if (keywords == null || string.IsNullOrEmpty(query)) return false;
            for (int i = 0; i < keywords.Count; i++)
            {
                var k = keywords[i];
                if (string.IsNullOrEmpty(k)) continue;
                if (exact) { if (string.Equals(k, query, StringComparison.OrdinalIgnoreCase)) return true; }
                else if (k.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

    }

}

#endif