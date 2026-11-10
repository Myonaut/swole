using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    [CreateAssetMenu(fileName = "RaycastCheckSet", menuName = "Swole/Physics/Raycast Check Set")]
    public class RaycastCheckSet : ScriptableObject 
    {

        public const string suffix_rayOrigin = "_rayOrigin";
        public const string suffix_rayDir = "_rayDir";
        public const string suffix_rayOffset = "_rayOffset";
        public const string suffix_rayMaxDistance = "_rayMaxDistance";

        public const string suffix_hitPoint = "_hitPoint";
        public const string suffix_hitNormal = "_hitNormal";

        public const string suffix_worldSpace = "@world";

        [Serializable]
        public enum CastType
        {
            Ray, Sphere, Box
        } 

        [Serializable]
        public enum ExternalParameterSourceType
        {
            DynamicAnimationProperty
        }

        [Serializable]
        public class ExternalFloatParameterSource
        {
            [Tooltip("The id used to reference this parameter source from a check.")]
            public string id;

            [Tooltip("The kind of runtime object this source resolves values from.")]
            public ExternalParameterSourceType sourceType;
        }

        [Header("External Parameters")]
        public ExternalFloatParameterSource[] externalParameters;

        public bool TryFindExternalParameterSource(string id, out ExternalFloatParameterSource source)
        {
            source = null;
            if (externalParameters == null) return false;

            foreach (var src in externalParameters)
            {
                if (src != null && src.id == id)
                {
                    source = src;
                    return true;
                }
            }
            return false;
        }

        public bool TryFindCheckById(string id, out Check check)
        {
            check = default;
            if (iterations == null) return false;

            foreach (var it in iterations)
            {
                if (it.TryGetCheck(id, out check)) return true;
            }
            return false;
        }

        public static void DrawCheckShapeGizmo(Matrix4x4 rootToWorld, Check check, Vector3 originInRoot, Vector3 offsetInRoot)
        {
            Vector3 worldOrigin = rootToWorld.MultiplyPoint(originInRoot);
            Vector3 worldOffset = rootToWorld.MultiplyVector(offsetInRoot);
            Vector3 worldEnd = worldOrigin + worldOffset;

            switch (check.castType)
            {
                case CastType.Sphere:
                    Gizmos.DrawWireSphere(worldOrigin, check.radius);
                    Gizmos.DrawWireSphere(worldEnd, check.radius);
                    Gizmos.DrawLine(worldOrigin, worldEnd);
                    break;

                case CastType.Box:
                    var orientation = rootToWorld.rotation * Quaternion.Euler(check.boxOrientationEuler);
                    var prevMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(worldOrigin, orientation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, check.halfExtents * 2f);
                    Gizmos.matrix = Matrix4x4.TRS(worldEnd, orientation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, check.halfExtents * 2f);
                    Gizmos.matrix = prevMatrix;
                    Gizmos.DrawLine(worldOrigin, worldEnd);
                    break;

                default:
                    Gizmos.DrawSphere(worldOrigin, 0.005f);
                    Gizmos.DrawRay(worldOrigin, worldOffset);
                    break;
            }
        }

        public void DrawGizmos(Matrix4x4 rootToWorld)
        {          
            if (iterations != null && iterations.Length > 0)
            {
                void BuildStartPoint(bool addOffsetToOrigin, ref Vector3 originInRoot, ref Vector3 offsetInRoot, int itIndex, RaycastCheckSet.Check check, out bool foundParent)
                {
                    foundParent = false;
                    bool addOrigin = true;
                    bool addOffset = true;
                    if (check.HasParent)
                    {
                        var parentCheck = new Check();
                        int j = 1;
                        while (!foundParent && j <= itIndex)
                        {
                            var prevIteration = iterations[itIndex - j];
                            if (prevIteration.checks != null && prevIteration.checks.Length > 0)
                            {
                                if (prevIteration.TryGetCheck(check.parentId, out parentCheck))
                                {
                                    foundParent = true;
                                    break;
                                }
                            }

                            j++;
                        }

                        if (foundParent)
                        {
                            Vector3 temp = Vector3.zero;
                            BuildStartPoint(true, ref originInRoot, ref temp, itIndex - j, parentCheck, out _);

                            if (check.useDifferentOriginForParent) 
                            { 
                                originInRoot += check.originForParent;
                                addOrigin = false;
                            }
                            if (check.useDifferentOffsetForParent) 
                            {
                                offsetInRoot += check.offsetForParent;
                                if (addOffsetToOrigin) originInRoot += check.offsetForParent;
                                addOffset = false;
                            }
                        }
                    }

                    if (addOrigin) originInRoot += check.origin;
                    if (addOffset) 
                    { 
                        offsetInRoot += check.offset;
                        if (addOffsetToOrigin) originInRoot += check.offset;
                    }
                }

                UnityEngine.Random.InitState(GetInstanceID());
                for(int i = 0; i < iterations.Length; i++)
                {
                    var iteration = iterations[i];
                    if (iteration.checks == null || iteration.checks.Length <= 0) continue;

                    Gizmos.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f);
                    foreach (var check in iteration.checks)
                    {
                        Vector3 originInRoot = Vector3.zero;
                        Vector3 offsetInRoot = Vector3.zero; 
                        BuildStartPoint(false, ref originInRoot, ref offsetInRoot, i, check, out bool foundParent);
                        if (check.HasParent)
                        {                  
                            if (!foundParent)
                            {
                                if (!check.executeIfParentNotPresent) continue;
                            }
                        }

                        DrawCheckShapeGizmo(rootToWorld, check, originInRoot, offsetInRoot);
                    }
                }
            }
        }

        public LayerMask layerMask;

        public CheckIteration[] iterations;
        public int IterationCount => iterations == null ? 0 : iterations.Length;

        [Serializable]
        public struct CheckIteration
        {
            [Header("Iteration Main")]
            public string id;

            public bool useLocalLayerMask;
            public LayerMask layerMask;

            [Header("Checks")]
            public Check[] checks;

            public bool TryGetCheck(string id, out Check check)
            {
                check = default;
                if (checks == null) return false;

                foreach(var check_ in checks)
                {
                    if (check_.id == id)
                    {
                        check = check_;
                        return true;
                    }
                }
                return false;
            }

            [Header("Post Checks")]
            public OperationSet[] postCheckOperations;
            public IterationResultRequirement[] validProceedRequirements;

            public bool CanProceed(RaycastCheckSetHandler handler)
            {
                if (validProceedRequirements == null || validProceedRequirements.Length <= 0) return true;

                foreach(var requirement in validProceedRequirements)
                {
                    if (requirement.IsSatisfied(handler)) return true;
                }

                return false;
            }

            public bool TryToComplete(Matrix4x4 rootToWorld, LayerMask defaultLayerMask, RaycastCheckSetHandler handler, RaycastCheckSet checkSet)
            {
                if (checks == null || checks.Length <= 0) return true;

                var worldToRoot = rootToWorld.inverse;
                foreach (var check in checks)
                {
                    if (!check.CanCheck(handler))
                    {
                        if (check.flags.HasFlag(CheckFlags.MustBePerformed)) return false;
                        continue;
                    }

                    Vector3 originInRoot = check.origin;
                    Vector3 offsetInRoot = check.offset;
                    if (check.HasParent)
                    {
                        if (handler.TryGetVector(check.parentId, out var parentPos))
                        {
                            if (check.useDifferentOriginForParent) originInRoot = check.originForParent;
                            if (check.useDifferentOffsetForParent) offsetInRoot = check.offsetForParent;
                            originInRoot += parentPos;
                        } 
                        else if (handler.TryGetResult(check.parentId, out var parentResult))
                        {
                            if (check.useDifferentOriginForParent) originInRoot = check.originForParent;
                            if (check.useDifferentOffsetForParent) offsetInRoot = check.offsetForParent;
                            originInRoot += parentResult.point;
                        } 
                        else if (!check.executeIfParentNotPresent)
                        {
                            if (check.flags.HasFlag(CheckFlags.MustBePerformed)) return false;
                            continue;
                        }
                    }

                    var mask = useLocalLayerMask ? layerMask : defaultLayerMask;
                    if (check.useLocalLayerMask) mask = check.layerMask;

                    originInRoot = check.externalOriginMultiplier.Apply(originInRoot, checkSet, handler);
                    offsetInRoot = check.externalOffsetMultiplier.Apply(offsetInRoot, checkSet, handler);
                    float radius = check.externalRadiusMultiplier.Apply(check.radius, checkSet, handler);
                    Vector3 halfExtents = check.externalHalfExtentsMultiplier.Apply(check.halfExtents, checkSet, handler);
                    Vector3 boxOrientationEuler = check.externalBoxOrientationMultiplier.Apply(check.boxOrientationEuler, checkSet, handler);

                    float maxDistance = offsetInRoot.magnitude;
                    Vector3 dir = maxDistance > 0f ? (offsetInRoot / maxDistance) : offsetInRoot;

                    RaycastHit hit = default;
                    bool isSatisfied = false;

                    Vector3 rayOrigin = originInRoot;
                    Vector3 rayDir = dir;
                    Vector3 rayOffset = rayDir * maxDistance;
                    handler.SetVector($"{check.id}{suffix_rayOrigin}", rayOrigin);
                    handler.SetVector($"{check.id}{suffix_rayDir}", rayDir);
                    handler.MarkAsDirectionVector($"{check.id}{suffix_rayDir}");
                    handler.SetVector($"{check.id}{suffix_rayOffset}", rayOffset);
                    handler.MarkAsDirectionVector($"{check.id}{suffix_rayOffset}");
                    handler.SetFloat($"{check.id}{suffix_rayMaxDistance}", maxDistance);
                    ;
                    rayOrigin = rootToWorld.MultiplyPoint(rayOrigin);
                    rayDir = rootToWorld.MultiplyVector(rayDir).normalized;
                    rayOffset = rayDir * maxDistance;
                    handler.SetVector($"{check.id}{suffix_rayOrigin}{suffix_worldSpace}", rayOrigin);
                    handler.SetVector($"{check.id}{suffix_rayDir}{suffix_worldSpace}", rayDir);
                    handler.MarkAsDirectionVector($"{check.id}{suffix_rayDir}{suffix_worldSpace}");
                    handler.SetVector($"{check.id}{suffix_rayOffset}{suffix_worldSpace}", rayOffset);
                    handler.MarkAsDirectionVector($"{check.id}{suffix_rayOffset}{suffix_worldSpace}");
                    handler.SetFloat($"{check.id}{suffix_rayMaxDistance}{suffix_worldSpace}", maxDistance); 

                    bool didHit;
                    switch (check.castType)
                    {
                        case CastType.Sphere:
                            didHit = Physics.SphereCast(rayOrigin, radius, rayDir, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
                            break;

                        case CastType.Box:
                            var boxOrientation = rootToWorld.rotation * Quaternion.Euler(boxOrientationEuler);
                            didHit = Physics.BoxCast(rayOrigin, halfExtents, rayDir, out hit, boxOrientation, maxDistance, mask, QueryTriggerInteraction.Ignore);
                            break;

                        default:
                            didHit = Physics.Raycast(rayOrigin, rayDir, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
                            break;
                    }

#if UNITY_EDITOR
                    Debug.DrawLine(rayOrigin, rayOrigin + rayDir * (didHit ? hit.distance : maxDistance), didHit ? Color.red : Color.green, 60f);
#endif

                    Vector3 worldHitPoint = Vector3.zero;
                    Vector3 worldHitNormal = Vector3.up;
                    if (didHit)
                    {
                        worldHitPoint = hit.point;
                        worldHitNormal = hit.normal;

                        hit.point = worldToRoot.MultiplyPoint(hit.point);
                        hit.normal = worldToRoot.MultiplyVector(hit.normal).normalized;         

                        if (check.flags.HasFlag(CheckFlags.SatisfiedOnHit)) isSatisfied = true;

                        if (check.floatsOnHit != null)
                        {
                            foreach(var p in check.floatsOnHit) handler.SetFloat(p.name, p.value);
                        }
                        if (check.flagsOnHit != null)
                        {
                            foreach (var p in check.flagsOnHit) handler.SetFlag(p.name, p.value);
                        }
                        if (check.distanceFloatsOnHit != null)
                        {
                            foreach (var p in check.distanceFloatsOnHit) handler.SetFloat(p, hit.distance);
                        }                    
                    } 
                    else
                    {
                        hit.point = originInRoot;
                        if (!check.flags.HasFlag(CheckFlags.SatisfiedOnHit)) isSatisfied = true;
                    }

                    if (isSatisfied)
                    {
                        if (check.flags.HasFlag(CheckFlags.StoreResultWhenSatisfied)) 
                        {
                            string resultId = string.IsNullOrWhiteSpace(check.resultId) ? check.id : check.resultId;
                            handler.SetResult(resultId, hit);

                            string positionResultId = string.IsNullOrWhiteSpace(check.resultPositionId) ? resultId : check.resultPositionId;
                            handler.SetVector(positionResultId + suffix_hitPoint + suffix_worldSpace, worldHitPoint);
                            string normalResultId = positionResultId + suffix_hitNormal + suffix_worldSpace;
                            handler.SetVector(normalResultId, worldHitNormal); 
                            handler.MarkAsDirectionVector(normalResultId);
                        }

                        if (check.floatsOnSatisfy != null)
                        {
                            foreach (var p in check.floatsOnSatisfy) handler.SetFloat(p.name, p.value);
                        }
                        if (check.flagsOnSatisfy != null)
                        {
                            foreach (var p in check.flagsOnSatisfy) handler.SetFlag(p.name, p.value);
                        }
                    } 
                    else
                    {
                        if (check.flags.HasFlag(CheckFlags.MustBeSatisfied)) return false;

                        if (check.flags.HasFlag(CheckFlags.StoreResultWhenNotSatisfied)) 
                        {
                            string resultId = string.IsNullOrWhiteSpace(check.resultId) ? check.id : check.resultId;
                            handler.SetResult(resultId, hit);

                            string positionResultId = string.IsNullOrWhiteSpace(check.resultPositionId) ? resultId : check.resultPositionId;
                            handler.SetVector(positionResultId + suffix_hitPoint + suffix_worldSpace, worldHitPoint);
                            string normalResultId = positionResultId + suffix_hitNormal + suffix_worldSpace;
                            handler.SetVector(normalResultId, worldHitNormal);
                            handler.MarkAsDirectionVector(normalResultId);
                        }

                        if (check.floatsOnFail != null)
                        {
                            foreach (var p in check.floatsOnFail) handler.SetFloat(p.name, p.value);
                        }
                        if (check.flagsOnFail != null)
                        {
                            foreach (var p in check.flagsOnFail) handler.SetFlag(p.name, p.value);
                        }
                    }
                }

                if (postCheckOperations != null)
                {
                    foreach(var set in postCheckOperations)
                    {
                        set.Execute(handler);
                    }
                }
                return true;
            }
        }

        [Serializable, Flags]
        public enum CheckFlags
        {
            None = 0, SatisfiedOnHit = 1, StoreResultWhenSatisfied = 2, StoreResultWhenNotSatisfied = 4, MustBePerformed = 8, MustBeSatisfied = 16
        }

        [Serializable]
        public struct CheckFloatParameter
        {
            public string name;
            public float value;
        }
        [Serializable]
        public struct CheckFlagParameter
        {
            public string name;
            public bool value;
        }

        [Serializable]
        public struct CheckFloatParameterRequirement
        {
            public string name;
            public float minValue;
            public float maxValue;
            public bool optional;
            public bool invert;
        }
        [Serializable]
        public struct CheckFlagParameterRequirement
        {
            public string name;
            public bool value;
            public bool optional;
            public bool invert;
        }

        [Serializable]
        public enum ExternalParameterApplicationMode
        {
            MultiplyTarget, MultiplyValue
        }

        [Serializable]
        public struct ExternalFloatParameterRef
        {
            public string sourceId;
            public string parameterName;

            [Tooltip("MultiplyTarget: target *= fetchedValue. MultiplyValue: target += (value * fetchedValue)")]
            public ExternalParameterApplicationMode mode;
            [Tooltip("Used as the multiplied operand when mode is MultiplyValue")]
            public float value;

            [Tooltip("Added to the final result regardless of mode")]
            public float addValue;

            public bool HasReference => !string.IsNullOrWhiteSpace(sourceId) && !string.IsNullOrWhiteSpace(parameterName);

            public bool TryGetSourceValue(RaycastCheckSet checkSet, RaycastCheckSetHandler handler, out float fetchedValue)
            {
                fetchedValue = 0f;
                if (!HasReference || checkSet == null || handler == null) return false;
                if (!checkSet.TryFindExternalParameterSource(sourceId, out var source)) return false;

                switch (source.sourceType)
                {
                    case ExternalParameterSourceType.DynamicAnimationProperty:
                        var properties = handler.dynamicAnimationProperties;
                        if (properties == null) return false;

                        int index = properties.IndexOf(parameterName);
                        if (index < 0) return false;

                        fetchedValue = properties.GetValue(index);
                        return true;
                }

                return false;
            }

            public float Apply(float target, RaycastCheckSet checkSet, RaycastCheckSetHandler handler)
            {
                float result = target;
                if (TryGetSourceValue(checkSet, handler, out var fetchedValue))
                {
                    switch (mode)
                    {
                        case ExternalParameterApplicationMode.MultiplyValue:
                            result = target + (value * fetchedValue);
                            break;

                        default:
                            result = target * fetchedValue;
                            break;
                    }
                }

                return result + addValue;
            }
        }

        [Serializable]
        public struct ExternalVectorParameterRef
        {
            public ExternalFloatParameterRef x;
            public ExternalFloatParameterRef y;
            public ExternalFloatParameterRef z;

            public Vector3 Apply(Vector3 target, RaycastCheckSet checkSet, RaycastCheckSetHandler handler)
            {
                return new Vector3(x.Apply(target.x, checkSet, handler), y.Apply(target.y, checkSet, handler), z.Apply(target.z, checkSet, handler));
            }
        }

        [Serializable]
        public struct Check
        {
            [Header("Check Main")]
            public string id;
            public CheckFlags flags;

            public string resultId;
            public string resultPositionId;

            [Tooltip("The start point of the raycast")]
            public Vector3 origin;

            [Tooltip("The direction * distance of the raycast")]
            public Vector3 offset;

            [Header("Cast Shape")]
            public CastType castType;
            [Tooltip("Radius used when castType is Sphere")]
            public float radius;
            [Tooltip("Half extents used when castType is Box")]
            public Vector3 halfExtents;
            [Tooltip("Local orientation of the box used when castType is Box")]
            public Vector3 boxOrientationEuler;

            [Header("External Parameter Multipliers")]
            [Tooltip("Multiplies the components of origin using values fetched from external parameter sources")]
            public ExternalVectorParameterRef externalOriginMultiplier;
            [Tooltip("Multiplies the components of offset using values fetched from external parameter sources")]
            public ExternalVectorParameterRef externalOffsetMultiplier;
            [Tooltip("Multiplies radius using a value fetched from an external parameter source")]
            public ExternalFloatParameterRef externalRadiusMultiplier;
            [Tooltip("Multiplies the components of halfExtents using values fetched from external parameter sources")]
            public ExternalVectorParameterRef externalHalfExtentsMultiplier;
            [Tooltip("Multiplies the components of boxOrientationEuler using values fetched from external parameter sources")]
            public ExternalVectorParameterRef externalBoxOrientationMultiplier;

            public bool useLocalLayerMask;
            public LayerMask layerMask;

            public string parentId;
            public bool HasParent => !string.IsNullOrWhiteSpace(parentId);
            public bool executeIfParentNotPresent;

            public bool useDifferentOriginForParent;
            public Vector3 originForParent;

            public bool useDifferentOffsetForParent;
            public Vector3 offsetForParent;

            [Header("Parameters To Set On Hit")]
            public CheckFloatParameter[] floatsOnHit;
            public CheckFlagParameter[] flagsOnHit;
            public string[] distanceFloatsOnHit;

            [Header("Parameters To Set When Satisfied")]
            public CheckFloatParameter[] floatsOnSatisfy;
            public CheckFlagParameter[] flagsOnSatisfy;

            [Header("Parameters To Set On Fail")]
            public CheckFloatParameter[] floatsOnFail;
            public CheckFlagParameter[] flagsOnFail;

            [Header("Parameter Requirements")]
            public int minimumSatisfiedFloats;
            public int maximumSatisfiedFloats;
            public CheckFloatParameterRequirement[] checkFloatParameterRequirements;
            public int minimumSatisfiedFlags;
            public int maximumSatisfiedFlags;
            public CheckFlagParameterRequirement[] checkFlagParameterRequirements;

            public bool CanCheck(RaycastCheckSetHandler handler)
            {
                if (checkFloatParameterRequirements != null && checkFloatParameterRequirements.Length > 0)
                {
                    int satisfied = 0;
                    foreach(var float_ in checkFloatParameterRequirements)
                    {
                        if (!handler.TryGetFloat(float_.name, out var val)) return false;

                        bool inRange = val >= float_.minValue && val <= float_.maxValue;
                        if (float_.invert) inRange = !inRange;

                        if (inRange)
                        {
                            satisfied++;
                        } 
                        else
                        {
                            if (!float_.optional) return false;
                        } 
                    }

                    if (satisfied < minimumSatisfiedFloats || (maximumSatisfiedFloats > 0 && satisfied > maximumSatisfiedFloats)) return false; 
                }

                if (checkFlagParameterRequirements != null && checkFlagParameterRequirements.Length > 0)
                {
                    int satisfied = 0;
                    foreach (var flag_ in checkFlagParameterRequirements)
                    {
                        if (!handler.TryGetFlag(flag_.name, out var val)) return false;

                        bool isSet = val == flag_.value;
                        if (flag_.invert) isSet = !isSet;

                        if (isSet)
                        {
                            satisfied++;
                        }
                        else
                        {
                            if (!flag_.optional) return false;
                        }
                    }

                    if (satisfied < minimumSatisfiedFlags || (maximumSatisfiedFlags > 0 && satisfied > maximumSatisfiedFlags)) return false;
                }

                if (HasParent)
                {
                    if (!executeIfParentNotPresent)
                    {
                        if (!handler.HasVector(parentId) && !handler.HasResult(parentId)) return false;
                    }
                }

                return true;
            }
        }

        [Serializable]
        public struct CheckResultRequirement
        {
            public string checkId;
            public bool didHit;
            public bool optional;

            public bool IsSatisfied(RaycastCheckSetHandler handler)
            {
                return (handler.HasResult(checkId) == didHit);
            }
        }

        [Serializable]
        public struct FlagRequirement
        {
            public string flagId;
            public bool isTrue;
            public bool optional;

            public bool IsSatisfied(RaycastCheckSetHandler handler)
            {
                if (handler.TryGetFlag(flagId, out var val)) return val == isTrue; 
                return false;
            }
        }

        [Serializable]
        public struct IterationResultRequirement
        {
            [Header("Result Requirements")]
            public int minimumSatisfiedRequirements;
            public int maximumSatisfiedRequirements;
            public CheckResultRequirement[] requirements;

            [Header("Flag Requirements")]
            public int minimumSatisfiedFlagRequirements;
            public int maximumSatisfiedFlagRequirements;
            public FlagRequirement[] flagRequirements;

            public bool IsSatisfied(RaycastCheckSetHandler handler)
            {
                bool satisfied = true;
                if (requirements != null && requirements.Length > 0)
                {
                    int count = 0;
                    foreach (var requirement in requirements)
                    {
                        if (requirement.IsSatisfied(handler))
                        {
                            count++;
                        }
                        else if (!requirement.optional) return false;
                    }

                    satisfied = count >= minimumSatisfiedRequirements && (maximumSatisfiedRequirements <= 0 || count <= maximumSatisfiedRequirements); 
                }

                if (flagRequirements != null && flagRequirements.Length > 0)
                {
                    int count = 0;
                    foreach (var requirement in flagRequirements)
                    {
                        if (requirement.IsSatisfied(handler))
                        {
                            count++;
                        }
                        else if (!requirement.optional) return false; 
                    }

                    satisfied = count >= minimumSatisfiedFlagRequirements && (maximumSatisfiedFlagRequirements <= 0 || count <= maximumSatisfiedFlagRequirements); 
                }

                return satisfied;
            }
        }

        [Serializable]
        public enum FloatSource
        {
            Custom, ID, HitDistance, RayMaxDistance
        }
        [Serializable]
        public enum VectorSource
        {
            Custom, ID, HitNormal, HitPoint, RayOrigin, RayDirection, RayOffset, HitNormalWorld, HitPointWorld, RayOriginWorld, RayDirectionWorld, RayOffsetWorld
        }

        [Serializable]
        public enum FloatOperation
        {
            Add, Subtract, Multiply, Divide, AbsoluteValue, Min, Max
        }
        [Serializable]
        public enum VectorToVectorOperation
        {
            Add, Subtract, Multiply, Divide, Cross, Normalize
        }
        [Serializable]
        public enum VectorToFloatOperation
        {
            Magnitude, SqrMagnitude, Dot
        }

        [Serializable]
        public enum FloatComparison
        {
            Equal, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
        }

        [Serializable]
        public struct OpFloat
        {
            public FloatSource source;
            public string id;
            public float customValue;

            public float GetValue(RaycastCheckSetHandler handler)
            {
                switch(source)
                {
                    case FloatSource.Custom:
                        return customValue;

                    case FloatSource.ID:
                        if (handler.TryGetFloat(id, out var val)) return val; 
                        break;

                    case FloatSource.HitDistance:
                        if (handler.TryGetResult(id, out var hit)) return hit.distance;
                        break;

                    case FloatSource.RayMaxDistance:
                        if (handler.TryGetFloat($"{id}{suffix_rayMaxDistance}", out var maxDist)) return maxDist;
                        break;
                }

                return 0f;
            }
        }
        [Serializable]
        public struct FloatOperationAction
        {
            public OpFloat floatA;
            public FloatOperation operation;
            public OpFloat floatB;

            public string resultId;

            public void Execute(RaycastCheckSetHandler handler)
            {
                var valA = floatA.GetValue(handler);
                var valB = floatB.GetValue(handler);

                var result = 0f;
                switch (operation)
                {
                    case FloatOperation.Add:
                        result = valA + valB;
                        break;

                    case FloatOperation.Subtract:
                        result = valA - valB;
                        break;

                    case FloatOperation.Multiply:
                        result = valA * valB;
                        break;

                    case FloatOperation.Divide:
                        result = valA / valB;
                        break;

                    case FloatOperation.AbsoluteValue:
                        result = Mathf.Abs(valA); 
                        break;

                    case FloatOperation.Min:
                        result = Mathf.Min(valA, valB);
                        break;

                    case FloatOperation.Max:
                        result = Mathf.Max(valA, valB);
                        break;
                }

                handler.SetFloat(resultId, result);
            }
        }

        [Serializable]
        public struct FloatComparisonAction
        {
            public OpFloat floatA;
            public FloatComparison comparison;
            public OpFloat floatB;

            public bool invertResult;
            public string resultFlagId;

            public void Execute(RaycastCheckSetHandler handler)
            {
                var valA = floatA.GetValue(handler);
                var valB = floatB.GetValue(handler);

                var result = false;
                switch (comparison)
                {
                    case FloatComparison.Equal:
                        result = valA == valB;
                        break;

                    case FloatComparison.GreaterThan:
                        result = valA > valB;
                        break;

                    case FloatComparison.LessThan:
                        result = valA < valB;
                        break;

                    case FloatComparison.GreaterThanOrEqual:
                        result = valA >= valB;
                        break;

                    case FloatComparison.LessThanOrEqual:
                        result = valA <= valB;
                        break;
                }

                handler.SetFlag(resultFlagId, invertResult ? !result : result);
            }
        }

        [Serializable]
        public struct OpVector
        {
            public VectorSource source;
            public string id;
            public Vector3 customValue;

            public Vector3 GetValue(RaycastCheckSetHandler handler)
            {
                switch (source)
                {
                    case VectorSource.Custom:
                        return customValue;

                    case VectorSource.ID:
                        if (handler.TryGetVector(id, out var val)) 
                        {
                            return val; 
                        } 
                        else if (handler.TryGetFloat(id, out var valF))
                        {
                            return new Vector3(valF, valF, valF);
                        }
                        break;

                    case VectorSource.HitNormal:
                        {
                            if (handler.TryGetResult(id, out var hit1)) return hit1.normal;
                        }
                        break;

                    case VectorSource.HitPoint:
                        {
                            if (handler.TryGetResult(id, out var hit2)) return hit2.point; 
                        }
                        break;

                    case VectorSource.RayDirection:
                        {
                            if (handler.TryGetVector($"{id}{suffix_rayDir}", out var rayDir)) return rayDir;
                        }
                        break;

                    case VectorSource.RayOffset:
                        {
                            if (handler.TryGetVector($"{id}{suffix_rayOffset}", out var rayOffset)) return rayOffset;
                        }
                        break;

                    case VectorSource.HitNormalWorld:
                        {
                            if (handler.TryGetVector($"{id}{suffix_hitNormal}{suffix_worldSpace}", out var hitNormal)) return hitNormal;
                        }
                        break;

                    case VectorSource.HitPointWorld:
                        {
                            if (handler.TryGetVector($"{id}{suffix_hitPoint}{suffix_worldSpace}", out var hitPoint)) return hitPoint;
                        }
                        break;

                    case VectorSource.RayDirectionWorld:
                        {
                            if (handler.TryGetVector($"{id}{suffix_rayDir}{suffix_worldSpace}", out var rayDir)) return rayDir;
                        }
                        break;

                    case VectorSource.RayOffsetWorld:
                        {
                            if (handler.TryGetVector($"{id}{suffix_rayOffset}{suffix_worldSpace}", out var rayOffset)) return rayOffset;
                        }
                        break;
                }

                return Vector3.zero;
            }
        }
        [Serializable]
        public struct VectorToVectorOperationAction
        {
            public OpVector vecA;
            public VectorToVectorOperation operation;
            public OpVector vecB;

            public string resultId;
            public bool resultIsDirection;
            public bool resultIsInWorldSpace;

            public void Execute(RaycastCheckSetHandler handler)
            {
                var valA = vecA.GetValue(handler);
                var valB = vecB.GetValue(handler);

                var result = Vector3.zero;
                switch (operation)
                {
                    case VectorToVectorOperation.Add:
                        result = valA + valB;
                        break;

                    case VectorToVectorOperation.Subtract:
                        result = valA - valB;
                        break;

                    case VectorToVectorOperation.Multiply:
                        result = Vector3.Scale(valA, valB);
                        break;

                    case VectorToVectorOperation.Divide:
                        result = new Vector3(valA.x / valB.x, valA.y / valB.y, valA.z / valB.z);
                        break;

                    case VectorToVectorOperation.Cross:
                        result = Vector3.Cross(valA, valB);
                        break;

                    case VectorToVectorOperation.Normalize:
                        result = Vector3.Normalize(valA);
                        break;
                }

                handler.SetVector(resultId, result); 
#if UNITY_EDITOR
                if (resultIsDirection) handler.MarkAsDirectionVector(resultId); else handler.UnmarkAsDirectionVector(resultId);
                if (resultIsInWorldSpace) handler.MarkAsWorldSpaceVector(resultId); else handler.UnmarkAsWorldSpaceVector(resultId);
#endif
            }
        }
        [Serializable]
        public struct VectorToFloatOperationAction
        {
            public OpVector vecA;
            public VectorToFloatOperation operation; 
            public OpVector vecB;

            public string resultId;

            public void Execute(RaycastCheckSetHandler handler)
            {
                var valA = vecA.GetValue(handler);
                var valB = vecB.GetValue(handler);

                var result = 0f;
                switch (operation)
                {
                    case VectorToFloatOperation.Magnitude:
                        result = valA.magnitude;
                        break;

                    case VectorToFloatOperation.SqrMagnitude:
                        result = valA.sqrMagnitude;
                        break;

                    case VectorToFloatOperation.Dot:
                        result = Vector3.Dot(valA, valB);
                        break;
                }

                handler.SetFloat(resultId, result);
            }
        }

        [Serializable]
        public struct OperationSet
        {

            public FloatOperationAction[] floatOperations;
            public VectorToVectorOperationAction[] vectorToVectorOperations;
            public VectorToFloatOperationAction[] vectorToFloatOperations;
            public FloatOperationAction[] lateFloatOperations;

            public FloatComparisonAction[] floatComparisons;

            public void Execute(RaycastCheckSetHandler handler)
            {
                if (floatOperations != null)
                {
                    foreach (var op in floatOperations) op.Execute(handler);
                }
                if (vectorToVectorOperations != null)
                {
                    foreach (var op in vectorToVectorOperations) op.Execute(handler);
                }
                if (vectorToFloatOperations != null)
                {
                    foreach (var op in vectorToFloatOperations) op.Execute(handler);
                }
                if (lateFloatOperations != null)
                {
                    foreach (var op in lateFloatOperations) op.Execute(handler);  
                }

                if (floatComparisons != null)
                {
                    foreach (var op in floatComparisons) op.Execute(handler);
                }
            }

        }
    }

    public class RaycastCheckSetHandler
    {
        private readonly Dictionary<string, RaycastHit> results = new Dictionary<string, RaycastHit>();

        private readonly Dictionary<string, Vector3> vectors = new Dictionary<string, Vector3>();
        /// <summary>
        /// Stores the ids of vectors that should be interpreted as a direction. Mainly used for debugging and should not be relied upon for gameplay logic.
        /// </summary>
        private List<string> directionVectors;
        /// <summary>
        /// Stores the ids of vectors that should be interpreted in world space. Mainly used for debugging and should not be relied upon for gameplay logic.
        /// </summary>
        private List<string> worldSpaceVectors;

        private readonly Dictionary<string, float> floats = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

        private int iteration;
        public int Iteration => iteration;
        public int IterationCount => checkSet == null ? 0 : checkSet.IterationCount;

        private bool failed;
        public bool Failed => failed;

        public void Reset()
        {
            results.Clear();
            vectors.Clear();
            directionVectors?.Clear();
            worldSpaceVectors?.Clear();

            floats.Clear();
            flags.Clear();

            iteration = 0;
            failed = false;
        }


        public bool HasResult(string id) => results.ContainsKey(id);
        public bool HasVector(string id) => vectors.ContainsKey(id);

        public bool TryGetResult(string id, out RaycastHit result)
        {
            if (results.TryGetValue(id, out result)) return true;
            return false;
        }
        public bool TryGetVector(string id, out Vector3 vector)
        {
            if (vectors.TryGetValue(id, out vector)) return true;
            return false;
        }

        public void SetResult(string id, RaycastHit result) => results[id] = result;
        public void SetVector(string id, Vector3 vector) => vectors[id] = vector;
        public void MarkAsDirectionVector(string id)
        {
            if (directionVectors == null) directionVectors = new List<string>();
            if (!directionVectors.Contains(id)) directionVectors.Add(id);
        }
        public void UnmarkAsDirectionVector(string id)
        {
            if (directionVectors == null) return;
            directionVectors.Remove(id);
        }
        public void MarkAsWorldSpaceVector(string id)
        {
            if (worldSpaceVectors == null) worldSpaceVectors = new List<string>();
            if (!worldSpaceVectors.Contains(id)) worldSpaceVectors.Add(id);
        }
        public void UnmarkAsWorldSpaceVector(string id)
        {
            if (worldSpaceVectors == null) return;
            worldSpaceVectors.Remove(id);
        }

        public bool HasFloat(string id) => floats.ContainsKey(id);
        public bool HasFlag(string id) => flags.ContainsKey(id);

        public bool TryGetFloat(string id, out float value)
        {
            if (floats.TryGetValue(id, out value)) return true;
            return false;
        }
        public bool TryGetFlag(string id, out bool value)
        {
            if (flags.TryGetValue(id, out value)) return true;
            return false;
        }

        public void SetFloat(string id, float val) => floats[id] = val;
        public void SetFlag(string id, bool val) => flags[id] = val;

        private RaycastCheckSet checkSet;
        public RaycastCheckSet CheckSet => checkSet;
        public void Initialize(RaycastCheckSet set)
        {
            checkSet = set;
            Reset();
        }

        public Swole.API.Unity.Animation.DynamicAnimationProperties dynamicAnimationProperties;

        public bool ExecuteNext() => ExecuteNext(Matrix4x4.identity);
        public bool ExecuteNext(Matrix4x4 rootToWorld)
        {
            if (checkSet == null || iteration >= checkSet.IterationCount) return false;

            var iteration_ = checkSet.iterations[iteration];
            if (!iteration_.TryToComplete(rootToWorld, checkSet.layerMask, this, checkSet))
            {
                failed = true;
                return false;
            }
            if (!iteration_.CanProceed(this))
            {
                failed = true;
                return false;
            }
             
            iteration++;
            return true;
        }

        public bool IsComplete => checkSet != null && iteration >= checkSet.IterationCount;

        public bool CompletedSuccessfully => IsComplete && !Failed;

        public void DrawResults(Matrix4x4 rootToWorld, Color color)
        {
            Gizmos.color = color;
            foreach(var entry in results)
            {
                string id = entry.Key;
                var result = entry.Value;
                Vector3 worldPoint = rootToWorld.MultiplyPoint(result.point);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPoint, id);
#endif
                Gizmos.DrawRay(worldPoint, rootToWorld.MultiplyVector(result.normal) * 0.1f);

                if (checkSet != null && checkSet.TryFindCheckById(id, out var check))
                {
                    switch (check.castType)
                    {
                        case RaycastCheckSet.CastType.Sphere:
                            Gizmos.DrawWireSphere(worldPoint, check.radius);
                            break;

                        case RaycastCheckSet.CastType.Box:
                            var orientation = rootToWorld.rotation * Quaternion.Euler(check.boxOrientationEuler);
                            var prevMatrix = Gizmos.matrix;
                            Gizmos.matrix = Matrix4x4.TRS(worldPoint, orientation, Vector3.one);
                            Gizmos.DrawWireCube(Vector3.zero, check.halfExtents * 2f);
                            Gizmos.matrix = prevMatrix;
                            break;
                    }
                }
            }
        }
        public void DrawVectors(Matrix4x4 rootToWorld, Color color)
        {
            int dirIndex = 0;
            Gizmos.color = color;
            foreach (var entry in vectors)
            {
                string id = entry.Key;
                bool isDirection = directionVectors != null && directionVectors.Contains(id);
                bool isWorldSpace = id.EndsWith(RaycastCheckSet.suffix_worldSpace) || (worldSpaceVectors != null && worldSpaceVectors.Contains(id));  
                if (isDirection)
                {
                    dirIndex++;

                    var result = isWorldSpace ? entry.Value : rootToWorld.MultiplyVector(entry.Value);
                    var drawOrigin = new Vector3(entry.Value.z, entry.Value.x, entry.Value.y).normalized * dirIndex * 0.1f;
                    if (!isWorldSpace) drawOrigin = rootToWorld.MultiplyPoint(drawOrigin); else drawOrigin = drawOrigin + rootToWorld.MultiplyPoint(Vector3.zero); 
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(drawOrigin + Vector3.up * 0.01f, id);
#endif
                    Gizmos.DrawRay(drawOrigin, result); 
                } 
                else
                {
                    var result = entry.Value;
                    if (!isWorldSpace) result = isDirection ? rootToWorld.MultiplyVector(result) : rootToWorld.MultiplyPoint(result);  
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(result + Vector3.up * 0.007f, id);
#endif
                    Gizmos.DrawWireSphere(result, 0.01f);
                }
            }
        }
    }

}
