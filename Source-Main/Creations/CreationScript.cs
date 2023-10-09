using System;
using Swole.Script;

namespace Swole
{

    [Serializable]
    public struct CreationScript
    {

        public ExecutableBehaviour NewExecutable(string identity, int priority, RuntimeEnvironment environment, SwoleLogger logger = null) => new ExecutableBehaviour(this, identity, priority, environment, logger);

        /// <summary>
        /// Does the creation not have any scripting?
        /// </summary>
        public bool IsEmpty =>
            string.IsNullOrEmpty(source_OnInitialize) &&
            string.IsNullOrEmpty(source_OnEarlyUpdate) &&
            string.IsNullOrEmpty(source_OnUpdate) &&
            string.IsNullOrEmpty(source_OnLateUpdate) &&
            string.IsNullOrEmpty(source_OnFixedUpdate) &&
            string.IsNullOrEmpty(source_OnEnable) &&
            string.IsNullOrEmpty(source_OnDisable) &&
            string.IsNullOrEmpty(source_OnDestroy) &&
            string.IsNullOrEmpty(source_OnCollisionEnter) &&
            string.IsNullOrEmpty(source_OnCollisionStay) &&
            string.IsNullOrEmpty(source_OnCollisionExit) &&
            string.IsNullOrEmpty(source_OnTriggerEnter) &&
            string.IsNullOrEmpty(source_OnTriggerStay) &&
            string.IsNullOrEmpty(source_OnTriggerExit) &&
            string.IsNullOrEmpty(source_OnInteract);


        public string source_OnInitialize;

        public string source_OnEarlyUpdate;

        public string source_OnUpdate;

        public string source_OnLateUpdate;

        public string source_OnFixedUpdate;

        public string source_OnEnable;

        public string source_OnDisable;

        public string source_OnDestroy;


        public string source_OnCollisionEnter;

        public string source_OnCollisionStay;

        public string source_OnCollisionExit;


        public string source_OnTriggerEnter;

        public string source_OnTriggerStay;

        public string source_OnTriggerExit;

        /// <summary>
        /// Called when the Creation has interaction points and one of them is used by a character.
        /// </summary>
        public string source_OnInteract;

    }

}
