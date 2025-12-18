#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.Morphing
{
    public class CustomizableCharacterMeshCollection : MonoBehaviour
    {

        #region Sub Types

        public struct V1
        {
            public string id;
            public CustomizableCharacterMesh mesh;
        }
        public struct V2
        {
            public string id;
            public CustomizableCharacterMeshV2 mesh;
        }

        #endregion

        [SerializeField]
        protected V1[] meshesV1;

        [SerializeField]
        protected V2[] meshesV2;

        public void AddToCollection(string id, ICustomizableCharacter mesh)
        {
            if (mesh is CustomizableCharacterMesh v1)
            {
                if (meshesV1 == null) meshesV1 = new V1[0];
                meshesV1 = (V1[])meshesV1.Add(new V1()
                {
                    id = id,
                    mesh = v1
                });
            } 
            else if (mesh is CustomizableCharacterMeshV2 v2)
            {
                if (meshesV2 == null) meshesV2 = new V2[0];
                meshesV2 = (V2[])meshesV2.Add(new V2()
                {
                    id = id,
                    mesh = v2
                });
            }
        }

        public bool TryGetMesh(string id, out ICustomizableCharacter mesh)
        {
            mesh = null;

            if (meshesV2 != null && meshesV2.Length > 0)
            {
                foreach(var mesh_ in meshesV2)
                {
                    if (mesh_.id == id)
                    {
                        mesh = mesh_.mesh;
                        return true;
                    }
                }
            }

            if (meshesV1 != null && meshesV1.Length > 0)
            {
                foreach (var mesh_ in meshesV1)
                {
                    if (mesh_.id == id)
                    {
                        mesh = mesh_.mesh;
                        return true;
                    }
                }
            }

            return false;
        }

        public int MeshCount => (meshesV1 == null ? 0 : meshesV1.Length) + (meshesV2 == null ? 0 : meshesV2.Length);

        public ICustomizableCharacter this[int index]
        {
            get
            {
                int indexOffset = 0;
                if (meshesV1 != null)
                {
                    if (index >= 0 && index < meshesV1.Length) return meshesV1[index].mesh;
                    indexOffset += meshesV1.Length;
                }

                if (meshesV2 != null)
                {
                    int ind = index - indexOffset;
                    if (ind >= 0 && ind < meshesV2.Length) return meshesV2[index].mesh;
                    indexOffset += meshesV2.Length;
                }

                return null;
            }
        }

    }
}

#endif