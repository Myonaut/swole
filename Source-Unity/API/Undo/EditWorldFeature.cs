#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.API.Unity.Worlds;
using WorldClass = Swole.API.Unity.Worlds.World;

namespace Swole.API.Unity.UndoSystem
{
    /// <summary>
    /// Generalized revertable action for editing an existing world feature (river, lake, fence, road, etc)
    /// - e.g. moving/adding/removing control points. Stores the record before and after the edit;
    /// reverting re-applies the "before" record to the live instance, reapplying restores "after".
    /// Works for any feature type registered with <see cref="WorldFeatureRegistry"/>.
    /// </summary>
    public struct EditWorldFeature : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => false;

        public WorldFeatureRecord before;
        public WorldFeatureRecord after;

        public void Revert() => Apply(before);
        public void Reapply() => Apply(after);

        private static void Apply(WorldFeatureRecord record)
        {
            if (record == null) return;

            IWorldFeatureInstance instance = WorldClass.Instance?.GetLiveFeatureInstance(record.id);
            if (instance == null)
            {
                // Live instance may have been torn down (e.g. scene reload) - reconstruct it.
                instance = WorldFeatureRegistry.Instantiate(record);
                if (instance != null) WorldClass.Instance?.RegisterLiveFeatureInstance(record.id, instance);
            }
            else
            {
                instance.ApplyRecord(record);
            }

            WorldClass.Instance?.worldEditSystem?.SubmitFeature(record);
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
        /// Constructs the action from the feature's record state before and after an edit. The caller is
        /// responsible for having already applied/submitted the "after" state before recording this action.
        /// </summary>
        public EditWorldFeature(WorldFeatureRecord before, WorldFeatureRecord after)
        {
            undoState = false;
            this.before = before?.Clone();
            this.after = after?.Clone();
        }
    }
}

#endif
