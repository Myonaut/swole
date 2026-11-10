#if UNITY_2017_1_OR_NEWER

using UnityEngine;

namespace Swole.Modding
{
    public interface IPreviewable
    {
        public Texture2D PreviewImageRaw { get; }

        public Sprite PreviewImage { get; }

        public void Cleanup();

    }
}

#endif