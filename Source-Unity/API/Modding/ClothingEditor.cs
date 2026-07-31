#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Swole.API.Unity;

using Swole.Modding;
using Swole.UI;
using Swole.Morphing;

namespace Swole.Modding
{
    public class ClothingEditor : MonoBehaviour
    {

        #region Saving

        [Header("Saving")]
        public string saveDir;
        public string assetName;
        public bool save;

        public void SaveMesh() => SaveMesh(saveDir, assetName);
        public void SaveMesh(string saveDir) => SaveMesh(saveDir, assetName);
        public void SaveMesh(string saveDir, string assetName)
        {
            if (meshEditor == null) return;

#if UNITY_EDITOR
            var mesh = meshEditor.EditedMesh;
            if (string.IsNullOrWhiteSpace(assetName))
            {
                mesh.name = meshEditor.OriginalMesh.name + "_edit";
            }
            else
            {
                mesh.name = assetName;
            }

            mesh.CreateOrReplaceAsset(mesh.CreateUnityAssetPathString(saveDir));
#endif
        }

        #endregion

#if BULKOUT_ENV
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "sc_RLD-Add"; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#else
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "";
#endif
        [Tooltip("Primary object used for manipulating the scene.")]
        public RuntimeEditor runtimeEditor;

        #region Windows

        [Header("Windows")]
        public RectTransform shapesWindow;
        public RectTransform poseWindow;
        public RectTransform statesWindow;
        public RectTransform editingWindow;
        public RectTransform maskWindow;

        #endregion

        #region Pose

        [Header("Pose")]
        public List<Animator> animators;
        public string timeControlParameter = "time";

        protected float refreshSkinningTimer;

        #endregion

        #region Shapes

        [Header("Shapes")]
        public bool overwriteExistingShapes;
        public string baseVertexMaskShape = "setup_mask";

        public GameObject selectDeselectAllShapesButton;
        private readonly List<int> activeShapes = new List<int>();
        private readonly List<int2> activeShapesAndFrames = new List<int2>();
        private readonly List<int4> activeShapesAndCollisionShapes = new List<int4>();
        protected void SyncActiveShapeCollections() 
        {
            activeShapesAndFrames.Clear();
            activeShapesAndCollisionShapes.Clear();

            if (meshEditor == null) return;

            for (int a = 0; a < activeShapes.Count; a++) 
            {
                var shapeIndex = activeShapes[a];
                var frameCount = meshEditor.GetBlendShapeFrameCount(shapeIndex);

                var collisionShapeIndex = meshEditor.FindCollisionShapeIndex(meshEditor.GetBlendShapeName(shapeIndex));
                var collisionFrameCount = collisionShapeIndex >= 0 ? meshEditor.GetBlendShapeFrameCount(collisionShapeIndex) : 0;

                for (int b = 0; b < frameCount; b++) 
                {
                    activeShapesAndFrames.Add(new int2(shapeIndex, b));
                    activeShapesAndCollisionShapes.Add(new int4(shapeIndex, b, collisionShapeIndex, Mathf.Min(b, collisionFrameCount - 1)));
                }
            }
        }

        private UIRecyclingList shapeList;

        public void SelectAllShapes()
        {
            if (meshEditor == null) return;

            activeShapes.Clear();
            for (int a = 0; a < meshEditor.EditedMesh.blendShapeCount; a++) activeShapes.Add(a);
            SyncActiveShapeCollections();

            if (shapeList != null) shapeList.Refresh();

            RefreshSelectDeselectAllShapesButton();
        }
        public void DeselectAllShapes()
        {
            activeShapes.Clear();
            SyncActiveShapeCollections();

            if (shapeList != null) shapeList.Refresh();

            RefreshSelectDeselectAllShapesButton();
        }
        public void RefreshSelectDeselectAllShapesButton()
        {
            if (selectDeselectAllShapesButton == null || meshEditor == null) return;

            if (activeShapes.Count >= meshEditor.EditedMesh.blendShapeCount)
            {
                CustomEditorUtils.SetComponentText(selectDeselectAllShapesButton, "DESELECT ALL");
                CustomEditorUtils.SetButtonOnClickAction(selectDeselectAllShapesButton, DeselectAllShapes); 
            } 
            else
            {
                CustomEditorUtils.SetComponentText(selectDeselectAllShapesButton, "SELECT ALL");
                CustomEditorUtils.SetButtonOnClickAction(selectDeselectAllShapesButton, SelectAllShapes);
            }
        }

        #endregion

        /*
        public int targetIndex;

        public Vector3 s0, s1;
        public Vector3 t0, t1, t2;

        public void OnDrawGizmos()
        {

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(t0, t1);
            Gizmos.DrawLine(t1, t2);
            Gizmos.DrawLine(t2, t0);

            Vector3 ab = t1 - t0;
            Vector3 ac = t2 - t0;
            Vector3 n = Vector3.Cross(ab, ac);

            Gizmos.DrawRay((t0 + t1 + t2) / 3f, n * 0.1f);

            Color c = Color.green;
            if (Maths.IntersectSegmentTriangle(s0, s1, t0, t1, t2, out var hit))
            {
                c = Color.red;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(hit.point, 0.01f); 
            }
            Gizmos.color = c;
            Gizmos.DrawLine(s0, s1);

            if (influenceData.IsCreated && targetIndex >= 0 && targetIndex < influenceData.Length)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(inputData_clothing[targetIndex].vertex.worldPosition, 0.001f);

                var inf = influenceData[targetIndex];
                if (inf.influenceA.meshIndex >= 0 && inf.influenceA.vertexIndex >= 0) 
                {
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, inf.influenceA.weight);
                    Gizmos.DrawSphere(inputData_character[inf.influenceA.meshIndex][inf.influenceA.vertexIndex].vertex.worldPosition, 0.002f);

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(inputData_clothing[targetIndex].vertex.worldPosition, inputData_character[inf.influenceA.meshIndex][inf.influenceA.vertexIndex].vertex.worldPosition);
                }
                if (inf.influenceB.meshIndex >= 0 && inf.influenceB.vertexIndex >= 0)
                {
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, inf.influenceB.weight); 
                    Gizmos.DrawSphere(inputData_character[inf.influenceB.meshIndex][inf.influenceB.vertexIndex].vertex.worldPosition, 0.002f);

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(inputData_clothing[targetIndex].vertex.worldPosition, inputData_character[inf.influenceB.meshIndex][inf.influenceB.vertexIndex].vertex.worldPosition);
                }
            }
        }*/

        [Range(0, 1)]
        public float distanceBindingWeight = 0.1f;

        private MeshEditor meshEditor;

        [Tooltip("If true, editing operations will only affect vertices that have delta data for the active shapes.")]
        public bool affectedVerticesOnly = true;
        [Tooltip("If true, editing operations will take into account the current weights of active shapes.")] 
        public bool meshOperationsRespectVisualization = true;

        [Range(0, 1)]
        public float smoothingFactor = 0.3f;
        public bool smoothingCollisions;

        [Range(0, 1)]
        public float preserveFactor = 0.3f;
        public bool preserveCollisions;

