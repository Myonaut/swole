#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace Swole.API.Unity
{
    /// <summary>
    /// Used to designate a region of the scene as a surface that can be conformed to by certain mesh materials. The main function of this component is to generate a heightmap for the surface using physics raycasting and custom mesh raycasting.
    /// </summary>
    public class SurfaceProxy : MonoBehaviour
    {

        /// <summary>
        /// Must remain identical to the sub shader graph equivalent (Sub Graphs/RemapPosition)
        /// </summary>
        public static void RemapPosition(

            Vector3 axisX,
            Vector3 axisY,
            Vector3 axisZ,
            Vector3 position,

            out Vector3 Out_position
            )
        {
            Out_position = new Vector3(math.dot(position, axisX), math.dot(position, axisY), math.dot(position, axisZ));
        }
        /// <summary>
        /// Must remain identical to the sub shader graph equivalent (Surfaces/CalculateSlopeOffset)
        /// </summary>
        public static void CalculateSlopeOffset(
            Texture2D heightMap,
            Vector2 uv,
            Vector2 uvOffset,
            Vector3 surfaceTangentWS,
            Vector3 surfaceBitangentWS,
            float centerHeightWS,
            float maxHeightWS,

            out Vector3 Out_offsetWS)
        {
            uv = uv + uvOffset;

            float height = heightMap.GetPixelBilinear(uv.x, uv.y).r; 
            height = height * maxHeightWS;
            height = centerHeightWS - height;

            Vector2 uvOffsetDir = uvOffset.normalized;

            Out_offsetWS = ((surfaceTangentWS * uvOffsetDir.x) + (surfaceBitangentWS * uvOffsetDir.y)) * height;
        }
        /// <summary>
        /// Must remain identical to the sub shader graph equivalent (Surfaces/AdjustSurfaceHeight)
        /// </summary>
        public static void AdjustSurfaceHeight(
            float deformationMix, 
            Vector3 rendererPositionWS, 
            Texture2D heightMap,  
            Vector3 posOS,
            Vector3 normOS,

            Vector3 surfaceCenterWS,
            Vector3 surfaceNormalWS,
            Vector3 surfaceTangentWS,
            Vector3 surfaceBitangentWS,

            float surfaceSizeX,
            float surfaceSizeY,
            float surfaceHeightMax,

            float defaultHeightOffset,
            float defaultLocalHeightOffset,

            float localHeightMix,

            Vector4 deformationBlendVertexColorMask,
            Color[] vertexColors,

            Vector3 localPositionOffset,

            float slopeStep,

            Matrix4x4 localToWorld,
            Matrix4x4 worldToLocal,

            out Vector3 Out_posOS, out Vector3 Out_normOS)
        {
            Out_posOS = posOS;
            Out_normOS = normOS;

            Vector3 surfaceNormalWS_Neg = -surfaceNormalWS;
            RemapPosition(surfaceTangentWS, surfaceNormalWS_Neg, surfaceBitangentWS, surfaceCenterWS, out Vector3 surfaceCenterSS);

            Vector3 posOS_offset = posOS + localPositionOffset;
            Vector3 posWS_offset = localToWorld.MultiplyPoint(posOS_offset);
            RemapPosition(surfaceTangentWS, surfaceNormalWS_Neg, surfaceBitangentWS, posWS_offset, out Vector3 posSS_offset);

            float localHeight = posSS_offset.y + defaultLocalHeightOffset;
            float defaultHeight = math.dot(rendererPositionWS, surfaceNormalWS_Neg) + defaultHeightOffset;

            posSS_offset = new Vector3(posSS_offset.x, Mathf.LerpUnclamped(defaultHeight, localHeight, localHeightMix), posSS_offset.z);

            Vector3 surfaceOffsetSS = posSS_offset - surfaceCenterSS;
            Vector2 centerUV = new Vector2(math.saturate((surfaceOffsetSS.x / surfaceSizeX) + 0.5f), math.saturate((surfaceOffsetSS.z / surfaceSizeY) + 0.5f));

            
            float centerHeightSample = heightMap.GetPixelBilinear(centerUV.x, centerUV.y).r;
            float centerHeightMul = math.select(0f, 1f, centerHeightSample > 0.0001f) * deformationMix;
            float centerHeightWS = centerHeightSample * surfaceHeightMax;

            //Debug.DrawLine(surfaceCenterWS, surfaceCenterWS + (surfaceTangentWS * (centerUV.x - 0.5f) * surfaceSizeX) + (surfaceNormalWS_Neg * centerHeightWS) + (surfaceBitangentWS * (centerUV.y - 0.5f) * surfaceSizeY), Color.red, 80f); 

            float heightOffset = centerHeightWS - surfaceOffsetSS.y; 
            float heightOffsetNormalized = math.saturate(heightOffset / surfaceHeightMax);

            float heightOffsetMasked = heightOffset * centerHeightMul;
            float heightOffsetNormalizedMasked = heightOffsetNormalized * centerHeightMul;

            float slopeStepNormalizeX = slopeStep / surfaceSizeX;
            float slopeStepNormalizeX_Neg = -slopeStepNormalizeX;

            float slopeStepNormalizeY = slopeStep / surfaceSizeY;
            float slopeStepNormalizeY_Neg = -slopeStepNormalizeY;

            Vector2 slopeUV_TL = new Vector2(slopeStepNormalizeX_Neg, slopeStepNormalizeY);
            Vector2 slopeUV_TM = new Vector2(0, slopeStepNormalizeY);
            Vector2 slopeUV_TR = new Vector2(slopeStepNormalizeX, slopeStepNormalizeY);

            Vector2 slopeUV_ML = new Vector2(slopeStepNormalizeX_Neg, 0);
            Vector2 slopeUV_MR = new Vector2(slopeStepNormalizeX, 0);

            Vector2 slopeUV_BL = new Vector2(slopeStepNormalizeX_Neg, slopeStepNormalizeY_Neg);
            Vector2 slopeUV_BM = new Vector2(0, slopeStepNormalizeY_Neg);
            Vector2 slopeUV_BR = new Vector2(slopeStepNormalizeX, slopeStepNormalizeY_Neg);

            Vector3 slopeOffsetAdd = Vector3.zero;
            Vector3 slopeOffset = Vector3.zero;

            CalculateSlopeOffset(heightMap, centerUV, slopeUV_TL, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;
            CalculateSlopeOffset(heightMap, centerUV, slopeUV_TM, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;
            CalculateSlopeOffset(heightMap, centerUV, slopeUV_TR, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;

            CalculateSlopeOffset(heightMap, centerUV, slopeUV_ML, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;
            CalculateSlopeOffset(heightMap, centerUV, slopeUV_MR, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;

            CalculateSlopeOffset(heightMap, centerUV, slopeUV_BL, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;
            CalculateSlopeOffset(heightMap, centerUV, slopeUV_BM, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;
            CalculateSlopeOffset(heightMap, centerUV, slopeUV_BR, surfaceTangentWS, surfaceBitangentWS, centerHeightWS, surfaceHeightMax, out slopeOffsetAdd);
            slopeOffset += slopeOffsetAdd;

            Vector3 posWS = localToWorld.MultiplyPoint(posOS);
            posWS = posWS + surfaceNormalWS_Neg * heightOffsetMasked;

            Out_posOS = worldToLocal.MultiplyPoint(posWS);

            Vector3 normWS = localToWorld.MultiplyVector(normOS);
            normWS = normWS + slopeOffset * heightOffsetNormalizedMasked;
            normWS = normWS.normalized;

            Out_normOS = worldToLocal.MultiplyVector(normWS).normalized;
        }


        [Tooltip("The texture size of the largest tile axis. Dimensions will get converted to powers of two. Determines how many casts to execute per axis.")]
        public int heightMapSize = 256;
        [Tooltip("The world space size of the surface tile in meters along the world x-axis. The width of the heightmap texture represents this space.")]
        public float tileSizeX = 100f;
        [Tooltip("The world space size of the surface tile in meters along the world z-axis. The height of the heightmap texture represents this space.")]
        public float tileSizeZ = 100f;

        public Vector3 SurfaceNormalWorld => transform.TransformDirection(Vector3.up).normalized; // vertex height = dot(vertexPos - surfaceCenterPos, -surfaceNormal)
        public Vector3 CastDirectionWorld => -SurfaceNormalWorld;
        public Vector3 CastTangentWorld => transform.TransformDirection(Vector3.right).normalized; // vertex u = (dot(vertexPos - surfaceCenterPos, surfaceTangent) / surfaceTileSizeX) + 0.5
        public Vector3 CastBitangentWorld => Vector3.Cross(SurfaceNormalWorld, CastTangentWorld).normalized;  // vertex v = (dot(vertexPos - surfaceCenterPos, surfaceBitangent) / surfaceTileSizeY) + 0.5
        
        public float castDistance = 100f;
        public bool castAgainstColliders = true;
        public bool castAgainstMeshes = true;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var center = transform.position;
            float halfX = tileSizeX * 0.5f;
            float halfZ = tileSizeZ * 0.5f;

            Vector3 castDir = CastDirectionWorld;
            Vector3 castTangent = CastTangentWorld;
            Vector3 castBitangent = CastBitangentWorld; 

            Vector3 p0 = center + (castTangent * -halfX) + (castBitangent * -halfZ);
            Vector3 p1 = center + (castTangent * halfX) + (castBitangent * -halfZ);
            Vector3 p2 = center + (castTangent * halfX) + (castBitangent * halfZ);
            Vector3 p3 = center + (castTangent * -halfX) + (castBitangent * halfZ);

            Vector3 castOffset = castDir * castDistance; 

            Vector3 p4 = p0 + castOffset;
            Vector3 p5 = p1 + castOffset;
            Vector3 p6 = p2 + castOffset;
            Vector3 p7 = p3 + castOffset;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);

            Gizmos.DrawLine(center, center + castOffset); 

            Gizmos.color = Color.Lerp(Color.green, Color.red, 0.35f);
            Gizmos.DrawLine(p4, p5);
            Gizmos.DrawLine(p5, p6);
            Gizmos.DrawLine(p6, p7);
            Gizmos.DrawLine(p7, p4);
        }
#endif

        public LayerMask colliderMask = ~0;
        public LayerMask meshMask = ~0;

        [Serializable]
        public struct CollidableMeshMaterial
        {
            public Material material;
            public float heightOffset;
        }
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public List<CollidableMeshMaterial> collidableMeshMaterials;

        [Header("Material Properties")]
        public SurfaceMaterialProperties materialProperties = SurfaceMaterialProperties.Default; 

        [Serializable]
        public struct SurfaceMaterialProperties
        {
            public string deformationMix_materialProperty;

            public string heightMap_materialProperty;

            public string surfaceCenter_materialProperty;

            public string surfaceNormal_materialProperty;
            public string surfaceTangent_materialProperty;
            public string surfaceBitangent_materialProperty;

            public string tileSizeX_materialProperty;
            public string tileSizeY_materialProperty;
            public string heightMax_materialProperty;
            public string defaultHeightOffset_materialProperty;
            public string localHeightOffset_materialProperty;

            public string localHeightMix_materialProperty;
            public string deformationBlendVertexColorMask_materialProperty;

            public string slopeStep_materialProperty;


            public string fadeStartHeightDifference_materialProperty;
            public string fadeMaxHeightDifference_materialProperty;
            public string fadeExponent_materialProperty;
            public string fadeCutoff_materialProperty;
            public string fadeInvert_materialProperty;
            public string useAbsoluteHeightDifference_materialProperty;
            public string invertHeightDifference_materialProperty;

            public string transparencyDitherAmount_materialProperty;

            public static SurfaceMaterialProperties Default
            {
                get
                {
                    var props = new SurfaceMaterialProperties();

                    props.deformationMix_materialProperty = "_DeformationMix";

                    props.heightMap_materialProperty = "_SurfaceHeightMap";

                    props.surfaceCenter_materialProperty = "_SurfaceCenter";

                    props.surfaceNormal_materialProperty = "_SurfaceNormal";
                    props.surfaceTangent_materialProperty = "_SurfaceTangent";
                    props.surfaceBitangent_materialProperty = "_SurfaceBitangent";

                    props.tileSizeX_materialProperty = "_SurfaceTileSizeX";
                    props.tileSizeY_materialProperty = "_SurfaceTileSizeY";
                    props.heightMax_materialProperty = "_SurfaceHeightMax";
                    props.defaultHeightOffset_materialProperty = "_DefaultHeightOffset";
                    props.localHeightOffset_materialProperty = "_LocalHeightOffset";
                    props.localHeightMix_materialProperty = "_LocalHeightMix";
                    props.deformationBlendVertexColorMask_materialProperty = "_DeformationBlendVertexColorMask";

                    props.slopeStep_materialProperty = "_SlopeStep";

                    props.fadeStartHeightDifference_materialProperty = "_FadeStartHeightDifference";
                    props.fadeMaxHeightDifference_materialProperty = "_FadeMaxHeightDifference";
                    props.fadeExponent_materialProperty = "_FadeExponent";
                    props.fadeCutoff_materialProperty = "_FadeCutoff";
                    props.fadeInvert_materialProperty = "_FadeInvert";
                    props.useAbsoluteHeightDifference_materialProperty = "_UseAbsoluteHeightDifference";
                    props.invertHeightDifference_materialProperty = "_InvertHeightDifference";
                    props.transparencyDitherAmount_materialProperty = "_TransparencyDitherAmount";

                    return props;
                }
            }

            public SurfaceMaterialProperties OverrideIfValid(SurfaceMaterialProperties overrides)
            {
                var result = this;

                if (!string.IsNullOrWhiteSpace(overrides.deformationMix_materialProperty))
                {
                    result.deformationMix_materialProperty = overrides.deformationMix_materialProperty;
                }

                if (!string.IsNullOrWhiteSpace(overrides.heightMap_materialProperty))
                {
                    result.heightMap_materialProperty = overrides.heightMap_materialProperty;
                }

                if (!string.IsNullOrWhiteSpace(overrides.surfaceCenter_materialProperty))
                {
                    result.surfaceCenter_materialProperty = overrides.surfaceCenter_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.surfaceNormal_materialProperty))
                {
                    result.surfaceNormal_materialProperty = overrides.surfaceNormal_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.surfaceTangent_materialProperty))
                {
                    result.surfaceTangent_materialProperty = overrides.surfaceTangent_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.surfaceBitangent_materialProperty))
                {
                    result.surfaceBitangent_materialProperty = overrides.surfaceBitangent_materialProperty;
                }

                if (!string.IsNullOrWhiteSpace(overrides.tileSizeX_materialProperty))
                {
                    result.tileSizeX_materialProperty = overrides.tileSizeX_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.tileSizeY_materialProperty))
                {
                    result.tileSizeY_materialProperty = overrides.tileSizeY_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.heightMax_materialProperty))
                {
                    result.heightMax_materialProperty = overrides.heightMax_materialProperty;
                }

                if (!string.IsNullOrWhiteSpace(overrides.defaultHeightOffset_materialProperty))
                {
                    result.defaultHeightOffset_materialProperty = overrides.defaultHeightOffset_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.localHeightOffset_materialProperty))
                {
                    result.localHeightOffset_materialProperty = overrides.localHeightOffset_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.localHeightMix_materialProperty))
                {
                    result.localHeightMix_materialProperty = overrides.localHeightMix_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.deformationBlendVertexColorMask_materialProperty))
                {
                    result.deformationBlendVertexColorMask_materialProperty = overrides.deformationBlendVertexColorMask_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.slopeStep_materialProperty))
                {
                    result.slopeStep_materialProperty = overrides.slopeStep_materialProperty;
                }

                if (!string.IsNullOrWhiteSpace(overrides.fadeStartHeightDifference_materialProperty))
                {
                    result.fadeStartHeightDifference_materialProperty = overrides.fadeStartHeightDifference_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.fadeMaxHeightDifference_materialProperty))
                {
                    result.fadeMaxHeightDifference_materialProperty = overrides.fadeMaxHeightDifference_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.fadeExponent_materialProperty))
                {
                    result.fadeExponent_materialProperty = overrides.fadeExponent_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.fadeCutoff_materialProperty))
                {
                    result.fadeCutoff_materialProperty = overrides.fadeCutoff_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.fadeInvert_materialProperty))
                {
                    result.fadeInvert_materialProperty = overrides.fadeInvert_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.useAbsoluteHeightDifference_materialProperty))
                {
                    result.useAbsoluteHeightDifference_materialProperty = overrides.useAbsoluteHeightDifference_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.invertHeightDifference_materialProperty))
                {
                    result.invertHeightDifference_materialProperty = overrides.invertHeightDifference_materialProperty;
                }
                if (!string.IsNullOrWhiteSpace(overrides.transparencyDitherAmount_materialProperty))
                {
                    result.transparencyDitherAmount_materialProperty = overrides.transparencyDitherAmount_materialProperty;
                }

                return result;
            }
        }

        [Header("Material Groups")]
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public List<MaterialGroup> materialGroups;

        [Serializable]
        public class MaterialGroup
        {
            public Material material;
            public SurfaceMaterialProperties materialPropertyNameOverrides;

            public bool isDeformable = true;

            [Range(-5f, 5f)]
            public float deformationMix;
            public float GetDeformationMix(SurfaceMaterialProperties matProps)
            {
                if (material == null) return deformationMix;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.deformationMix_materialProperty)) return material.GetFloat(matProps.deformationMix_materialProperty);

                return deformationMix;
            }

            public float defaultHeightOffset;
            public float GetDefaultHeightOffset(SurfaceMaterialProperties matProps)
            {
                if (material == null) return defaultHeightOffset;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.defaultHeightOffset_materialProperty)) return material.GetFloat(matProps.defaultHeightOffset_materialProperty);

                return defaultHeightOffset;
            }

            public float localHeightOffset;
            public float GetLocalHeightOffset(SurfaceMaterialProperties matProps)
            {
                if (material == null) return localHeightOffset;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.localHeightOffset_materialProperty)) return material.GetFloat(matProps.localHeightOffset_materialProperty);

                return localHeightOffset;
            }

            [Range(0f, 1f), Tooltip("Mix between using the object's starting height and the vertex starting height.")]
            public float localHeightMix;
            public float GetLocalHeightMix(SurfaceMaterialProperties matProps)
            {
                if (material == null) return localHeightMix;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.localHeightMix_materialProperty)) return material.GetFloat(matProps.localHeightMix_materialProperty);

                return localHeightMix;
            }

            [Tooltip("Fractions of the vertex color channels to use as multipliers for height adjustment.")]
            public Vector4 deformationBlendVertexColorMask;
            public Vector4 GetDeformationBlendVertexColorMask(SurfaceMaterialProperties matProps)
            {
                if (material == null) return deformationBlendVertexColorMask;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.deformationBlendVertexColorMask_materialProperty)) return material.GetVector(matProps.deformationBlendVertexColorMask_materialProperty);

                return deformationBlendVertexColorMask;
            }

            [Tooltip("The step size for slope detection (in meters). It is a small offset applied to sample the heightmap around the vertex and determine the resulting slope.")]
            public float slopeStep = 0.1f;
            public float GetSlopeStep(SurfaceMaterialProperties matProps)
            {
                if (material == null) return slopeStep;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.slopeStep_materialProperty)) return material.GetFloat(matProps.slopeStep_materialProperty);

                return slopeStep;
            }

            [Tooltip("The height difference at or below which the vertex is completely faded away.")]
            public float fadeStartHeightDifference = 0f;
            public float GetFadeStartHeightDifference(SurfaceMaterialProperties matProps)
            {
                if (material == null) return fadeStartHeightDifference;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.fadeStartHeightDifference_materialProperty)) return material.GetFloat(matProps.fadeStartHeightDifference_materialProperty);

                return fadeStartHeightDifference;
            }
            [Tooltip("The height difference range used to blend vertex fading.")]
            public float fadeMaxHeightDifference = 0.1f;
            public float GetFadeMaxHeightDifference(SurfaceMaterialProperties matProps)
            {
                if (material == null) return fadeMaxHeightDifference;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.fadeMaxHeightDifference_materialProperty)) return material.GetFloat(matProps.fadeMaxHeightDifference_materialProperty);

                return fadeMaxHeightDifference;
            }
            [Tooltip("The exponential falloff of fading. Values greater than one shrink spread. Values less than one expand spread.")]
            public float fadeExponent = 1f;
            public float GetFadeExponent(SurfaceMaterialProperties matProps)
            {
                if (material == null) return fadeExponent;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.fadeExponent_materialProperty)) return material.GetFloat(matProps.fadeExponent_materialProperty);

                return fadeExponent;
            }
            [Tooltip("Alpha cutoff for fading.")]
            public float fadeCutoff = 0.5f;
            public float GetFadeCutoff(SurfaceMaterialProperties matProps)
            {
                if (material == null) return fadeCutoff;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.fadeCutoff_materialProperty)) return material.GetFloat(matProps.fadeCutoff_materialProperty);

                return fadeCutoff;
            }
            [Tooltip("Should vertex fading be inverted? Meaning that vertices which are further away from the surface are faded out.")]
            public bool fadeInvert = false;
            public bool GetFadeInvert(SurfaceMaterialProperties matProps)
            {
                if (material == null) return fadeInvert;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.fadeInvert_materialProperty)) return material.GetInteger(matProps.fadeInvert_materialProperty) != 0;

                return fadeInvert;
            }
            [Tooltip("Should the height difference be converted to its absolute value?")]
            public bool useAbsoluteHeightDifference = false;
            public bool GetUseAbsoluteHeightDifference(SurfaceMaterialProperties matProps)
            {
                if (material == null) return useAbsoluteHeightDifference;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.useAbsoluteHeightDifference_materialProperty)) return material.GetInteger(matProps.useAbsoluteHeightDifference_materialProperty) != 0;

                return useAbsoluteHeightDifference;
            }

            public bool invertHeightDifference = false;
            public bool GetInvertHeightDifference(SurfaceMaterialProperties matProps)
            {
                if (material == null) return invertHeightDifference;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.invertHeightDifference_materialProperty)) return material.GetInteger(matProps.invertHeightDifference_materialProperty) != 0;

                return invertHeightDifference;
            }


            [Tooltip("Amount of dithering used for fake transparency.")]
            public float transparencyDitherAmount = 0.8f;
            public float GetTransparencyDitherAmount(SurfaceMaterialProperties matProps)
            {
                if (material == null) return transparencyDitherAmount;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.transparencyDitherAmount_materialProperty)) return material.GetFloat(matProps.transparencyDitherAmount_materialProperty);

                return transparencyDitherAmount;
            }

            public Vector3 GetSurfaceCenterWS(SurfaceMaterialProperties matProps, Vector3 defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.surfaceCenter_materialProperty)) return material.GetVector(matProps.surfaceCenter_materialProperty);

                return defaultValue;
            }
            public Vector3 GetSurfaceNormalWS(SurfaceMaterialProperties matProps, Vector3 defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.surfaceNormal_materialProperty)) return material.GetVector(matProps.surfaceNormal_materialProperty);

                return defaultValue;
            }
            public Vector3 GetSurfaceTangentWS(SurfaceMaterialProperties matProps, Vector3 defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.surfaceTangent_materialProperty)) return material.GetVector(matProps.surfaceTangent_materialProperty);

                return defaultValue;
            }
            public Vector3 GetSurfaceBitangentWS(SurfaceMaterialProperties matProps, Vector3 defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.surfaceBitangent_materialProperty)) return material.GetVector(matProps.surfaceBitangent_materialProperty);

                return defaultValue;
            }

            public float GetSurfaceSizeX(SurfaceMaterialProperties matProps, float defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.tileSizeX_materialProperty)) return material.GetFloat(matProps.tileSizeX_materialProperty);

                return defaultValue;
            }
            public float GetSurfaceSizeY(SurfaceMaterialProperties matProps, float defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.tileSizeY_materialProperty)) return material.GetFloat(matProps.tileSizeY_materialProperty);

                return defaultValue;
            }

            public float GetSurfaceHeightMax(SurfaceMaterialProperties matProps, float defaultValue)
            {
                if (material == null) return defaultValue;

                matProps = matProps.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(matProps.heightMax_materialProperty)) return material.GetFloat(matProps.heightMax_materialProperty);

                return defaultValue;
            }

            public void PushHeightMap(SurfaceProxy proxy)
            {
                if (material == null) return;

                var materialProperties = proxy.materialProperties.OverrideIfValid(materialPropertyNameOverrides);
                PushHeightMap(proxy, materialProperties);
            }
            public void PushHeightMap(SurfaceProxy proxy, SurfaceMaterialProperties materialProperties)
            {
                if (material == null) return;

                if (material.HasProperty(materialProperties.heightMap_materialProperty))
                {
                    material.SetTexture(materialProperties.heightMap_materialProperty, proxy.heightMap);
                }

                if (material.HasProperty(materialProperties.surfaceCenter_materialProperty))
                {
                    material.SetVector(materialProperties.surfaceCenter_materialProperty, proxy.transform.position);
                }
                if (material.HasProperty(materialProperties.surfaceNormal_materialProperty))
                {
                    material.SetVector(materialProperties.surfaceNormal_materialProperty, proxy.SurfaceNormalWorld);
                }
                if (material.HasProperty(materialProperties.surfaceTangent_materialProperty))
                {
                    material.SetVector(materialProperties.surfaceTangent_materialProperty, proxy.CastTangentWorld);
                }
                if (material.HasProperty(materialProperties.surfaceBitangent_materialProperty))
                {
                    material.SetVector(materialProperties.surfaceBitangent_materialProperty, proxy.CastBitangentWorld);
                }

                if (material.HasProperty(materialProperties.tileSizeX_materialProperty))
                {
                    material.SetFloat(materialProperties.tileSizeX_materialProperty, proxy.tileSizeX);
                }
                if (material.HasProperty(materialProperties.tileSizeY_materialProperty))
                {
                    material.SetFloat(materialProperties.tileSizeY_materialProperty, proxy.tileSizeZ);
                }
                if (material.HasProperty(materialProperties.heightMax_materialProperty))
                {
                    material.SetFloat(materialProperties.heightMax_materialProperty, proxy.castDistance);
                }
            }

            public bool pushProperties;
            public void PushProperties(SurfaceProxy proxy)
            {
                if (material == null) return;

                var materialProperties = proxy.materialProperties.OverrideIfValid(materialPropertyNameOverrides);
                PushHeightMap(proxy, materialProperties);

                if (material.HasProperty(materialProperties.deformationMix_materialProperty))
                {
                    material.SetFloat(materialProperties.deformationMix_materialProperty, deformationMix);
                }

                if (material.HasProperty(materialProperties.defaultHeightOffset_materialProperty))
                {
                    material.SetFloat(materialProperties.defaultHeightOffset_materialProperty, defaultHeightOffset);
                }
                if (material.HasProperty(materialProperties.localHeightOffset_materialProperty))
                {
                    material.SetFloat(materialProperties.localHeightOffset_materialProperty, localHeightOffset);
                }
                if (material.HasProperty(materialProperties.localHeightMix_materialProperty))
                {
                    material.SetFloat(materialProperties.localHeightMix_materialProperty, localHeightMix);
                }
                if (material.HasProperty(materialProperties.deformationBlendVertexColorMask_materialProperty))
                {
                    material.SetVector(materialProperties.deformationBlendVertexColorMask_materialProperty, deformationBlendVertexColorMask);
                }
                if (material.HasProperty(materialProperties.slopeStep_materialProperty))
                {
                    material.SetFloat(materialProperties.slopeStep_materialProperty, slopeStep);
                }

                if (material.HasProperty(materialProperties.fadeStartHeightDifference_materialProperty))
                {
                    material.SetFloat(materialProperties.fadeStartHeightDifference_materialProperty, fadeStartHeightDifference);
                }
                if (material.HasProperty(materialProperties.fadeMaxHeightDifference_materialProperty))
                {
                    material.SetFloat(materialProperties.fadeMaxHeightDifference_materialProperty, fadeMaxHeightDifference);
                }
                if (material.HasProperty(materialProperties.fadeExponent_materialProperty))
                {
                    material.SetFloat(materialProperties.fadeExponent_materialProperty, fadeExponent);
                }
                if (material.HasProperty(materialProperties.fadeCutoff_materialProperty))
                {
                    material.SetFloat(materialProperties.fadeCutoff_materialProperty, fadeCutoff);
                }
                if (material.HasProperty(materialProperties.fadeInvert_materialProperty))
                {
                    material.SetInt(materialProperties.fadeInvert_materialProperty, fadeInvert ? 1 : 0);
                }
                if (material.HasProperty(materialProperties.useAbsoluteHeightDifference_materialProperty))
                {
                    material.SetInt(materialProperties.useAbsoluteHeightDifference_materialProperty, useAbsoluteHeightDifference ? 1 : 0);
                }
                if (material.HasProperty(materialProperties.invertHeightDifference_materialProperty))
                {
                    material.SetInt(materialProperties.invertHeightDifference_materialProperty, invertHeightDifference ? 1 : 0);
                }
                if (material.HasProperty(materialProperties.transparencyDitherAmount_materialProperty))
                {
                    material.SetFloat(materialProperties.transparencyDitherAmount_materialProperty, transparencyDitherAmount);
                }
            }

            public bool pullProperties;
            public void PullProperties(SurfaceProxy proxy)
            {
                if (material == null) return;

                var materialProperties = proxy.materialProperties.OverrideIfValid(materialPropertyNameOverrides);
                if (material.HasProperty(materialProperties.deformationMix_materialProperty))
                {
                    deformationMix = material.GetFloat(materialProperties.deformationMix_materialProperty);
                }

                if (material.HasProperty(materialProperties.defaultHeightOffset_materialProperty))
                {
                    defaultHeightOffset = material.GetFloat(materialProperties.defaultHeightOffset_materialProperty);
                }
                if (material.HasProperty(materialProperties.localHeightOffset_materialProperty))
                {
                    localHeightOffset = material.GetFloat(materialProperties.localHeightOffset_materialProperty);
                }
                if (material.HasProperty(materialProperties.localHeightMix_materialProperty))
                {
                    localHeightMix = material.GetFloat(materialProperties.localHeightMix_materialProperty);
                }
                if (material.HasProperty(materialProperties.deformationBlendVertexColorMask_materialProperty))
                {
                    deformationBlendVertexColorMask = material.GetVector(materialProperties.deformationBlendVertexColorMask_materialProperty);
                }
                if (material.HasProperty(materialProperties.slopeStep_materialProperty))
                {
                    slopeStep = material.GetFloat(materialProperties.slopeStep_materialProperty);
                }

                if (material.HasProperty(materialProperties.fadeStartHeightDifference_materialProperty))
                {
                    fadeStartHeightDifference = material.GetFloat(materialProperties.fadeStartHeightDifference_materialProperty);
                }
                if (material.HasProperty(materialProperties.fadeMaxHeightDifference_materialProperty))
                {
                    fadeMaxHeightDifference = material.GetFloat(materialProperties.fadeMaxHeightDifference_materialProperty);
                }
                if (material.HasProperty(materialProperties.fadeExponent_materialProperty))
                {
                    fadeExponent = material.GetFloat(materialProperties.fadeExponent_materialProperty);
                }
                if (material.HasProperty(materialProperties.fadeCutoff_materialProperty))
                {
                    fadeCutoff = material.GetFloat(materialProperties.fadeCutoff_materialProperty); 
                }
                if (material.HasProperty(materialProperties.fadeInvert_materialProperty))
                {
                    fadeInvert = material.GetInt(materialProperties.fadeInvert_materialProperty) != 0;
                }
                if (material.HasProperty(materialProperties.useAbsoluteHeightDifference_materialProperty))
                {
                    useAbsoluteHeightDifference = material.GetInt(materialProperties.useAbsoluteHeightDifference_materialProperty) != 0;
                }
                if (material.HasProperty(materialProperties.invertHeightDifference_materialProperty))
                {
                    invertHeightDifference = material.GetInt(materialProperties.invertHeightDifference_materialProperty) != 0;
                }
                if (material.HasProperty(materialProperties.transparencyDitherAmount_materialProperty))
                {
                    transparencyDitherAmount = material.GetFloat(materialProperties.transparencyDitherAmount_materialProperty);
                }
            }

            public void EditorCheck(SurfaceProxy proxy)
            {
                if (pushProperties)
                {
                    pushProperties = false;
                    PushProperties(proxy);
                }
                if (pullProperties)
                {
                    pullProperties = false;
                    PullProperties(proxy);
                }
            }
        }

        [Header("Heightmap")]
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public string assetSaveDir;
        public string heightMapName;

        [SerializeField]
        public enum HeightMapFormat
        {
            R8, R16, RFloat
        }

        public HeightMapFormat heightMapFormat;
        public Texture2D heightMap;

        public Texture2D GenerateHeightMap(int initialSize, HeightMapFormat heightMapFormat)
        {
            int width = Maths.AsPowerOf2(initialSize);
            int height = width;

            if (tileSizeX < tileSizeZ)
            {
                width = Maths.AsPowerOf2((int)(height * (tileSizeX / tileSizeZ)));
            } 
            else if (tileSizeZ < tileSizeX)
            {
                height = Maths.AsPowerOf2((int)(width * (tileSizeZ / tileSizeX)));
            }

            var format = TextureFormat.R16;
            switch(heightMapFormat)
            {
                case HeightMapFormat.R8:
                    format = TextureFormat.R8;
                    break;
                case HeightMapFormat.R16:
                    format = TextureFormat.R16;
                    break;
                case HeightMapFormat.RFloat:
                    format = TextureFormat.RFloat;
                    break;
            }


            var center = transform.position;
            float halfX = tileSizeX * 0.5f;
            float halfZ = tileSizeZ * 0.5f;

            Vector3 castDir = CastDirectionWorld;
            Vector3 castOffset = castDir * castDistance;
            Vector3 castTangent = CastTangentWorld;
            Vector3 castBitangent = CastBitangentWorld;

            Vector3 p0 = center + (castTangent * -halfX) + (castBitangent * -halfZ);
            Vector3 p1 = center + (castTangent * halfX) + (castBitangent * -halfZ);
            Vector3 p2 = center + (castTangent * halfX) + (castBitangent * halfZ);
            Vector3 p3 = center + (castTangent * -halfX) + (castBitangent * halfZ);

            float3 p0f = (float3)p0;
            float3 p1f = (float3)p1;
            float3 p2f = (float3)p2;
            float3 p3f = (float3)p3;

            bool castAgainstMeshes = this.castAgainstMeshes && (collidableMeshMaterials != null && collidableMeshMaterials.Count > 0);

            Texture2D heightMap = new Texture2D(width, height, format, false, true);
            heightMap.wrapMode = TextureWrapMode.Clamp;
            NativeArray<float> worldHeightsMeshes = castAgainstMeshes ? new NativeArray<float>(width * height, Allocator.Persistent) : default;
            float[] worldHeightsColliders = new float[width * height];

            JobHandle jobHandle = default;
            NativeArray<float3>[] meshWorldVertices = null;
            NativeArray<int>[] meshTriangles = null;
            NativeArray<float>[] meshOutputs = null;

            var minQueue = new NativeQueue<float>(Allocator.Persistent);
            
            if (castAgainstMeshes)
            {
                var meshFilterComponents = new List<MeshFilter>(GameObject.FindObjectsOfType<MeshFilter>(true)); 
                var lodControllers = GameObject.FindObjectsOfType<LODGroup>(true);
                foreach (LODGroup lod in lodControllers)
                {
                    if (lod.lodCount > 1) // only cast against the highest level of detail
                    {
                        var lods = lod.GetLODs();
                        if (lods != null)
                        {
                            for (int a = 1; a < lods.Length; a++)
                            {
                                var lod_ = lods[a];
                                if (lod_.renderers != null)
                                {
                                    foreach (var renderer in lod_.renderers) if (renderer != null) meshFilterComponents.RemoveAll(i => i == null || ReferenceEquals(i.gameObject, renderer.gameObject)); 
                                }
                            }
                        }
                    }
                }

                for(int a = meshFilterComponents.Count - 1; a >= 0; a--)
                {
                    var meshFilter = meshFilterComponents[a];
                    if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                    if (((1 << meshFilter.gameObject.layer) & meshMask.value) == 0) // check if the mesh is in the correct layer
                    {
                        meshFilterComponents.RemoveAt(a);
                        continue;
                    }

                    var meshRenderer = meshFilter.GetComponent<MeshRenderer>(); 
                    if (meshRenderer == null)
                    {
                        meshFilterComponents.RemoveAt(a);
                        continue;
                    }

                    bool valid = false;
                    var mats = meshRenderer.sharedMaterials;
                    if (mats != null && mats.Length > 0)
                    {
                        foreach(var mat in mats)
                        {
                            if (mat == null) continue;

                            foreach(var matGroup in collidableMeshMaterials)
                            {
                                if (ReferenceEquals(mat, matGroup.material))
                                {
                                    valid = true;
                                    break;
                                }
                            }

                            if (valid) break;
                        }
                    }

                    if (!valid)
                    {
                        meshFilterComponents.RemoveAt(a);
                    }
                }

                meshWorldVertices = new NativeArray<float3>[meshFilterComponents.Count];
                meshTriangles = new NativeArray<int>[meshFilterComponents.Count];
                meshOutputs = new NativeArray<float>[meshFilterComponents.Count];
                List<int> tempTris = new List<int>();
                for (int a = 0; a < meshFilterComponents.Count; a++)
                {
                    var meshFilter = meshFilterComponents[a];
                    if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                    var mesh = meshFilter.sharedMesh;
                    var meshTransform = meshFilter.transform;

                    var localVertices = mesh.vertices;
                    NativeArray<float3> worldVertices = new NativeArray<float3>(localVertices.Length, Allocator.Persistent);

                    for (int b = 0; b < localVertices.Length; b++)
                    {
                        worldVertices[b] = meshTransform.TransformPoint(localVertices[b]); // convert local mesh vertices to world space
                    }

                    meshWorldVertices[a] = worldVertices;

                    var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                    var mats = meshRenderer.sharedMaterials;

                    tempTris.Clear();
                    for (int b = 0; b < mats.Length; b++)
                    {
                        var mat = mats[b];
                        if (mat == null) continue;

                        foreach (var matGroup in collidableMeshMaterials)
                        {
                            if (ReferenceEquals(mat, matGroup.material))
                            {
                                var tris = mesh.GetTriangles(b);
                                if (tris != null && tris.Length > 0)
                                {
                                    tempTris.AddRange(tris);
                                    //Debug.Log($"Adding {tris.Length} tris for material {matGroup.material.name} ... new count: {tempTris.Count}");
                                }
                                break;
                            }
                        }
                    }

                    NativeArray<int> triangles = new NativeArray<int>(tempTris.ToArray(), Allocator.Persistent);
                    meshTriangles[a] = triangles;
                    NativeArray<float> outputs = new NativeArray<float>(triangles.Length / 3, Allocator.Persistent);
                    meshOutputs[a] = outputs;

                    for (int t = 0; t < outputs.Length; t++)
                    {
                        int ind = t * 3;

                        var i0 = triangles[ind];
                        var i1 = triangles[ind + 1];
                        var i2 = triangles[ind + 2];

                        float3 v0 = worldVertices[i0];
                        float3 v1 = worldVertices[i1];
                        float3 v2 = worldVertices[i2];
                    }

                    float widthF = width;
                    float heightF = height;
                    for (int x = 0; x < width; x++)
                    {
                        float u = ((x + 0.5f) / widthF);

                        for (int y = 0; y < height; y++)
                        {
                            float v = ((y + 0.5f) / heightF);

                            int writeIndex = x + (y * width);

                            float3 castPoint = math.lerp(math.lerp(p0f, p1f, u), math.lerp(p3f, p2f, u), v);
                            var job = new CastAgainstMeshJob()
                            {
                                castOrigin = castPoint,
                                castOffset = castOffset,
                                castDistance = castDistance,

                                worldVertices = worldVertices,
                                triangles = triangles,

                                worldHeights = outputs
                            };

                            jobHandle = job.Schedule(outputs.Length, 128, jobHandle); 

                            jobHandle = new SortMinJob
                            {
                                ToSort = outputs,
                                Min = minQueue.AsParallelWriter()
                            }.ScheduleBatch(outputs.Length, 1024, jobHandle);

                            jobHandle = new CombineJob
                            {
                                Mins = minQueue,
                                writeBuffer = worldHeightsMeshes,
                                writeIndex = writeIndex
                            }.Schedule(jobHandle);
                        }
                    }

                }
            }

            if (castAgainstColliders)
            {
                float widthF = width;
                float heightF = height;
                for(int x = 0; x < width; x++)
                {
                    float u = ((x + 0.5f) / widthF);

                    for(int y = 0; y < height; y++)
                    {
                        float v = ((y + 0.5f) / heightF);

                        Vector3 castPoint = Vector3.LerpUnclamped(Vector3.LerpUnclamped(p0, p1, u), Vector3.LerpUnclamped(p3, p2, u), v);
                        if (Physics.Raycast(castPoint, castDir, out RaycastHit hit, castDistance, colliderMask, QueryTriggerInteraction.Ignore))
                        {
                            //Debug.DrawRay(castPoint, castDir * hit.distance, Color.Lerp(Color.red, Color.yellow, (hit.distance / castDistance)), 50f);   
                            int index = x + (y * width);

                            var currentHeight = worldHeightsColliders[index];
                            float hitHeight = hit.distance;
                            if (hit.collider is TerrainCollider tc)
                            {
                                var terrain = tc.GetComponent<Terrain>();
                                if (terrain != null) 
                                {
                                    var terrainTransform = terrain.transform;

                                    Vector3 terrainSpaceHit = terrainTransform.InverseTransformPoint(hit.point);
                                    terrainSpaceHit.y = terrain.SampleHeight(hit.point);

                                    hitHeight = Vector3.Dot(terrain.transform.TransformPoint(terrainSpaceHit), castDir) - Vector3.Dot(castPoint, castDir);  
                                }
                            }

                            if (currentHeight <= 0 || hitHeight < currentHeight) worldHeightsColliders[index] = hitHeight; // if casting "down", a closer hit means a larger height
                        } 
                        else
                        {
                            //Debug.DrawRay(castPoint, castDir * castDistance, Color.green, 100f);
                        }
                    }
                }
            }

            jobHandle.Complete();

            Color[] finalPixels = new Color[width * height];
            if (castAgainstColliders && castAgainstMeshes)
            {
                for (int a = 0; a < finalPixels.Length; a++)
                {
                    float pixHeightA = math.saturate(worldHeightsColliders[a] / castDistance);
                    float pixHeightB = math.saturate(worldHeightsMeshes[a] / castDistance);
                    float pixHeight = (pixHeightA <= 0f ? pixHeightB : (pixHeightB <= 0f ? pixHeightA : math.min(pixHeightA, pixHeightB)));

                    finalPixels[a] = new Color(pixHeight, pixHeight, pixHeight, pixHeight);
                }
            } 
            else if (castAgainstColliders)
            {
                for (int a = 0; a < finalPixels.Length; a++)
                {
                    float pixHeight = math.saturate(worldHeightsColliders[a] / castDistance);
                    finalPixels[a] = new Color(pixHeight, pixHeight, pixHeight, pixHeight);
                }
            } 
            else if (castAgainstMeshes)
            {
                for (int a = 0; a < finalPixels.Length; a++)
                {
                    float pixHeight = math.saturate(worldHeightsMeshes[a] / castDistance);
                    finalPixels[a] = new Color(pixHeight, pixHeight, pixHeight, pixHeight);
                }
            }

            if (worldHeightsMeshes.IsCreated) worldHeightsMeshes.Dispose();
            if (meshWorldVertices != null)
            {
                foreach (var array in meshWorldVertices) if (array.IsCreated) array.Dispose();
                meshWorldVertices = null;
            }
            if (meshTriangles != null)
            {
                foreach(var array in meshTriangles) if (array.IsCreated) array.Dispose();
                meshTriangles = null;
            }
            if (meshOutputs != null)
            {
                foreach (var array in meshOutputs) if (array.IsCreated) array.Dispose();
                meshOutputs = null;
            }

            minQueue.Dispose();

            heightMap.SetPixels(finalPixels);
            heightMap.Apply(); 

            return heightMap;
        }


        public void PushMaterialProperties()
        {
            if (materialGroups != null)
            {
                foreach (var matGroup in materialGroups)
                {
                    if (matGroup != null)
                    {
                        matGroup.PushProperties(this);
                    }
                }
            }
        }
        public void PullMaterialProperties()
        {
            if (materialGroups != null)
            {
                foreach (var matGroup in materialGroups)
                {
                    if (matGroup != null)
                    {
                        matGroup.PullProperties(this);
                    }
                }
            }
        }

        public void UpdateRendererBounds() // TODO: Add functionality to update renderer bounds based on possible displacement
        {
            var meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>(true);
        }

        [SerializeField, HideInInspector]
        protected List<CollisionMeshBinding> instantiatedCollisionMeshes;
        [Serializable]
        public struct CollisionMeshBinding
        {
            public Mesh instantiatedMesh;
            public Mesh baseMesh;
            public MeshCollider collider;
        }
        public void ResetNullCollisionMeshes()
        {
            if (instantiatedCollisionMeshes != null)
            {
                for(int a = instantiatedCollisionMeshes.Count - 1; a >= 0; a--)
                {
                    var binding = instantiatedCollisionMeshes[a];

                    if (binding.instantiatedMesh == null)
                    {
                        if (binding.collider != null && (binding.collider.sharedMesh == null || ReferenceEquals(binding.collider.sharedMesh, binding.instantiatedMesh))) binding.collider.sharedMesh = binding.baseMesh;

                        instantiatedCollisionMeshes.RemoveAt(a);
                    }
                }
            }
        }
        public bool IsInstantiatedCollisionMesh(Mesh mesh) => IsInstantiatedCollisionMesh(mesh, out _);
        public bool IsInstantiatedCollisionMesh(Mesh mesh, out CollisionMeshBinding binding)
        {
            binding = default;
            if (instantiatedCollisionMeshes == null) return false;

            foreach (var binding_ in instantiatedCollisionMeshes)
            {
                if (ReferenceEquals(binding_.instantiatedMesh, mesh))
                {
                    binding = binding_;
                    return true;
                }
            }

            return false;
        }
        protected virtual void BakeCollisionMesh(string saveDir, MeshCollider collider, Mesh baseMesh, MaterialGroup[] materialGroups, DeformableSurface deformableSurface)
        {
            if (materialGroups == null) return;

            bool isDeformable = false;
            foreach(var mg in materialGroups)
            {
                if (mg == null || !mg.isDeformable) continue; 

                isDeformable = true;
                break;
            }
            if (!isDeformable) return;

#if UNITY_EDITOR
            Debug.Log("Baking collision mesh for " + collider.transform.GetPathString());
#endif

            Mesh newMesh = MeshUtils.DuplicateMesh(baseMesh);
            newMesh.name = $"collision_{baseMesh.name}_{(collider.GetInstanceID())}{Time.frameCount}";

            while (IsInstantiatedCollisionMesh(collider.sharedMesh, out var binding))
            {
                newMesh.name = collider.sharedMesh.name;

                instantiatedCollisionMeshes.RemoveAll(i => ReferenceEquals(i.instantiatedMesh, collider.sharedMesh) || ReferenceEquals(i.collider, collider));

#if UNITY_EDITOR
                if (binding.instantiatedMesh != null && AssetDatabase.Contains(binding.instantiatedMesh)) 
                { 
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(binding.instantiatedMesh));
                    AssetDatabase.Refresh();
                }
#endif
            }
            instantiatedCollisionMeshes.RemoveAll(i => ReferenceEquals(i.instantiatedMesh, collider.sharedMesh) || ReferenceEquals(i.collider, collider));  

            Vector3 rootPositionWS = collider.transform.position;

            Vector3 proxyCenterWS = transform.position;
            Vector3 proxyNormalWS = SurfaceNormalWorld;
            Vector3 proxyTangentWS = CastTangentWorld;
            Vector3 proxyBitangentWS = CastBitangentWorld;

            float proxySurfaceSizeX = tileSizeX;
            float proxySurfaceSizeY = tileSizeZ;

            float proxyHeightMax = castDistance;


            var localToWorld = collider.transform.localToWorldMatrix;
            var worldToLocal = collider.transform.worldToLocalMatrix; 
            var vertices = newMesh.vertices;
            var normals = newMesh.normals;
            var colors = newMesh.colors;

            List<Vector4> localPositionOffsets = deformableSurface == null ? new List<Vector4>(new Vector4[vertices.Length]) : newMesh.GetUVsByChannelAsListV4(deformableSurface.surfaceOffsetsUVChannel);
            if (localPositionOffsets == null || localPositionOffsets.Count < vertices.Length) new List<Vector4>(new Vector4[vertices.Length]);

            bool[] modificationFlags = new bool[vertices.Length];

            int[][] triangles = new int[newMesh.subMeshCount][];
            for (int a = 0; a < newMesh.subMeshCount; a++) triangles[a] = newMesh.GetTriangles(a);

            var count = Mathf.Min(newMesh.subMeshCount, materialGroups.Length);
            for(int a = 0; a < count; a++)
            {
                var materialGroup = materialGroups[a];
                if (materialGroup == null || !materialGroup.isDeformable) continue;

                var tris = triangles[a];
                if (tris == null) continue;


                Vector3 surfaceCenterWS = materialGroup.GetSurfaceCenterWS(materialProperties, proxyCenterWS);

                Vector3 surfaceNormalWS = materialGroup.GetSurfaceNormalWS(materialProperties, proxyNormalWS);
                Vector3 surfaceTangentWS = materialGroup.GetSurfaceTangentWS(materialProperties, proxyTangentWS);
                Vector3 surfaceBitangentWS = materialGroup.GetSurfaceBitangentWS(materialProperties, proxyBitangentWS);

                float surfaceSizeX = materialGroup.GetSurfaceSizeX(materialProperties, proxySurfaceSizeX);
                float surfaceSizeY = materialGroup.GetSurfaceSizeY(materialProperties, proxySurfaceSizeY);

                float surfaceHeightMax = materialGroup.GetSurfaceHeightMax(materialProperties, proxyHeightMax);

                float deformationMix = materialGroup.GetDeformationMix(materialProperties);
                float defaultHeightOffset = materialGroup.GetDefaultHeightOffset(materialProperties);
                float defaultLocalHeightOffset = materialGroup.GetLocalHeightOffset(materialProperties);
                float localHeightMix = materialGroup.GetLocalHeightMix(materialProperties);

                Vector4 deformationBlendVertexColorMask = materialGroup.GetDeformationBlendVertexColorMask(materialProperties);

                float slopeStep = materialGroup.GetSlopeStep(materialProperties); 

                void ModifyVertex(int index)
                {
                    if (!modificationFlags[index])
                    {
                        var v = vertices[index];
                        var n = normals[index];
                        var localPositionOffset = localPositionOffsets[index];

                        AdjustSurfaceHeight(
                            deformationMix, rootPositionWS, heightMap, v, n,
                            surfaceCenterWS, surfaceNormalWS, surfaceTangentWS, surfaceBitangentWS, surfaceSizeX, surfaceSizeY, surfaceHeightMax,

                            defaultHeightOffset, defaultLocalHeightOffset, localHeightMix,

                            deformationBlendVertexColorMask, colors, 

                            localPositionOffset, slopeStep, localToWorld, worldToLocal, 
                            
                            out v, out n);

                        vertices[index] = v;
                        normals[index] = n;
                        modificationFlags[index] = true;
                    }
                }
                for (int b = 0; b < tris.Length; b += 3)
                {
                    int i0 = tris[b];
                    int i1 = tris[b + 1];
                    int i2 = tris[b + 2];

                    ModifyVertex(i0);
                    ModifyVertex(i1);
                    ModifyVertex(i2);
                }
            }

            newMesh.vertices = vertices;
            newMesh.normals = normals; 

