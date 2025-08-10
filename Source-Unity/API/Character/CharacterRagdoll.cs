#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if BULKOUT_ENV
//using RootMotion;
#endif

namespace Swole.API.Unity.Animation
{
    [ExecuteAlways]
    public class CharacterRagdoll : MonoBehaviour
    {
        public const string _transformPathSeparator = "/";

        public bool saveSettingsAsset;
        public string saveSettingsPath;
        public string saveSettingsName;
        public bool copySettingsFromComponents;

        public bool rebuild;
        public bool removeComponents;

        #if UNITY_EDITOR
        protected void Update()
        {
            if (saveSettingsAsset)
            {
                saveSettingsAsset = false;
                SaveSettingsAsset(saveSettingsPath, saveSettingsName); 
            }
            if (copySettingsFromComponents)
            {
                copySettingsFromComponents = false;  
                CopySettingsFromComponents(); 
            }
            if (rebuild)
            {
                rebuild = false;
                Build();
            }
            if (removeComponents)
            {
                removeComponents = false;  
                RemoveComponents(true);   
            }
        }
        public void SaveSettingsAsset(string path, string assetName)
        {
            if (settings == null) return;

            if (settingsAsset == null) settingsAsset = ScriptableObject.CreateInstance<CharacterRagdollSettingsAsset>();
            settingsAsset.name = assetName;
            settingsAsset.settings = settings;

            settingsAsset.CreateOrReplaceAsset((path.StartsWith("Assets") ? "" : "Assets/") + path + (path.EndsWith("/") ? "" : "/") + $"{assetName}.asset");
            SetSettingsAsset(settingsAsset);
        }
        #endif

        [SerializeField]
        protected CharacterRagdollSettingsAsset settingsAsset;
        public void SetSettingsAsset(CharacterRagdollSettingsAsset settingsAsset)
        {
            this.settingsAsset = settingsAsset;
            Settings = settingsAsset == null ? null : settingsAsset.settings;
            settings = null;
        }

        [SerializeField]
        protected Transform muscleRoot;

        [SerializeField]
        protected string musclePrefix = "muscleJoint_";   

        [SerializeField]
        protected SwolePuppetMaster puppet;
        public SwolePuppetMaster Puppet
        {
            get
            {
                if (puppet == null) puppet = gameObject.GetComponentInChildren<SwolePuppetMaster>(true);
                return puppet;
            }
        }

