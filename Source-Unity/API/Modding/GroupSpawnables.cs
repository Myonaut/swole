using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class GroupSpawnables : MonoBehaviour
{

    [Serializable]
    public struct PrefabToSpawn
    {
        public GameObject prefab;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public Vector3 localScale;
    }

    [Serializable]
    public class SpawnGroup
    {
        public Transform root;

        public float spawnChance = 1;

        public Vector3 localPosition;
        public Vector3 localRotation;

        public PrefabToSpawn[] prefabsToSpawn;

        public GameObject[] objectsToDestroy;

        public void Spawn()
        {
            spawned = true;

            if (root == null || prefabsToSpawn == null) return;

            Quaternion offsetRot = Quaternion.Euler(localRotation);
            foreach (var prefab in prefabsToSpawn) 
            { 
                var inst = Instantiate(prefab.prefab, root.TransformPoint(prefab.localPosition + localPosition), root.rotation * (offsetRot * Quaternion.Euler(prefab.localRotation)), root);
                inst.transform.localScale = prefab.localScale; 
            }

            if (objectsToDestroy != null)
            {
                foreach(var obj in objectsToDestroy) if (obj != null) GameObject.Destroy(obj); 
            }
        }

        [NonSerialized]
        public bool spawned;
    }
    
    public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();

    public int SpawnGroupCount => spawnGroups == null ? 0 : spawnGroups.Count;

    public void Spawn(int index)
    {
        if (index < 0 || index >= SpawnGroupCount) return;

        spawnGroups[index].Spawn();
    }
    public void SpawnIfNotSpawned(int index)
    {
        if (index < 0 || index >= SpawnGroupCount) return;

        var spawner = spawnGroups[index];
        if (!spawner.spawned) spawner.Spawn();
    }

    public void ChanceSpawn(int index, float chanceVal)
    {
        if (index < 0 || index >= SpawnGroupCount) return;

        var spawner = spawnGroups[index];
        if (chanceVal > spawner.spawnChance) return; 

        spawnGroups[index].Spawn();
    }
    public void ChanceSpawnIfNotSpawned(int index, float chanceVal)
    {
        if (index < 0 || index >= SpawnGroupCount) return;

        var spawner = spawnGroups[index];
        if (chanceVal > spawner.spawnChance) return;

        if (!spawner.spawned) spawner.Spawn();
    }

    public int defaultSpawnCount = 1;

    public bool spawnRandomOnAwake;
    public bool spawnIndicesOnAwake;
    public List<int> indicesToSpawn;

    protected void Awake()
    {
        if (spawnRandomOnAwake)
        {
            Spawn(UnityEngine.Random.Range(0, SpawnGroupCount)); 
        }

        if (spawnIndicesOnAwake)
        {
            if (indicesToSpawn != null)
            {
                foreach (var ind in indicesToSpawn) Spawn(ind);
            }
        }
    }

}
