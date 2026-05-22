#if UNITY_EDITOR

using System.Collections;

using UnityEngine;
using UnityEditor;

namespace Swole
{

    // Created By: Bunny83 (https://discussions.unity.com/u/bunny83)
    // Source: https://discussions.unity.com/t/recover-path-information-for-an-asset-but-at-runtime/157894/2

    // Edited By Nox

    [CustomEditor(typeof(ResourceDB))]
    public class ResourceDBEditor : Editor
    {
        ResourceDB m_Target;
        void OnEnable()
        {
            m_Target = (ResourceDB)target;
        }
        public override void OnInspectorGUI()
        {

            GUI.enabled = false;
            DrawDefaultInspector();
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Now"))
            {
                m_Target.UpdateDB(true);
            }
            m_Target.UpdateAutomatically = GUILayout.Toggle(m_Target.UpdateAutomatically, "AutoUpdate", "Button");
            if (GUI.changed)
            {
                EditorUtility.SetDirty(m_Target);
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndHorizontal();
            m_Target.notifyWhenOutdated = GUILayout.Toggle(m_Target.notifyWhenOutdated, "Notify When Outdated");
            m_Target.notificationType = (ResourceDB.NotificationType)EditorGUILayout.EnumPopup("Notify Log Type", m_Target.notificationType); 
            EditorGUILayout.LabelField("Folders:", m_Target.FolderCount.ToString());
            EditorGUILayout.LabelField("Files:", m_Target.FileCount.ToString());
        }
    }

}

#endif