        protected CharacterRagdollSettings settings;
        public CharacterRagdollSettings Settings
        {
            get 
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && settingsAsset != null) return settingsAsset.settings; 
#endif
                return settings == null ? (settingsAsset == null ? null : settingsAsset.settings) : settings;
            }
            set
            {
                settings = value;
            }
        }

        protected Dictionary<int, ConfigurableJoint> unityJoints = new Dictionary<int, ConfigurableJoint>();
        protected Dictionary<int, Rigidbody> unityRigidbodies = new Dictionary<int, Rigidbody>();

        public struct MuscleJoint
        {
            public MuscleMotorGroup group;
            public bool isProp;
            public ConfigurableJoint joint;
            public Transform reference;
        }

        public void Build() => Build(this.transform);
        public void Build(Transform root)
        {
            if (Puppet == null)
            {
                BuildLocal(root, muscleRoot, musclePrefix);
            } 
            else
            {
                puppet.BuildRagdoll(this, muscleRoot, musclePrefix);   
            }
        }
        public void BuildLocal() => BuildLocal(this.transform, muscleRoot, musclePrefix); 
        public void BuildLocalWithMuscleRoot(Transform muscleRoot, string musclePrefix = "muscle_") => BuildLocal(this.transform, muscleRoot, musclePrefix);
        public void BuildLocalWithMuscleRoot(Transform muscleRoot, List<MuscleJoint> muscleJointsList, string musclePrefix = "muscle_") => BuildLocalWithMuscleRoot(out _, muscleRoot, muscleJointsList, musclePrefix); 
        public void BuildLocalWithMuscleRoot(out List<MuscleJoint> muscleJoints, Transform muscleRoot, List<MuscleJoint> muscleJointsList = null, string musclePrefix = "muscle_") => BuildLocal(out muscleJoints, this.transform, muscleRoot, muscleJointsList, musclePrefix);
        public void BuildLocal(Transform root, Transform muscleRoot = null, string musclePrefix = "muscle_") => BuildLocal(out _, root, muscleRoot, null, musclePrefix);
        public void BuildLocal(Transform root, Transform muscleRoot, List<MuscleJoint> muscleJointsList, string musclePrefix = "muscle_") => BuildLocal(out _, root, muscleRoot, muscleJointsList, musclePrefix);
        public void BuildLocal(out List<MuscleJoint> muscleJoints, Transform root, Transform muscleRoot = null, List<MuscleJoint> muscleJointsList = null, string musclePrefix = "muscle_")
        {
            if (musclePrefix == null) musclePrefix = string.Empty;

            muscleJoints = muscleJointsList;
            if (muscleJoints == null) muscleJoints = new List<MuscleJoint>();

            if (muscleRoot == null) muscleRoot = root;

            var settings = Settings;
            if (settings == null) return;

            if (unityJoints == null) unityJoints = new Dictionary<int, ConfigurableJoint>(); 
            unityJoints.Clear();

            if (unityRigidbodies == null) unityRigidbodies = new Dictionary<int, Rigidbody>();
            unityRigidbodies.Clear();

            if (settings.joints != null)
            {
                Transform CreateMuscleTransform(string name, out Transform muscleReference)
                {
                    muscleReference = root.Find(name);
                    if (muscleReference == null) muscleReference = root.FindDeepChildLiberal(name);
                    
                    var muscleName = musclePrefix + Path.GetFileName(SwoleUtil.ConvertToDirectorySeparators(name, _transformPathSeparator, false));
                    var muscleT = muscleRoot.FindTopLevelChild(muscleName);
                    if (muscleT == null)
                    {
                        muscleT = new GameObject(muscleName).transform; 
                        muscleT.SetParent(muscleRoot, false);
                        if (muscleReference != null)
                        {
                            muscleReference.GetPositionAndRotation(out var pos, out var rot);
                            muscleT.SetPositionAndRotation(pos, rot);
                            muscleT.localScale = muscleReference.localScale;
                        }
                    }

                    return muscleT;
                }

                for (int a = 0; a < settings.joints.Count; a++) // pre-create muscle transforms
                {
                    var joint = settings.joints[a];
                    if (joint.isMuscle || joint.isPropMuscle)
                    {
                        CreateMuscleTransform(joint.name, out _);
                    }
                }
                
                for (int a = 0; a < settings.joints.Count; a++)
                {
                    var joint = settings.joints[a];

                    bool isMuscle = joint.isMuscle || joint.isPropMuscle;

                    Transform obj;
                    Transform muscleReference = null;
                    if (isMuscle)
                    {
                        obj = CreateMuscleTransform(joint.name, out muscleReference); 
                    } 
                    else
                    {
                        obj = root.Find(joint.name);
                        if (obj == null) obj = root.FindDeepChildLiberal(joint.name);
                    }

                    if (obj == null) continue;
                     
                    unityJoints[a] = joint.AsComponent(obj.gameObject, isMuscle ? muscleRoot : root, out var localRB, false, isMuscle ? root : null, !isMuscle, true, isMuscle ? musclePrefix : null, null);  
                    unityRigidbodies[a] = localRB; 
                    
                    if (isMuscle)
                    {
                        muscleJoints.Add(new MuscleJoint()
                        {
                            group = joint.muscleMotorGroup,
                            isProp = joint.isPropMuscle,
                            joint = unityJoints[a],
                            reference = muscleReference
                        });
                    }
                }
            }
            
            if (settings.connectedColliders != null)
            {
                foreach (var collider in settings.connectedColliders)
                {
                    collider.Create(root.gameObject);
                }
            }

            ApplyParameterOverrides();
        }
        public void RemoveComponents(bool immediate) => RemoveComponents(this.transform, muscleRoot, immediate, musclePrefix);
        public void RemoveComponentsWithMuscleRoot(Transform muscleRoot, bool immediate, string musclePrefix = "muscle_") => RemoveComponents(this.transform, muscleRoot, immediate, musclePrefix);
        public void RemoveComponents(Transform root, Transform muscleRoot, bool immediate, string musclePrefix = "muscle_")
        {
            var settings_ = Settings;
            if (settings_ == null) return;

            if (muscleRoot == null) muscleRoot = root; 

            if (settings_.joints != null)
            {
                for (int a = 0; a < settings_.joints.Count; a++)
                {
                    var joint = settings_.joints[a];

                    bool isMuscle = joint.isMuscle || joint.isPropMuscle; 

                    if (isMuscle) 
                    {
                        var muscleName = Path.GetFileName(SwoleUtil.ConvertToDirectorySeparators(joint.name, _transformPathSeparator, false));
                        var muscleT = muscleRoot.FindTopLevelChild(muscleName);  
                        if (muscleT != null)
                        {
                            if (immediate) GameObject.DestroyImmediate(muscleT.gameObject); else GameObject.Destroy(muscleT.gameObject); 
                        } 
                        else if (musclePrefix != null)
                        {
                            muscleName = musclePrefix + muscleName;
                            muscleT = muscleRoot.FindTopLevelChild(muscleName);
                            if (muscleT != null)
                            {
                                if (immediate) GameObject.DestroyImmediate(muscleT.gameObject); else GameObject.Destroy(muscleT.gameObject);
                            }
                        }
                    }

                    var obj = root.Find(joint.name);
                    if (obj == null) obj = root.FindDeepChildLiberal(joint.name);  
                    if (obj == null) continue;

                    joint.DestroyComponent(obj.gameObject, isMuscle ? muscleRoot : root, immediate, isMuscle ? root : null, !isMuscle, true, isMuscle ? musclePrefix : null, null);  
                }
            }

            if (settings_.connectedColliders != null)
            {
                foreach (var collider in settings_.connectedColliders)
                {
                    collider.Destroy(root.gameObject, immediate);  
                }
            }
        }

        public void CopySettingsFromComponents() => CopySettingsFromComponents(gameObject, musclePrefix);
        public void CopySettingsFromComponents(GameObject root, string musclePrefix = "muscle_")
        {
            if (settings == null && settingsAsset == null)
            {
                settings = new CharacterRagdollSettings();
            }

            var settingsInst = settings;
            if (settingsAsset != null)
            {
                if (settingsAsset.settings == null) settingsAsset.settings = new CharacterRagdollSettings(); 
                settingsInst = settingsAsset.settings;
            }

            if (settingsInst.joints == null) settingsInst.joints = new List<RagdollJoint>();
            settingsInst.joints.Clear();

            HashSet<Rigidbody> nonJointRigidbodies = new HashSet<Rigidbody>();
            bool IsJoint(Rigidbody rb)
            {
                var chj = rb.GetComponent<CharacterJoint>();
                var coj = rb.GetComponent<ConfigurableJoint>();

                return chj != null || coj != null;
            }

            var characterJoints = root.GetComponentsInChildren<CharacterJoint>(true);
            if (characterJoints != null)
            {
                foreach (var joint in characterJoints) 
                {
                    var dollJoint = RagdollJoint.FromComponent(joint, this.transform, muscleRoot, true, musclePrefix);
                    if (Puppet != null && puppet.muscles != null)
                    {
                        string jointName = SwoleUtil.ConvertToDirectorySeparators(dollJoint.name, _transformPathSeparator, false);
                        jointName = Path.GetFileName(jointName);
                        var jointNameNoPrefix = CharacterRagdollUtils.GetNonPrefixedName(jointName, musclePrefix, null, false);

                        foreach (var muscle in puppet.muscles)
                        {
                            var muscleNameNoPrefix = CharacterRagdollUtils.GetNonPrefixedName(muscle.name, musclePrefix, null, false);
                            if (muscle.name == jointName || muscle.name == jointNameNoPrefix || muscleNameNoPrefix == jointName || muscleNameNoPrefix == jointNameNoPrefix) 
                            {
                                dollJoint.isMuscle = true;
                                dollJoint.isPropMuscle = muscle.isPropMuscle;
                                if (muscle.props != null) dollJoint.muscleMotorGroup = muscle.props.group.AsSwoleType();

                                break;
                            }
                        }
                    }
                    settingsInst.joints.Add(dollJoint);

                    if (joint.connectedBody != null && !IsJoint(joint.connectedBody)) nonJointRigidbodies.Add(joint.connectedBody);  
                }
            }

            var configurableJoints = root.GetComponentsInChildren<ConfigurableJoint>(true); 
            if (configurableJoints != null)
            {
                foreach (var joint in configurableJoints) 
                {
                    var dollJoint = RagdollJoint.FromComponent(joint, this.transform, muscleRoot, true, musclePrefix);
                    if (Puppet != null && puppet.muscles != null)
                    {
                        string jointName = SwoleUtil.ConvertToDirectorySeparators(dollJoint.name, _transformPathSeparator, false); 
                        jointName = Path.GetFileName(jointName);
                        var jointNameNoPrefix = CharacterRagdollUtils.GetNonPrefixedName(jointName, musclePrefix, null, false);

                        foreach (var muscle in puppet.muscles)
                        {
                            var muscleNameNoPrefix = CharacterRagdollUtils.GetNonPrefixedName(muscle.name, musclePrefix, null, false);
                            if (muscle.name == jointName || muscle.name == jointNameNoPrefix || muscleNameNoPrefix == jointName || muscleNameNoPrefix == jointNameNoPrefix) 
                            {
                                dollJoint.isMuscle = true;
                                dollJoint.isPropMuscle = muscle.isPropMuscle;
                                if (muscle.props != null) dollJoint.muscleMotorGroup = muscle.props.group.AsSwoleType();

                                break;
                            }
                        }
                    }
                    settingsInst.joints.Add(dollJoint);

                    if (joint.connectedBody != null && !IsJoint(joint.connectedBody)) nonJointRigidbodies.Add(joint.connectedBody);
                }
            }

            foreach(var rb in nonJointRigidbodies)
            {
                if (settingsInst.connectedColliders == null) settingsInst.connectedColliders = new List<UnityColliderObject>();
                settingsInst.connectedColliders.Clear();
                
                CharacterRagdollUtils.FindColliders(root.transform, settingsInst.connectedColliders, rb.transform, false, true, null); 
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (settingsAsset != null)
                {
                    settingsAsset.settings = settingsInst;
                    AssetDatabase.SaveAssetIfDirty(settingsAsset);  
                }
            }
#endif
        }

        [Header("Parameter Overrides"), SerializeField]
        protected bool overridePreprocessing;
        [SerializeField]
        protected bool enablePreprocessing;

        [SerializeField]
        protected bool overrideCollisionDetectionMode;
        [SerializeField]
        protected CollisionDetectionMode collisionDetectionMode;

        public void ApplyParameterOverrides()
        {
            if (overridePreprocessing) SetEnablePreprocessing(enablePreprocessing);
            if (overrideCollisionDetectionMode) SetCollisionDetectionMode(collisionDetectionMode);
        } 

        public void SetTotalJointMassRatio(float ratio)
        {
            var settings = Settings;
            if (settings == null) return;

            SetTotalJointMass(settings.TotalJointMass * ratio); 
        }
        public void SetTotalJointMass(float mass)
        {
            var settings = Settings;
            if (settings == null) return;

            var totalMass = settings.TotalJointMass;
            float ratio = mass / totalMass;

            if (settings.joints != null && unityJoints != null)
            {
                for (int a = 0; a < settings.joints.Count; a++)
                {
                    var joint = settings.joints[a];
                    if (!unityJoints.TryGetValue(a, out var component)) continue;

                    var localBody = component.GetComponent<Rigidbody>();
                    if (localBody == null) continue;

                    localBody.mass = joint.localBody.rigidbody.mass * ratio;
                }
            }
        }

        public void SetEnablePreprocessing(bool enablePreprocessing) 
        {
            if (unityJoints != null)
            {
                foreach (var joint in unityJoints) if (joint.Value != null) joint.Value.enablePreprocessing = enablePreprocessing; 
            }
        }

        public void SetCollisionDetectionMode(CollisionDetectionMode collisionDetectionMode)
        {
            if (unityRigidbodies != null)
            {
                foreach (var rb in unityRigidbodies) if (rb.Value != null) rb.Value.collisionDetectionMode = collisionDetectionMode; 
            }
        }
    }
}

#endif