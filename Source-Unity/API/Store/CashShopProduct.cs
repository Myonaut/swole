using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity.Store
{

    [CreateAssetMenu(fileName = "product", menuName = "Swole/Store", order = 0)]
    public class CashShopProduct : ScriptableObject
    {

        public string ID => name;

        [SerializeField]
        protected string displayName;
        public string DisplayName => displayName;

        [SerializeField, TextArea]
        protected string description;
        public string Description => description;

        [SerializeField]
        protected string category;
        public string Category => category;

        [SerializeField]
        protected string group;
        public string Group => group;

        [SerializeField]
        protected Sprite previewThumbnail;
        public Sprite PreviewThumbnail => previewThumbnail == null ? previewFull : previewThumbnail;

        [SerializeField]
        protected Sprite previewFull;
        public Sprite PreviewFull => previewFull;

    }

}
