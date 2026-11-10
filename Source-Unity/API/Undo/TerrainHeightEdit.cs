#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    /// <summary>
    /// Revertable action for a single terrain sculpting stroke.
    /// before and after the stroke was applied so it can be reverted/reapplied independent of the
    /// live brush operation.
    /// </summary>
    public struct TerrainHeightEdit : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => false;

        public Terrain terrain;

        public int startX;
        public int startZ;

        public float[,] heightsBefore;
        public float[,] heightsAfter;

        public void Revert()
        {
            if (terrain == null || heightsBefore == null) return;
            terrain.terrainData.SetHeights(startX, startZ, heightsBefore);
        }

        public void Reapply()
        {
            if (terrain == null || heightsAfter == null) return;
            terrain.terrainData.SetHeights(startX, startZ, heightsAfter);
        }

        public void Perpetuate() { }
        public void PerpetuateUndo() { }

        public bool undoState;
        public bool GetUndoState() => undoState;
        public IRevertableAction SetUndoState(bool undone)
        {
            var newState = this;
            newState.undoState = undone;
            return newState;
        }

        public TerrainHeightEdit(Terrain terrain, int startX, int startZ, float[,] heightsBefore, float[,] heightsAfter)
        {
            undoState = false;

            this.terrain = terrain;
            this.startX = startX;
            this.startZ = startZ;
            this.heightsBefore = heightsBefore;
            this.heightsAfter = heightsAfter;
        }
    }
}

#endif
