using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.Morphing
{

    public class MorphableSkinnedMesh : InstanceableSkinnedMeshBase
    {

        public override void Dispose()
        {  
        }

        [SerializeField]
        protected MorphableMeshData meshData;
        public void SetMeshData(MorphableMeshData data) => meshData = data;
        public override InstanceableMeshData MeshData => meshData;
        public override InstancedMeshGroup MeshGroup => meshData.meshGroups[meshGroupIndex];

        [SerializeField]
        protected CustomAvatar avatar;
        public void SetAvatar(CustomAvatar av) => avatar = av; 
        [SerializeField]
        protected Transform rigRoot;
        public void SetRigRoot(Transform root) => rigRoot = root;
        public override Transform BoundsRootTransform => rigRoot;

        [SerializeField]
        protected string shapeBufferId;
        public void SetShapeBufferID(string id) => shapeBufferId = id;
        public override string ShapeBufferID => shapeBufferId;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (rigRoot == null) rigRoot = transform; 
        }

        public override string RigID => rigRoot.GetInstanceID().ToString(); 

        [SerializeField]
        protected string rigBufferId;
        public void SetRigBufferID(string id) => rigBufferId = id;
        public override string RigBufferID => rigBufferId; 

        protected Transform[] bones;
        public override Transform[] Bones
        {
            get
            {
                if (bones == null)
                {
                    if (avatar == null)
                    {
                        bones = new Transform[] { rigRoot };
                    } 
                    else
                    {
                        bones = new Transform[avatar.bones.Length]; 
                        for (int a = 0; a < avatar.bones.Length; a++) bones[a] = rigRoot.FindDeepChildLiberal(avatar.bones[a]); 
                    }
                }

                return bones;
            }
        }

        public override int BoneCount => avatar == null ? 1 : avatar.bones.Length;

        public override Matrix4x4[] BindPose => meshData.ManagedBindPose;
    }

}