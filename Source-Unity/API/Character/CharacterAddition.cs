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
    public class CharacterAddition : ScriptableObject, ISelectableAsset
    {

        [Header("UI")]
        public Sprite icon;

        [Serializable]
        public enum MeshType
        {
            UnitySkinnedMesh, CustomizableCharacterMesh
        }

        [Header("Settings")]
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

        private Dictionary<string, Mesh> instantiatedMeshes = new Dictionary<string, Mesh>();
        public SkinnedMeshRenderer SpawnAsSkinnedMeshRenderer(Transform parent, string name = null, bool forceNewBindpose = false, string bindposeId = null, Matrix4x4[] bindpose = null)
        {
            var renderer = new GameObject(this.name).AddComponent<SkinnedMeshRenderer>();
            var transform = renderer.transform;

            if (!string.IsNullOrWhiteSpace(name)) transform.name = name;

            if (parent != null) transform.SetParent(parent, false);

            transform.SetLocalPositionAndRotation(offset, Quaternion.Euler(rotation));
            transform.localScale = scale;

            var mesh = this.mesh;
            if (forceNewBindpose && !string.IsNullOrWhiteSpace(bindposeId) && bindpose != null)
            {
                mesh = mesh.Duplicate();



                instantiatedMeshes[bindposeId] = mesh;
            }

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
        public CustomizableCharacterMeshV2_DATA customizableCharacterMeshDataV2;
        public int meshGroupIndex;
        public int subMeshIndex;

        public string rigBufferId;
        public string shapeBufferId;
        public string morphBufferId;

        public string shapesInstanceReference;
        public string rigInstanceReference;
        public string characterInstanceReference;

        public ICustomizableCharacter SpawnAsCustomizableCharacterMesh(Transform parent, string name = null)
        {
            bool isV2 = customizableCharacterMeshDataV2 != null;

            ICustomizableCharacter ccm;
            if (isV2)
            {
                ccm = new GameObject(this.name).AddComponent<CustomizableCharacterMeshV2>();
            }
            else
            {
                ccm = new GameObject(this.name).AddComponent<CustomizableCharacterMesh>();
            }
                
            var transform = ccm.GameObject.transform;

            if (!string.IsNullOrWhiteSpace(name)) transform.name = name;

            if (parent != null) transform.SetParent(parent, false);

            transform.SetLocalPositionAndRotation(offset, Quaternion.Euler(rotation));
            transform.localScale = scale;

            if (avatar != null) ccm.SetAvatar(avatar);

            ccm.SetRigBufferID(rigBufferId);
            ccm.SetShapeBufferID(shapeBufferId);
            ccm.SetMorphBufferID(morphBufferId);

            if (ccm is CustomizableCharacterMeshV2 v2)
            {
                v2.SetData(customizableCharacterMeshDataV2); 
            }
            if (ccm is CustomizableCharacterMesh v1)
            {
                v1.SetMeshData(customizableCharacterMeshData);
                v1.SetMeshGroupIndex(meshGroupIndex);
                v1.SetSubMeshindex(subMeshIndex);
            }

            if (!string.IsNullOrWhiteSpace(shapesInstanceReference))
            {
                var child = parent.FindDeepChildLiberal(shapesInstanceReference);
                if (child != null)
                {
                    ccm.ShapesInstanceReference = child.GetComponent<ICustomizableCharacter>(); 
                }
            }
            if (!string.IsNullOrWhiteSpace(rigInstanceReference))
            {
                var child = parent.FindDeepChildLiberal(rigInstanceReference);
                if (child != null)
                {
                    ccm.RigInstanceReference = child.GetComponent<InstanceableSkinnedMeshBase>();
                }
            }
            if (!string.IsNullOrWhiteSpace(characterInstanceReference))
            {
                var child = parent.FindDeepChildLiberal(characterInstanceReference);
                if (child != null)
                {
                    ccm.CharacterInstanceReference = child.GetComponent<ICustomizableCharacter>();
                }
            }

            return ccm;
        }

        public GameObject Spawn(Transform root)
        {
            switch (meshType)
            {
                case CharacterAddition.MeshType.UnitySkinnedMesh:
                    var smr = SpawnAsSkinnedMeshRenderer(root);
                    if (smr != null) return smr.gameObject;
                    break;

                case CharacterAddition.MeshType.CustomizableCharacterMesh:
                    var ccm = SpawnAsCustomizableCharacterMesh(root);
                    if (ccm != null) return ccm.GameObject; 
                    break;
            }

            return null;
        }

        [Header("Metadata")]
        public string displayName;
        public string description;
        public string[] attributes;
        public string[] tags;

        public string Name => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string ID => name;

        public string Description => description;

        public int AttributeCount => attributes == null ? 0 : attributes.Length;

        public int TagCount => tags == null ? 0 : tags.Length;

        public string GetAttribute(int index)
        {
            if (index < 0 || index >= attributes.Length) return string.Empty;
            return attributes[index];
        }

        public string GetTag(int index)
        {
            if (index < 0 || index >= tags.Length) return string.Empty;
            return tags[index];
        }

        public bool HasAttribute(string attribute)
        {
            if (attributes == null) return false;
            foreach (var attr in attributes)
            {
                if (attr == attribute) return true;
            }

            return false;
        }

        public bool HasPrefixAttribute(string attributePrefix)
        {
            if (attributes == null) return false;
            foreach (var attr in attributes)
            {
                if (attr.StartsWith(attributePrefix)) return true;
            }

            return false;
        }

        public bool HasTag(string tag)
        {
            if (tags == null) return false;
            foreach (var tag_ in tags)
            {
                if (tag_ == tag) return true;
            }

            return false;
        }

    }

}

#endif