using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Swole.User;

namespace Swole.API.Unity
{
    public class SwoleUserAccount : MonoBehaviour, ISwoleUser
    {

        private static SwoleUserAccount activeAccount;
        public static SwoleUserAccount ActiveAccount => activeAccount;

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

        public void SetUser(ISwoleUser user)
        {
            if (!ReferenceEquals(this.user, null))
            {
                this.user.StopListeningForConnectionInitiation(ConnectingToServer);
                this.user.StopListeningForConnectionConfirmation(ConnectedToServer);
                this.user.StopListeningForDisconnect(DisconnectedFromServer);
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

                if (usernameDisplay != null) CustomEditorUtils.SetComponentText(usernameDisplay, user.Username);
                if (swoleBucksDisplay != null) CustomEditorUtils.SetComponentText(swoleBucksDisplay, user.SwoleBucks.ToString()); 

                user.ListenForConnectionInitiation(ConnectingToServer);
                user.ListenForConnectionConfirmation(ConnectedToServer);
                user.ListenForDisconnect(DisconnectedFromServer);
            }
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

        public string Username => HasUser ? string.Empty : user.Username;

        public int SwoleBucks => HasUser ? 0 : user.SwoleBucks;

        public void OpenSwoleBucksStore()
        {
            if (!HasUser) return;
            user.OpenSwoleBucksStore();
        }

        public string FetchCashShop(string storeId)
        {
            if (!HasUser) return null;
            return user.FetchCashShop(storeId);
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
        public void FetchPopups(Action callback)
        {
            if (!HasUser) return;
            user.FetchPopups(callback);
        }

        public void ListenForConnectionInitiation(Action onInitiateConnection)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onInitiateConnection);
        }

        public void ListenForConnectionConfirmation(Action onConnect)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onConnect);
        }

        public void ListenForDisconnect(Action onDisconnect)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onDisconnect);
        }

        public void ListenForDataChanges(Action onDataChanged)
        {
            if (!HasUser) return;
            user.ListenForDataChanges(onDataChanged);
        }

        public void StopListeningForConnectionInitiation(Action onInitiateConnection)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onInitiateConnection);
        }

        public void StopListeningForConnectionConfirmation(Action onConnect)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onConnect);
        }

        public void StopListeningForDisconnect(Action onDisconnect)
        {
            if (!HasUser) return;
            user.ListenForConnectionInitiation(onDisconnect);
        }

        public void StopListeningForDataChanges(Action onDataChanged)
        {
            if (!HasUser) return;
            user.StopListeningForDataChanges(onDataChanged);
        }

        #endregion

    }
}
