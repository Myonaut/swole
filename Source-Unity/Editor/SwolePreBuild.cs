#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Swole
{
    public class SwolePreBuild : IPreprocessBuildWithReport
    {

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {

            Debug.Log($"Running {nameof(SwolePreBuild)}.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);

            if (ResourceDB.FindInstance() != null)
            {
                if (ResourceDB.Instance.IsOutdated) ResourceDB.Instance.UpdateDB(true); 
            }
        }
    }
}

#endif