        [Range(0, 2)]
        public float depenFactor = 1f;
        [Range(0, 2)]
        public float depenLocalCollisionFactor = 1f;
        public float depenThickness = 0.001f;

        public bool selectAll;
        public bool clearSelection;
        public bool selectDepenMask;
        public bool clearDepenMask;

        public SkinnedMeshRenderer clothingRenderer;
        public Vector3 clothingRotationOffset;
        public bool useMeshIslands;
        public string meshIslandBlendMask;
        public string meshIslandRootMask;
        [Serializable]
        public struct WeightedRenderer
        {
            public SkinnedMeshRenderer renderer;
            public float weight;
        }
        public WeightedRenderer[] characterRenderers;

        private static readonly Dictionary<string, int> boneNameIndexConverter = new Dictionary<string, int>();

        private SkinningBasedBindingData bindingData;

        //private NativeList<VertexInfluence4> influenceData;
        private InfluenceDataFull[] influenceData;

        private AmalgamatedSkinnedMeshDataTracker tempCharacterData;

        private void OnDestroy()
        {
            /*if (influenceData.IsCreated)
            {
                influenceData.Dispose();
                influenceData = default;
            }*/

            if (bindingData != null) bindingData.Dispose();

            if (tempCharacterData != null)
            {
                tempCharacterData.Dispose();
                tempCharacterData = null;
            }
        }

