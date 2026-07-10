using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.Modding
{
    // Example component demonstrating runtime usage of BodyMoldSystem.
    // Attach to a GameObject and assign clothingSource (MeshFilter) and bodySource (MeshFilter).
    public class BodyMoldExample : MonoBehaviour
    {
        public SkinnedMeshRenderer clothingSource; // source clothing mesh (will be copied)
        public ComputeShader bodyMoldCompute;
        public BodyMoldBindings precomputedBindings;

        public int iterationsPerFrame = 1;

        public int targetSdfIndex = 0; // which SDF to use for the mold (if multiple are present in the bindings)

        BodyMoldSystem moldSystem;
        Mesh workingClothingMesh; 

        void Start()
        {
            if (clothingSource == null || bodyMoldCompute == null || precomputedBindings == null)
            {
                Debug.LogError("BodyMoldExample: assign clothingSource, compute shader, and precomputed Bindings.");
                return;
            }

            // Make a copy of the clothing mesh so we don't modify the original asset
            //workingClothingMesh = Instantiate(clothingSource.sharedMesh);
            //workingClothingMesh.name = clothingSource.sharedMesh.name + "_molded";
            //clothingSource.sharedMesh = workingClothingMesh;

            moldSystem = gameObject.AddOrGetComponent<BodyMoldSystem>();  
            moldSystem.computeShader = bodyMoldCompute;
            moldSystem.SetupFromBindings(precomputedBindings);
            //moldSystem.tempMesh = workingClothingMesh;  
        }

        void Update()
        {
            if (moldSystem == null) return;

            //if (InputProxy.JumpButtonDown)
            if (InputProxy.JumpButton)
            {
                Debug.Log($"Dispatching {iterationsPerFrame} iterations");
                // Run iterative smoothing and apply to the working mesh
                moldSystem.DispatchIterations(iterationsPerFrame); 
                //moldSystem.ApplyToMesh(targetSdfIndex, workingClothingMesh);
            }
        }
    }
}
