#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.API.Unity.Worlds;
using WorldClass = Swole.API.Unity.Worlds.World;

namespace Swole.API.Unity.UndoSystem
{
    /// <summary>
    /// Generalized revertable action for deleting an existing world feature (river, lake, fence, road, etc).
    /// Mirrors <see cref="CreateWorldFeature"/> but inverted: reverting a delete recreates the feature from
    /// its captured record; reapplying the delete removes it again.
    /// </summary>
    public struct DeleteWorldFeature : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => false;

        public WorldFeatureRecord record;

        public void Revert()
        {
            if (record == null) return;

            IWorldFeatureInstance instance = WorldFeatureRegistry.Instantiate(record);
            if (instance != null) WorldClass.Instance?.RegisterLiveFeatureInstance(record.id, instance);

            WorldClass.Instance?.worldEditSystem?.SubmitFeature(record);
        }

        public void Reapply()
        {
            if (record == null) return;

            WorldClass.Instance?.RemoveLiveFeatureInstance(record.id);
            WorldClass.Instance?.worldEditSystem?.RemoveFeature(record.id);
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

        /// <summary>
        /// Constructs the action from a record capturing the feature's state at the moment of deletion.
        /// The caller is responsible for removing the live instance/submission before or after recording
        /// this action into undo history.
        /// </summary>
        public DeleteWorldFeature(WorldFeatureRecord record)
        {
            undoState = false;
            this.record = record?.Clone();
        }
    }
}

#endif
