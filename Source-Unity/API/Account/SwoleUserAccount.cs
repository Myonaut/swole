using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Swole.User;
using Swole.DataStructures;

namespace Swole.API.Unity
{
    public class SwoleUserAccount : MonoBehaviour, ISwoleUser
    {

        private static SwoleUserAccount activeAccount;
        public static SwoleUserAccount ActiveAccount => activeAccount;
        public static bool IsLoggedIn => activeAccount != null && activeAccount.HasUser;

        [SerializeField]
        protected GameObject usernameDisplay; 

        [SerializeField]
        protected GameObject swoleBucksDisplay;

        [SerializeField]
        protected UnityEvent OnConnect = new UnityEvent();
        [SerializeField]
        protected UnityEvent OnConnecting = new UnityEvent();
        [SerializeField]
        protected UnityEvent OnDisconnect = new UnityEvent();

        [NonSerialized]
        private ISwoleUser user;
        public ISwoleUser User => user;
        public bool HasUser => !ReferenceEquals(user, null);

        public string ApiEndPoint => HasUser ? user.ApiEndPoint : null;

        public void SetUser(ISwoleUser user)
        {
            if (!ReferenceEquals(this.user, null))
            {
                this.user.StopListeningForConnectionInitiation(ConnectingToServer);
                this.user.StopListeningForConnectionConfirmation(ConnectedToServer);
                this.user.StopListeningForDisconnect(DisconnectedFromServer);
                this.user.StopListeningForDataChanges(RefreshDisplays);
            }

            this.user = user;

            if (ReferenceEquals(user, null))
            {
                if (ReferenceEquals(activeAccount, this)) activeAccount = null;

                if (usernameDisplay != null) CustomEditorUtils.SetComponentText(usernameDisplay, "OFFLINE");
                if (swoleBucksDisplay != null) CustomEditorUtils.SetComponentText(swoleBucksDisplay, "0");
            }
            else
            {
                activeAccount = this;

                RefreshDisplays();

                user.ListenForConnectionInitiation(ConnectingToServer);
                user.ListenForConnectionConfirmation(ConnectedToServer);
                user.ListenForDisconnect(DisconnectedFromServer);
                user.ListenForDataChanges(RefreshDisplays);
            }
        }

        protected void RefreshDisplays()
        {
            if (!HasUser) return;
            if (usernameDisplay != null) CustomEditorUtils.SetComponentText(usernameDisplay, user.Username);
            if (swoleBucksDisplay != null) CustomEditorUtils.SetComponentText(swoleBucksDisplay, user.SwoleBucks.ToString());
        }

        protected void ConnectedToServer()
        {
            OnConnect?.Invoke();
        }
        protected void ConnectingToServer()
        {
            OnConnecting?.Invoke();
        }
        protected void DisconnectedFromServer()
        {
            OnDisconnect?.Invoke();
        }

        protected void Awake()
        {
            SetUser(null);
        }

        protected void OnDestroy() 
        {
            SetUser(null);
        }

        #region ISwoleUser

        public string Username => HasUser ? user.Username : string.Empty;

        public int SwoleBucks => HasUser ? user.SwoleBucks : 0;

        public void OpenSwoleBucksStore()
        {
            if (!HasUser) return;
            user.OpenSwoleBucksStore();
        }

        public void FetchCashShop(string storeId, Action<string> callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCashShop(storeId, callback, errorCallback);
        }

        public void FetchCashShopProduct(string productId, Action<string> callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCashShopProduct(productId, callback, errorCallback);
        }

        public void FetchCashShopProductPrice(string productId, Action<float> callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCashShopProductPrice(productId, callback, errorCallback);
        }

        public void FetchCashShopProductCurrencyID(string productId, Action<string> callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCashShopProductCurrencyID(productId, callback, errorCallback);
        }

        public void FetchCashShopProductPriceString(string productId, Action<string> callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCashShopProductPriceString(productId, callback, errorCallback);
        }

