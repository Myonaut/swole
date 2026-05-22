using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity.Store
{
    [CreateAssetMenu(fileName = "catalogue", menuName = "Swole/Store", order = 1)]
    public class CashShopCatalogue : ScriptableObject, IMarketableCatalogueProvider
    {

        [SerializeField]
        protected CashShopProduct[] products;
        public IEnumerable<CashShopProduct> GetProducts() => products;

        [SerializeField]
        protected GameCurrency[] currencies;
        public IEnumerable<GameCurrency> GetCurrencies() => currencies;

    }
}
