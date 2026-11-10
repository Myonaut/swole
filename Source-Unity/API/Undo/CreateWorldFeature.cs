#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.API.Unity.Worlds;
using WorldClass = Swole.API.Unity.Worlds.World;

namespace Swole.API.Unity.UndoSystem
{
    /// <summary>
    /// Generalized revertable action for placing a new world feature (river, lake, fence, road, etc).
    /// Works for any feature type registered with <see cref="WorldFeatureRegistry"/> - it only depends on
    /// the generic <see cref="WorldFeatureRecord"/>/<see cref="WorldClass"/>/<see cref="WorldEdit"/> APIs, not
    /// on any concrete rendering/generation system.
    /// Reverting removes the live instance and its <see cref="WorldEdit"/> submission; reapplying
    /// reconstructs the instance from the record and re-submits it.
    /// </summary>
    public struct CreateWorldFeature : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => false;

        public WorldFeatureRecord record;

        public void Revert()
        {
            if (record == null) return;

            WorldClass.Instance?.RemoveLiveFeatureInstance(record.id);
            WorldClass.Instance?.worldEditSystem?.RemoveFeature(record.id);
        }

        public void Reapply()
        {
            if (record == null) return;

            IWorldFeatureInstance instance = WorldFeatureRegistry.Instantiate(record);
            if (instance != null) WorldClass.Instance?.RegisterLiveFeatureInstance(record.id, instance);

            WorldClass.Instance?.worldEditSystem?.SubmitFeature(record);
        }

        public void Perpetuate() { }
        public void PerpetuateUndo()
        {
            // Falling out of history while reverted - fully forget the submission too.
            if (record != null) WorldClass.Instance?.worldEditSystem?.RemoveFeature(record.id);
        }

        public bool undoState;
        public bool GetUndoState() => undoState;
        public IRevertableAction SetUndoState(bool undone)
        {
            var newState = this;
            newState.undoState = undone;
            return newState;
        }

        /// <summary>
        /// Constructs the action from a record describing a feature that has already been created/submitted
        /// (the action itself does not create anything on construction - only on <see cref="Reapply"/>/<see cref="Revert"/>).
        /// </summary>
        public CreateWorldFeature(WorldFeatureRecord record)
        {
            undoState = false;
            this.record = record?.Clone();
        }
    }
}

#endif
