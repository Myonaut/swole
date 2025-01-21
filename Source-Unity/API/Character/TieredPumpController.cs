#if UNITY_STANDALONE || UNITY_EDITOR

using UnityEngine;

namespace Swole.API.Unity
{
    public class TieredPumpController : MonoBehaviour
    {
        public MuscularRenderedCharacter character;

        public float level1Threshold = 0.2f;
        public float level2Threshold = 0.4f;
        public float level3Threshold = 1;
        public float pumpGainRate = 2f;
        public float pumpDecayRate = 0.025f;

        [HideInInspector]
        public float pumpDecayDelay;

        public float pumpDecayMultiplier = 1;

        public float[] pumpLevels;

        protected bool initialized;
        public void Initialize()
        {
            if (initialized) return;

            initialized = true;

            if (character == null) character = GetComponent<MuscularRenderedCharacter>();
            if (character == null)
            {
                GameObject.Destroy(this);
                return;
            }

            pumpLevels = new float[character.MuscleGroupCount];
        }
        protected virtual void Start()
        {
            Initialize();
        }
        protected virtual void LateUpdate()
        {
            if (pumpLevels == null || character == null) return;

            float decayRate = 0;
            if (pumpDecayDelay > 0)
            {
                pumpDecayDelay -= Time.deltaTime;
            }
            else
            {
                decayRate = Time.deltaTime * pumpDecayRate * pumpDecayMultiplier;
            }

            for (int a = 0; a < pumpLevels.Length; a++)
            {
                var pump = pumpLevels[a];
                pump = Mathf.Max(0, pump - decayRate);
                pumpLevels[a] = pump;
                
                float tieredPump = pump < level1Threshold ? 0 : (pump < level2Threshold ? level1Threshold : (pump < level3Threshold ? level2Threshold : level3Threshold));
                float visualPump = character.GetMuscleGroupPumpUnsafe(a);
                if (visualPump != tieredPump) 
                {
                    visualPump = Mathf.LerpUnclamped(visualPump, tieredPump, Mathf.Min(1, Time.deltaTime * pumpGainRate));
                    character.SetMuscleGroupPumpUnsafe(a, visualPump, false, false);
                    character.UpdateMuscleGroupMaterialPropertiesUnsafe(a);
                }
            }
        }
        public float GetPumpUnsafe(int muscleIndex) => pumpLevels[muscleIndex];
        public float GetPump(int muscleIndex) => pumpLevels == null || muscleIndex < 0 || muscleIndex >= pumpLevels.Length ? 0 : GetPumpUnsafe(muscleIndex);

        public void SetPumpUnsafe(int muscleIndex, float pump) => pumpLevels[muscleIndex] = pump;
        public void SetPump(int muscleIndex, float pump) 
        {
            if (pumpLevels == null || muscleIndex < 0 || muscleIndex >= pumpLevels.Length) return;
            pumpLevels[muscleIndex] = pump;
        }
    }
}

#endif