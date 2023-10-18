using System;
using System.Collections.Generic;

using Swole.Script;

namespace Swole
{

    [Serializable]
    public struct CreationScript : IPackageDependent
    {

        [Serializable, Flags]
        public enum LogicInjectionPoint
        {

            None = 0, OnInitialize = 1, OnEarlyUpdate = 2, OnUpdate = 4, OnLateUpdate = 8, OnFixedUpdate = 16, OnEnable = 32, OnDisable = 64, OnDestroy = 128, OnCollisionEnter = 256, OnCollisionStay = 512, OnCollisionExit = 1024, OnTriggerEnter = 2048, OnTriggerStay = 4096, OnTriggerExit = 8192, OnInteract = 16384

        }

        public ExecutableBehaviour NewExecutable(string identity, int priority, RuntimeEnvironment environment, SwoleLogger logger = null) => new ExecutableBehaviour(this, identity, priority, environment, logger);

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            if (!string.IsNullOrEmpty(source_OnInitialize)) dependencies = Swole.ExtractPackageDependencies(source_OnInitialize, dependencies);
            if (!string.IsNullOrEmpty(source_OnEarlyUpdate)) dependencies = Swole.ExtractPackageDependencies(source_OnEarlyUpdate, dependencies);
            if (!string.IsNullOrEmpty(source_OnUpdate)) dependencies = Swole.ExtractPackageDependencies(source_OnUpdate, dependencies);
            if (!string.IsNullOrEmpty(source_OnLateUpdate)) dependencies = Swole.ExtractPackageDependencies(source_OnLateUpdate, dependencies);
            if (!string.IsNullOrEmpty(source_OnDestroy)) dependencies = Swole.ExtractPackageDependencies(source_OnDestroy, dependencies);

            if (!string.IsNullOrEmpty(source_OnFixedUpdate)) dependencies = Swole.ExtractPackageDependencies(source_OnFixedUpdate, dependencies);

            if (!string.IsNullOrEmpty(source_OnEnable)) dependencies = Swole.ExtractPackageDependencies(source_OnEnable, dependencies);
            if (!string.IsNullOrEmpty(source_OnDisable)) dependencies = Swole.ExtractPackageDependencies(source_OnDisable, dependencies);

            if (!string.IsNullOrEmpty(source_OnCollisionEnter)) dependencies = Swole.ExtractPackageDependencies(source_OnCollisionEnter, dependencies);
            if (!string.IsNullOrEmpty(source_OnCollisionStay)) dependencies = Swole.ExtractPackageDependencies(source_OnCollisionStay, dependencies);
            if (!string.IsNullOrEmpty(source_OnCollisionExit)) dependencies = Swole.ExtractPackageDependencies(source_OnCollisionExit, dependencies);

            if (!string.IsNullOrEmpty(source_OnTriggerEnter)) dependencies = Swole.ExtractPackageDependencies(source_OnTriggerEnter, dependencies);
            if (!string.IsNullOrEmpty(source_OnTriggerStay)) dependencies = Swole.ExtractPackageDependencies(source_OnTriggerStay, dependencies);
            if (!string.IsNullOrEmpty(source_OnTriggerExit)) dependencies = Swole.ExtractPackageDependencies(source_OnTriggerExit, dependencies);

            if (!string.IsNullOrEmpty(source_OnInteract)) dependencies = Swole.ExtractPackageDependencies(source_OnInteract, dependencies);

            return dependencies;
        }

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
