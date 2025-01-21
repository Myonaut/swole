using System;
using System.Collections.Generic;

using Swole.Script;

namespace Swole
{

    [Serializable]
    public struct CreationScript : IPackageDependent, IEquatable<CreationScript>
    {

        public ExecutableBehaviour NewExecutable(string identity, int priority, IRuntimeEnvironment environment, SwoleLogger logger = null, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null) => new ExecutableBehaviour(this, identity, priority, environment, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

#if SWOLE_ENV
            if (!string.IsNullOrWhiteSpace(source_OnLoadExperience)) dependencies = swole.ExtractPackageDependencies(source_OnLoadExperience, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnUnloadExperience)) dependencies = swole.ExtractPackageDependencies(source_OnUnloadExperience, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnBeginExperience)) dependencies = swole.ExtractPackageDependencies(source_OnBeginExperience, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnEndExperience)) dependencies = swole.ExtractPackageDependencies(source_OnEndExperience, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnRestartExperience)) dependencies = swole.ExtractPackageDependencies(source_OnRestartExperience, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnSaveProgress)) dependencies = swole.ExtractPackageDependencies(source_OnSaveProgress, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnLoadProgress)) dependencies = swole.ExtractPackageDependencies(source_OnLoadProgress, dependencies);

            if (!string.IsNullOrWhiteSpace(source_OnInitialize)) dependencies = swole.ExtractPackageDependencies(source_OnInitialize, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnEarlyUpdate)) dependencies = swole.ExtractPackageDependencies(source_OnEarlyUpdate, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnUpdate)) dependencies = swole.ExtractPackageDependencies(source_OnUpdate, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnLateUpdate)) dependencies = swole.ExtractPackageDependencies(source_OnLateUpdate, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnDestroy)) dependencies = swole.ExtractPackageDependencies(source_OnDestroy, dependencies);

            if (!string.IsNullOrWhiteSpace(source_OnFixedUpdate)) dependencies = swole.ExtractPackageDependencies(source_OnFixedUpdate, dependencies);

            if (!string.IsNullOrWhiteSpace(source_OnEnable)) dependencies = swole.ExtractPackageDependencies(source_OnEnable, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnDisable)) dependencies = swole.ExtractPackageDependencies(source_OnDisable, dependencies);

            if (!string.IsNullOrWhiteSpace(source_OnCollisionEnter)) dependencies = swole.ExtractPackageDependencies(source_OnCollisionEnter, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnCollisionStay)) dependencies = swole.ExtractPackageDependencies(source_OnCollisionStay, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnCollisionExit)) dependencies = swole.ExtractPackageDependencies(source_OnCollisionExit, dependencies);

            if (!string.IsNullOrWhiteSpace(source_OnTriggerEnter)) dependencies = swole.ExtractPackageDependencies(source_OnTriggerEnter, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnTriggerStay)) dependencies = swole.ExtractPackageDependencies(source_OnTriggerStay, dependencies);
            if (!string.IsNullOrWhiteSpace(source_OnTriggerExit)) dependencies = swole.ExtractPackageDependencies(source_OnTriggerExit, dependencies);

            if (!string.IsNullOrWhiteSpace(source_OnInteract)) dependencies = swole.ExtractPackageDependencies(source_OnInteract, dependencies);
#endif

            return dependencies;
        }

        /// <summary>
        /// Does the creation not have any scripting?
        /// </summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(source_OnLoadExperience) &&
            string.IsNullOrWhiteSpace(source_OnUnloadExperience) &&
            string.IsNullOrWhiteSpace(source_OnBeginExperience) &&
            string.IsNullOrWhiteSpace(source_OnEndExperience) &&
            string.IsNullOrWhiteSpace(source_OnRestartExperience) &&
            string.IsNullOrWhiteSpace(source_OnSaveProgress) &&
            string.IsNullOrWhiteSpace(source_OnLoadProgress) &&
            string.IsNullOrWhiteSpace(source_OnInitialize) &&
            string.IsNullOrWhiteSpace(source_OnEarlyUpdate) &&
            string.IsNullOrWhiteSpace(source_OnUpdate) &&
            string.IsNullOrWhiteSpace(source_OnLateUpdate) &&
            string.IsNullOrWhiteSpace(source_OnFixedUpdate) &&
            string.IsNullOrWhiteSpace(source_OnEnable) &&
            string.IsNullOrWhiteSpace(source_OnDisable) &&
            string.IsNullOrWhiteSpace(source_OnDestroy) &&
            string.IsNullOrWhiteSpace(source_OnCollisionEnter) &&
            string.IsNullOrWhiteSpace(source_OnCollisionStay) &&
            string.IsNullOrWhiteSpace(source_OnCollisionExit) &&
            string.IsNullOrWhiteSpace(source_OnTriggerEnter) &&
            string.IsNullOrWhiteSpace(source_OnTriggerStay) &&
            string.IsNullOrWhiteSpace(source_OnTriggerExit) &&
            string.IsNullOrWhiteSpace(source_OnInteract);

        public string source_OnLoadExperience;
        public string source_OnUnloadExperience;

        public string source_OnBeginExperience;
        public string source_OnEndExperience;
        public string source_OnRestartExperience;

        public string source_OnSaveProgress;
        public string source_OnLoadProgress;

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

        public bool Equals(CreationScript other)
        {
            if (source_OnLoadExperience != other.source_OnLoadExperience) return false;
            if (source_OnUnloadExperience != other.source_OnUnloadExperience) return false;
            if (source_OnBeginExperience != other.source_OnBeginExperience) return false;
            if (source_OnEndExperience != other.source_OnEndExperience) return false;
            if (source_OnRestartExperience != other.source_OnRestartExperience) return false;
            if (source_OnSaveProgress != other.source_OnSaveProgress) return false;
            if (source_OnLoadProgress != other.source_OnLoadProgress) return false;

            if (source_OnInitialize != other.source_OnInitialize) return false;
            if (source_OnEarlyUpdate != other.source_OnEarlyUpdate) return false;
            if (source_OnUpdate != other.source_OnUpdate) return false;
            if (source_OnLateUpdate != other.source_OnLateUpdate) return false;
            if (source_OnFixedUpdate != other.source_OnFixedUpdate) return false;
            if (source_OnEnable != other.source_OnEnable) return false;
            if (source_OnDisable != other.source_OnDisable) return false;
            if (source_OnDestroy != other.source_OnDestroy) return false;

            if (source_OnCollisionEnter != other.source_OnCollisionEnter) return false;
            if (source_OnCollisionStay != other.source_OnCollisionStay) return false;
            if (source_OnCollisionExit != other.source_OnCollisionExit) return false;

            if (source_OnTriggerEnter != other.source_OnTriggerEnter) return false;
            if (source_OnTriggerStay != other.source_OnTriggerStay) return false;  
            if (source_OnTriggerExit != other.source_OnTriggerExit) return false;

            return true;
        }

        public static bool operator ==(CreationScript A, CreationScript B) => A.Equals(B);
        public static bool operator !=(CreationScript A, CreationScript B) => !A.Equals(B);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is CreationScript other) return this == other;
            return base.Equals(obj);
        }

    }

}
