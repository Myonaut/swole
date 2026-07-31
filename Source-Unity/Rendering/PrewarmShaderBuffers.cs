#if UNITY_2017_1_OR_NEWER

using UnityEngine;

namespace Swole.Unity
{

    public static class PrewarmShaderBuffers
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            Debug.Log("Prewarming shader buffers.");

            var defaultBuffer = PersistentJobDataTracker.GetEmptyGraphicsBuffer();

            Shader.SetGlobalBuffer(Swole.API.Unity.BodyMoldSystem.clothingMatProp_skinBindings, defaultBuffer);
            Shader.SetGlobalBuffer(Swole.API.Unity.BodyMoldSystem.bodyMatProp_finalWorldDeltas, defaultBuffer); 
            
            Shader.SetGlobalBuffer(Swole.API.Unity.BoundMesh.defaultMatProp_meshProxyBindings, defaultBuffer);

            Shader.SetGlobalBuffer("_SkinningMatrices", defaultBuffer);
            Shader.SetGlobalBuffer("_MeshShapeIndices", defaultBuffer);
            Shader.SetGlobalBuffer("_MeshShapeFrameWeights", defaultBuffer);
            Shader.SetGlobalBuffer("_VertexGroups", defaultBuffer);
            Shader.SetGlobalBuffer("_ControlMuscleGroups", defaultBuffer);
            Shader.SetGlobalBuffer("_ControlFatGroups", defaultBuffer);
            Shader.SetGlobalBuffer("_ControlVariationShapes", defaultBuffer);
            Shader.SetGlobalBuffer("_VariationGroups", defaultBuffer);
            Shader.SetGlobalBuffer("_MuscleGroupInfluences", defaultBuffer);
            Shader.SetGlobalBuffer("_FatGroupInfluences", defaultBuffer);
            Shader.SetGlobalBuffer("_PerVertexDeltaData", defaultBuffer);
            Shader.SetGlobalBuffer("_VertexColorDeltas", defaultBuffer);
            Shader.SetGlobalBuffer("_LeftRightFlags", defaultBuffer); 
        }
    }

}

#endif