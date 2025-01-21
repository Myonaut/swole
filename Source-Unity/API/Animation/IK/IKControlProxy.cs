#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used by animations to control inverse kinematics
    /// </summary>
    [AnimatablePropertyPrefix("IK_")] 
    public abstract class IKControlProxy : ControlProxy
    {

        [SerializeField]
        protected CustomIKManager manager;
        public CustomIKManager Manager
        {
            get
            {
                if (manager == null) manager = gameObject.GetComponent<CustomIKManager>();
                return manager;
            }
        }

        protected void SetActiveByProxy(bool active, List<int> indices)
        {
            if (indices == null || Manager == null) return;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                controller.SetActive(active);
            }
        }
        protected bool GetActiveByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Manager == null) return false;
            bool value = true;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                value = value && controller.IsActive;
            }
            return value;
        }
        protected void SetWeightByProxy(float value, List<int> indices)
        {
            if (indices == null || Manager == null) return;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                controller.SetWeight(value);
            }
        }
        protected float GetWeightByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Manager == null) return 0;
            float value = 0;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                value += controller.GetWeight();
            }
            return value / indices.Count;
        }
        protected void SetPositionWeightByProxy(float value, List<int> indices)
        {
            if (indices == null || Manager == null) return;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                controller.SetPositionWeight(value);
            }
        }
        protected float GetPositionWeightByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Manager == null) return 0;
            float value = 0;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                value += controller.GetPositionWeight();
            }
            return value / indices.Count;
        }
        protected void SetRotationWeightByProxy(float value, List<int> indices)
        {
            if (indices == null || Manager == null) return;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                controller.SetRotationWeight(value);
            }
        }
        protected float GetRotationWeightByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Manager == null) return 0;
            float value = 0;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                value += controller.GetRotationWeight();
            }
            return value / indices.Count;
        }
        protected void SetBendGoalWeightByProxy(float value, List<int> indices)
        {
            if (indices == null || Manager == null) return;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                controller.SetBendGoalWeight(value);
            }
        }
        protected float GetBendGoalWeightByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Manager == null) return 0;
            float value = 0;
            foreach (var index in indices)
            {
                var controller = manager[index];
                if (controller == null) continue;

                value += controller.GetBendGoalWeight();
            }
            return value / indices.Count;
        }

        public override int FindBindingIndex(string binding)
        {
            if (Manager == null) return -1;
            return manager.FindIKControllerIndex(binding);
        }
    }
}

#endif
