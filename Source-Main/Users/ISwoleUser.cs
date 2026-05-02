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

        public void ListenForConnectionInitiation(Action onInitiateConnection);
        public void ListenForConnectionConfirmation(Action onConnect);
        public void ListenForDisconnect(Action onDisconnect);

        public void StopListeningForConnectionInitiation(Action onInitiateConnection);
        public void StopListeningForConnectionConfirmation(Action onConnect);
        public void StopListeningForDisconnect(Action onDisconnect);
    }
}
