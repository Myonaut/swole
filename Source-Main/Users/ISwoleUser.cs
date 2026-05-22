using System;
using System.Collections;
using System.Collections.Generic;

using Swole.DataStructures;

namespace Swole.User
{
    public interface ISwoleUser
    {
        public string ApiEndPoint { get; }

        public string Username { get; }
        public int SwoleBucks { get; }

        public void OpenSwoleBucksStore();

        public string FetchCashShop(string storeId);
  
        public void FetchCurrencyBalance(string currencyId, Action<float> callback, Action<string> errorCallback, bool forceRefetch = false);
        public void FetchCurrrencyBalanceAsInt(string currencyId, Action<int> callback, Action<string> errorCallback, bool forceRefetch = false);

        public int PopupCount { get; }
        public Popup GetPopup(int index);
        public void DismissPopup(int index);
        public void DismissPopup(string id);
        public void DismissAllPopups();
        public void FetchPopups(Action callback, Action<string> errorCallback);

        public void ListenForConnectionInitiation(Action onInitiateConnection);
        public void ListenForConnectionConfirmation(Action onConnect);
        public void ListenForDisconnect(Action onDisconnect);
        public void ListenForDataChanges(Action onDataChanged);

        public void StopListeningForConnectionInitiation(Action onInitiateConnection);
        public void StopListeningForConnectionConfirmation(Action onConnect);
        public void StopListeningForDisconnect(Action onDisconnect);
        public void StopListeningForDataChanges(Action onDataChanged);

        public delegate void HttpsResponseHandler(string response);

        public IEnumerator HttpsGet(string url, HttpsResponseHandler callback, HttpsResponseHandler errorCallback, Action callbackAction, Action<string> callbackActionStr, Action errorCallbackAction, Action<string> errorCallbackActionStr, IEnumerable<StringPair> headers = null, int timeout = 10);

        public IEnumerator HttpsPost(string url, string json, HttpsResponseHandler callback, HttpsResponseHandler errorCallback, Action callbackAction, Action<string> callbackActionStr, Action errorCallbackAction, Action<string> errorCallbackActionStr, IEnumerable<StringPair> headers = null, int timeout = 10);

    }

    public static class SwoleUserExtensions
    {
        // GET
        public static IEnumerator HttpsGetWithHandler(this ISwoleUser user, string url, ISwoleUser.HttpsResponseHandler callback, ISwoleUser.HttpsResponseHandler errorCallback, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            yield return user.HttpsGet(url, callback, errorCallback, null, null, null, null, headers, timeout);
        }
        public static IEnumerator HttpsGetWithAction(this ISwoleUser user, string url, Action callbackAction, Action errorCallbackAction, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            yield return user.HttpsGet(url, null, null, callbackAction, null, errorCallbackAction, null, headers, timeout);
        }
        public static IEnumerator HttpsGetWithActionStr(this ISwoleUser user, string url, Action<string> callbackActionStr, Action<string> errorCallbackActionStr, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            yield return user.HttpsGet(url, null, null, null, callbackActionStr, null, errorCallbackActionStr, headers, timeout);
        }

        // POST
        public static IEnumerator HttpsPostWithHandler(this ISwoleUser user, string url, string json, ISwoleUser.HttpsResponseHandler callback, ISwoleUser.HttpsResponseHandler errorCallback, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            yield return user.HttpsPost(url, json, callback, errorCallback, null, null, null, null, headers, timeout);
        }
        public static IEnumerator HttpsPostWithAction(this ISwoleUser user, string url, string json, Action callbackAction, Action errorCallbackAction, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            yield return user.HttpsPost(url, json, null, null, callbackAction, null, errorCallbackAction, null, headers, timeout);
        }
        public static IEnumerator HttpsPostWithActionStr(this ISwoleUser user, string url, string json, Action<string> callbackActionStr, Action<string> errorCallbackActionStr, IEnumerable<StringPair> headers = null, int timeout = 10)
        {
            yield return user.HttpsPost(url, json, null, null, null, callbackActionStr, null, errorCallbackActionStr, headers, timeout);
        }
    }

    [Serializable]
    public struct Popup
    {
        public string id;
        public string subject;
        public string body;
    }

}
