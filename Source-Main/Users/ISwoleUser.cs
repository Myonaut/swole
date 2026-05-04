using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole.User
{
    public interface ISwoleUser
    {
        public string Username { get; }
        public int SwoleBucks { get; }

        public void OpenSwoleBucksStore();

        public string FetchCashShop(string storeId);

        public int PopupCount { get; }
        public Popup GetPopup(int index);
        public void DismissPopup(int index);
        public void DismissPopup(string id);
        public void DismissAllPopups();
        public void FetchPopups(Action callback);

        public void ListenForConnectionInitiation(Action onInitiateConnection);
        public void ListenForConnectionConfirmation(Action onConnect);
        public void ListenForDisconnect(Action onDisconnect);
        public void ListenForDataChanges(Action onDataChanged);

        public void StopListeningForConnectionInitiation(Action onInitiateConnection);
        public void StopListeningForConnectionConfirmation(Action onConnect);
        public void StopListeningForDisconnect(Action onDisconnect);
        public void StopListeningForDataChanges(Action onDataChanged);
    }

    [Serializable]
    public struct Popup
    {
        public string id;
        public string subject;
        public string body;
    }

}
