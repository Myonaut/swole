using System;
using System.Collections.Generic;

#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif

namespace Swole.Store
{
    public interface IPurchasable
    {

        public string StoreID { get; }

        public string ID { get; }

        public string ProductID { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string Category { get; }

        public string Group { get; }

        public void FetchPrice(Action<float> callback, Action<string> errorCallback);
        public void FetchCurrencyID(Action<string> callback, Action<string> errorCallback);
        public void FetchPriceString(Action<string> callback, Action<string> errorCallback);

#if UNITY_2017_1_OR_NEWER
        public Sprite PreviewSmall { get; }

        public Sprite PreviewLarge { get; }
#endif

        public IReadOnlyList<string> Tags { get; }
        public int TagCount { get; }
        public string GetTag(int index);
        public bool HasTag(string tag);

        public IReadOnlyList<string> Keywords { get; }
        public int KeywordCount { get; }
        public string GetKeyword(int index);
        public bool MatchesKeyword(string query, bool exact = false);

    }
}
