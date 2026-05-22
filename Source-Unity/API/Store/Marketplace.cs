using System;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity.Store
{

    /// <summary>
    /// Frontend API for querying player-driven marketplace listings that live on a backend database.
    /// The game is expected to populate the local product catalogue with the archetypes it knows about;
    /// search calls will resolve product-based queries against the local catalogue first and then
    /// dispatch the resulting product ids to the backend. Seller/game-scoped queries are forwarded
    /// directly to the backend. All inventory mutation and purchase fulfilment is handled server-side;
    /// this class only issues requests and surfaces results.
    /// </summary>
    public static class Marketplace
    {

        #region Local Catalogue

        public static void RegisterCatalogue(IMarketableCatalogueProvider catalogueProvider)
        {
            RegisterProducts(catalogueProvider);
            RegisterCurrencies(catalogueProvider);
        }

        public static void ClearCatalogue()
        {
            ClearProducts();
            ClearCurrencies();
        }

        private static readonly Dictionary<string, CashShopProduct> catalogue = new Dictionary<string, CashShopProduct>(StringComparer.OrdinalIgnoreCase);

        public static int CatalogueCount => catalogue.Count;

        public static bool RegisterProduct(CashShopProduct product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ID)) return false;
            catalogue[product.ID] = product;
            return true;
        }

        public static int RegisterProducts(IEnumerable<CashShopProduct> products)
        {
            if (products == null) return 0;
            int count = 0;
            foreach (var p in products) if (RegisterProduct(p)) count++;
            return count;
        }

        public static int RegisterProducts(IMarketableCatalogueProvider catalogueProvider)
        {
            return RegisterProducts(catalogueProvider.GetProducts());
        }

        public static bool UnregisterProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) return false;
            return catalogue.Remove(productId);
        }

        public static bool UnregisterProduct(CashShopProduct product) => product != null && UnregisterProduct(product.ID);

        public static void ClearProducts() => catalogue.Clear();

        public static bool TryGetProduct(string productId, out CashShopProduct product)
        {
            product = null;
            if (string.IsNullOrWhiteSpace(productId)) return false;
            return catalogue.TryGetValue(productId, out product);
        }

        public static CashShopProduct GetProduct(string productId) => TryGetProduct(productId, out var p) ? p : null;

        public static IEnumerable<CashShopProduct> AllProducts => catalogue.Values;

        public static List<CashShopProduct> FindProducts(Predicate<CashShopProduct> predicate, List<CashShopProduct> output = null)
        {
            if (output == null) output = new List<CashShopProduct>();
            if (predicate == null) return output;
            foreach (var p in catalogue.Values) if (p != null && predicate(p)) output.Add(p);
            return output;
        }

        public static List<CashShopProduct> FindProductsByName(string nameQuery, bool exact = false, List<CashShopProduct> output = null)
        {
            if (output == null) output = new List<CashShopProduct>();
            if (string.IsNullOrWhiteSpace(nameQuery)) return output;
            foreach (var p in catalogue.Values)
            {
                if (p == null) continue;
                if (MatchesProductName(p, nameQuery, exact)) output.Add(p);
            }
            return output;
        }

        public static List<CashShopProduct> FindProductsByCategory(string category, List<CashShopProduct> output = null)
        {
            if (output == null) output = new List<CashShopProduct>();
            if (string.IsNullOrWhiteSpace(category)) return output;
            foreach (var p in catalogue.Values) if (p != null && string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase)) output.Add(p);
            return output;
        }

        public static List<CashShopProduct> FindProductsByGroup(string group, List<CashShopProduct> output = null)
        {
            if (output == null) output = new List<CashShopProduct>();
            if (string.IsNullOrWhiteSpace(group)) return output;
            foreach (var p in catalogue.Values) if (p != null && string.Equals(p.Group, group, StringComparison.OrdinalIgnoreCase)) output.Add(p);
            return output;
        }

        public static List<CashShopProduct> FindProductsByTag(string tag, List<CashShopProduct> output = null)
        {
            if (output == null) output = new List<CashShopProduct>();
            if (string.IsNullOrWhiteSpace(tag)) return output;
            foreach (var p in catalogue.Values) if (p != null && p.HasTag(tag)) output.Add(p);
            return output;
        }

        public static List<CashShopProduct> FindProductsByKeyword(string keyword, bool exact = false, List<CashShopProduct> output = null)
        {
            if (output == null) output = new List<CashShopProduct>();
            if (string.IsNullOrWhiteSpace(keyword)) return output;
            foreach (var p in catalogue.Values) if (p != null && p.MatchesKeyword(keyword, exact)) output.Add(p);
            return output;
        }

        /// <summary>
        /// Matches a name-style query against a product's display name, id, and keyword list.
        /// </summary>
        public static bool MatchesProductName(CashShopProduct p, string query, bool exact)
        {
            if (p == null || string.IsNullOrEmpty(query)) return false;
            if (MatchesText(p.DisplayName, query, exact)) return true;
            if (MatchesText(p.ID, query, exact)) return true;
            if (p.MatchesKeyword(query, exact)) return true;
            return false;
        }

        private static bool MatchesText(string source, string query, bool exact)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return exact
                ? string.Equals(source, query, StringComparison.OrdinalIgnoreCase)
                : source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private static readonly Dictionary<string, GameCurrency> currencies = new Dictionary<string, GameCurrency>(StringComparer.OrdinalIgnoreCase);

        public static int CurrencyCount => currencies.Count;

        public static bool RegisterCurrency(GameCurrency currency)
        {
            if (currency == null || string.IsNullOrWhiteSpace(currency.ID)) return false;
            currencies[currency.ID] = currency;
            return true;
        }

        public static int RegisterCurrencies(IEnumerable<GameCurrency> currencies)
        {
            if (currencies == null) return 0;
            int count = 0;
            foreach (var c in currencies) if (RegisterCurrency(c)) count++;
            return count;
        }

        public static int RegisterCurrencies(IMarketableCatalogueProvider catalogueProvider)
        {
            return RegisterCurrencies(catalogueProvider.GetCurrencies());
        }

        public static bool UnregisterCurrency(string currencyId)
        {
            if (string.IsNullOrWhiteSpace(currencyId)) return false;
            return currencies.Remove(currencyId);
        }

        public static bool UnregisterCurrency(GameCurrency currency) => currency != null && UnregisterProduct(currency.ID);

        public static void ClearCurrencies() => currencies.Clear();

        public static bool TryGetCurrency(string currencyId, out GameCurrency currency)
        {
            currency = null;
            if (string.IsNullOrWhiteSpace(currencyId)) return false;
            return currencies.TryGetValue(currencyId, out currency);
        }

        public static GameCurrency GetCurrency(string currencyId) => TryGetCurrency(currencyId, out var c) ? c : null;

        public static IEnumerable<GameCurrency> AllCurrencies => currencies.Values;


        #endregion

        #region Search API

        public enum SortOrder
        {
            None,
            PriceAscending,
            PriceDescending,
            NewestFirst,
            OldestFirst,
        }

        /// <summary>
        /// Search descriptor used by <see cref="Search"/>. Any field left null/default is ignored.
        /// Product-based fields (name/category/group) are resolved against the local catalogue first
        /// to produce a set of product ids that get forwarded to the backend. Seller and game scoped
        /// fields are forwarded directly to the backend without consulting the local catalogue.
        /// </summary>
        [Serializable]
        public struct SearchQuery
        {
            public string nameQuery;
            public string category;
            public string group;
            public string tag;
            public string[] tagsAll;
            public bool exactNameMatch;

            public string sellerName;
            public string sellerId;
            public string gameId;

            public int minPrice;
            public int maxPrice;
            public bool hasPriceFilter;

            public SortOrder sortOrder;
            public int maxResults;
            public int pageOffset;
        }

        public delegate void ListingsCallback(MarketplaceListing[] listings);
        public delegate void ErrorCallback(string error);
        public delegate void PurchaseCallback(PurchaseResult result);

        /// <summary>
        /// Dispatches the appropriate backend lookup for the supplied <paramref name="query"/>.
        /// Product-scoped fields are resolved against the local catalogue first; if none match, the
        /// backend is not contacted and an empty result is returned via <paramref name="onComplete"/>.
        /// </summary>
        public static void Search(SearchQuery query, ListingsCallback onComplete, ErrorCallback onError = null)
        {
            bool hasSellerLookup = !string.IsNullOrWhiteSpace(query.sellerId) || !string.IsNullOrWhiteSpace(query.sellerName);
            bool hasGameLookup = !string.IsNullOrWhiteSpace(query.gameId);

            if (hasSellerLookup)
            {
                SearchListingsBySeller(query.sellerId, query.sellerName, query, onComplete, onError);
                return;
            }

            if (hasGameLookup)
            {
                SearchListingsByGame(query.gameId, query, onComplete, onError);
                return;
            }

            bool hasTagsAll = query.tagsAll != null && query.tagsAll.Length > 0;
            bool hasProductFilter = !string.IsNullOrWhiteSpace(query.nameQuery)
                || !string.IsNullOrWhiteSpace(query.category)
                || !string.IsNullOrWhiteSpace(query.group)
                || !string.IsNullOrWhiteSpace(query.tag)
                || hasTagsAll;

            if (hasProductFilter)
            {
                var matches = ResolveLocalProductIds(query);
                if (matches.Count <= 0)
                {
                    onComplete?.Invoke(Array.Empty<MarketplaceListing>());
                    return;
                }
                SearchListingsByProductIds(matches, query, onComplete, onError);
                return;
            }

            SearchAllListings(query, onComplete, onError);
        }

        public static List<string> ResolveLocalProductIds(SearchQuery query, List<string> output = null)
        {
            if (output == null) output = new List<string>();
            bool hasTagsAll = query.tagsAll != null && query.tagsAll.Length > 0;
            foreach (var p in catalogue.Values)
            {
                if (p == null) continue;

                if (!string.IsNullOrWhiteSpace(query.category) && !string.Equals(p.Category, query.category, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(query.group) && !string.Equals(p.Group, query.group, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(query.tag) && !p.HasTag(query.tag)) continue;
                if (hasTagsAll)
                {
                    bool missing = false;
                    for (int i = 0; i < query.tagsAll.Length; i++)
                    {
                        var t = query.tagsAll[i];
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        if (!p.HasTag(t)) { missing = true; break; }
                    }
                    if (missing) continue;
                }
                if (!string.IsNullOrWhiteSpace(query.nameQuery) && !MatchesProductName(p, query.nameQuery, query.exactNameMatch)) continue;

                output.Add(p.ID);
            }
            return output;
        }

        #endregion

        #region Backend Requests

        /// <summary>
        /// Base URL of the marketplace backend. Endpoints below are concatenated onto this root.
        /// Override at boot before issuing any requests.
        /// </summary>
        public static string BackendUrl = string.Empty;

        public const string EndpointSearchByProductIds = "/marketplace/listings/byProducts";
        public const string EndpointSearchBySeller = "/marketplace/listings/bySeller";
        public const string EndpointSearchByGame = "/marketplace/listings/byGame";
        public const string EndpointSearchAll = "/marketplace/listings";
        public const string EndpointBuyNow = "/marketplace/purchase";

        public static void SearchListingsByProductIds(IList<string> productIds, SearchQuery query, ListingsCallback onComplete, ErrorCallback onError = null)
        {
            if (productIds == null || productIds.Count <= 0)
            {
                onComplete?.Invoke(Array.Empty<MarketplaceListing>());
                return;
            }

            var body = new ProductIdsRequest()
            {
                productIds = new string[productIds.Count],
                minPrice = query.hasPriceFilter ? query.minPrice : 0,
                maxPrice = query.hasPriceFilter ? query.maxPrice : 0,
                hasPriceFilter = query.hasPriceFilter,
                sortOrder = (int)query.sortOrder,
                maxResults = query.maxResults,
                pageOffset = query.pageOffset,
            };
            for (int i = 0; i < productIds.Count; i++) body.productIds[i] = productIds[i];

            SendListingsRequest(EndpointSearchByProductIds, swole.ToJson(body), onComplete, onError);
        }

        public static void SearchListingsBySeller(string sellerId, string sellerName, SearchQuery query, ListingsCallback onComplete, ErrorCallback onError = null)
        {
            var body = new SellerRequest()
            {
                sellerId = sellerId,
                sellerName = sellerName,
                minPrice = query.hasPriceFilter ? query.minPrice : 0,
                maxPrice = query.hasPriceFilter ? query.maxPrice : 0,
                hasPriceFilter = query.hasPriceFilter,
                sortOrder = (int)query.sortOrder,
                maxResults = query.maxResults,
                pageOffset = query.pageOffset,
            };
            SendListingsRequest(EndpointSearchBySeller, swole.ToJson(body), onComplete, onError);
        }

        public static void SearchListingsByGame(string gameId, SearchQuery query, ListingsCallback onComplete, ErrorCallback onError = null)
        {
            var body = new GameRequest()
            {
                gameId = gameId,
                minPrice = query.hasPriceFilter ? query.minPrice : 0,
                maxPrice = query.hasPriceFilter ? query.maxPrice : 0,
                hasPriceFilter = query.hasPriceFilter,
                sortOrder = (int)query.sortOrder,
                maxResults = query.maxResults,
                pageOffset = query.pageOffset,
            };
            SendListingsRequest(EndpointSearchByGame, swole.ToJson(body), onComplete, onError);
        }

        public static void SearchAllListings(SearchQuery query, ListingsCallback onComplete, ErrorCallback onError = null)
        {
            var body = new GenericRequest()
            {
                minPrice = query.hasPriceFilter ? query.minPrice : 0,
                maxPrice = query.hasPriceFilter ? query.maxPrice : 0,
                hasPriceFilter = query.hasPriceFilter,
                sortOrder = (int)query.sortOrder,
                maxResults = query.maxResults,
                pageOffset = query.pageOffset,
            };
            SendListingsRequest(EndpointSearchAll, swole.ToJson(body), onComplete, onError);
        }

        /// <summary>
        /// Immediately purchases the supplied listing. Inventory mutation, balance checks and
        /// fulfilment are performed entirely server-side; the callback only reports the outcome.
        /// </summary>
        public static void BuyNow(string listingId, PurchaseCallback onComplete, ErrorCallback onError = null)
        {
            if (string.IsNullOrWhiteSpace(listingId))
            {
                onError?.Invoke("listingId was null or empty.");
                return;
            }

            var body = new PurchaseRequest() { listingId = listingId };
            SendPurchaseRequest(EndpointBuyNow, swole.ToJson(body), onComplete, onError);
        }

        #endregion

        #region Transport (placeholder)

        /// <summary>
        /// Placeholder transport that should issue an HTTPS POST to <paramref name="endpoint"/>
        /// with <paramref name="jsonBody"/> and deserialise the response into a <see cref="ListingsResponse"/>.
        /// Replace the body of this method with the actual networking implementation.
        /// </summary>
        public static void SendListingsRequest(string endpoint, string jsonBody, ListingsCallback onComplete, ErrorCallback onError)
        {
            // TODO: issue HTTPS POST to BackendUrl + endpoint with jsonBody, parse response via swole.FromJson<ListingsResponse>,
            // and invoke onComplete(response.listings) or onError(message) on the main thread.
            onComplete?.Invoke(Array.Empty<MarketplaceListing>());
        }

        /// <summary>
        /// Placeholder transport that should issue an HTTPS POST to <paramref name="endpoint"/>
        /// with <paramref name="jsonBody"/> and deserialise the response into a <see cref="PurchaseResult"/>.
        /// Replace the body of this method with the actual networking implementation.
        /// </summary>
        public static void SendPurchaseRequest(string endpoint, string jsonBody, PurchaseCallback onComplete, ErrorCallback onError)
        {
            // TODO: issue HTTPS POST to BackendUrl + endpoint with jsonBody, parse response via swole.FromJson<PurchaseResult>,
            // and invoke onComplete(result) or onError(message) on the main thread.
            onComplete?.Invoke(default);
        }

        #endregion

        #region DTOs

        [Serializable]
        public struct ProductIdsRequest
        {
            public string[] productIds;
            public int minPrice;
            public int maxPrice;
            public bool hasPriceFilter;
            public int sortOrder;
            public int maxResults;
            public int pageOffset;
        }

        [Serializable]
        public struct SellerRequest
        {
            public string sellerId;
            public string sellerName;
            public int minPrice;
            public int maxPrice;
            public bool hasPriceFilter;
            public int sortOrder;
            public int maxResults;
            public int pageOffset;
        }

        [Serializable]
        public struct GameRequest
        {
            public string gameId;
            public int minPrice;
            public int maxPrice;
            public bool hasPriceFilter;
            public int sortOrder;
            public int maxResults;
            public int pageOffset;
        }

        [Serializable]
        public struct GenericRequest
        {
            public int minPrice;
            public int maxPrice;
            public bool hasPriceFilter;
            public int sortOrder;
            public int maxResults;
            public int pageOffset;
        }

        [Serializable]
        public struct PurchaseRequest
        {
            public string listingId;
        }

        /// <summary>
        /// Shape of the JSON envelope returned by the backend for any listings query.
        /// Reserved for future fields (paging cursors, totals, etc.) without breaking call sites.
        /// </summary>
        [Serializable]
        public struct ListingsResponse
        {
            public MarketplaceListing[] listings;
            public int totalResults;
            public int pageOffset;
        }

        #endregion
    }

    /// <summary>
    /// A single listing on the marketplace. Mirrors the JSON structure returned by the backend.
    /// Additional fields can be appended without breaking existing consumers.
    /// </summary>
    [Serializable]
    public struct MarketplaceListing
    {
        public string listingId;
        public string productId;
        public string sellerId;
        public string sellerName;
        public int price;
        public string currencyId;
        public int quantity;
        public long listedAtUnix;
        public string gameId;
    }

    /// <summary>
    /// Result envelope returned by <see cref="Marketplace.BuyNow"/>. Extend as the backend evolves.
    /// </summary>
    [Serializable]
    public struct PurchaseResult
    {
        public bool success;
        public string listingId;
        public string productId;
        public int pricePaid;
        public string currency;
        public string transactionId;
        public string message;
    }

    public interface IMarketableCatalogueProvider
    {
        public IEnumerable<CashShopProduct> GetProducts();
        public IEnumerable<GameCurrency> GetCurrencies();
    }

}