        public void TryPurchaseCashShopProduct(string productId, float price, string currencyId, Action onSuccess, Action<string> onError)
        {
            if (!HasUser)
            {
                onError?.Invoke("User is not logged in.");
                return;
            }

            user.TryPurchaseCashShopProduct(productId, price, currencyId, onSuccess, onError);
        }

        public bool DoesOwnCashShopProduct(string productId)
        {
            if (!HasUser) return false;
            return user.DoesOwnCashShopProduct(productId);
        }

        public void DoesOwnCashShopProduct(string productId, Action<bool> callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.DoesOwnCashShopProduct(productId, callback, errorCallback);
        }


        public void FetchCurrencyBalance(string currencyId, Action<float> callback, Action<string> errorCallback, bool forceRefetch = false)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCurrencyBalance(currencyId, callback, errorCallback, forceRefetch);
        }

        public void FetchCurrrencyBalanceAsInt(string currencyId, Action<int> callback, Action<string> errorCallback, bool forceRefetch = false)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchCurrrencyBalanceAsInt(currencyId, callback, errorCallback, forceRefetch);
        }

        public int PopupCount =>HasUser ? user.PopupCount : 0;
        public Popup GetPopup(int index)
        {
            if (!HasUser) return default;
            return user.GetPopup(index);
        }
        public void DismissPopup(int index)
        {
            if (!HasUser) return;
            user.DismissPopup(index);
        }
        public void DismissPopup(string id)
        {
            if (!HasUser) return;
            user.DismissPopup(id);
        }
        public void DismissAllPopups()
        {
            if (!HasUser) return;
            user.DismissAllPopups();
        }
        public void FetchPopups(Action callback, Action<string> errorCallback)
        {
            if (!HasUser)
            {
                errorCallback?.Invoke("User is not logged in.");
                return;
            }

            user.FetchPopups(callback, errorCallback);
        }

        public void ListenForConnectionInitiation(Action onInitiateConnection)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onInitiateConnection);
        }

        public void ListenForConnectionConfirmation(Action onConnect)
        {
            if (!HasUser) return;
            user.ListenForConnectionConfirmation(onConnect);
        }

        public void ListenForDisconnect(Action onDisconnect)
        {
            if (!HasUser) return;
            user.ListenForDisconnect(onDisconnect);
        }

        public void ListenForDataChanges(Action onDataChanged)
        {
            if (!HasUser) return;
            user.ListenForDataChanges(onDataChanged);
        }

        public void StopListeningForConnectionInitiation(Action onInitiateConnection)
        {
            if (!HasUser) return;
            user.StopListeningForConnectionInitiation(onInitiateConnection);
        }

        public void StopListeningForConnectionConfirmation(Action onConnect)
        {
            if (!HasUser) return;
            user.StopListeningForConnectionConfirmation(onConnect);
        }

        public void StopListeningForDisconnect(Action onDisconnect)
        {
            if (!HasUser) return;
            user.StopListeningForDisconnect(onDisconnect);
        }

        public void StopListeningForDataChanges(Action onDataChanged)
        {
            if (!HasUser) return;
            user.StopListeningForDataChanges(onDataChanged);
        }

        public IEnumerator HttpsGet(string url, ISwoleUser.HttpsResponseHandler callback, ISwoleUser.HttpsResponseHandler errorCallback, Action callbackAction, Action<string> callbackActionStr, Action errorCallbackAction, Action<string> errorCallbackActionStr, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            if (!HasUser) yield break;
            yield return user.HttpsGet(url, callback, errorCallback, callbackAction, callbackActionStr, errorCallbackAction, errorCallbackActionStr, headers, timeout);
        }

        public IEnumerator HttpsPost(string url, string json, ISwoleUser.HttpsResponseHandler callback, ISwoleUser.HttpsResponseHandler errorCallback, Action callbackAction, Action<string> callbackActionStr, Action errorCallbackAction, Action<string> errorCallbackActionStr, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            if (!HasUser) yield break;
            yield return user.HttpsPost(url, json, callback, errorCallback, callbackAction, callbackActionStr, errorCallbackAction, errorCallbackActionStr, headers, timeout);
        }

        #endregion

    }
}
