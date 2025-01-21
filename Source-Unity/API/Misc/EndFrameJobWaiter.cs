#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections.Generic;

using Unity.Jobs;

namespace Swole.API.Unity
{
    public class EndFrameJobWaiter : SingletonBehaviour<EndFrameJobWaiter>
    {

        public override bool ExecuteInStack => false;
        public override bool DestroyOnLoad => false;

        public override void OnFixedUpdate() {}
        public override void OnUpdate() {}

        private readonly List<JobHandle> toComplete = new List<JobHandle>();

        protected override void OnAwake()
        {
            base.OnAwake();
            SingletonCallStack.PostLateUpdate += OnLateUpdate;
        }

        protected virtual void OnDestroy()
        {
            SingletonCallStack.PostLateUpdate -= OnLateUpdate;
        }

        public override void OnLateUpdate()
        {
            foreach (var job in toComplete) job.Complete();
            toComplete.Clear();
        }

        public static void WaitFor(JobHandle handle)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.toComplete.Add(handle);
        }

    }
}

#endif