#if UNITY_EDITOR
            newMesh = newMesh.CreateOrReplaceAsset(newMesh.CreateUnityAssetPathString(saveDir, "asset"));
#endif

            if (instantiatedCollisionMeshes == null) instantiatedCollisionMeshes = new List<CollisionMeshBinding>();
            instantiatedCollisionMeshes.Add(new CollisionMeshBinding()
            {
                instantiatedMesh = newMesh,
                baseMesh = baseMesh,
                collider = collider
            });

            collider.sharedMesh = newMesh;
        }
        public void RebakeTargetCollisionMeshes()
        {
            if (materialGroups == null) return;

            ResetNullCollisionMeshes();

            var activeScene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            var saveDir = Path.Combine(Path.GetDirectoryName(activeScene.path), activeScene.name, "Surfaces", "Collision");

#else
            var saveDir = string.Empty;
#endif

            foreach(var colliderParams in targetMeshColliders)
            {
                if (colliderParams.materialGroupIndicesPerSubmesh == null || colliderParams.meshCollider == null || colliderParams.baseMesh == null || colliderParams.baseMesh.subMeshCount <= 0) continue;

                MaterialGroup[] matGroups = new MaterialGroup[colliderParams.baseMesh.subMeshCount];
                for(int a = 0; a < colliderParams.materialGroupIndicesPerSubmesh.Length; a++)
                {
                    var mg = materialGroups[colliderParams.materialGroupIndicesPerSubmesh[a]];
                    if (mg == null) continue;

                    matGroups[a] = mg;
                }

                BakeCollisionMesh(saveDir, colliderParams.meshCollider, colliderParams.baseMesh, matGroups, colliderParams.surface);
            }
        }
        public void RebakeCollisionMeshes()
        {
            if (materialGroups == null) return;

            ResetNullCollisionMeshes();

            RebakeTargetCollisionMeshes();

            var activeScene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            var saveDir = Path.Combine(Path.GetDirectoryName(activeScene.path), activeScene.name, "Surfaces", "Collision");

#else
            var saveDir = string.Empty;
#endif

            var meshFilterComponents = GameObject.FindObjectsOfType<MeshFilter>(true); 
            foreach(var filter in meshFilterComponents)
            {
                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    var parent = filter.transform.parent;
                    if (parent != null && parent.GetComponent<MeshFilter>() == null)
                    {
                        collider = parent.GetComponent<MeshCollider>();
                    }
                }
                if (collider == null || IsTargetMeshCollider(collider)) continue;

                var visualMesh = filter.sharedMesh;
                var collisionMesh = collider.sharedMesh;
                if (visualMesh == null) continue;

                bool isCompatibleMesh = collisionMesh == null || ReferenceEquals(visualMesh, collisionMesh);  
                if (!isCompatibleMesh)
                {
                    isCompatibleMesh = visualMesh.vertexCount == collisionMesh.vertexCount && visualMesh.subMeshCount == collisionMesh.subMeshCount;
                    if (!isCompatibleMesh) continue;
                }

                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;

                var mats = renderer.sharedMaterials;
                if (mats == null || mats.Length <= 0) continue;

                var surface = filter.gameObject.GetComponentInParent<DeformableSurface>(true);
                if (surface != null && surface.filters == null)
                {
                    surface = null;

                    bool flag = true;
                    foreach(var filter_ in surface.filters)
                    {
                        if (ReferenceEquals(filter, filter_))
                        {
                            flag = false;
                            break;
                        }
                    }

                    if (flag) surface = null;
                }

                bool hasValidMaterial = false;
                MaterialGroup[] matGroups = new MaterialGroup[mats.Length];
                for(int a = 0; a < mats.Length; a++)
                {
                    var mat = mats[a];

                    foreach(var mg in materialGroups)
                    {
                        if (mg == null || !ReferenceEquals(mat, mg.material)) continue;

                        matGroups[a] = mg;

                        hasValidMaterial = true;
                        break;
                    }
                }

                if (!hasValidMaterial) continue;

                BakeCollisionMesh(saveDir, collider, visualMesh, matGroups, surface);
            }
        }

        [Header("Collision Meshes")]
        public bool rebakeTargetCollisionMeshes;
        public bool rebakeAllCollisionMeshes;

        [Serializable]
        public struct MeshColliderBakeParams
        {
            public MeshCollider meshCollider;
            public DeformableSurface surface;
            public Mesh baseMesh;
            public int[] materialGroupIndicesPerSubmesh;
        }
        public List<MeshColliderBakeParams> targetMeshColliders;
        public bool IsTargetMeshCollider(MeshCollider meshCollider)
        {
            if (targetMeshColliders == null) return false;

            foreach (var mc in targetMeshColliders) if (ReferenceEquals(mc.meshCollider, meshCollider)) return true;
            return false;
        }


        [Header("Actions")]
        public bool generateHeightMap;
        public bool pushMaterialProperties;
        public bool pullMaterialProperties;
        public bool updateRendererBounds;
        public bool reset;

        public void ResetSceneModifications()
        {
            if (instantiatedCollisionMeshes != null)
            {
                foreach(var mod in instantiatedCollisionMeshes)
                {
                    if (mod.collider == null) return;

                    if (mod.collider.sharedMesh == null || ReferenceEquals(mod.collider.sharedMesh, mod.instantiatedMesh)) mod.collider.sharedMesh = mod.baseMesh; 
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (materialGroups != null)
            {
                foreach(var mg in materialGroups)
                {
                    if (mg != null) mg.EditorCheck(this);
                }
            }

            if (reset)
            {
                reset = false;

                ResetSceneModifications();
            }

            if (generateHeightMap)
            {
                generateHeightMap = false;

                heightMap = GenerateHeightMap(heightMapSize, heightMapFormat);
                if (heightMap != null)
                {
                    if (string.IsNullOrWhiteSpace(heightMapName))
                    {
                        heightMapName = $"{name}_{GetInstanceID()}{Time.frameCount}"; 
                    }

                    heightMap.name = heightMapName;

                    string saveDir = assetSaveDir;
                    if (string.IsNullOrWhiteSpace(saveDir))
                    {
                        var activeScene = SceneManager.GetActiveScene();
                        saveDir = Path.Combine(Path.GetDirectoryName(activeScene.path), activeScene.name, "Surfaces", "Maps");
                    }

                    if (!string.IsNullOrWhiteSpace(saveDir))
                    {
                        heightMap = heightMap.CreateOrReplaceAsset(heightMap.CreateUnityAssetPathString(saveDir, "asset"));
                    }

                    if (materialGroups != null)
                    {
                        foreach (var mg in materialGroups)
                        {
                            if (mg == null) continue;

                            mg.PushHeightMap(this);
                        }
                    }
                }
            }

            if (updateRendererBounds)
            {
                updateRendererBounds = false;
                UpdateRendererBounds();
            }

            if (pushMaterialProperties)
            {
                pushMaterialProperties = false;
                PushMaterialProperties();
            }
            if (pullMaterialProperties)
            {
                pullMaterialProperties = false;
                PullMaterialProperties();
            }

            if (rebakeTargetCollisionMeshes)
            {
                rebakeTargetCollisionMeshes = false;

                RebakeTargetCollisionMeshes();
            }

            if (rebakeAllCollisionMeshes)
            {
                rebakeAllCollisionMeshes = false;

                RebakeCollisionMeshes();
            }
        }
#endif

        [BurstCompile]
        private struct CastAgainstMeshJob : IJobParallelFor
        {
            public float castDistance;

            public float3 castOrigin;
            public float3 castOffset; 

            [ReadOnly]
            public NativeArray<float3> worldVertices; 
            [ReadOnly]
            public NativeArray<int> triangles;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> worldHeights;

            public void Execute(int index)
            {
                int ind = index * 3;

                var i0 = triangles[ind];
                var i1 = triangles[ind + 1];
                var i2 = triangles[ind + 2];

                float3 v0 = worldVertices[i0];
                float3 v1 = worldVertices[i1];
                float3 v2 = worldVertices[i2];

                bool hit = Maths.seg_intersect_triangle(castOrigin, castOffset, v0, v1, v2, out float t, out _, out _, out _);
                worldHeights[index] = math.select(0f, t * castDistance, hit); 
            }
        }

        [BurstCompile]
        private struct SortMinJob : IJobParallelForBatch
        {
            [ReadOnly]
            public NativeArray<float> ToSort;

            // Output
            public NativeQueue<float>.ParallelWriter Min;

            public void Execute(int startIndex, int count)
            {
                float min = 0f;

                for (var i = 0; i < count; i++)
                {
                    var f = this.ToSort[i + startIndex];
                    min = math.select(min, f, f > 0f && (f < min || min <= 0));
                }

                this.Min.Enqueue(min);
            }
        }

        [BurstCompile]
        private struct CombineJob : IJob
        {
            public float heightOffset;

            public NativeQueue<float> Mins;

            public int writeIndex;
            public NativeArray<float> writeBuffer;

            public void Execute()
            {
                float min = 0f;

                while (this.Mins.TryDequeue(out var f))
                {
                    min = math.select(min, f, f > 0f && (f < min || min <= 0));  
                }

                float currentHeight = this.writeBuffer[writeIndex];
                this.writeBuffer[writeIndex] = math.select(currentHeight, min + heightOffset, currentHeight <= 0f || min < currentHeight); 
            }
        }

    }
}

#endif