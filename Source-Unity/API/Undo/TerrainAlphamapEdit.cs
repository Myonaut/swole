#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

using Swole;

namespace Swole.API.Unity.UndoSystem
{
    /// <summary>
    /// Revertable action for a single terrain texture (alphamap) painting stroke, storing the affected
    /// rectangle's before/after alphamap weights so it can be reverted/reapplied independent of the live
    /// paint operation. Mirrors <see cref="TerrainHeightEdit"/>'s rectangle-snapshot approach.
    /// </summary>
    public struct TerrainAlphamapEdit : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => false;

        public Terrain terrain;

        public int startX;
        public int startZ;

        /// <summary>Alphamap weights, shaped [height, width, layerCount], as used by TerrainData.Get/SetAlphamaps.</summary>
        public float[,,] weightsBefore;
        public float[,,] weightsAfter;

        public void Revert()
        {
            if (terrain == null || weightsBefore == null) return;
            terrain.terrainData.SetAlphamaps(startX, startZ, weightsBefore);
        }

        public void Reapply()
        {
            if (terrain == null || weightsAfter == null) return;
            terrain.terrainData.SetAlphamaps(startX, startZ, weightsAfter);
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

        public TerrainAlphamapEdit(Terrain terrain, int startX, int startZ, float[,,] weightsBefore, float[,,] weightsAfter)
        {
            undoState = false;

            this.terrain = terrain;
            this.startX = startX;
            this.startZ = startZ;
            this.weightsBefore = weightsBefore;
            this.weightsAfter = weightsAfter;
        }
    }
}

#endif
