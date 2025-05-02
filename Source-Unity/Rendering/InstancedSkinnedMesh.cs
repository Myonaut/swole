#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole
{
    public class InstancedSkinnedMesh : InstanceableSkinnedMeshBase
    {

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (rigInstanceReference != null)
            {
                rigInstanceReference.OnCreateInstanceID -= SetRigInstanceID; 
                rigInstanceReference = null;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            InitInstanceIDs();
        }

        [SerializeField]
        protected InstancedSkinnedMeshData meshData;
        public void SetMeshData(InstancedSkinnedMeshData data) => meshData = data;
        public override InstanceableMeshDataBase MeshData => meshData;
        public virtual InstancedSkinnedMeshData FinalMeshData => meshData;
        public override InstancedMeshGroup MeshGroup => meshData.meshGroups[meshGroupIndex];

        [SerializeField]
        protected CustomAvatar avatar;
        public void SetAvatar(CustomAvatar av) => avatar = av;
        [SerializeField]
        protected Transform rigRoot;
        public void SetRigRoot(Transform root) => rigRoot = root;
        public Transform RigRoot
        {
            get
            {
                if (rigRoot == null)
                {
                    if (avatar != null && !string.IsNullOrWhiteSpace(avatar.rigContainer))
                    {
                        rigRoot = (transform.parent == null ? transform : transform.parent).FindDeepChildLiberal(avatar.rigContainer);
                    }

                    if (rigRoot == null) rigRoot = transform;
                }

                return rigRoot;
            }
        }
        public override Transform BoundsRootTransform => RigRoot;

        public override string ShapeBufferID => null;

        public override string RigID => rigRoot.GetInstanceID().ToString();

        [SerializeField]
        protected string rigBufferId;
        public void SetRigBufferID(string id) => rigBufferId = id;
        public override string RigBufferID => rigBufferId;

        [NonSerialized]
        protected int rigInstanceID;
        public override int RigInstanceID => rigInstanceID <= 0 ? InstanceSlot : (rigInstanceID - 1);
        public InstanceableSkinnedMeshBase rigInstanceReference;

        public override Rigs.StandaloneSampler RigSampler
        {
            get
            {
                if (rigInstanceReference == null && rigInstanceID <= 0) return base.RigSampler;
                return null;
            }
        }

        public void SetRigInstanceID(int id)
        {
            rigInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(FinalMeshData.RigInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(FinalMeshData.RigInstanceIDPropertyName, id);
                }
            }
        }

        protected virtual void InitInstanceIDs()
        {
            if (rigInstanceReference != null)
            {
                SetRigInstanceID(rigInstanceReference.InstanceSlot);
                rigInstanceReference.OnCreateInstanceID += SetRigInstanceID;
            }
        }

        protected Transform[] bones;
        public override Transform[] Bones
        {
            get
            {
                if (bones == null)
                {
                    var rig_root = RigRoot;

                    if (avatar == null)
                    {
                        bones = new Transform[] { rig_root };
                    }
                    else
                    {
                        bones = new Transform[avatar.bones.Length];
                        for (int a = 0; a < avatar.bones.Length; a++) bones[a] = rig_root.FindDeepChildLiberal(avatar.bones[a]);
                    }
                }

                return bones;
            }
        }

        public override int BoneCount => avatar == null ? 1 : avatar.bones.Length;

        public override Matrix4x4[] BindPose => meshData.ManagedBindPose;

        protected override void SetupSkinnedMeshSyncs()
        {
            throw new NotImplementedException();
        }

    }
}

#endif