        private void Awake()
        {

            if (poseWindow != null) 
            { 
                var poseSlider = poseWindow.GetComponentInChildren<Slider>(true);
                if (poseSlider != null)
                {
                    poseSlider.minValue = 0;
                    poseSlider.maxValue = 1;
                    poseSlider.SetValueWithoutNotify(0);

                    if (poseSlider.onValueChanged == null) poseSlider.onValueChanged = new Slider.SliderEvent(); else poseSlider.onValueChanged.RemoveAllListeners();
                    poseSlider.onValueChanged.AddListener((float value) =>
                    {
                        if (animators != null && animators.Count > 0)
                        {
                            refreshSkinningTimer = 0.3f;

                            foreach (var animator in animators)
                            {
                                animator.enabled = true;
                                animator.SetFloat(timeControlParameter, value);
                            }

                            syncShapes = true;

                            IEnumerator WaitOne()
                            {
                                yield return null;

                                foreach (var animator in animators)
                                {
                                    animator.enabled = false;
                                }

                                syncShapes = true;
                            }
                            StartCoroutine(WaitOne());
                        }
                    });
                }

                poseWindow.gameObject.SetActive(false);
            }

            if (statesWindow != null) 
            { 
                statesWindow.gameObject.SetActive(false);
            }

            if (editingWindow != null)
            {
                var actions = editingWindow.FindDeepChildLiberal("actions");
                if (actions != null) 
                {
                    var avo = actions.FindDeepChildLiberal("affectedVerticesOnly");
                    if (avo != null)
                    {
                        var toggle = avo.GetComponentInChildren<Toggle>(true);
                        if (toggle != null)
                        {
                            if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                            toggle.onValueChanged.RemoveAllListeners();
                            toggle.onValueChanged.AddListener((bool val) => affectedVerticesOnly = val);

                            affectedVerticesOnly = toggle.isOn;
                        }
                    }

                    var rv = actions.FindDeepChildLiberal("respectVisualization");
                    if (rv != null)
                    {
                        var toggle = rv.GetComponentInChildren<Toggle>(true);
                        if (toggle != null)
                        {
                            if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                            toggle.onValueChanged.RemoveAllListeners();
                            toggle.onValueChanged.AddListener((bool val) => meshOperationsRespectVisualization = val);

                            meshOperationsRespectVisualization = toggle.isOn;
                        }
                    }

                    var smooth = actions.FindDeepChildLiberal("smooth");
                    if (smooth != null)
                    {
                        CustomEditorUtils.SetButtonOnClickAction(smooth, Smooth);

                        var weight = smooth.FindDeepChildLiberal("weight");
                        if (weight != null)
                        {
                            var slider = weight.GetComponentInChildren<Slider>();
                            if (slider != null)
                            {
                                slider.minValue = 0;
                                slider.maxValue = 1.25f;

                                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); else slider.onValueChanged.RemoveAllListeners();
                                slider.SetValueWithoutNotify(smoothingFactor);
                                slider.onValueChanged.AddListener((float val) => smoothingFactor = val);

                                slider.SetValueWithoutNotify(smoothingFactor);
                            }
                        }

                        var col = smooth.FindDeepChildLiberal("collisions");
                        if (col != null)
                        {
                            var toggle = col.GetComponentInChildren<Toggle>(true);
                            if (toggle != null)
                            {
                                if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                                toggle.onValueChanged.RemoveAllListeners();
                                toggle.onValueChanged.AddListener((bool val) => smoothingCollisions = val);

                                smoothingCollisions = toggle.isOn;
                            }
                        }
                    }

                    var mold = actions.FindDeepChildLiberal("mold");
                    if (mold != null)
                    {
                        CustomEditorUtils.SetButtonOnClickAction(mold, Mold);

                        var weight = mold.FindDeepChildLiberal("weight");
                        if (weight != null)
                        {
                            var slider = weight.GetComponentInChildren<Slider>();
                            if (slider != null)
                            {
                                slider.minValue = 0;
                                slider.maxValue = 1.25f;

                                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); else slider.onValueChanged.RemoveAllListeners();
                                slider.SetValueWithoutNotify(preserveFactor);
                                slider.onValueChanged.AddListener((float val) => preserveFactor = val);

                                slider.SetValueWithoutNotify(preserveFactor); 
                            }
                        }

                        var col = mold.FindDeepChildLiberal("collisions");
                        if (col != null)
                        {
                            var toggle = col.GetComponentInChildren<Toggle>(true);
                            if (toggle != null)
                            {
                                if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                                toggle.onValueChanged.RemoveAllListeners();
                                toggle.onValueChanged.AddListener((bool val) => preserveCollisions = val);

                                preserveCollisions = toggle.isOn;
                            }
                        }
                    }

                    var depen = actions.FindDeepChildLiberal("depenetrate");
                    if (depen != null)
                    {
                        CustomEditorUtils.SetButtonOnClickAction(depen, Depenetrate);

                        var weight = depen.FindDeepChildLiberal("weight");
                        if (weight != null)
                        {
                            var slider = weight.GetComponentInChildren<Slider>();
                            if (slider != null)
                            {
                                slider.minValue = 0;
                                slider.maxValue = 1.25f;

                                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); else slider.onValueChanged.RemoveAllListeners();
                                slider.SetValueWithoutNotify(depenFactor);
                                slider.onValueChanged.AddListener((float val) => depenFactor = val);

                                slider.SetValueWithoutNotify(depenFactor);
                            }
                        }
                        var weight2 = depen.FindDeepChildLiberal("weight2");
                        if (weight2 != null)
                        {
                            var slider = weight2.GetComponentInChildren<Slider>();
                            if (slider != null)
                            {
                                slider.minValue = 0;
                                slider.maxValue = 1.25f;

                                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); else slider.onValueChanged.RemoveAllListeners();
                                slider.SetValueWithoutNotify(depenLocalCollisionFactor);
                                slider.onValueChanged.AddListener((float val) => depenLocalCollisionFactor = val);

                                slider.SetValueWithoutNotify(depenLocalCollisionFactor);
                            }
                        }
                        var weight3 = depen.FindDeepChildLiberal("weight3");
                        if (weight3 != null)
                        {
                            var slider = weight3.GetComponentInChildren<Slider>();
                            if (slider != null)
                            {
                                slider.minValue = 0f;
                                slider.maxValue = 0.005f;

                                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); else slider.onValueChanged.RemoveAllListeners();
                                slider.SetValueWithoutNotify(depenThickness);
                                slider.onValueChanged.AddListener((float val) => depenThickness = val);

                                slider.SetValueWithoutNotify(depenThickness);
                            }
                        }
                    }

                    actions.gameObject.SetActive(false); 
                }

                var generate = editingWindow.FindDeepChildLiberal("generate");
                if (generate != null)
                {
                    generate.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(generate, () =>
                    {
                        GenerateShapes();
                        generate.gameObject.SetActive(false);

                        if (actions != null) actions.gameObject.SetActive(true);
                        if (poseWindow != null) poseWindow.gameObject.SetActive(true);
                        if (statesWindow != null) statesWindow.gameObject.SetActive(true);
                    });
                }
            }

            if (maskWindow != null)
            {
                CustomEditorUtils.SetButtonOnClickActionByName(maskWindow, "selectall", () =>
                {
                    if (meshEditor != null)
                    {
                        meshEditor.SelectAllVertices();
                    }
                });

                CustomEditorUtils.SetButtonOnClickActionByName(maskWindow, "clearselection", () =>
                {
                    if (meshEditor != null)
                    {
                        meshEditor.ClearSelectedVertices(); 
                    }
                });

                CustomEditorUtils.SetButtonOnClickActionByName(maskWindow, "selectdepen", () =>
                {
                    if (meshEditor != null)
                    {
                        meshEditor.SelectDepenetrationMask();
                    }
                });

                CustomEditorUtils.SetButtonOnClickActionByName(maskWindow, "cleardepen", () =>
                {
                    if (meshEditor != null)
                    {
                        meshEditor.ClearDepenetrationMask();
                    }
                });
            }

            void FailSetup(string msg)
            {
                swole.LogError(msg);
                //OnSetupFail?.Invoke();
                //ShowSetupError(msg);
                Destroy(this);
            }

            if (string.IsNullOrEmpty(additiveEditorSetupScene))
            {
                string msg = "No additive editor setup scene was set. There will be no way to control the scene camera or to load and manipulate prefabs without it!";
                swole.LogWarning(msg);
                //OnSetupFail?.Invoke();
                //ShowSetupError(msg);
            }
            else
            {

                try
                {

                    Camera mainCam = Camera.main;
                    EventSystem[] eventSystems = GameObject.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

                    SceneManager.LoadScene(additiveEditorSetupScene, LoadSceneMode.Additive);
                    var scn = SceneManager.GetSceneByName(additiveEditorSetupScene);
                    if (scn.IsValid())
                    {

                        bool hadExistingCamera = false;
                        Vector3 existingCameraPos = Vector3.zero;
                        Quaternion existingCameraRot = Quaternion.identity;
                        if (mainCam != null)
                        {
                            hadExistingCamera = true;
                            var camT = mainCam.transform;
                            existingCameraPos = camT.position;
                            existingCameraRot = camT.rotation;
                            Destroy(mainCam.gameObject); // Make room for new main camera in setup scene
                        }
                        foreach (var system in eventSystems) if (system != null) Destroy(system.gameObject); // Remove any existing event systems and use the one in the setup scene

                        IEnumerator FindRuntimeEditor()
                        {
                            while (!scn.isLoaded)
                            {
                                yield return null;
                                scn = SceneManager.GetSceneByBuildIndex(scn.buildIndex);
                            }

                            if (runtimeEditor == null) runtimeEditor = GameObject.FindFirstObjectByType<RuntimeEditor>();
                            if (runtimeEditor == null)
                            {
                                FailSetup("No RuntimeEditor object was found! Objects cannot be edited without it. The additive editor setup scene should contain one.");
                            }
                            else
                            {
                                /*runtimeEditor.OnPreSelect += OnPreSelectCustomize;
                                runtimeEditor.OnSelectionChanged += OnSelectionChange;
                                runtimeEditor.OnBeginManipulateTransforms += BeginManipulationAction;
                                runtimeEditor.OnManipulateTransformsStep += ManipulationActionStep;
                                runtimeEditor.OnManipulateTransforms += RecordManipulationAction;*/

                                runtimeEditor.DisableClickSelect = true;
                                runtimeEditor.DisableGrid = true;
                                runtimeEditor.DisableUndoRedo = true;
                                runtimeEditor.DisableGroupSelect = true;
                                runtimeEditor.DisableSelectionBoundingBox = true;

                                if (hadExistingCamera)
                                {
                                    mainCam = Camera.main;
                                    if (mainCam != null)
                                    {
                                        var camT = mainCam.transform;
                                        camT.SetPositionAndRotation(existingCameraPos, existingCameraRot);
                                    }
                                }

                                //initialized = true;
                                //OnSetupSuccess.Invoke();
                            }

                        }

                        StartCoroutine(FindRuntimeEditor()); // Wait for scene to fully load, then find the runtime editor.

                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            
            if (clothingRenderer == null) clothingRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            boneNameIndexConverter.Clear();
            bindingData = GenerateSkinningBasedBindingData(boneNameIndexConverter, baseVertexMaskShape, true, true, true, true, clothingRenderer, characterRenderers);
        }

        public bool syncShapes;

        public void Update()
        {
#if UNITY_EDITOR
            if (save)
            {
                save = false;
                SaveMesh();
            }
#endif
        }

        public void LateUpdate()
        {
            if (syncShapes)
            {
                syncShapes = false;
                ResyncShapes();
            }

            if (refreshSkinningTimer > 0)
            {
                refreshSkinningTimer -= Time.deltaTime;
                if (refreshSkinningTimer <= 0)
                {
                    if (meshEditor != null)
                    {
                        meshEditor.RefreshPerVertexSkinningMatrices(true);
                        meshEditor.UpdateCollisionVertexSkinning(tempCharacterData.GetUpToDatePerVertexSkinningMatrices());
                    }
                }
            }
        }

        #region Editing

        public void Smooth()
        {
            if (meshEditor != null)
            {
                meshEditor.SmoothPreserveShapeDeltaVolumes(smoothingFactor, bindingData.baseMask_clothing, activeShapesAndFrames, affectedVerticesOnly, default, true, smoothingCollisions, depenThickness).Complete();
                meshEditor.ApplyBlendShapeDataEdits();
                meshEditor.RefreshMeshBlendShapes();

                //clothingRenderer.sharedMesh = meshEditor.EditedMesh;
            }
        }
        public void Mold()
        {
            if (meshEditor != null)
            {
                meshEditor.PreserveShapeDeltaVolumes(preserveFactor, bindingData.baseMask_clothing, activeShapesAndFrames, affectedVerticesOnly, default, true, preserveCollisions, depenThickness).Complete();
                meshEditor.ApplyBlendShapeDataEdits();
                meshEditor.RefreshMeshBlendShapes();
                
                //clothingRenderer.sharedMesh = meshEditor.EditedMesh;
            }
        }

        public void Depenetrate()
        {
            if (meshEditor != null)
            {
                meshEditor.DepenetrateShapeDeltas(depenFactor, bindingData.baseMask_clothing, depenLocalCollisionFactor, depenThickness, activeShapesAndCollisionShapes, affectedVerticesOnly); 

                //clothingRenderer.sharedMesh = meshEditor.EditedMesh;
            }
        }

        #endregion

        public void ResyncShapes() 
        {
            if (meshEditor != null)
            {
                for (int a = 0; a < clothingRenderer.sharedMesh.blendShapeCount; a++) 
                {
                    string shapeName = clothingRenderer.sharedMesh.GetBlendShapeName(a);
                    float weight = clothingRenderer.GetBlendShapeWeight(a);
                    for(int b = 0; b < characterRenderers.Length; b++)
                    {
                        var renderer = characterRenderers[b].renderer;
                        if (renderer == null || renderer.sharedMesh == null) continue;

                        int shapeIndex = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
                        if (shapeIndex < 0) continue;

                        renderer.SetBlendShapeWeight(shapeIndex, weight);
                    }
                }
            }
        }

        public float FetchDynamicBlendShapeFrameWeight(string shapeName, int frameIndex)  
        {
            if (meshOperationsRespectVisualization)
            {
                var shapeIndex = clothingRenderer.sharedMesh.GetBlendShapeIndex(shapeName);
                if (shapeIndex >= 0)
                {
                    float shapeWeight = clothingRenderer.GetBlendShapeWeight(shapeIndex);
                    int frameCount = clothingRenderer.sharedMesh.GetBlendShapeFrameCount(shapeIndex);
                    
                    if (frameIndex > 0)
                    {
                        if (frameCount - 1 > frameIndex) // middle frames
                        {
                            float frameWeightPrev = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1);
                            float frameWeight = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                            float frameWeightNext = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex + 1);
                            if (shapeWeight >= frameWeightNext)
                            {
                                return 0f;
                            } 
                            else if (shapeWeight <= frameWeightPrev)
                            {
                                return 0f;
                            }
                            else if (shapeWeight < frameWeight)
                            {
                                return (shapeWeight - frameWeightPrev) / (frameWeight - frameWeightPrev);
                            }
                            else
                            {
                                return (shapeWeight - frameWeight) / (frameWeightNext - frameWeight);
                            }
                        } 
                        else // last frame
                        {
                            float frameWeightPrev = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1);
                            if (shapeWeight <= frameWeightPrev)
                            {
                                return 0f;
                            }
                            else
                            {
                                float frameWeight = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                                return (shapeWeight - frameWeightPrev) / (frameWeight - frameWeightPrev);
                            }
                        }
                    }
                    else // first frame
                    {
                        float frameWeight = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                        if (frameCount > 1)
                        {
                            float frameWeightNext = clothingRenderer.sharedMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex + 1);
                            if (shapeWeight >= frameWeightNext)
                            {
                                return 0f;
                            }
                            else
                            {
                                return (shapeWeight - frameWeight) / (frameWeightNext - frameWeight);
                            }
                        } 
                        else
                        {
                            return shapeWeight / frameWeight;
                        }
                    }
                }
            }

            return 1f;
        }

        public class SkinningBasedBindingData : IDisposable
        {
            public Vector3[] originalVertices_clothing;
            public NativeList<float> baseMask_clothing;
            public NativeList<SkinnedVertex8Reference> skinningData_clothing;
            public MeshDataTools.WeldedVertex[] mergeData_clothing;
            public NativeList<int> indices_clothing;

            public NativeList<SkinnedVertex8Reference>[]  skinningData_character;
            public NativeList<MeshDataTools.Triangle>[]  triangles_character;
            public NativeList<int>[] indices_character;
            public BlendShape[][] shapeData_character;
            public Vector3[][] normalData_character;

            public void Dispose()
            {
                if (baseMask_clothing.IsCreated)
                {
                    baseMask_clothing.Dispose();
                    baseMask_clothing = default;
                }
                if (skinningData_clothing.IsCreated)
                {
                    skinningData_clothing.Dispose();
                    skinningData_clothing = default;
                }
                if (indices_clothing.IsCreated)
                {
                    indices_clothing.Dispose();
                    indices_clothing = default;  
                }

                if (skinningData_character != null)
                {
                    for (int a = 0; a < skinningData_character.Length; a++)
                    {
                        if (skinningData_character[a].IsCreated) skinningData_character[a].Dispose();
                    }
                    skinningData_character = null;
                }
                if (triangles_character != null)
                {
                    for (int a = 0; a < triangles_character.Length; a++)
                    {
                        if (triangles_character[a].IsCreated) triangles_character[a].Dispose();
                    }
                    triangles_character = null;
                }
                if (indices_character != null)
                {
                    for (int a = 0; a < indices_character.Length; a++)
                    {
                        if (indices_character[a].IsCreated) indices_character[a].Dispose();
                    }
                    indices_character = null;
                }
            }
        }


        public static SkinningBasedBindingData GenerateSkinningBasedBindingData(Dictionary<string, int> boneNameIndexConverter, string baseVertexMaskShape, bool includeMergeData, bool includeVertices, bool includeNormals, bool includeBlendShapes, SkinnedMeshRenderer clothingRenderer, WeightedRenderer[] characterRenderers, string optionalBindingShapeName = null, float optionalBindingShapeWeight = 1f)
        {
            SkinnedMeshRenderer[] characterRenderers_ = new SkinnedMeshRenderer[characterRenderers.Length];
            for (int i = 0; i < characterRenderers.Length; i++) characterRenderers_[i] = characterRenderers[i].renderer;
            return GenerateSkinningBasedBindingData(boneNameIndexConverter, baseVertexMaskShape, includeMergeData, includeVertices, includeNormals, includeBlendShapes, clothingRenderer, characterRenderers_, optionalBindingShapeName, optionalBindingShapeWeight);
        }
        public static SkinningBasedBindingData GenerateSkinningBasedBindingData(Dictionary<string, int> boneNameIndexConverter, string baseVertexMaskShape, bool includeMergeData, bool includeVertices, bool includeNormals, bool includeBlendShapes, SkinnedMeshRenderer clothingRenderer, SkinnedMeshRenderer[] characterRenderers, string optionalBindingShapeName = null, float optionalBindingShapeWeight = 1f)
        {
            boneNameIndexConverter.Clear();

            var bindingData = new SkinningBasedBindingData();
            if (clothingRenderer == null)
            {
                bindingData.baseMask_clothing = new NativeList<float>(0, Allocator.Persistent);
                bindingData.skinningData_clothing = new NativeList<SkinnedVertex8Reference>(0, Allocator.Persistent);
                bindingData.mergeData_clothing = new MeshDataTools.WeldedVertex[0];
                bindingData.indices_clothing = new NativeList<int>(0, Allocator.Persistent);
            }
            else
            {
                bindingData.baseMask_clothing = new NativeList<float>(clothingRenderer.sharedMesh.vertexCount, Allocator.Persistent);
                for (int a = 0; a < clothingRenderer.sharedMesh.vertexCount; a++) bindingData.baseMask_clothing.Add(1f);
                int maskIndex = string.IsNullOrWhiteSpace(baseVertexMaskShape) ? -1 : clothingRenderer.sharedMesh.GetBlendShapeIndex(baseVertexMaskShape);
                if (maskIndex >= 0)
                {
                    var shape = new BlendShape(clothingRenderer.sharedMesh, baseVertexMaskShape);
                    var vgroup = VertexGroup.ConvertToVertexGroup(shape);
                    if (vgroup != null)
                    {
                        Debug.Log("FOUND MASK SHAPE : " + shape.name);
                        for (int a = 0; a < clothingRenderer.sharedMesh.vertexCount; a++) bindingData.baseMask_clothing[a] = vgroup[a];
                    }
                }

                if (includeVertices || includeMergeData) bindingData.originalVertices_clothing = clothingRenderer.sharedMesh.vertices;
                if (includeMergeData) bindingData.mergeData_clothing = MeshDataTools.WeldVertices(bindingData.originalVertices_clothing);

                Vector3[] bindingShapeDeltas = null;
                if (!string.IsNullOrWhiteSpace(optionalBindingShapeName))
                {
                    int bindingShapeIndex = clothingRenderer.sharedMesh.GetBlendShapeIndex(optionalBindingShapeName);
                    if (bindingShapeIndex >= 0)
                    {
                        Debug.Log("FOUND BIND SHAPE : " + optionalBindingShapeName);
                        var shape = new BlendShape(clothingRenderer.sharedMesh, optionalBindingShapeName);
                        bindingShapeDeltas = new Vector3[clothingRenderer.sharedMesh.vertexCount];
                        shape.GetTransformedVertices(bindingShapeDeltas, optionalBindingShapeWeight, false); // writes to deltas array
                    }
                }

                bindingData.skinningData_clothing = MeshEditing.GetSkinnedVertex8DataAsList(clothingRenderer, clothingRenderer.sharedMesh.vertices, bindingShapeDeltas, 1f); 

                bindingData.indices_clothing = new NativeList<int>(bindingData.skinningData_clothing.Length, Allocator.Persistent);
                for (int a = 0; a < bindingData.skinningData_clothing.Length; a++) bindingData.indices_clothing.Add(a);

                var bones = clothingRenderer.bones;
                for (int a = 0; a < bones.Length; a++) if (bones[a] != null && !string.IsNullOrWhiteSpace(bones[a].name)) boneNameIndexConverter[bones[a].name] = a;
            }

            bindingData.skinningData_character = new NativeList<SkinnedVertex8Reference>[characterRenderers.Length];
            bindingData.triangles_character = new NativeList<MeshDataTools.Triangle>[characterRenderers.Length];
            bindingData.indices_character = new NativeList<int>[characterRenderers.Length];
            bindingData.shapeData_character = new BlendShape[characterRenderers.Length][];
            bindingData.normalData_character = new Vector3[characterRenderers.Length][];
            for (int a = 0; a < bindingData.skinningData_character.Length; a++)
            {
                var skinnedVertexData = MeshEditing.GetSkinnedVertex8DataAsList(characterRenderers[a]);
                bindingData.skinningData_character[a] = skinnedVertexData;

                var trisList = new NativeList<MeshDataTools.Triangle>(skinnedVertexData.Length, Allocator.Persistent);
                bindingData.triangles_character[a] = trisList;
                var tris = characterRenderers[a].sharedMesh.triangles;
                for (int b = 0; b < tris.Length; b += 3)
                {
                    trisList.Add(new MeshDataTools.Triangle() { i0 = tris[b], i1 = tris[b + 1], i2 = tris[b + 2] });
                }

                var bones = characterRenderers[a].bones;
                for (int b = 0; b < skinnedVertexData.Length; b++)
                {
                    var data = skinnedVertexData[b];
                    var vert = data.vertex;

                    var boneWeights = vert.boneWeights;

                    if (boneWeights.boneIndex.x >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex.x];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex.x = ind;
                        if (ind < 0) boneWeights.weight.x = 0;
                    }
                    if (boneWeights.boneIndex.y >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex.y];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex.y = ind;
                        if (ind < 0) boneWeights.weight.y = 0;
                    }
                    if (boneWeights.boneIndex.z >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex.z];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex.z = ind;
                        if (ind < 0) boneWeights.weight.z = 0;
                    }
                    if (boneWeights.boneIndex.w >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex.w];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex.w = ind;
                        if (ind < 0) boneWeights.weight.w = 0;
                    }

                    if (boneWeights.boneIndex2.x >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex2.x];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex2.x = ind;
                        if (ind < 0) boneWeights.weight2.x = 0;
                    }
                    if (boneWeights.boneIndex2.y >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex2.y];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex2.y = ind;
                        if (ind < 0) boneWeights.weight2.y = 0;
                    }
                    if (boneWeights.boneIndex2.z >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex2.z];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex2.z = ind;
                        if (ind < 0) boneWeights.weight2.z = 0;
                    }
                    if (boneWeights.boneIndex2.w >= 0)
                    {
                        var bone = bones[boneWeights.boneIndex2.w];
                        if (!boneNameIndexConverter.TryGetValue(bone.name, out var ind)) ind = -1;
                        boneWeights.boneIndex2.w = ind;
                        if (ind < 0) boneWeights.weight2.w = 0;
                    }

                    vert.boneWeights = boneWeights;

                    data.vertex = vert;
                    skinnedVertexData[b] = data;
                }

                var indicesList = new NativeList<int>(bindingData.skinningData_character[a].Length, Allocator.Persistent);
                bindingData.indices_character[a] = indicesList;
                for (int b = 0; b < bindingData.skinningData_character[a].Length; b++) indicesList.Add(b);
                if (includeBlendShapes) bindingData.shapeData_character[a] = characterRenderers[a].sharedMesh.GetBlendShapes().ToArray();
                if (includeNormals) bindingData.normalData_character[a] = characterRenderers[a].sharedMesh.normals;
            }

            return bindingData;
        }

        public struct InfluenceDataFull
        {
            public VertexInfluence4 influences;
            public MorphUtils.TempVertexInfo meshIslandVertexInfo;
        }

        /// <summary>
        /// Generates character mesh influences for a clothing mesh.
        /// </summary>
        /// <param name="characterRenderers">The character renderers with optional global weights.</param>
        /// <param name="bindingData">The precalculated binding data obtained from <see cref="GenerateSkinningBasedBindingData"/></param>
        /// <param name="distanceBindingWeight">Weights the influence of distance on binding score. 0 = No influence. 1 = Full influence.</param>
        /// <returns></returns>
        public static InfluenceDataFull[] GenerateInfluenceData(WeightedRenderer[] characterRenderers, SkinningBasedBindingData bindingData, float distanceBindingWeight = 0.1f)
        {
            JobHandle tempHandle = default;
            //influenceData = new NativeList<InfluenceDataFull>(inputData_clothing.Length, Allocator.Persistent);
            InfluenceDataFull[] influenceData = new InfluenceDataFull[bindingData.skinningData_clothing.Length];

            // CRITICAL FIX: Instead of keeping only top-2 influences globally, keep the best influence PER body mesh
            // This ensures each body gets its best binding, which is then filtered by distance masking
            using (var perBodyInfluences = new NativeArray<VertexInfluence2>(bindingData.skinningData_clothing.Length * characterRenderers.Length, Allocator.TempJob))
            {
                var perBodyInfluences_ = perBodyInfluences;
                // Initialize all influences
                //for (int i = 0; i < perBodyInfluences.Length; i++)
                //{
                //    perBodyInfluences_[i] = new VertexInfluence2 { influenceA = new VertexInfluence { meshIndex = -1, vertexIndex = -1, score = 0f, weight = 0f }, influenceB = new VertexInfluence { meshIndex = -1, vertexIndex = -1, score = 0f, weight = 0f } };
                //}

                // Calculate best influence for each body mesh independently
                for (int bodyIndex = 0; bodyIndex < bindingData.skinningData_character.Length; bodyIndex++)
                {
                    var list = bindingData.skinningData_character[bodyIndex];
                    var index_list = bindingData.indices_character[bodyIndex];

                    // Job to find best match for this body mesh
                    tempHandle = new MeshEditing.CalculateVertexInfluencePerBodyJob()
                    {
                        mask = bindingData.baseMask_clothing,
                        maxDistance = 1f,
                        scoreMultiplier = characterRenderers[bodyIndex].weight,
                        distanceBindingWeight = distanceBindingWeight,
                        referenceIndex = bodyIndex,
                        referenceSkinnedVertices = list,
                        referenceVertexIndices = index_list,
                        localSkinnedVertices = bindingData.skinningData_clothing,
                        localVertexIndices = bindingData.indices_clothing,
                        perBodyInfluences = perBodyInfluences,
                        bodyCount = characterRenderers.Length
                    }.Schedule(bindingData.indices_clothing.Length, 64, tempHandle);
                }
                tempHandle.Complete();

                // Now consolidate per-body influences into top-4 for each clothing vertex
                for (int clothingIdx = 0; clothingIdx < influenceData.Length; clothingIdx++)
                {
                    // Collect all non-empty influences for this clothing vertex
                    var influences = new System.Collections.Generic.List<VertexInfluence>();
                    for (int bodyIdx = 0; bodyIdx < characterRenderers.Length; bodyIdx++)
                    {
                        int idx = clothingIdx * characterRenderers.Length + bodyIdx;
                        var inf = perBodyInfluences[idx];
                        if (inf.influenceA.meshIndex >= 0 && inf.influenceA.score > 0f)
                        {
                            influences.Add(inf.influenceA);
                        }
                        if (inf.influenceB.meshIndex >= 0 && inf.influenceB.score > 0f)
                        {
                            influences.Add(inf.influenceB);
                        }
                    }

                    // Sort by score descending and take top 4
                    influences.Sort((a, b) => b.score.CompareTo(a.score));

                    VertexInfluence4 inf4 = new VertexInfluence4();
                    float totalScore = 0f;
                    for (int i = 0; i < influences.Count && i < 4; i++)
                    {
                        totalScore += influences[i].score;
                    }

                    // Assign and calculate weights
                    if (influences.Count > 0)
                    {
                        inf4.influenceA = influences[0];
                        inf4.influenceA.weight = totalScore > 0f ? (inf4.influenceA.score / totalScore) * bindingData.baseMask_clothing[clothingIdx] : 0f;
                    }
                    if (influences.Count > 1)
                    {
                        inf4.influenceB = influences[1];
                        inf4.influenceB.weight = totalScore > 0f ? (inf4.influenceB.score / totalScore) * bindingData.baseMask_clothing[clothingIdx] : 0f;
                    }
                    if (influences.Count > 2)
                    {
                        inf4.influenceC = influences[2];
                        inf4.influenceC.weight = totalScore > 0f ? (inf4.influenceC.score / totalScore) * bindingData.baseMask_clothing[clothingIdx] : 0f;
                    }
                    if (influences.Count > 3)
                    {
                        inf4.influenceD = influences[3];
                        inf4.influenceD.weight = totalScore > 0f ? (inf4.influenceD.score / totalScore) * bindingData.baseMask_clothing[clothingIdx] : 0f; 
                    }

                    InfluenceDataFull infData = new InfluenceDataFull();
                    infData.influences = inf4;
                    influenceData[clothingIdx] = infData;
                }
            }

            return influenceData;
        }

        /// <summary>
        /// Generates per-body mesh influences without global normalization.
        /// Returns the best influence for each clothing vertex from each body mesh independently.
        /// Used by BodyMoldBindingGenerator to ensure each body gets proper bindings.
        /// </summary>
        public static VertexInfluence2[][] GeneratePerBodyInfluences(WeightedRenderer[] characterRenderers, SkinningBasedBindingData bindingData, float distanceBindingWeight = 0.1f, string optionalBindingShapeName = null, float optionalBindingShapeWeight = 1f)
        {
            JobHandle tempHandle = default;
            VertexInfluence2[][] perBodyInfluences = new VertexInfluence2[characterRenderers.Length][];

            using (var perBodyInfluencesTemp = new NativeArray<VertexInfluence2>(bindingData.skinningData_clothing.Length * characterRenderers.Length, Allocator.TempJob))
            {
                var perBodyInfluencesTemp_ = perBodyInfluencesTemp;
                // Initialize all influences
                //for (int i = 0; i < perBodyInfluencesTemp.Length; i++)
                //{
                //    perBodyInfluencesTemp_[i] = new VertexInfluence2 { influenceA = new VertexInfluence { meshIndex = -1, vertexIndex = -1, score = 0f, weight = 0f }, influenceB = new VertexInfluence { meshIndex = -1, vertexIndex = -1, score = 0f, weight = 0f } };
                //}

                int batchSize = 32; // Complete every N passes to prevent job queue overflow
                // Calculate best influence for each body mesh independently
                for (int bodyIndex = 0; bodyIndex < bindingData.skinningData_character.Length; bodyIndex++)
                {
                    var list = bindingData.skinningData_character[bodyIndex];
                    var index_list = bindingData.indices_character[bodyIndex];

                    tempHandle = new MeshEditing.CalculateVertexInfluencePerBodyJob()
                    {
                        mask = bindingData.baseMask_clothing,
                        maxDistance = 1f,
                        scoreMultiplier = characterRenderers[bodyIndex].weight,
                        distanceBindingWeight = distanceBindingWeight,
                        referenceIndex = bodyIndex,
                        referenceSkinnedVertices = list,
                        referenceVertexIndices = index_list,
                        localSkinnedVertices = bindingData.skinningData_clothing,
                        localVertexIndices = bindingData.indices_clothing,
                        perBodyInfluences = perBodyInfluencesTemp,
                        bodyCount = characterRenderers.Length
                    }.Schedule(bindingData.indices_clothing.Length, 64, tempHandle);

                    if ((bodyIndex + 1) % batchSize == 0 || bodyIndex == characterRenderers.Length - 1)
                    {
                        tempHandle.Complete();
                        tempHandle = default; 
                    }
                }
                tempHandle.Complete();

                // Convert to per-body arrays with proper weights (normalized per-body, not globally)
                for (int bodyIdx = 0; bodyIdx < characterRenderers.Length; bodyIdx++)
                {
                    perBodyInfluences[bodyIdx] = new VertexInfluence2[bindingData.skinningData_clothing.Length];

                    for (int clothingIdx = 0; clothingIdx < bindingData.skinningData_clothing.Length; clothingIdx++)
                    {
                        int idx = clothingIdx * characterRenderers.Length + bodyIdx;
                        var inf = perBodyInfluencesTemp[idx];
                        perBodyInfluences[bodyIdx][clothingIdx] = inf; 
                    }
                }
            }

            return perBodyInfluences;
        }

        protected void GenerateShapes()
        {
            influenceData = GenerateInfluenceData(characterRenderers, bindingData, distanceBindingWeight);

            Dictionary<string, BlendShape> blendShapes = new Dictionary<string, BlendShape>();
            var tempShapes = clothingRenderer.sharedMesh.GetBlendShapes();
            foreach (var shape in tempShapes) blendShapes[shape.name] = shape;

            if (useMeshIslands)
            {
                var islands = MeshDataTools.CalculateMeshIslands(clothingRenderer.sharedMesh);
                float[] blendMask = null;
                if (!string.IsNullOrWhiteSpace(meshIslandBlendMask))
                {
                    if (blendShapes.TryGetValue(meshIslandBlendMask, out var maskShape))
                    {
                        var blendMask_ = VertexGroup.ConvertToVertexGroup(maskShape);
                        blendMask = blendMask_.AsLinearWeightArray(clothingRenderer.sharedMesh.vertexCount);

                        //blendShapes.Remove(meshIslandBlendMask);
                    }
                }
                float[] rootMask = null;
                if (!string.IsNullOrWhiteSpace(meshIslandRootMask))
                {
                    if (blendShapes.TryGetValue(meshIslandRootMask, out var maskShape))
                    {
                        var rootMask_ = VertexGroup.ConvertToVertexGroup(maskShape);
                        rootMask = rootMask_.AsLinearWeightArray(clothingRenderer.sharedMesh.vertexCount);

                        //blendShapes.Remove(meshIslandRootMask);
                    }
                }

                for (int a = 0; a < islands.Count; a++)
                {
                    var island = islands[a];
                    if (island.vertices == null || island.vertices.Length == 0) continue;

                    float highestScore = float.MinValue;
                    int originIndex = 0;
                    for (int b = 0; b < island.vertices.Length; b++)
                    {
                        var vIndex = island.vertices[b];
                        var infData = influenceData[vIndex];
                        float score = (infData.influences.influenceA.score + infData.influences.influenceB.score) * (rootMask == null ? 1f : rootMask[vIndex]);
                        if (score > highestScore)
                        {
                            highestScore = score;
                            originIndex = b;
                        }
                    }

                    island.originIndex = originIndex;
                    islands[a] = island;
                }

                for (int a = 0; a < islands.Count; a++)
                {
                    var island = islands[a];
                    if (island.vertices == null || island.vertices.Length == 0) continue;

                    var rootInfData = influenceData[island.vertices[island.originIndex]];
                    for (int b = 0; b < island.vertices.Length; b++)
                    {
                        var vIndex = island.vertices[b];
                        var infData = influenceData[vIndex];

                        float blendWeight = blendMask == null ? 1f : blendMask[vIndex];
                        float blendWeightInv = 1f - blendWeight;
                        infData.influences.influenceA.weight = infData.influences.influenceA.weight * blendWeightInv;
                        infData.influences.influenceB.weight = infData.influences.influenceB.weight * blendWeightInv;
                        infData.influences.influenceC = rootInfData.influences.influenceA;
                        infData.influences.influenceD = rootInfData.influences.influenceB;
                        infData.influences.influenceC.weight = infData.influences.influenceC.weight * blendWeight;
                        infData.influences.influenceD.weight = infData.influences.influenceD.weight * blendWeight;

                        var vertexInfo = new MorphUtils.TempVertexInfo();
                        vertexInfo.meshIsland = island;
                        vertexInfo.hasOrigin = blendWeight > 0f;
                        if (vertexInfo.hasOrigin)
                        {
                            vertexInfo.centerPoint = bindingData.originalVertices_clothing[island.vertices[island.originIndex]];
                            vertexInfo.originOffset = bindingData.originalVertices_clothing[vIndex] - vertexInfo.centerPoint;
                            vertexInfo.originOffsetDist = vertexInfo.originOffset.magnitude;

                            vertexInfo.hasOrigin = false;
                            if (vertexInfo.originOffsetDist > 0.0001f)
                            {
                                vertexInfo.originOffset = vertexInfo.originOffset / vertexInfo.originOffsetDist;
                                vertexInfo.hasOrigin = true;
                            }
                        }
                        infData.meshIslandVertexInfo = vertexInfo;

                        influenceData[vIndex] = infData;
                    }
                }
            }

            List<string> ignoredShapes = null;
            if (overwriteExistingShapes)
            {
                foreach (var shape in tempShapes)
                {
                    shape.flag = true;
                }
            }
            else
            {
                ignoredShapes = new List<string>();
                foreach (var shape in tempShapes) ignoredShapes.Add(shape.name);
            }

            var toLocal = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(clothingRotationOffset), Vector3.one) * clothingRenderer.transform.worldToLocalMatrix;
            void AddShapeData(int vIndex, BlendShape[] referenceShapes, Vector3[] referenceNormals, Matrix4x4 toWorld, VertexInfluence inf, MorphUtils.TempVertexInfo vertexInfo)
            {
                foreach (var referenceShape in referenceShapes)
                {
                    if (ignoredShapes != null) // don't overwrite existing shapes
                    {
                        if (ignoredShapes.Contains(referenceShape.name)) continue;
                    }

                    if (!blendShapes.TryGetValue(referenceShape.name, out var localShape))
                    {
                        localShape = new BlendShape(referenceShape.name, referenceShape.frames, clothingRenderer.sharedMesh.vertexCount, false);
                        blendShapes[referenceShape.name] = localShape;
                    }
                    else if (localShape.flag)
                    {
                        localShape.flag = false;
                        if (localShape.frames != null)
                        {
                            foreach (var frame in localShape.frames)
                            {
                                for (int a = 0; a < clothingRenderer.sharedMesh.vertexCount; a++)
                                {
                                    frame.deltaVertices[a] = Vector3.zero;
                                    frame.deltaNormals[a] = Vector3.zero;
                                    frame.deltaTangents[a] = Vector3.zero;
                                }
                            }
                        }
                    }

                    var dependencyNormal = toLocal.MultiplyVector(toWorld.MultiplyVector(referenceNormals[inf.vertexIndex]));

                    for (int a = 0; a < localShape.frames.Length; a++)
                    {
                        var refFrame = referenceShape.frames[a];
                        var localFrame = localShape.frames[a];

                        var deltaVertex = toLocal.MultiplyVector(toWorld.MultiplyVector(refFrame.deltaVertices[inf.vertexIndex]));
                        var deltaNormal = toLocal.MultiplyVector(toWorld.MultiplyVector(refFrame.deltaNormals[inf.vertexIndex]));

                        deltaVertex = MorphUtils.AddNormalBasedRotationToDelta(vertexInfo, bindingData.originalVertices_clothing[vIndex], dependencyNormal, deltaVertex, deltaNormal, 1f);

                        localFrame.deltaVertices[vIndex] = localFrame.deltaVertices[vIndex] + deltaVertex * inf.weight;
                        localFrame.deltaNormals[vIndex] = localFrame.deltaNormals[vIndex] + deltaNormal * inf.weight;
                        localFrame.deltaTangents[vIndex] = localFrame.deltaTangents[vIndex] + toLocal.MultiplyVector(toWorld.MultiplyVector(refFrame.deltaTangents[inf.vertexIndex])) * inf.weight;

                    }
                }
            }
            for (int a = 0; a < influenceData.Length; a++)
            {
                var data = influenceData[a];

                if (data.influences.influenceA.weight > 0f) AddShapeData(a, bindingData.shapeData_character[data.influences.influenceA.meshIndex], bindingData.normalData_character[data.influences.influenceA.meshIndex], characterRenderers[data.influences.influenceA.meshIndex].renderer.transform.localToWorldMatrix, data.influences.influenceA, default);
                if (data.influences.influenceB.weight > 0f) AddShapeData(a, bindingData.shapeData_character[data.influences.influenceB.meshIndex], bindingData.normalData_character[data.influences.influenceB.meshIndex], characterRenderers[data.influences.influenceB.meshIndex].renderer.transform.localToWorldMatrix, data.influences.influenceB, default);
                if (data.influences.influenceC.weight > 0f) AddShapeData(a, bindingData.shapeData_character[data.influences.influenceC.meshIndex], bindingData.normalData_character[data.influences.influenceC.meshIndex], characterRenderers[data.influences.influenceC.meshIndex].renderer.transform.localToWorldMatrix, data.influences.influenceC, data.meshIslandVertexInfo);
                if (data.influences.influenceD.weight > 0f) AddShapeData(a, bindingData.shapeData_character[data.influences.influenceD.meshIndex], bindingData.normalData_character[data.influences.influenceD.meshIndex], characterRenderers[data.influences.influenceD.meshIndex].renderer.transform.localToWorldMatrix, data.influences.influenceD, data.meshIslandVertexInfo);
            }

            Mesh m = MeshUtils.DuplicateMesh(clothingRenderer.sharedMesh);
            m.name = clothingRenderer.sharedMesh.name;
            m.ClearBlendShapes();

            shapeList = shapesWindow == null ? null : shapesWindow.GetComponentInChildren<UIRecyclingList>();
            shapeList.Clear();

            if (!string.IsNullOrWhiteSpace(baseVertexMaskShape) && blendShapes.ContainsKey(baseVertexMaskShape))
            {
                blendShapes.Remove(baseVertexMaskShape);
            }

            int shapeIndex = 0;
            foreach (var shape in blendShapes.Values)
            {
                shape.AddToMesh(m);

                if (shapeList != null)
                {
                    shapeList.AddNewMember(shape.name, null, false, (UIRecyclingList.MemberData memberData, GameObject instance) =>
                    {

                        var slider = instance.GetComponentInChildren<Slider>(true);
                        if (slider != null)
                        {
                            slider.gameObject.SetActive(true);
                            slider.enabled = true;

                            slider.minValue = 0;
                            slider.maxValue = shape.frames[shape.frames.Length - 1].weight;

                            if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); else slider.onValueChanged.RemoveAllListeners();
                            slider.SetValueWithoutNotify(clothingRenderer.GetBlendShapeWeight((int)memberData.storage));
                            slider.onValueChanged.AddListener((float val) =>
                            {
                                clothingRenderer.SetBlendShapeWeight((int)memberData.storage, val);
                                syncShapes = true;
                            });
                        }

                        var toggle = instance.GetComponentInChildren<Toggle>(true);
                        if (toggle != null)
                        {
                            toggle.SetIsOnWithoutNotify(activeShapes.Contains((int)memberData.storage));

                            if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                            toggle.onValueChanged.AddListener((bool isOn) =>
                            {
                                if (isOn)
                                {
                                    if (!activeShapes.Contains((int)memberData.storage))
                                    {
                                        activeShapes.Add((int)memberData.storage);
                                        SyncActiveShapeCollections();
                                    }
                                }
                                else
                                {
                                    activeShapes.RemoveAll(i => i == (int)memberData.storage);
                                    SyncActiveShapeCollections();
                                }

                                RefreshSelectDeselectAllShapesButton();
                            });
                        }

                    }, shapeIndex);
                }

                shapeIndex++;
            }
            if (shapeList != null) shapeList.Refresh();

            meshEditor = gameObject.AddOrGetComponent<MeshEditor>();
            meshEditor.meshViewerSkinnedRenderer = clothingRenderer;
            meshEditor.SetFetchDynamicBlendShapeFrameWeightDelegate(FetchDynamicBlendShapeFrameWeight);
            meshEditor.StartEditingMesh(m);
            meshEditor.InitializeBlendShapeEdits();

            clothingRenderer.sharedMesh = meshEditor.EditedMesh;
            for (int a = 0; a < m.blendShapeCount; a++) clothingRenderer.SetBlendShapeWeight(a, 0);

            var tempIndices = new int[m.vertexCount];
            for (int a = 0; a < tempIndices.Length; a++) tempIndices[a] = a;
            meshEditor.SelectVertices(tempIndices);

            if (tempCharacterData == null)
            {
                var meshInputs = new AmalgamatedSkinnedMeshDataTracker.MeshInput[characterRenderers.Length];
                for (int a = 0; a < meshInputs.Length; a++)
                {
                    meshInputs[a] = new AmalgamatedSkinnedMeshDataTracker.MeshInput() { renderer = characterRenderers[a].renderer };
                }
                tempCharacterData = new AmalgamatedSkinnedMeshDataTracker(meshInputs);
            }
            tempCharacterData.BuildCollisionData();
            meshEditor.SetCollisionData(
                tempCharacterData.CollisionTriangles.AsArray(), tempCharacterData.CollisionVertexData.AsArray(), tempCharacterData.CollisionVertexSkinningData.AsArray(),
                tempCharacterData.GetBlendShapeNames(), tempCharacterData.CollisionBlendShapeData.AsArray(), tempCharacterData.CollisionBlendShapeFrameCounts.AsArray(), tempCharacterData.CollisionBlendShapeStartIndices.AsArray()
                );

            DeselectAllShapes();

            meshEditor.CalculateInitialPenetrationMask();
        }

    }
}

#endif