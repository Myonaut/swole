using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    using Swole.API.Unity.UndoSystem;
    using TerrainHeightEdit = Swole.API.Unity.UndoSystem.TerrainHeightEdit;
    using TerrainAlphamapEdit = Swole.API.Unity.UndoSystem.TerrainAlphamapEdit;

    /// <summary>
    /// Runtime brush/stamp-based terrain sculpting tool. Stays isolated from any external drawing/spline
    /// systems - it only exposes generic data (brush world position/radius/mode) so other systems
    /// (like a world editor UI) can draw previews and drive input, while this class owns the actual
    /// terrain data manipulation (height + texture channels), stroke undo snapshotting, and WorldEdit
    /// delta recording.
    ///
    /// Brush and stamp "types" are authored as <see cref="BrushPreset"/>/<see cref="StampPreset"/>
    /// ScriptableObjects, each optionally combining a texture-driven or curve-driven falloff/shape with
    /// local size/strength overrides and a set of bound <see cref="TerrainTextureChannel"/>s that paint
    /// terrain textures alongside height changes. This is intentionally generalized so a future
    /// texture-painting-only tool can reuse the same channel/preset machinery.
    /// </summary>
    public class RuntimeTerrainEditor : MonoBehaviour
    {
        public enum BrushMode { Raise, Lower, Flatten, Smooth, SetHeight }

        [Header("Brush Settings")]
        public BrushMode mode = BrushMode.Raise;
        public float brushRadius = 10f;
        [Tooltip("Brush strength in terrain-height meters per second. Kept independent of the terrain's max height (TerrainData.size.y) so a given value feels consistent across terrains with different height ranges.")]
        public float brushStrength = 2f;
        [Tooltip("Smoothing convergence speed, used only by Smooth mode. Kept separate from brushStrength so smoothing can be tuned independently of raise/lower/flatten strength.")]
        public float smoothStrength = 2f;
        [Tooltip("Target normalized height used by Flatten mode. If left at -1, the average height within the brush radius (sampled on first touch of a stroke) is used instead.")]
        public float flattenTargetHeight = -1f;
        [Tooltip("Target height, in world-height meters, used by the SetHeight mode (moves terrain exactly to this height within the brush footprint). Clamped to the current terrain's max height.")]
        public float setHeightTarget = 0f;

        [Header("Brush Preset")]
        [Tooltip("Optional brush 'type' asset. Provides falloff shape and optional local radius/strength overrides plus bound terrain-texture channels.")]
        public BrushPreset activeBrushPreset;

        [Header("Height Invert")]
        [Tooltip("Persistent toggle: when enabled, Raise/Lower directions are swapped.")]
        public bool invertHeight;
        [Tooltip("Held to temporarily invert height direction in addition to the persistent toggle.")]
        public KeyCode invertModifierKey = KeyCode.LeftAlt;
        [Tooltip("Held to temporarily force Smooth mode regardless of the currently selected brush mode.")]
        public KeyCode smoothModifierKey = KeyCode.LeftShift;

        [Header("Stamp Preset")]
        [Tooltip("Optional stamp 'type' asset used by PlaceStampAt/PlaceStampAtCurrentPosition.")]
        public StampPreset activeStampPreset;
        [Tooltip("Global multiplier applied once, at the moment a stamp is placed (like brush strength, but one-shot). Negative values indent instead of raise.")]
        [Range(-1f, 1f)]
        public float stampHeightMultiplier = 1f;
        [Tooltip("Additional rotation (degrees, around Y) layered on top of the active stamp preset's startRotation. Adjusted via the stamp rotation slider or the stamp transform gizmo.")]
        public float stampRotationOffset = 0f;
        [Tooltip("Absolute multiplier applied to the active stamp preset's startScale (never negative). Adjusted via the stamp scale slider or the stamp transform gizmo.")]
        public float stampScaleMultiplier = 1f;

        // -- Stamp gizmo placement state: the world position a stamp is currently 'parked' at while its
        // rotation/scale/height are being adjusted via UI sliders or the stamp transform gizmo, prior to
        // being committed (placed) onto the terrain. --
        public Vector3 StampGizmoPosition { get; private set; }
        public bool HasStampGizmoPosition { get; private set; }

        /// <summary>
        /// A persistent, additive stamp placement. Unlike the legacy one-shot <see cref="PlaceStampAt"/>
        /// flow, instances remain editable (moved/rotated/scaled/re-heighted or removed) at any time -
        /// each change first subtracts the instance's previously-applied contribution from the terrain(s)
        /// it touched, then re-bakes a fresh contribution from its current transform state.
        /// </summary>
        public class StampInstance
        {
            public string id;
            public StampPreset preset;
            public Vector3 position;
            /// <summary>Additional rotation (degrees, around Y) layered on top of the preset's startRotation.</summary>
            public float rotationOffset;
            /// <summary>Absolute multiplier (never negative) applied to the preset's startScale.</summary>
            public float scaleMultiplier = 1f;
            /// <summary>Global multiplier applied to the preset's height texture/defaultMaxHeight. Negative values indent.</summary>
            public float heightMultiplier = 1f;
            public Dictionary<Terrain, StampContribution> contributions = new Dictionary<Terrain, StampContribution>();
        }

        /// <summary>The exact height delta a single <see cref="StampInstance"/> has added to a single terrain's heightmap rectangle, so it can be precisely subtracted later (on move/edit/remove) without affecting other overlapping stamps or brush edits.</summary>
        public class StampContribution
        {
            public int startX, startZ, width, height;
            public float[,] delta;
        }

        private readonly List<StampInstance> stampInstances = new List<StampInstance>();
        public IReadOnlyList<StampInstance> StampInstances => stampInstances;
        public StampInstance SelectedStampInstance { get; private set; }

        [Header("Layer Mask")]
        public LayerMask terrainLayer;

        [Header("World Integration")]
        [Tooltip("Optional. If unassigned, will attempt to use World.Instance.worldEditSystem.")]
        public WorldEdit worldEditSystem;

        // -- Brush preview state (read by external systems, e.g. gizmo drawers) --
        public Vector3 CurrentBrushWorldPosition { get; private set; }
        public Vector3 CurrentBrushWorldNormal { get; private set; } = Vector3.up;
        public bool HasValidBrushPosition { get; private set; }
        public float BrushRadius => ResolveBrushRadius();
        /// <summary>The terrain instance currently under the brush preview (if any), used to resolve terrain-specific max height for UI clamping.</summary>
        public Terrain CurrentBrushTerrain { get; private set; }

        // -- Runtime channel state (per texture channel, shared by brush and stamp) --
        private readonly Dictionary<TerrainTextureChannel, TerrainTextureChannelState> brushChannelStates = new Dictionary<TerrainTextureChannel, TerrainTextureChannelState>();
        private readonly Dictionary<TerrainTextureChannel, TerrainTextureChannelState> stampChannelStates = new Dictionary<TerrainTextureChannel, TerrainTextureChannelState>();

        // -- Stroke tracking for undo --
        private bool strokeActive;

        /// <summary>
        /// Tracks, per terrain touched during the active stroke, the bounding rectangle of heightmap
        /// pixels that have been modified so far along with a snapshot of their original (pre-stroke)
        /// values. Only this (typically small) rectangle is stored rather than the full heightmap, to
        /// keep undo memory usage proportional to the area actually edited.
        ///
        /// <see cref="working"/> is a full-precision (not yet quantized to Unity's internal 16-bit
        /// heightmap format) running accumulation buffer used as the source of truth for the stroke.
        /// Deltas are always applied to this buffer rather than to values re-read from the terrain
        /// (which are quantized to 1/65535ths on every <see cref="TerrainData.SetHeights"/> call), so
        /// small per-frame deltas don't get lost/rounded inconsistently across neighboring vertices -
        /// which is what causes visible stepping/terracing artifacts during continuous brush strokes.
        /// The terrain itself is still updated every frame (from <see cref="working"/>, clamped to
        /// 0-1) purely for visual feedback.
        /// </summary>
        private class StrokeSnapshot
        {
            public int minX, minZ, maxX, maxZ;
            public float[,] before;
            public float[,] working;
        }
        private readonly Dictionary<Terrain, StrokeSnapshot> strokeSnapshots = new Dictionary<Terrain, StrokeSnapshot>();

        /// <summary>Tracks per-terrain alphamap (texture) snapshots for the active stroke, mirroring StrokeSnapshot for heights.</summary>
        private class AlphamapStrokeSnapshot
        {
            public int minX, minZ, maxX, maxZ;
            public float[,,] before;
        }
        private readonly Dictionary<Terrain, AlphamapStrokeSnapshot> alphamapStrokeSnapshots = new Dictionary<Terrain, AlphamapStrokeSnapshot>();

        private float? strokeFlattenHeight;

        /// <summary>
        /// True once the smoothing modifier has been held at any point during the active stroke. Once
        /// latched, the stroke stays in Smooth mode even if the modifier key is released while the
        /// sculpt button is still held, so a stroke never "jumps" back to raising/lowering terrain
        /// mid-drag. The latch is cleared when a new stroke begins (i.e. on the next press).
        /// </summary>
        private bool strokeSmoothLatched;

        void Update()
        {
            UpdateBrushPreview();
        }

        #region UI Bindings
        // Plain setter methods (rather than only public fields) so Unity UI components
        // (Slider.onValueChanged, Dropdown.onValueChanged, Toggle.onValueChanged, Button.onClick)
        // can bind directly via the Inspector without extra glue code.

        public void SetBrushMode(BrushMode newMode) => mode = newMode;
        /// <summary>Binds to Dropdown.onValueChanged (int index matching BrushMode enum order).</summary>
        public void SetBrushMode(int modeIndex) => mode = (BrushMode)modeIndex;

        /// <summary>Binds to Slider.onValueChanged.</summary>
        public void SetBrushRadius(float radius) => brushRadius = Mathf.Max(0.01f, radius);
        /// <summary>Binds to Slider.onValueChanged.</summary>
        public void SetBrushStrength(float strength) => brushStrength = Mathf.Max(0f, strength);
        /// <summary>Binds to Slider.onValueChanged. Sets the Smooth mode's independent convergence speed.</summary>
        public void SetSmoothStrength(float strength) => smoothStrength = Mathf.Max(0f, strength);
        /// <summary>Binds to Slider.onValueChanged. Pass a negative value to disable the fixed target and use the brush-radius average height instead.</summary>
        public void SetFlattenTargetHeight(float normalizedHeight) => flattenTargetHeight = normalizedHeight;
        /// <summary>Binds to Slider.onValueChanged. Value is in world-height meters, clamped to the current terrain's max height.</summary>
        public void SetSetHeightTarget(float meters) => setHeightTarget = Mathf.Clamp(meters, 0f, GetMaxTerrainHeight());

        /// <summary>Returns the max terrain height (TerrainData.size.y), in meters, for the terrain currently under the brush (or any active terrain / a sane default as a fallback). Used to clamp/scale the SetHeight UI slider.</summary>
        public float GetMaxTerrainHeight()
        {
            if (CurrentBrushTerrain != null) return Mathf.Max(0.01f, CurrentBrushTerrain.terrainData.size.y);
            Terrain any = Terrain.activeTerrain;
            if (any != null) return Mathf.Max(0.01f, any.terrainData.size.y);
            return 500f;
        }

        public BrushMode GetBrushMode() => mode;
        public float GetBrushRadius() => brushRadius;
        public float GetBrushStrength() => brushStrength;

        /// <summary>Binds to Toggle.onValueChanged. Persistent invert toggle (combines with the Alt-key modifier).</summary>
        public void SetInvertHeight(bool invert) => invertHeight = invert;
        public bool GetInvertHeight() => invertHeight;

        /// <summary>Binds to a brush-preset picker (e.g. a dropdown populated from a BrushPreset[] list).</summary>
        public void SetActiveBrushPreset(BrushPreset preset)
        {
            activeBrushPreset = preset;
            brushChannelStates.Clear();
        }
        public BrushPreset GetActiveBrushPreset() => activeBrushPreset;

        /// <summary>Binds to a stamp-preset picker.</summary>
        public void SetActiveStampPreset(StampPreset preset)
        {
            activeStampPreset = preset;
            stampChannelStates.Clear();
        }
        public StampPreset GetActiveStampPreset() => activeStampPreset;

        /// <summary>Binds to Slider.onValueChanged for the stamp placement's one-shot global height multiplier. Negative values indent instead of raise.</summary>
        public void SetStampHeightMultiplier(float multiplier) => stampHeightMultiplier = Mathf.Clamp(multiplier, -1f, 1f);
        public float GetStampHeightMultiplier() => stampHeightMultiplier;

        /// <summary>Binds to Slider.onValueChanged for the stamp placement's rotation offset (degrees, around Y), layered on top of the stamp preset's own startRotation.</summary>
        public void SetStampRotationOffset(float degrees) => stampRotationOffset = degrees;
        public float GetStampRotationOffset() => stampRotationOffset;

        /// <summary>Binds to Slider.onValueChanged for the stamp placement's absolute scale multiplier (never negative), layered on top of the stamp preset's own startScale.</summary>
        public void SetStampScaleMultiplier(float scale) => stampScaleMultiplier = Mathf.Max(0f, scale);
        public float GetStampScaleMultiplier() => stampScaleMultiplier;

        /// <summary>Parks the active stamp at a world position so its rotation/scale/height can be tuned (via UI sliders or the stamp transform gizmo) before being committed with <see cref="PlaceStampAtCurrentPosition"/>/<see cref="PlaceStampAt(Vector3, StampPreset, out List{TerrainAlphamapEdit})"/>.</summary>
        public void SetStampGizmoPosition(Vector3 worldPosition)
        {
            StampGizmoPosition = worldPosition;
            HasStampGizmoPosition = true;
        }
        /// <summary>Clears the current stamp gizmo placement (e.g. cancel).</summary>
        public void ClearStampGizmoPosition() => HasStampGizmoPosition = false;

        /// <summary>Binds to a per-channel Toggle for a brush texture channel.</summary>
        public void SetBrushChannelEnabled(TerrainTextureChannel channel, bool enabled)
        {
            var state = GetOrCreateChannelState(brushChannelStates, channel);
            if (state != null) state.enabled = enabled;
        }
        /// <summary>Binds to a per-channel Slider for a brush texture channel's local mix value.</summary>
        public void SetBrushChannelMix(TerrainTextureChannel channel, float mix)
        {
            var state = GetOrCreateChannelState(brushChannelStates, channel);
            if (state != null) state.mix = Mathf.Clamp01(mix);
        }
        public TerrainTextureChannelState GetBrushChannelState(TerrainTextureChannel channel) => GetOrCreateChannelState(brushChannelStates, channel);

        /// <summary>Binds to a per-channel Toggle for a stamp texture channel.</summary>
        public void SetStampChannelEnabled(TerrainTextureChannel channel, bool enabled)
        {
            var state = GetOrCreateChannelState(stampChannelStates, channel);
            if (state != null) state.enabled = enabled;
        }
        /// <summary>Binds to a per-channel Slider for a stamp texture channel's local mix value.</summary>
        public void SetStampChannelMix(TerrainTextureChannel channel, float mix)
        {
            var state = GetOrCreateChannelState(stampChannelStates, channel);
            if (state != null) state.mix = Mathf.Clamp01(mix);
        }
        public TerrainTextureChannelState GetStampChannelState(TerrainTextureChannel channel) => GetOrCreateChannelState(stampChannelStates, channel);

        private static TerrainTextureChannelState GetOrCreateChannelState(Dictionary<TerrainTextureChannel, TerrainTextureChannelState> states, TerrainTextureChannel channel)
        {
            if (channel == null) return null;
            if (!states.TryGetValue(channel, out TerrainTextureChannelState state))
            {
                state = new TerrainTextureChannelState(channel);
                states[channel] = state;
            }
            return state;
        }
        #endregion

        /// <summary>Combines the active brush preset's local radius override (if any) with the global brush radius.</summary>
        float ResolveBrushRadius() => activeBrushPreset != null ? activeBrushPreset.ResolveRadius(brushRadius) : brushRadius;

        /// <summary>Combines the active brush preset's local strength override (if any) with the global brush strength.</summary>
        float ResolveBrushStrength() => activeBrushPreset != null ? activeBrushPreset.ResolveStrength(brushStrength) : brushStrength;

        /// <summary>Resolves the effective per-mode strength: Smooth mode uses its own independent <see cref="smoothStrength"/> so smoothing can be tuned separately from raise/lower/flatten strength.</summary>
        float ResolveEffectiveStrength(BrushMode effectiveMode) => effectiveMode == BrushMode.Smooth ? smoothStrength : ResolveBrushStrength();

        /// <summary>True while the smoothing modifier key (Left Shift by default) is held, temporarily forcing Smooth mode.</summary>
        bool IsSmoothModifierHeld() => Input.GetKey(smoothModifierKey);

        /// <summary>True while the invert modifier key (Left Alt by default) is held.</summary>
        bool IsInvertModifierHeld() => Input.GetKey(invertModifierKey);

        /// <summary>Resolves the effective brush mode for this frame, accounting for the temporary Smooth modifier.
        /// Once the modifier has been held during a stroke, the stroke stays latched to Smooth mode for its
        /// remaining duration even if the key is released, so releasing the modifier mid-stroke never
        /// switches to raising/lowering terrain until the next stroke begins.</summary>
        BrushMode ResolveEffectiveMode()
        {
            if (IsSmoothModifierHeld()) strokeSmoothLatched = true;
            return strokeSmoothLatched ? BrushMode.Smooth : mode;
        }

        public void ResetEffectiveMode()
        {
            strokeSmoothLatched = false;
        }

        /// <summary>True if the brush is currently (or, mid-stroke, latched to) Smooth mode - either because the modifier key is held right now, or because it was held earlier in the active stroke. Used by UI to reflect the mode being force-activated by other logic (e.g. the smooth modifier key) rather than an explicit mode-button click.</summary>
        public bool IsEffectivelySmoothing() => strokeSmoothLatched || IsSmoothModifierHeld();

        /// <summary>Resolves whether height changes should be inverted this frame (persistent toggle XOR modifier key).</summary>
        bool ResolveEffectiveInvert() => invertHeight ^ IsInvertModifierHeld();

        /// <summary>
        /// Raycasts against the terrain layer and updates the current brush preview position/normal.
        /// Safe to call every frame independent of whether a stroke is active.
        /// </summary>
        public bool UpdateBrushPreview()
        {
            if (Camera.main == null)
            {
                HasValidBrushPosition = false;
                return false;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayer))
            {
                CurrentBrushWorldPosition = hit.point;
                CurrentBrushWorldNormal = hit.normal;
                CurrentBrushTerrain = hit.collider != null ? hit.collider.GetComponent<Terrain>() : null;
                HasValidBrushPosition = true;
                return true;
            }

            HasValidBrushPosition = false;
            return false;
        }

        /// <summary>
        /// Begins tracking a sculpting stroke. Call once when the user starts holding the brush input.
        /// </summary>
        public void BeginStroke()
        {
            strokeActive = true;
            strokeFlattenHeight = flattenTargetHeight >= 0f ? flattenTargetHeight : (float?)null;
            strokeSmoothLatched = IsSmoothModifierHeld();
            strokeSnapshots.Clear();
            alphamapStrokeSnapshots.Clear();
        }

        /// <summary>
        /// Applies the brush at the current preview position (or a supplied world position). Should be
        /// called every frame/tick while the stroke is held.
        /// </summary>
        public void ApplyBrushAtCurrentPosition()
        {
            if (!HasValidBrushPosition) return;
            ApplyBrushAt(CurrentBrushWorldPosition);
        }

        public void ApplyBrushAt(Vector3 brushWorldCenter)
        {
            float radius = ResolveBrushRadius();
            Collider[] hitTerrains = Physics.OverlapSphere(brushWorldCenter, radius, terrainLayer);

            // Collected up-front so DeformSingleTerrainInstance can tell whether a shared-edge neighbor is
            // ALSO being processed this call (see skipEastEdge/skipNorthEdge there) - a neighbor only
            // "handles" the shared seam vertex itself if it's actually being touched this frame, which
            // isn't always true since the brush is rarely centered exactly on a seam.
            var terrainsHitThisCall = new HashSet<Terrain>();
            foreach (var col in hitTerrains)
            {
                Terrain terrain = col.GetComponent<Terrain>();
                if (terrain != null) terrainsHitThisCall.Add(terrain);
            }

            foreach (var terrain in terrainsHitThisCall)
            {
                DeformSingleTerrainInstance(terrain, brushWorldCenter, radius, terrainsHitThisCall);
                if (activeBrushPreset != null && activeBrushPreset.textureChannels != null && activeBrushPreset.textureChannels.Length > 0)
                {
                    PaintChannelsSingleTerrainInstance(terrain, brushWorldCenter, radius, activeBrushPreset.textureChannels, brushChannelStates); 
                }
            }
        }

        /// <summary>
        /// Places the active stamp preset at the current preview position, if one is assigned and the
        /// brush preview currently has a valid hit.
        /// </summary>
        public List<TerrainHeightEdit> PlaceStampAtCurrentPosition(out List<TerrainAlphamapEdit> alphamapEdits)
        {
            alphamapEdits = new List<TerrainAlphamapEdit>();
            if (activeStampPreset == null) return new List<TerrainHeightEdit>();

            Vector3 worldCenter;
            if (HasStampGizmoPosition) worldCenter = StampGizmoPosition;
            else if (HasValidBrushPosition) worldCenter = CurrentBrushWorldPosition;
            else return new List<TerrainHeightEdit>();

            return PlaceStampAt(worldCenter, activeStampPreset, out alphamapEdits);
        }

        /// <summary>
        /// Places a stamp footprint centered at <paramref name="worldCenter"/>, applying its height texture
        /// (scaled by the stamp's default max height and the one-shot <see cref="stampHeightMultiplier"/>),
        /// layering the current <see cref="stampRotationOffset"/>/<see cref="stampScaleMultiplier"/> on top of
        /// the stamp preset's own startRotation/startScale, and any bound texture channels. Returns undo-able
        /// edits for the caller to record.
        /// </summary>
        public List<TerrainHeightEdit> PlaceStampAt(Vector3 worldCenter, StampPreset stamp, out List<TerrainAlphamapEdit> alphamapEdits)
            => PlaceStampAt(worldCenter, stamp, stampRotationOffset, stampScaleMultiplier, stampHeightMultiplier, out alphamapEdits);

        /// <summary>
        /// Places a stamp footprint centered at <paramref name="worldCenter"/> using explicit rotation/scale/height
        /// overrides rather than the instance's current slider/gizmo state. <paramref name="scaleMultiplier"/> is
        /// always treated as an absolute (non-negative) value; <paramref name="heightMultiplier"/> may be negative
        /// to indent instead of raise.
        /// </summary>
        public List<TerrainHeightEdit> PlaceStampAt(Vector3 worldCenter, StampPreset stamp, float rotationOffsetDegrees, float scaleMultiplier, float heightMultiplier, out List<TerrainAlphamapEdit> alphamapEdits)
        {
            var heightEdits = new List<TerrainHeightEdit>();
            alphamapEdits = new List<TerrainAlphamapEdit>();
            if (stamp == null || stamp.heightTexture == null) return heightEdits;

            scaleMultiplier = Mathf.Max(0f, scaleMultiplier);
            Vector3 effectiveScale = stamp.startScale * scaleMultiplier;
            Quaternion effectiveRotation = stamp.startRotation * Quaternion.Euler(0f, rotationOffsetDegrees, 0f);

            float footprintRadius = 0.5f * Mathf.Max(effectiveScale.x, effectiveScale.z);
            Collider[] hitTerrains = Physics.OverlapSphere(worldCenter, footprintRadius, terrainLayer);

            foreach (var col in hitTerrains)
            {
                Terrain terrain = col.GetComponent<Terrain>();
                if (terrain == null) continue;

                var heightEdit = StampSingleTerrainInstance(terrain, worldCenter, stamp, effectiveRotation, effectiveScale, heightMultiplier);
                if (heightEdit.HasValue) heightEdits.Add(heightEdit.Value);

                if (stamp.textureChannels != null && stamp.textureChannels.Length > 0)
                {
                    var alphaEdit = PaintChannelsSingleTerrainInstanceWithUndo(terrain, worldCenter, footprintRadius, stamp.textureChannels, stampChannelStates);
                    if (alphaEdit.HasValue) alphamapEdits.Add(alphaEdit.Value);
                }
            }

            return heightEdits;
        }

        #region Persistent Stamp Instances
        /// <summary>
        /// Creates a new persistent stamp instance at <paramref name="worldPosition"/> using the given
        /// preset (or <see cref="activeStampPreset"/> if null), selects it, and bakes its initial terrain
        /// contribution. Returns null if no preset is available.
        /// </summary>
        public StampInstance CreateStampInstance(Vector3 worldPosition, StampPreset preset = null)
        {
            preset ??= activeStampPreset;
            if (preset == null) return null;

            var instance = new StampInstance
            {
                id = System.Guid.NewGuid().ToString("N"),
                preset = preset,
                position = worldPosition,
                rotationOffset = stampRotationOffset,
                scaleMultiplier = Mathf.Max(0f, stampScaleMultiplier),
                heightMultiplier = stampHeightMultiplier
            };

            stampInstances.Add(instance);
            SelectedStampInstance = instance;
            RebakeStampInstance(instance);
            return instance;
        }

        /// <summary>Selects an existing stamp instance so subsequent slider/gizmo edits target it.</summary>
        public void SelectStampInstance(StampInstance instance) => SelectedStampInstance = instance;

        /// <summary>Deselects the current stamp instance (e.g. clicking empty space).</summary>
        public void DeselectStampInstance() => SelectedStampInstance = null;

        /// <summary>Returns the nearest stamp instance to <paramref name="worldPosition"/> within <paramref name="maxDistance"/>, or null.</summary>
        public StampInstance FindNearestStampInstance(Vector3 worldPosition, float maxDistance)
        {
            StampInstance best = null;
            float bestDist = maxDistance;
            foreach (var inst in stampInstances)
            {
                float d = Vector3.Distance(new Vector3(inst.position.x, worldPosition.y, inst.position.z), new Vector3(worldPosition.x, worldPosition.y, worldPosition.z));
                d = Vector2.Distance(new Vector2(inst.position.x, inst.position.z), new Vector2(worldPosition.x, worldPosition.z));
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = inst;
                }
            }
            return best;
        }

        /// <summary>Moves the given (or currently selected) stamp instance and re-bakes its terrain contribution.</summary>
        public void SetStampInstancePosition(Vector3 worldPosition, StampInstance instance = null)
        {
            instance ??= SelectedStampInstance;
            if (instance == null) return;
            instance.position = worldPosition;
            RebakeStampInstance(instance);
        }

        /// <summary>Sets rotation offset (degrees) on the given (or currently selected) stamp instance and re-bakes.</summary>
        public void SetStampInstanceRotation(float rotationOffsetDegrees, StampInstance instance = null)
        {
            instance ??= SelectedStampInstance;
            if (instance == null) return;
            instance.rotationOffset = rotationOffsetDegrees;
            RebakeStampInstance(instance);
        }

        /// <summary>Sets the absolute (never negative) scale multiplier on the given (or currently selected) stamp instance and re-bakes.</summary>
        public void SetStampInstanceScale(float scaleMultiplier, StampInstance instance = null)
        {
            instance ??= SelectedStampInstance;
            if (instance == null) return;
            instance.scaleMultiplier = Mathf.Max(0f, scaleMultiplier);
            RebakeStampInstance(instance);
        }

        /// <summary>Sets the (possibly negative, to indent) height multiplier on the given (or currently selected) stamp instance and re-bakes.</summary>
        public void SetStampInstanceHeight(float heightMultiplier, StampInstance instance = null)
        {
            instance ??= SelectedStampInstance;
            if (instance == null) return;
            instance.heightMultiplier = heightMultiplier;
            RebakeStampInstance(instance);
        }

        /// <summary>Removes a stamp instance, subtracting its terrain contribution first. If null, removes the currently selected instance.</summary>
        public void RemoveStampInstance(StampInstance instance = null)
        {
            instance ??= SelectedStampInstance;
            if (instance == null) return;

            RemoveStampContribution(instance);
            stampInstances.Remove(instance);
            if (SelectedStampInstance == instance) SelectedStampInstance = null;
        }

        /// <summary>Subtracts a stamp instance's previously-applied contribution (if any) from every terrain it touched.</summary>
        void RemoveStampContribution(StampInstance instance)
        {
            foreach (var kvp in instance.contributions)
            {
                Terrain terrain = kvp.Key;
                if (terrain == null) continue;
                StampContribution contribution = kvp.Value;

                TerrainData terrainData = terrain.terrainData;
                float[,] current = terrainData.GetHeights(contribution.startX, contribution.startZ, contribution.width, contribution.height);
                for (int z = 0; z < contribution.height; z++)
                {
                    for (int x = 0; x < contribution.width; x++)
                    {
                        current[z, x] = Mathf.Clamp01(current[z, x] - contribution.delta[z, x]);
                    }
                }
                terrainData.SetHeights(contribution.startX, contribution.startZ, current);
                RecordDeltas(terrain, contribution.startX, contribution.startZ, contribution.width, contribution.height, current);
                PropagateEdgeToNeighbors(terrain, contribution.startX, contribution.startZ, contribution.width, contribution.height, current);
            }
            instance.contributions.Clear();
        }

        /// <summary>
        /// Removes the instance's current contribution (if any) and re-applies it from scratch using its
        /// present position/rotation/scale/height, so any live edit (move/rotate/scale/height/selection
        /// slider) is immediately reflected on the terrain. Called automatically by the Set* methods above.
        /// </summary>
        public void RebakeStampInstance(StampInstance instance)
        {
            if (instance == null || instance.preset == null || instance.preset.heightTexture == null) return;

            RemoveStampContribution(instance);

            float scaleMultiplier = Mathf.Max(0f, instance.scaleMultiplier);
            Vector3 effectiveScale = instance.preset.startScale * scaleMultiplier;
            Quaternion effectiveRotation = instance.preset.startRotation * Quaternion.Euler(0f, instance.rotationOffset, 0f);

            float footprintRadius = 0.5f * Mathf.Max(effectiveScale.x, effectiveScale.z);
            Collider[] hitTerrains = Physics.OverlapSphere(instance.position, footprintRadius, terrainLayer);

            foreach (var col in hitTerrains)
            {
                Terrain terrain = col.GetComponent<Terrain>();
                if (terrain == null) continue;

                var contribution = StampSingleTerrainInstanceAdditive(terrain, instance.position, instance.preset, effectiveRotation, effectiveScale, instance.heightMultiplier);
                if (contribution != null) instance.contributions[terrain] = contribution;

                if (instance.preset.textureChannels != null && instance.preset.textureChannels.Length > 0)
                {
                    PaintChannelsSingleTerrainInstance(terrain, instance.position, footprintRadius, instance.preset.textureChannels, stampChannelStates);
                }
            }
        }
        #endregion


        /// <summary>
        /// Ends the current stroke and returns undo-able edits for every terrain that was touched.
        /// The caller (e.g. a world editor orchestrator) is responsible for recording these into an
        /// undo system.
        /// </summary>
        public List<TerrainHeightEdit> EndStroke()
        {
            var edits = new List<TerrainHeightEdit>();

            foreach (var kvp in strokeSnapshots)
            {
                Terrain terrain = kvp.Key;
                StrokeSnapshot snap = kvp.Value;

                int width = snap.maxX - snap.minX + 1;
                int height = snap.maxZ - snap.minZ + 1;
                float[,] after = terrain.terrainData.GetHeights(snap.minX, snap.minZ, width, height);

                edits.Add(new TerrainHeightEdit(terrain, snap.minX, snap.minZ, snap.before, after));

                // Defensive final reconciliation: live per-frame edge propagation (in DeformSingleTerrainInstance/
                // FlattenSingleTerrainInstance/StampSingleTerrainInstance) keeps neighbors in sync during the
                // stroke, but re-push the final, authoritative border values here too so any missed frame
                // (e.g. a neighbor that wasn't loaded yet, or a resolution mismatch that resolved mid-stroke)
                // can never leave a lingering seam once the stroke ends.
                ReconcileTouchedEdges(terrain, snap.minX, snap.minZ, width, height, after);
            }


            strokeActive = false;
            strokeFlattenHeight = null;
            strokeSnapshots.Clear();
            alphamapStrokeSnapshots.Clear();

            ResetEffectiveMode();

            return edits;
        }

        /// <summary>
        /// Ends the current stroke's texture channel painting and returns undo-able alphamap edits.
        /// Call alongside/after <see cref="EndStroke"/> when the active brush preset has texture channels.
        /// </summary>
        public List<TerrainAlphamapEdit> EndStrokeAlphamaps()
        {
            var edits = new List<TerrainAlphamapEdit>();

            foreach (var kvp in alphamapStrokeSnapshots)
            {
                Terrain terrain = kvp.Key;
                AlphamapStrokeSnapshot snap = kvp.Value;

                int width = snap.maxX - snap.minX + 1;
                int height = snap.maxZ - snap.minZ + 1;
                float[,,] after = terrain.terrainData.GetAlphamaps(snap.minX, snap.minZ, width, height);

                edits.Add(new TerrainAlphamapEdit(terrain, snap.minX, snap.minZ, snap.before, after));
            }

            return edits;
        }

        /// <summary>
        /// Blends terrain heights toward a flattened profile along a path of world positions. Used
        /// by systems that place linear/area features (e.g. rivers, lakes) so the terrain settles
        /// around the generated mesh. Kept generic (Vector3-based) so callers don't need to expose
        /// their own spline/mesh types to this system.
        /// </summary>
        public void FlattenAlongPath(IList<Vector3> worldPositions, float width)
        {
            if (worldPositions == null || worldPositions.Count == 0) return;

            float radius = Mathf.Max(0.01f, width * 0.5f);

            foreach (var point in worldPositions)
            {
                Collider[] hitTerrains = Physics.OverlapSphere(point, radius, terrainLayer);
                foreach (var col in hitTerrains)
                {
                    Terrain terrain = col.GetComponent<Terrain>();
                    if (terrain == null) continue;

                    FlattenSingleTerrainInstance(terrain, point, radius, point.y);
                }
            }
        }

        /// <summary>Cheap pre-check so callers can skip the neighbor-propagation work entirely for the common
        /// case of brush edits that don't touch any terrain border pixel.</summary>
        static bool TouchesAnyBorder(int startX, int startZ, int width, int height, int res)
            => startX == 0 || startZ == 0 || (startX + width) == res || (startZ + height) == res;

        /// <summary>Cheap pre-check so callers can skip the neighbor-propagation work entirely when a
        /// terrain has no linked neighbors at all (e.g. isolated chunk, or neighbors not yet streamed in).</summary>
        static bool HasAnyNeighbor(Terrain terrain)
            => terrain.leftNeighbor != null || terrain.rightNeighbor != null || terrain.topNeighbor != null || terrain.bottomNeighbor != null;

        /// <summary>
        /// Unity only auto-stitches neighboring terrains' shared edge for rendering normals; it does NOT
        /// keep the actual heightmap values in sync, so if a brush stroke only touches one side of a
        /// chunk boundary the two terrains' edge vertices can end up at different heights - a seam. This
        /// copies any edited heightmap pixels lying on this terrain's border directly into the matching
        /// edge of same-resolution neighbors (only same-resolution tiers share an exact 1:1 vertex
        /// mapping along the border; cross-tier/far seams are handled separately via edge quantization).
        /// Called every frame a brush edit touches a border pixel, so neighbors stay in sync live during
        /// the stroke rather than only at the end.
        /// </summary>
        void PropagateEdgeToNeighbors(Terrain terrain, int startX, int startZ, int width, int height, float[,] editedHeights)
        {
            int res = terrain.terrainData.heightmapResolution;
            if (!TouchesAnyBorder(startX, startZ, width, height, res)) return; 
            if (!HasAnyNeighbor(terrain)) return;

            int endXExclusive = startX + width;
            int endZExclusive = startZ + height;

            // West edge (x == 0) -> neighbor's east edge (x == res - 1)
            if (startX == 0 && terrain.leftNeighbor != null && terrain.leftNeighbor.terrainData.heightmapResolution == res)
            {
                CopyColumnToNeighbor(editedHeights, 0, startZ, height, terrain.leftNeighbor, res - 1);
            }

            // East edge (x == res - 1) -> neighbor's west edge (x == 0)
            if (endXExclusive == res && terrain.rightNeighbor != null && terrain.rightNeighbor.terrainData.heightmapResolution == res)
            {
                CopyColumnToNeighbor(editedHeights, width - 1, startZ, height, terrain.rightNeighbor, 0);
            }

            // South edge (z == 0) -> neighbor's north edge (z == res - 1)
            if (startZ == 0 && terrain.bottomNeighbor != null && terrain.bottomNeighbor.terrainData.heightmapResolution == res)
            {
                CopyRowToNeighbor(editedHeights, 0, startX, width, terrain.bottomNeighbor, res - 1);
            }

            // North edge (z == res - 1) -> neighbor's south edge (z == 0)
            if (endZExclusive == res && terrain.topNeighbor != null && terrain.topNeighbor.terrainData.heightmapResolution == res)
            {
                CopyRowToNeighbor(editedHeights, height - 1, startX, width, terrain.topNeighbor, 0);
            }
        }

        /// <summary>
        /// Defensive final pass called from <see cref="EndStroke"/>: re-applies the same border-copy logic
        /// as <see cref="PropagateEdgeToNeighbors"/> using the stroke's final authoritative heights, so any
        /// edge frame that was missed during the live per-frame propagation (e.g. a neighbor chunk that
        /// streamed in mid-stroke) is guaranteed to be reconciled once the stroke completes.
        /// </summary>
        void ReconcileTouchedEdges(Terrain terrain, int startX, int startZ, int width, int height, float[,] finalHeights)
        {
            PropagateEdgeToNeighbors(terrain, startX, startZ, width, height, finalHeights);
        }

        /// <summary>Copies a single column (constant x) out of a local [z, x] edited-heights array into a neighbor terrain at the given x column, matching z indices 1:1.</summary>
        void CopyColumnToNeighbor(float[,] editedHeights, int localX, int startZ, int count, Terrain neighbor, int neighborX)
        {
            int neighborRes = neighbor.terrainData.heightmapResolution;
            int clampedStartZ = Mathf.Clamp(startZ, 0, neighborRes - 1);
            int clampedCount = Mathf.Min(count, neighborRes - clampedStartZ);
            if (clampedCount <= 0) return;

            float[,] column = new float[clampedCount, 1];
            for (int i = 0; i < clampedCount; i++) column[i, 0] = editedHeights[i, localX];

            neighbor.terrainData.SetHeights(neighborX, clampedStartZ, column);

            // The neighbor may have its own active stroke working-buffer (StrokeSnapshot.working) covering
            // this column, used as the source of truth for its OWN edits (see DeformSingleTerrainInstance).
            // SetHeights above only updates the neighbor's real TerrainData, not that buffer, so unless we
            // patch it too, the neighbor's next edit pass reads its now-stale working value, re-derives a
            // result from it, and writes that back over the just-propagated value here - reverting this
            // update. This is what caused stale/reverted seam vertices to reappear mid-stroke once the
            // brush moved and the "ownership" of an edge column flipped between terrains. Patch it directly
            // so both sides' working buffers agree with the real terrain data at every propagated pixel.
            if (strokeSnapshots.TryGetValue(neighbor, out StrokeSnapshot neighborSnap))
            {
                for (int i = 0; i < clampedCount; i++)
                {
                    int z = clampedStartZ + i;
                    if (z < neighborSnap.minZ || z > neighborSnap.maxZ || neighborX < neighborSnap.minX || neighborX > neighborSnap.maxX) continue;
                    neighborSnap.working[z - neighborSnap.minZ, neighborX - neighborSnap.minX] = column[i, 0];
                }
            }
        }

        /// <summary>Copies a single row (constant z) out of a local [z, x] edited-heights array into a neighbor terrain at the given z row, matching x indices 1:1.</summary>
        void CopyRowToNeighbor(float[,] editedHeights, int localZ, int startX, int count, Terrain neighbor, int neighborZ)
        {
            int neighborRes = neighbor.terrainData.heightmapResolution;
            int clampedStartX = Mathf.Clamp(startX, 0, neighborRes - 1);
            int clampedCount = Mathf.Min(count, neighborRes - clampedStartX);
            if (clampedCount <= 0) return;

            float[,] row = new float[1, clampedCount];
            for (int i = 0; i < clampedCount; i++) row[0, i] = editedHeights[localZ, i];

            neighbor.terrainData.SetHeights(clampedStartX, neighborZ, row);

            // See the matching comment in CopyColumnToNeighbor - keep the neighbor's own active stroke
            // working buffer in sync with what was just propagated into its real TerrainData.
            if (strokeSnapshots.TryGetValue(neighbor, out StrokeSnapshot neighborSnap))
            {
                for (int i = 0; i < clampedCount; i++)
                {
                    int x = clampedStartX + i;
                    if (x < neighborSnap.minX || x > neighborSnap.maxX || neighborZ < neighborSnap.minZ || neighborZ > neighborSnap.maxZ) continue; 
                    neighborSnap.working[neighborZ - neighborSnap.minZ, x - neighborSnap.minX] = row[0, i];
                }
            }
        }

        void DeformSingleTerrainInstance(Terrain terrain, Vector3 brushWorldCenter, float radius, HashSet<Terrain> terrainsHitThisCall = null)
        {
            TerrainData terrainData = terrain.terrainData;
            int res = terrainData.heightmapResolution;

            Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(brushWorldCenter);

            float normalizedX = terrainLocalPos.x / terrainData.size.x;
            float normalizedZ = terrainLocalPos.z / terrainData.size.z;

            int mapX = Mathf.FloorToInt(normalizedX * res);
            int mapZ = Mathf.FloorToInt(normalizedZ * res);

            int radiusInPixels = Mathf.FloorToInt((radius / terrainData.size.x) * res);

            int startX = Mathf.Clamp(mapX - radiusInPixels, 0, res - 1);
            int startZ = Mathf.Clamp(mapZ - radiusInPixels, 0, res - 1);
            int endX = Mathf.Clamp(mapX + radiusInPixels, 0, res - 1);
            int endZ = Mathf.Clamp(mapZ + radiusInPixels, 0, res - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;

            if (width <= 0 || height <= 0) return;

            EnsureStrokeSnapshotCoversRect(terrain, startX, startZ, width, height);

            // Use the stroke's full-precision running accumulation buffer (if a stroke is active) as the
            // source of truth for edits, rather than re-reading values back from the terrain each frame.
            // TerrainData quantizes heights to 16 bits on every SetHeights call, so repeatedly reading
            // back and adding tiny per-frame deltas to already-quantized values causes neighboring
            // vertices (which accumulate slightly different rounding) to snap to different quantization
            // steps at different times - producing visible stepping/terracing. Applying deltas to an
            // unquantized buffer instead, and only quantizing once (on write for display), keeps the
            // resulting surface smooth and consistent with the true accumulated falloff shape.
            StrokeSnapshot snap = null;
            bool usingWorkingBuffer = strokeActive && strokeSnapshots.TryGetValue(terrain, out snap);

            float[,] areaHeights;
            int workingOffsetX = 0, workingOffsetZ = 0;
            if (usingWorkingBuffer)
            {
                areaHeights = snap.working;
                workingOffsetX = startX - snap.minX;
                workingOffsetZ = startZ - snap.minZ;
            }
            else
            {
                // Unity creates this array as float[height, width], which means [z, x]
                areaHeights = terrainData.GetHeights(startX, startZ, width, height);
            }

            // Local accessor so the edit loop below can transparently address either the full stroke
            // working buffer (offset) or the smaller directly-fetched areaHeights array (no offset).
            float Get(int x, int z) => areaHeights[z + workingOffsetZ, x + workingOffsetX];
            void Set(int x, int z, float value) => areaHeights[z + workingOffsetZ, x + workingOffsetX] = value;

            bool dataChanged = false;

            BrushMode effectiveMode = ResolveEffectiveMode();
            bool invert = ResolveEffectiveInvert();
            float strength = ResolveEffectiveStrength(effectiveMode);

            // For Smooth mode's kernel sampling: the bounds of the buffer backing "Get"/areaHeights, in
            // terrain-pixel space, so samples that fall outside it (near a terrain edge) can be resolved
            // against the real neighboring terrain instead of clamping/duplicating this side's own edge
            // values (which is what caused smoothing to widen rather than heal height seams).
            int bufMinX = usingWorkingBuffer ? snap.minX : startX;
            int bufMinZ = usingWorkingBuffer ? snap.minZ : startZ;
            int bufWidth = areaHeights.GetLength(1);
            int bufHeight = areaHeights.GetLength(0);

            // brushStrength is authored in world-height meters/second; convert to a normalized-height
            // (0-1) delta per second using this terrain's max height so a given strength value feels
            // consistent regardless of the terrain's height range.
            float terrainHeight = Mathf.Max(0.0001f, terrainData.size.y);
            float normalizedStrength = strength / terrainHeight;

            // For flatten mode, lock in the target height on first touch within a stroke. Rather than a single
            // sampled vertex, average every currently-touched pixel that falls within the brush radius so the
            // whole footprint slowly converges toward a representative average height (per user request),
            // instead of toward whatever single vertex happened to be touched first.
            if (effectiveMode == BrushMode.Flatten && strokeFlattenHeight == null)
            {
                double heightSum = 0;
                int sampleCount = 0;

                for (int sz = 0; sz < height; sz++)
                {
                    for (int sx = 0; sx < width; sx++)
                    {
                        int samplePixelX = startX + sx;
                        int samplePixelZ = startZ + sz;
                        float sampleValue = Get(sx, sz);

                        Vector3 sampleWorldPos = terrain.transform.TransformPoint(new Vector3(
                            (float)samplePixelX / (res - 1) * terrainData.size.x,
                            sampleValue * terrainData.size.y,
                            (float)samplePixelZ / (res - 1) * terrainData.size.z
                        ));

                        float sampleDistance = Vector2.Distance(
                            new Vector2(brushWorldCenter.x, brushWorldCenter.z),
                            new Vector2(sampleWorldPos.x, sampleWorldPos.z));

                        if (sampleDistance <= radius)
                        {
                            heightSum += sampleValue;
                            sampleCount++;
                        }
                    }
                }

                if (sampleCount > 0)
                {
                    strokeFlattenHeight = (float)(heightSum / sampleCount);
                }
                else
                {
                    int localZ = mapZ - startZ;
                    int localX = mapX - startX;
                    strokeFlattenHeight = Get(
                        localX >= 0 && localX < width ? localX : 0,
                        localZ >= 0 && localZ < height ? localZ : 0);
                }
            }

            // A terrain's east/north border vertices are the exact same physical vertices as its
            // same-resolution right/top neighbor's west/south border vertices (Unity terrains share
            // border vertices 1:1 for seamless rendering). When a brush overlaps a seam, ApplyBrushAt
            // hits both terrain colliders and runs this method for each of them - without this guard,
            // the shared vertex would have the brush delta applied twice (once from each side) every
            // frame, causing seams to rise/fall roughly twice as fast as the surrounding surface and
            // spike/recess relative to it. Each shared edge is therefore only skipped here when the
            // neighbor on that side is ALSO being processed this call (it will handle the shared vertex
            // itself and PropagateEdgeToNeighbors syncs it back) - otherwise (the much more common case
            // of the brush not being centered exactly on the seam) this side must still edit its own
            // edge normally, since nothing else will.
            bool skipEastEdge = terrain.rightNeighbor != null && terrain.rightNeighbor.terrainData.heightmapResolution == res
                && terrainsHitThisCall != null && terrainsHitThisCall.Contains(terrain.rightNeighbor);
            bool skipNorthEdge = terrain.topNeighbor != null && terrain.topNeighbor.terrainData.heightmapResolution == res
                && terrainsHitThisCall != null && terrainsHitThisCall.Contains(terrain.topNeighbor);

            for (int z = 0; z < height; z++)
            {
                int currentPixelZ = startZ + z;
                if (skipNorthEdge && currentPixelZ == res - 1) continue;

                for (int x = 0; x < width; x++)
                {
                    int currentPixelX = startX + x;
                    if (skipEastEdge && currentPixelX == res - 1) continue;

                    float currentValue = Get(x, z);

                    Vector3 currentVertexWorldPos = terrain.transform.TransformPoint(new Vector3(
                        (float)currentPixelX / (res - 1) * terrainData.size.x,
                        currentValue * terrainData.size.y,
                        (float)currentPixelZ / (res - 1) * terrainData.size.z
                    ));

                    float distance = Vector2.Distance(
                        new Vector2(brushWorldCenter.x, brushWorldCenter.z),
                        new Vector2(currentVertexWorldPos.x, currentVertexWorldPos.z)
                    );

                    if (distance <= radius)
                    {
                        float normalizedDistance = distance / radius;
                        float falloff = activeBrushPreset != null ? activeBrushPreset.SampleFalloff(normalizedDistance) : (1f - normalizedDistance);

                        switch (effectiveMode)
                        {
                            case BrushMode.Raise:
                                {
                                    float delta = normalizedStrength * falloff * Time.deltaTime;
                                    if (invert) delta = -delta;
                                    Set(x, z, Mathf.Clamp01(currentValue + delta));
                                    break;
                                }
                            case BrushMode.Lower:
                                {
                                    float delta = normalizedStrength * falloff * Time.deltaTime;
                                    if (invert) delta = -delta;
                                    Set(x, z, Mathf.Clamp01(currentValue - delta));
                                    break;
                                }
                            case BrushMode.Flatten:
                                {
                                    float target = strokeFlattenHeight ?? currentValue;
                                    Set(x, z, Mathf.Lerp(currentValue, target, falloff * strength * 10f * Time.deltaTime));
                                    break;
                                }
                            case BrushMode.Smooth:
                                {
                                    float average = SampleSmoothedNeighborHeightSeamAware(terrain, areaHeights, bufMinX, bufMinZ, bufWidth, bufHeight, currentPixelX, currentPixelZ, res);
                                    Set(x, z, Mathf.Lerp(currentValue, average, Mathf.Clamp01(falloff * strength * 5f * Time.deltaTime)));
                                    break;
                                }
                            case BrushMode.SetHeight:
                                {
                                    float targetNormalized = Mathf.Clamp01(setHeightTarget / terrainHeight);
                                    Set(x, z, Mathf.Lerp(currentValue, targetNormalized, falloff * strength * 10f * Time.deltaTime));
                                    break;
                                }
                        }

                        dataChanged = true;
                    }
                }
            }

            if (dataChanged)
            {
                // IMPORTANT: skipEastEdge/skipNorthEdge above only prevented the *edit loop* from writing
                // new values into that column/row of areaHeights - but areaHeights still holds whatever
                // (unedited, stale) value was already there for that column/row. Writing the *full* rect
                // back to terrainData would therefore blindly overwrite the shared seam vertex with that
                // stale value, stomping over whatever the neighbor terrain just (or is about to) propagate
                // into it via PropagateEdgeToNeighbors - which is exactly what caused seam vertices to
                // stop responding to raise/lower entirely. So the actual write/record/propagate rect must
                // be shrunk to exclude any skipped edge column/row, not just the edit loop.
                int writeStartX = startX;
                int writeStartZ = startZ;
                int writeWidth = width;
                int writeHeight = height;

                if (skipEastEdge && (startX + width - 1) == res - 1) writeWidth--;
                if (skipNorthEdge && (startZ + height - 1) == res - 1) writeHeight--;

                if (writeWidth > 0 && writeHeight > 0)
                {
                    float[,] displayHeights = usingWorkingBuffer
                        ? ExtractRect(areaHeights, workingOffsetX, workingOffsetZ, writeWidth, writeHeight)
                        : ExtractRect(areaHeights, 0, 0, writeWidth, writeHeight);
                    terrainData.SetHeights(writeStartX, writeStartZ, displayHeights);
                    RecordDeltas(terrain, writeStartX, writeStartZ, writeWidth, writeHeight, displayHeights);
                    PropagateEdgeToNeighbors(terrain, writeStartX, writeStartZ, writeWidth, writeHeight, displayHeights);
                }
            }
        }

        /// <summary>Copies a sub-rectangle out of a larger [z, x] height array into a new, smaller array.</summary>
        static float[,] ExtractRect(float[,] source, int offsetX, int offsetZ, int width, int height)
        {
            float[,] result = new float[height, width];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[z, x] = source[z + offsetZ, x + offsetX];
                }
            }
            return result;
        }

        void FlattenSingleTerrainInstance(Terrain terrain, Vector3 brushWorldCenter, float radius, float targetWorldHeight)
        {
            TerrainData terrainData = terrain.terrainData;
            int res = terrainData.heightmapResolution;

            Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(brushWorldCenter);
            float normalizedX = terrainLocalPos.x / terrainData.size.x;
            float normalizedZ = terrainLocalPos.z / terrainData.size.z;

            int mapX = Mathf.FloorToInt(normalizedX * res);
            int mapZ = Mathf.FloorToInt(normalizedZ * res);

            int radiusInPixels = Mathf.FloorToInt((radius / terrainData.size.x) * res);

            int startX = Mathf.Clamp(mapX - radiusInPixels, 0, res - 1);
            int startZ = Mathf.Clamp(mapZ - radiusInPixels, 0, res - 1);
            int endX = Mathf.Clamp(mapX + radiusInPixels, 0, res - 1);
            int endZ = Mathf.Clamp(mapZ + radiusInPixels, 0, res - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;
            if (width <= 0 || height <= 0) return;

            float[,] areaHeights = terrainData.GetHeights(startX, startZ, width, height);
            float normalizedTargetHeight = Mathf.Clamp01(targetWorldHeight / terrainData.size.y);
            bool dataChanged = false;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentPixelX = startX + x;
                    int currentPixelZ = startZ + z;

                    Vector3 currentVertexWorldPos = terrain.transform.TransformPoint(new Vector3(
                        (float)currentPixelX / (res - 1) * terrainData.size.x,
                        areaHeights[z, x] * terrainData.size.y,
                        (float)currentPixelZ / (res - 1) * terrainData.size.z
                    ));

                    float distance = Vector2.Distance(
                        new Vector2(brushWorldCenter.x, brushWorldCenter.z),
                        new Vector2(currentVertexWorldPos.x, currentVertexWorldPos.z)
                    );

                    if (distance <= radius)
                    {
                        float falloff = 1f - (distance / radius);
                        areaHeights[z, x] = Mathf.Lerp(areaHeights[z, x], normalizedTargetHeight, falloff);
                        dataChanged = true;
                    }
                }
            }

            if (dataChanged)
            {
                terrainData.SetHeights(startX, startZ, areaHeights);
                RecordDeltas(terrain, startX, startZ, width, height, areaHeights);
                PropagateEdgeToNeighbors(terrain, startX, startZ, width, height, areaHeights);
            }
        }

        /// <summary>
        /// Applies a stamp's height texture to a single terrain instance, scaled by the stamp's default
        /// max height and the one-shot <see cref="stampHeightMultiplier"/>. Additive with existing terrain
        /// height (rather than a hard overwrite) so overlapping placements blend naturally.
        /// </summary>
        TerrainHeightEdit? StampSingleTerrainInstance(Terrain terrain, Vector3 worldCenter, StampPreset stamp, Quaternion rotation, Vector3 scale, float heightMultiplier)
        {
            TerrainData terrainData = terrain.terrainData;
            int res = terrainData.heightmapResolution;

            Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(worldCenter); 
            float normalizedX = terrainLocalPos.x / terrainData.size.x;
            float normalizedZ = terrainLocalPos.z / terrainData.size.z;

            int mapX = Mathf.FloorToInt(normalizedX * res);
            int mapZ = Mathf.FloorToInt(normalizedZ * res);

            float footprintRadiusX = 0.5f * scale.x;
            float footprintRadiusZ = 0.5f * scale.z;

            int radiusPixelsX = Mathf.FloorToInt((footprintRadiusX / terrainData.size.x) * res);
            int radiusPixelsZ = Mathf.FloorToInt((footprintRadiusZ / terrainData.size.z) * res);

            int startX = Mathf.Clamp(mapX - radiusPixelsX, 0, res - 1);
            int startZ = Mathf.Clamp(mapZ - radiusPixelsZ, 0, res - 1);
            int endX = Mathf.Clamp(mapX + radiusPixelsX, 0, res - 1);
            int endZ = Mathf.Clamp(mapZ + radiusPixelsZ, 0, res - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;
            if (width <= 0 || height <= 0) return null;

            float[,] before = terrainData.GetHeights(startX, startZ, width, height);
            float[,] areaHeights = (float[,])before.Clone();

            float maxHeightNormalized = stamp.defaultMaxHeight / terrainData.size.y;
            float globalMultiplier = heightMultiplier;
            bool invert = ResolveEffectiveInvert();

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Footprint-relative UV: 0..1 across the stamp's placed extent, rotation applied around center.
                    float px = (startX + x - mapX) / (float)Mathf.Max(1, radiusPixelsX * 2);
                    float pz = (startZ + z - mapZ) / (float)Mathf.Max(1, radiusPixelsZ * 2);

                    Vector2 rotated = RotateUv(px, pz, rotation);
                    float u = rotated.x + 0.5f;
                    float v = rotated.y + 0.5f;

                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;

                    float sample = stamp.SampleHeight(u, v);
                    float delta = sample * maxHeightNormalized * globalMultiplier;
                    if (invert) delta = -delta;

                    areaHeights[z, x] = Mathf.Clamp01(areaHeights[z, x] + delta);
                }
            }

            terrainData.SetHeights(startX, startZ, areaHeights);
            RecordDeltas(terrain, startX, startZ, width, height, areaHeights);
            PropagateEdgeToNeighbors(terrain, startX, startZ, width, height, areaHeights);

            return new TerrainHeightEdit(terrain, startX, startZ, before, areaHeights);
        }

        /// <summary>
        /// Same footprint/height-sampling logic as <see cref="StampSingleTerrainInstance"/>, but tracks and
        /// returns the raw additive delta actually applied (clamped against the terrain's existing height),
        /// so a persistent <see cref="StampInstance"/> can precisely subtract just its own contribution
        /// later (on move/edit/remove) without disturbing other overlapping stamps or brush edits.
        /// </summary>
        StampContribution StampSingleTerrainInstanceAdditive(Terrain terrain, Vector3 worldCenter, StampPreset stamp, Quaternion rotation, Vector3 scale, float heightMultiplier)
        {
            TerrainData terrainData = terrain.terrainData;
            int res = terrainData.heightmapResolution;

            Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(worldCenter);
            float normalizedX = terrainLocalPos.x / terrainData.size.x;
            float normalizedZ = terrainLocalPos.z / terrainData.size.z;

            int mapX = Mathf.FloorToInt(normalizedX * res);
            int mapZ = Mathf.FloorToInt(normalizedZ * res);

            float footprintRadiusX = 0.5f * scale.x;
            float footprintRadiusZ = 0.5f * scale.z;

            int radiusPixelsX = Mathf.FloorToInt((footprintRadiusX / terrainData.size.x) * res);
            int radiusPixelsZ = Mathf.FloorToInt((footprintRadiusZ / terrainData.size.z) * res);

            int startX = Mathf.Clamp(mapX - radiusPixelsX, 0, res - 1);
            int startZ = Mathf.Clamp(mapZ - radiusPixelsZ, 0, res - 1);
            int endX = Mathf.Clamp(mapX + radiusPixelsX, 0, res - 1);
            int endZ = Mathf.Clamp(mapZ + radiusPixelsZ, 0, res - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;
            if (width <= 0 || height <= 0) return null;

            float[,] areaHeights = terrainData.GetHeights(startX, startZ, width, height);
            float[,] appliedDelta = new float[height, width];

            float maxHeightNormalized = stamp.defaultMaxHeight / terrainData.size.y;
            float globalMultiplier = heightMultiplier;
            bool invert = ResolveEffectiveInvert();
            bool dataChanged = false;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = (startX + x - mapX) / (float)Mathf.Max(1, radiusPixelsX * 2);
                    float pz = (startZ + z - mapZ) / (float)Mathf.Max(1, radiusPixelsZ * 2);

                    Vector2 rotated = RotateUv(px, pz, rotation);
                    float u = rotated.x + 0.5f;
                    float v = rotated.y + 0.5f;

                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;

                    float sample = stamp.SampleHeight(u, v);
                    float delta = sample * maxHeightNormalized * globalMultiplier;
                    if (invert) delta = -delta;

                    float before = areaHeights[z, x];
                    float after = Mathf.Clamp01(before + delta);
                    appliedDelta[z, x] = after - before;
                    areaHeights[z, x] = after;
                    if (after != before) dataChanged = true;
                }
            }

            if (dataChanged)
            {
                terrainData.SetHeights(startX, startZ, areaHeights);
                RecordDeltas(terrain, startX, startZ, width, height, areaHeights);
                PropagateEdgeToNeighbors(terrain, startX, startZ, width, height, areaHeights);
            }

            return new StampContribution { startX = startX, startZ = startZ, width = width, height = height, delta = appliedDelta };
        }

        /// <summary>Rotates a footprint-relative (-0.5..0.5) UV offset by a quaternion's yaw component.</summary>
        static Vector2 RotateUv(float x, float y, Quaternion rotation)
        {
            float yaw = rotation.eulerAngles.y * Mathf.Deg2Rad;
            float cos = Mathf.Cos(-yaw);
            float sin = Mathf.Sin(-yaw);
            return new Vector2(x * cos - y * sin, x * sin + y * cos);
        }

        /// <summary>
        /// Paints enabled/mixed texture channels onto a single terrain's alphamaps within the given
        /// footprint, without undo tracking (used for continuous brush strokes; the stroke as a whole is
        /// snapshotted via <see cref="EnsureAlphamapStrokeSnapshotCoversRect"/>/<see cref="EndStrokeAlphamaps"/>).
        /// </summary>
        void PaintChannelsSingleTerrainInstance(Terrain terrain, Vector3 worldCenter, float radius, TerrainTextureChannel[] channels, Dictionary<TerrainTextureChannel, TerrainTextureChannelState> channelStates)
        {
            TerrainData terrainData = terrain.terrainData;
            var layers = terrainData.terrainLayers;
            if (layers == null || layers.Length == 0) return;

            int alphaRes = terrainData.alphamapResolution;

            Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(worldCenter);
            float normalizedX = terrainLocalPos.x / terrainData.size.x;
            float normalizedZ = terrainLocalPos.z / terrainData.size.z;

            int mapX = Mathf.FloorToInt(normalizedX * alphaRes);
            int mapZ = Mathf.FloorToInt(normalizedZ * alphaRes);

            int radiusInPixels = Mathf.FloorToInt((radius / terrainData.size.x) * alphaRes);

            int startX = Mathf.Clamp(mapX - radiusInPixels, 0, alphaRes - 1);
            int startZ = Mathf.Clamp(mapZ - radiusInPixels, 0, alphaRes - 1);
            int endX = Mathf.Clamp(mapX + radiusInPixels, 0, alphaRes - 1);
            int endZ = Mathf.Clamp(mapZ + radiusInPixels, 0, alphaRes - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;
            if (width <= 0 || height <= 0) return;

            EnsureAlphamapStrokeSnapshotCoversRect(terrain, startX, startZ, width, height);

            float[,,] weights = terrainData.GetAlphamaps(startX, startZ, width, height);
            bool changed = ApplyChannelsToWeights(weights, layers, channels, channelStates, terrain, worldCenter, radius, startX, startZ, width, height, alphaRes, terrainData);

            if (changed) terrainData.SetAlphamaps(startX, startZ, weights);
        }

        /// <summary>Same as <see cref="PaintChannelsSingleTerrainInstance"/> but captures a before/after snapshot for undo (used by one-shot stamp placement).</summary>
        TerrainAlphamapEdit? PaintChannelsSingleTerrainInstanceWithUndo(Terrain terrain, Vector3 worldCenter, float radius, TerrainTextureChannel[] channels, Dictionary<TerrainTextureChannel, TerrainTextureChannelState> channelStates)
        {
            TerrainData terrainData = terrain.terrainData;
            var layers = terrainData.terrainLayers;
            if (layers == null || layers.Length == 0) return null;

            int alphaRes = terrainData.alphamapResolution;

            Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(worldCenter);
            float normalizedX = terrainLocalPos.x / terrainData.size.x;
            float normalizedZ = terrainLocalPos.z / terrainData.size.z;

            int mapX = Mathf.FloorToInt(normalizedX * alphaRes);
            int mapZ = Mathf.FloorToInt(normalizedZ * alphaRes);

            int radiusInPixels = Mathf.FloorToInt((radius / terrainData.size.x) * alphaRes);

            int startX = Mathf.Clamp(mapX - radiusInPixels, 0, alphaRes - 1);
            int startZ = Mathf.Clamp(mapZ - radiusInPixels, 0, alphaRes - 1);
            int endX = Mathf.Clamp(mapX + radiusInPixels, 0, alphaRes - 1);
            int endZ = Mathf.Clamp(mapZ + radiusInPixels, 0, alphaRes - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;
            if (width <= 0 || height <= 0) return null;

            float[,,] before = terrainData.GetAlphamaps(startX, startZ, width, height);
            float[,,] weights = (float[,,])before.Clone();

            bool changed = ApplyChannelsToWeights(weights, layers, channels, channelStates, terrain, worldCenter, radius, startX, startZ, width, height, alphaRes, terrainData);
            if (!changed) return null;

            terrainData.SetAlphamaps(startX, startZ, weights);
            return new TerrainAlphamapEdit(terrain, startX, startZ, before, weights);
        }

        bool ApplyChannelsToWeights(float[,,] weights, TerrainLayer[] layers, TerrainTextureChannel[] channels, Dictionary<TerrainTextureChannel, TerrainTextureChannelState> channelStates, Terrain terrain, Vector3 worldCenter, float radius, int startX, int startZ, int width, int height, int alphaRes, TerrainData terrainData)
        {
            bool changed = false;

            foreach (var channel in channels)
            {
                if (channel == null) continue;

                var state = GetOrCreateChannelState(channelStates, channel);
                if (state == null || !state.enabled) continue;

                int layerIndex = System.Array.IndexOf(layers, channel.terrainLayer);
                if (layerIndex < 0) continue;

                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pixelX = startX + x;
                        int pixelZ = startZ + z;

                        Vector3 worldPos = terrain.transform.TransformPoint(new Vector3(
                            (float)pixelX / (alphaRes - 1) * terrainData.size.x,
                            0f,
                            (float)pixelZ / (alphaRes - 1) * terrainData.size.z
                        ));

                        float distance = Vector2.Distance(
                            new Vector2(worldCenter.x, worldCenter.z),
                            new Vector2(worldPos.x, worldPos.z)
                        );
                        if (distance > radius) continue;

                        float normalizedDistance = distance / radius;
                        float falloff = 1f - normalizedDistance;

                        float u = (pixelX - startX) / (float)Mathf.Max(1, width - 1);
                        float v = (pixelZ - startZ) / (float)Mathf.Max(1, height - 1);
                        float mask = channel.SampleMask(u, v);

                        float weight = mask * falloff * state.mix;
                        if (weight <= 0f) continue;

                        // Blend this layer up and proportionally reduce all others so the alphamap stays normalized.
                        float existing = weights[z, x, layerIndex];
                        float newValue = Mathf.Clamp01(existing + weight * (1f - existing));
                        float delta = newValue - existing;
                        if (Mathf.Abs(delta) <= 0.0001f) continue;

                        float remaining = 1f - existing;
                        if (remaining > 0.0001f)
                        {
                            float scale = (remaining - delta) / remaining;
                            for (int l = 0; l < layers.Length; l++)
                            {
                                if (l == layerIndex) continue;
                                weights[z, x, l] *= scale;
                            }
                        }
                        weights[z, x, layerIndex] = newValue;

                        changed = true;
                    }
                }
            }

            return changed;
        }

        float SampleAverageNeighborHeight(float[,] areaHeights, int x, int z, int width, int height)
        {
            float sum = 0f;
            int count = 0;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    sum += areaHeights[nz, nx];
                    count++;
                }
            }

            return count > 0 ? sum / count : areaHeights[z, x];
        }

        /// <summary>
        /// Expands the stroke's tracked snapshot for <paramref name="terrain"/> so it covers the given
        /// pixel rectangle, preserving already-captured original values and only pulling fresh "before"
        /// data (from the terrain's current, not-yet-touched state) for newly covered pixels.
        /// </summary>
        void EnsureStrokeSnapshotCoversRect(Terrain terrain, int startX, int startZ, int width, int height)
        {
            if (!strokeActive) return;

            int newMinX = startX;
            int newMinZ = startZ;
            int newMaxX = startX + width - 1;
            int newMaxZ = startZ + height - 1;

            if (!strokeSnapshots.TryGetValue(terrain, out StrokeSnapshot snap))
            {
                float[,] initialBefore = terrain.terrainData.GetHeights(newMinX, newMinZ, width, height);
                strokeSnapshots[terrain] = new StrokeSnapshot
                {
                    minX = newMinX,
                    minZ = newMinZ,
                    maxX = newMaxX,
                    maxZ = newMaxZ,
                    before = initialBefore,
                    working = (float[,])initialBefore.Clone()
                };
                return;
            }

            int unionMinX = Mathf.Min(snap.minX, newMinX);
            int unionMinZ = Mathf.Min(snap.minZ, newMinZ);
            int unionMaxX = Mathf.Max(snap.maxX, newMaxX);
            int unionMaxZ = Mathf.Max(snap.maxZ, newMaxZ);

            // Already fully covered by the existing snapshot rect - nothing new to capture.
            if (unionMinX == snap.minX && unionMinZ == snap.minZ && unionMaxX == snap.maxX && unionMaxZ == snap.maxZ) return;

            int unionWidth = unionMaxX - unionMinX + 1;
            int unionHeight = unionMaxZ - unionMinZ + 1;

            // Pixels newly entering the union haven't been touched yet this stroke, so their current
            // terrain value is still the original value - safe to use directly as "before"/"working" data.
            float[,] unionBefore = terrain.terrainData.GetHeights(unionMinX, unionMinZ, unionWidth, unionHeight);
            float[,] unionWorking = (float[,])unionBefore.Clone();

            // Overlay the previously captured (true original, possibly already-edited-this-stroke) values.
            int offsetX = snap.minX - unionMinX;
            int offsetZ = snap.minZ - unionMinZ;
            int oldWidth = snap.maxX - snap.minX + 1;
            int oldHeight = snap.maxZ - snap.minZ + 1;
            for (int z = 0; z < oldHeight; z++)
            {
                for (int x = 0; x < oldWidth; x++)
                {
                    unionBefore[z + offsetZ, x + offsetX] = snap.before[z, x];
                    unionWorking[z + offsetZ, x + offsetX] = snap.working[z, x];
                }
            }

            snap.minX = unionMinX;
            snap.minZ = unionMinZ;
            snap.maxX = unionMaxX;
            snap.maxZ = unionMaxZ;
            snap.before = unionBefore;
            snap.working = unionWorking;
        }

        /// <summary>Same as <see cref="EnsureStrokeSnapshotCoversRect"/> but for the alphamap (texture channel) stroke snapshot.</summary>
        void EnsureAlphamapStrokeSnapshotCoversRect(Terrain terrain, int startX, int startZ, int width, int height)
        {
            if (!strokeActive) return;

            int newMinX = startX;
            int newMinZ = startZ;
            int newMaxX = startX + width - 1;
            int newMaxZ = startZ + height - 1;

            if (!alphamapStrokeSnapshots.TryGetValue(terrain, out AlphamapStrokeSnapshot snap))
            {
                alphamapStrokeSnapshots[terrain] = new AlphamapStrokeSnapshot
                {
                    minX = newMinX,
                    minZ = newMinZ,
                    maxX = newMaxX,
                    maxZ = newMaxZ,
                    before = terrain.terrainData.GetAlphamaps(newMinX, newMinZ, width, height)
                };
                return;
            }

            int unionMinX = Mathf.Min(snap.minX, newMinX);
            int unionMinZ = Mathf.Min(snap.minZ, newMinZ);
            int unionMaxX = Mathf.Max(snap.maxX, newMaxX);
            int unionMaxZ = Mathf.Max(snap.maxZ, newMaxZ);

            if (unionMinX == snap.minX && unionMinZ == snap.minZ && unionMaxX == snap.maxX && unionMaxZ == snap.maxZ) return;

            int unionWidth = unionMaxX - unionMinX + 1;
            int unionHeight = unionMaxZ - unionMinZ + 1;
            int layerCount = snap.before.GetLength(2);

            float[,,] unionBefore = terrain.terrainData.GetAlphamaps(unionMinX, unionMinZ, unionWidth, unionHeight);

            int offsetX = snap.minX - unionMinX;
            int offsetZ = snap.minZ - unionMinZ;
            int oldWidth = snap.maxX - snap.minX + 1;
            int oldHeight = snap.maxZ - snap.minZ + 1;
            for (int z = 0; z < oldHeight; z++)
            {
                for (int x = 0; x < oldWidth; x++)
                {
                    for (int l = 0; l < layerCount; l++)
                    {
                        unionBefore[z + offsetZ, x + offsetX, l] = snap.before[z, x, l];
                    }
                }
            }

            snap.minX = unionMinX;
            snap.minZ = unionMinZ;
            snap.maxX = unionMaxX;
            snap.maxZ = unionMaxZ;
            snap.before = unionBefore;
        }

        void RecordDeltas(Terrain terrain, int startX, int startZ, int width, int height, float[,] areaHeights)
        {
            WorldEdit worldEdit = worldEditSystem;
            if (worldEdit == null && World.Instance != null) worldEdit = World.Instance.worldEditSystem;
            if (worldEdit == null || World.Instance == null || World.Instance.terrainPoolManager == null) return;

            float chunkSize = World.Instance.terrainPoolManager.chunkSize;
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt(terrain.transform.position.x / chunkSize),
                Mathf.FloorToInt(terrain.transform.position.z / chunkSize)
            );

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    worldEdit.RecordLiveVertexEdit(chunkCoord, startX + x, startZ + z, areaHeights[z, x]);
                }
            }
        }

        // 5x5 Gaussian-like weight kernel (unnormalized) used by SampleSmoothedNeighborHeight. A wider,
        // weighted kernel produces a noticeably smoother result than a plain 3x3 average, and - since it
        // is applied to the unquantized stroke working buffer rather than to already-quantized terrain
        // samples - it no longer introduces its own small-scale stepping artifacts.
        static readonly int[,] smoothKernel = new int[5, 5]
        {
            { 1, 4,  6, 4, 1 },
            { 4, 16, 24, 16, 4 },
            { 6, 24, 36, 24, 6 },
            { 4, 16, 24, 16, 4 },
            { 1, 4,  6, 4, 1 }
        };

        /// <summary>
        /// Samples a weighted 5x5 average around terrain-pixel (pixelX, pixelZ), preferring values from
        /// <paramref name="areaHeights"/> (a buffer covering [bufMinX, bufMinZ) .. [bufMinX+bufWidth,
        /// bufMinZ+bufHeight) in terrain-pixel space) but resolving any sample that falls outside that
        /// buffer - i.e. near/at a terrain edge - against the real neighboring Terrain's heightmap instead
        /// of clamping to this side's own edge value.
        ///
        /// Clamping (the previous behavior) effectively duplicates the edge row/column as padding, which
        /// biases the smoothing average toward this side's own height and never lets it blend toward
        /// whatever the neighbor's actual edge height is. Since edits are also propagated across the seam
        /// every frame (see <see cref="PropagateEdgeToNeighbors"/>), the two sides start each frame equal at
        /// the border - but with clamped smoothing, this side's *interior* keeps getting pulled toward its
        /// own edge while the neighbor's interior gets pulled toward its edge, so any interior difference
        /// across the seam actually grows over time instead of blending out. Sampling the true neighbor data
        /// here makes the smoothing kernel treat the seam exactly like any other interior region, so height
        /// differences flatten out across it the same way they do everywhere else on the surface.
        /// </summary>
        float SampleSmoothedNeighborHeightSeamAware(Terrain terrain, float[,] areaHeights, int bufMinX, int bufMinZ, int bufWidth, int bufHeight, int pixelX, int pixelZ, int res)
        {
            float sum = 0f;
            int weightSum = 0;

            for (int kz = -2; kz <= 2; kz++)
            {
                int sampleZ = pixelZ + kz;
                for (int kx = -2; kx <= 2; kx++)
                {
                    int sampleX = pixelX + kx;
                    int weight = smoothKernel[kz + 2, kx + 2];

                    sum += SampleHeightAcrossSeam(terrain, areaHeights, bufMinX, bufMinZ, bufWidth, bufHeight, sampleX, sampleZ, res) * weight;
                    weightSum += weight;
                }
            }

            return weightSum > 0 ? sum / weightSum : areaHeights[pixelZ - bufMinZ, pixelX - bufMinX];
        }

        /// <summary>
        /// Resolves a single terrain-pixel height sample, preferring the in-progress edit buffer when the
        /// sample lies within it, otherwise reading from the owning terrain (self or the appropriate
        /// neighbor if the sample crosses a border), falling back to clamping only when no neighbor exists
        /// (e.g. world edge / neighbor not streamed in).
        /// </summary>
        float SampleHeightAcrossSeam(Terrain terrain, float[,] areaHeights, int bufMinX, int bufMinZ, int bufWidth, int bufHeight, int sampleX, int sampleZ, int res)
        {
            int localX = sampleX - bufMinX;
            int localZ = sampleZ - bufMinZ;
            if (localX >= 0 && localX < bufWidth && localZ >= 0 && localZ < bufHeight)
            {
                return areaHeights[localZ, localX];
            }

            Terrain sampleTerrain = terrain;
            int terrainX = sampleX;
            int terrainZ = sampleZ;

            if (terrainX < 0)
            {
                sampleTerrain = terrain.leftNeighbor;
                terrainX += res - 1;
            }
            else if (terrainX >= res)
            {
                sampleTerrain = terrain.rightNeighbor;
                terrainX -= res - 1;
            }

            if (sampleTerrain != null && terrainZ < 0)
            {
                Terrain belowNeighbor = sampleTerrain == terrain ? terrain.bottomNeighbor : null;
                if (belowNeighbor != null) { sampleTerrain = belowNeighbor; terrainZ += res - 1; }
            }
            else if (sampleTerrain != null && terrainZ >= res)
            {
                Terrain aboveNeighbor = sampleTerrain == terrain ? terrain.topNeighbor : null;
                if (aboveNeighbor != null) { sampleTerrain = aboveNeighbor; terrainZ -= res - 1; }
            }
            else if (sampleTerrain == terrain)
            {
                // Pure Z-direction crossing (X in range) handled here since the X branch above didn't apply.
                if (terrainZ < 0) { sampleTerrain = terrain.bottomNeighbor; terrainZ += res - 1; }
                else if (terrainZ >= res) { sampleTerrain = terrain.topNeighbor; terrainZ -= res - 1; }
            }

            if (sampleTerrain == null || sampleTerrain.terrainData.heightmapResolution != res)
            {
                // No usable neighbor (world edge, or a differently-resolved tier) - fall back to clamping
                // against this terrain's own buffer, same as the old behavior, rather than crashing/guessing.
                int clampedLocalX = Mathf.Clamp(localX, 0, bufWidth - 1);
                int clampedLocalZ = Mathf.Clamp(localZ, 0, bufHeight - 1);
                return areaHeights[clampedLocalZ, clampedLocalX];
            }

            terrainX = Mathf.Clamp(terrainX, 0, res - 1);
            terrainZ = Mathf.Clamp(terrainZ, 0, res - 1);
            return sampleTerrain.terrainData.GetHeight(terrainX, terrainZ) / sampleTerrain.terrainData.size.y;
        }

    }
}
