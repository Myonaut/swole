#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;

namespace Swole.API.Unity
{

    /// <summary>
    /// An in-engine instance of a Creation object.
    /// </summary>
    public class CreationBehaviour : MonoBehaviour
    {

        [NonSerialized]
        private SwoleLogger m_logger;
        public SwoleLogger Logger => m_logger;

        [NonSerialized]
        private int m_executionPriority;

        [NonSerialized]
        private Creation m_creation;
        public Creation Creation => m_creation;

        public static CreationBehaviour AddToObject(GameObject obj, Creation creation, int executionPriority = 0, SwoleLogger logger = null)
        {
            if (obj == null) return null;
            var b = obj.AddComponent<CreationBehaviour>();
            b.m_creation = creation;
            b.m_executionPriority = executionPriority;
            b.m_logger = logger;
            return b;
        }

        public static CreationBehaviour New(Creation creation, int executionPriority = 0, SwoleLogger logger = null)
        {
            if (creation == null) return null;
            GameObject obj = new GameObject(creation.Name);
            return AddToObject(obj, creation, executionPriority, logger);
        }

        [NonSerialized]
        private RuntimeEnvironment m_environment;

        [NonSerialized]
        private ExecutableBehaviour m_behaviour;

        protected virtual void Start()
        {

            if (!swole.IsInPlayMode || destroyed || m_behaviour != null || m_creation == null || !m_creation.HasScripting) return;

            List<IVar> vars = new List<IVar>();
            // TODO: Capture variables to pass into the local environment
            m_environment = new RuntimeEnvironment(gameObject.name + "_envCR", vars);
            m_behaviour = Creation.Script.NewExecutable(gameObject.name + "_bvrCR", m_executionPriority, m_environment, m_logger);

            m_behaviour.ExecuteToCompletion(ExecutionLayer.Initialization, 1); 

            SwoleScriptPlayModeEnvironment.AddBehaviour(m_behaviour);

        }

        public virtual void Initialize() => Start();

        public bool IsInitialized => m_behaviour != null || m_creation == null || (m_creation != null && !m_creation.HasScripting);

        protected virtual void OnEnable()
        {
            if (swole.IsInPlayMode) 
            {
                if (!IsInitialized) Initialize();
                m_behaviour?.ExecuteToCompletion(ExecutionLayer.Enable); 
            }
        }

        protected virtual void OnDisable()
        {
            if (swole.IsInPlayMode) m_behaviour?.ExecuteToCompletion(ExecutionLayer.Disable); 
        }

        [NonSerialized]
        private bool destroyed;
        protected virtual void OnDestroy()
        {
            destroyed = true;
            if (swole.IsInPlayMode) m_behaviour?.ExecuteToCompletion(ExecutionLayer.Destroy);
            m_behaviour?.Dispose();
            m_behaviour = null;
            m_environment?.Dispose();
            m_environment = null;
            m_creation = null;
        }

        protected virtual void FixedUpdate()
        {
            if (swole.IsInPlayMode) 
            {
                m_behaviour?.Execute(ExecutionLayer.FixedUpdate);      
            }
        }

        protected virtual void OnCollisionEnter(Collision col) { }
        protected virtual void OnCollisionStay(Collision col) { }
        protected virtual void OnCollisionExit(Collision col) { }

        protected virtual void OnTriggerEnter(Collider col) { }
        protected virtual void OnTriggerStay(Collider col) { }
        protected virtual void OnTriggerExit(Collider col) { }

        public GameObject FindUnityObject(string name) 
        {           
            var t = transform.FindDeepChildLiberal(name);
            if (t == null) return null;
            return t.gameObject;
        }

        public EngineInternal.GameObject FindObject(string name)
        {
            var t = transform.FindDeepChildLiberal(name);
            if (t == null) return default;
            return new EngineInternal.GameObject(t.gameObject, new EngineInternal.Transform(t));
        }



    }

}

#endif