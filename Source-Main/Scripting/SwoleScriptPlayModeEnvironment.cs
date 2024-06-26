namespace Swole.Script
{

    /// <summary>
    /// Handles the consistent execution of SwoleScript code in play mode.
    /// </summary>
    public class SwoleScriptPlayModeEnvironment : SingletonBehaviour<SwoleScriptPlayModeEnvironment>
    {

        public override bool DestroyOnLoad => false;

        private readonly ExecutionStack m_executionStack = new ExecutionStack();
        public static ExecutionStack ExecutionStack
        {

            get
            {

                var instance = Instance;
                if (instance == null) return null;

                return instance.m_executionStack;

            }

        }

        public static void Insert(ExecutionLayer layer, IExecutable exe)
        {

            var instance = Instance;
            if (instance == null) return;

            instance.m_executionStack.Insert(layer, exe);

        }

        /// <summary>
        /// Removes all occurances of the executable in a specific layer.
        /// </summary>
        public static bool RemoveAll(ExecutionLayer layer, IExecutable exe)
        {

            var instance = Instance;
            if (instance == null) return false;

            return instance.m_executionStack.RemoveAll(layer, exe);

        }
        /// <summary>
        /// Removes all occurances of the executable from the environment.
        /// </summary>
        public static bool RemoveAll(IExecutable exe)
        {

            var instance = Instance;
            if (instance == null) return false;

            return instance.m_executionStack.RemoveAll(exe);

        }

        public static void AddBehaviour(ExecutableBehaviour behaviour, bool force = false)
        {
            if (behaviour == null) return;
            behaviour.AddToStackLayerIfHasScript(ExecutionLayer.EarlyUpdate, ExecutionStack, force);
            behaviour.AddToStackLayerIfHasScript(ExecutionLayer.Update, ExecutionStack, force);
            behaviour.AddToStackLayerIfHasScript(ExecutionLayer.LateUpdate, ExecutionStack, force);
        }

        public static void RemoveBehaviour(ExecutableBehaviour behaviour)
        {
            if (behaviour == null) return;
            behaviour.RemoveFromStackLayer(ExecutionLayer.EarlyUpdate, ExecutionStack);
            behaviour.RemoveFromStackLayer(ExecutionLayer.Update, ExecutionStack);
            behaviour.RemoveFromStackLayer(ExecutionLayer.LateUpdate, ExecutionStack);
        }

        public override int Priority => 10;

        public static void Reset() => Instance?.m_executionStack.Reset();

        public override void OnUpdate() 
        {

            if (!swole.IsInPlayMode) return;

            m_executionStack.Evaluate(); 

        }

        public override void OnLateUpdate() 
        {

            if (!swole.IsInPlayMode) return;

            m_executionStack.LateEvaluate(); 
        
        }

        public override void OnFixedUpdate() { }

        public override void OnDestroyed() => m_executionStack.Dispose();

    }

}