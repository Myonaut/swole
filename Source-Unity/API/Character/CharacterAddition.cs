#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;
using Swole.Morphing;

namespace Swole
{

    [CreateAssetMenu(fileName = "CharacterAddition", menuName = "Swole/Character/CharacterAddition", order = 1)]
    public class CharacterAddition : ScriptableObject
    {

        [Serializable]
        public enum MeshType
        {
            UnitySkinnedMesh, CustomizableCharacterMesh
        }

        public MeshType meshType;

        public Vector3 offset;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one; 

        [Header("Skinning")]
        public CustomAvatar avatar;

        [Header("Unity Mesh Renderer")]
        public Mesh mesh;
        public Material[] materials;

        public Vector3 boundsCenter;
        public Vector3 boundsExtents;

        public MeshRenderer SpawnAsMeshRenderer(Transform parent, string name = null)
        {
            var filter = new GameObject(this.name).AddComponent<MeshFilter>();
            var renderer = filter.gameObject.AddComponent<MeshRenderer>();
            var transform = filter.transform;

            if (!string.IsNullOrWhiteSpace(name)) transform.name = name;

            if (parent != null) transform.SetParent(parent, false);

            transform.SetLocalPositionAndRotation(offset, Quaternion.Euler(rotation));
            transform.localScale = scale;

            filter.sharedMesh = mesh;
            renderer.sharedMaterials = materials;
            renderer.localBounds = new Bounds(boundsCenter, boundsExtents);

            return renderer;
        }

        public SkinnedMeshRenderer SpawnAsSkinnedMeshRenderer(Transform parent, string name = null)
        {
            var renderer = new GameObject(this.name).AddComponent<SkinnedMeshRenderer>();
            var transform = renderer.transform;

            if (!string.IsNullOrWhiteSpace(name)) transform.name = name;

            if (parent != null) transform.SetParent(parent, false);

            transform.SetLocalPositionAndRotation(offset, Quaternion.Euler(rotation));
            transform.localScale = scale;

            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = materials;
            renderer.localBounds = new Bounds(boundsCenter, boundsExtents);

            if (avatar != null)
            {
                var rigContainer = parent.FindDeepChildLiberal(avatar.rigContainer);
                if (rigContainer == null) rigContainer = parent == null ? transform : parent;

                if (rigContainer != null)
                {
                    Transform[] bones = new Transform[avatar.bones.Length];
                    for (int a = 0; a < avatar.bones.Length; a++) bones[a] = rigContainer.FindDeepChildLiberal(avatar.bones[a]);

                    if (bones.Length > 0) renderer.rootBone = bones[0]; 
                    renderer.bones = bones;
                }
            }

            return renderer;
        }

        [Header("Customizable Character Mesh")]
        public CustomizableCharacterMeshData customizableCharacterMeshData;
        public int meshGroupIndex;
        public int subMeshIndex;

        public string rigBufferId;
        public string shapeBufferId;
        public string morphBufferId;

        public string shapesInstanceReference;
        public string rigInstanceReference;
        public string characterInstanceReference;

        public CustomizableCharacterMesh SpawnAsCustomizableCharacterMesh(Transform parent, string name = null)
        {
            var ccm = new GameObject(this.name).AddComponent<CustomizableCharacterMesh>();
            var transform = ccm.transform;

            if (!string.IsNullOrWhiteSpace(name)) transform.name = name;

            if (parent != null) transform.SetParent(parent, false);

            transform.SetLocalPositionAndRotation(offset, Quaternion.Euler(rotation)); 
            transform.localScale = scale;

            ccm.SetMeshData(customizableCharacterMeshData);
            ccm.SetMeshGroupIndex(meshGroupIndex);
            ccm.SetSubMeshindex(subMeshIndex);
            
            if (avatar != null) ccm.SetAvatar(avatar);

            ccm.SetRigBufferID(rigBufferId); 
            ccm.SetShapeBufferID(shapeBufferId);
            ccm.SetMorphBufferID(morphBufferId);

            if (!string.IsNullOrWhiteSpace(shapesInstanceReference))
            {
                var child = parent.FindDeepChildLiberal(shapesInstanceReference);
                if (child != null)
                {
                    ccm.shapesInstanceReference = child.GetComponent<InstanceableSkinnedMeshBase>(); 
                }
            }
            if (!string.IsNullOrWhiteSpace(rigInstanceReference))
            {
                var child = parent.FindDeepChildLiberal(rigInstanceReference);
                if (child != null)
                {
                    ccm.rigInstanceReference = child.GetComponent<InstanceableSkinnedMeshBase>();
                }
            }
            if (!string.IsNullOrWhiteSpace(characterInstanceReference))
            {
                var child = parent.FindDeepChildLiberal(characterInstanceReference);
                if (child != null)
                {
                    ccm.characterInstanceReference = child.GetComponent<CustomizableCharacterMesh>();
                }
            }

            return ccm;
        }

    }

}

#endif