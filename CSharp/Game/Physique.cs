using System;

namespace Swolescript
{

    /// <summary>
    /// A proxy class used to control a character's muscles.
    /// </summary>
    [Serializable]
    public class Physique
    {

        public Physique(MuscleState[] muscleStates)
        {
            this.muscleStates = muscleStates;
        }

        protected MuscleState[] muscleStates;

        public int MuscleGroupCount => muscleStates == null ? 0 : muscleStates.Length;

        public MuscleState this[int muscleIndex]
        {

            set => SetMuscleState(muscleIndex, value);

            get => GetMuscleState(muscleIndex);

        }

        public MuscleState GetMuscleState(int muscleIndex)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return default;

            return muscleStates[muscleIndex];

        }

        public delegate void MuscleStateChangeDelegate(int muscleIndex, MuscleState oldState, MuscleState newState);

        public event MuscleStateChangeDelegate OnStateChange;

        public void ExecutePerMuscle(MuscleStateChangeDelegate listener) => OnStateChange += listener;
        public void TerminatePerMuscle(MuscleStateChangeDelegate listener) => OnStateChange -= listener;
        public void ClearListeners() => OnStateChange = null;

        #region Muscle Property Setters

        #region Individual Muscles

        #region Three Properties

        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleState(int muscleIndex, ushort mass, ushort flex, ushort pump)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;

            state.mass = mass;
            state.flex = flex;
            state.pump = pump;

            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);

        }

        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleState(int muscleIndex, float mass, float flex, float pump) => SetMuscleState(muscleIndex, MuscleState.FloatToMass(mass), MuscleState.FloatToFlex(flex), MuscleState.FloatToPump(pump));

        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleState(int muscleIndex, MuscleState referenceState) => SetMuscleState(muscleIndex, referenceState.mass, referenceState.flex, referenceState.pump);

        #endregion

        #region Two Properties

        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleMassFlex(int muscleIndex, ushort mass, ushort flex)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;

            state.mass = mass;
            state.flex = flex;

            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);

        }
        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleMassPump(int muscleIndex, ushort mass, ushort pump)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;

            state.mass = mass;
            state.pump = pump;

            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);

        }
        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleFlexPump(int muscleIndex, ushort flex, ushort pump)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;

            state.flex = flex;
            state.pump = pump;

            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);

        }

        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleMassFlex(int muscleIndex, float mass, float flex) => SetMuscleMassFlex(muscleIndex, MuscleState.FloatToMass(mass), MuscleState.FloatToFlex(flex));
        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleMassPump(int muscleIndex, float mass, float pump) => SetMuscleMassPump(muscleIndex, MuscleState.FloatToMass(mass), MuscleState.FloatToPump(pump));
        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleFlexPump(int muscleIndex, float flex, float pump) => SetMuscleFlexPump(muscleIndex, MuscleState.FloatToFlex(flex), MuscleState.FloatToPump(pump));

        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleMassFlex(int muscleIndex, MuscleState referenceState) => SetMuscleMassFlex(muscleIndex, referenceState.mass, referenceState.flex);
        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleMassPump(int muscleIndex, MuscleState referenceState) => SetMuscleMassPump(muscleIndex, referenceState.mass, referenceState.pump);
        /// <summary>
        /// More efficient than setting muscle properties individually.
        /// </summary>
        public void SetMuscleFlexPump(int muscleIndex, MuscleState referenceState) => SetMuscleFlexPump(muscleIndex, referenceState.flex, referenceState.pump);

        #endregion

        #region One Property

        public void SetMuscleMass(int muscleIndex, ushort mass)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;
            state.mass = mass;
            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);
        }
        public void SetMuscleFlex(int muscleIndex, ushort flex)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;
            state.flex = flex;
            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);

        }
        public void SetMusclePump(int muscleIndex, ushort pump)
        {

            if (muscleIndex < 0 || muscleIndex >= MuscleGroupCount) return;

            var state = muscleStates[muscleIndex];
            var prevState = state;
            state.pump = pump;
            muscleStates[muscleIndex] = state;
            OnStateChange?.Invoke(muscleIndex, prevState, state);

        }

        public void SetMuscleMass(int muscleIndex, float mass) => SetMuscleMass(muscleIndex, MuscleState.FloatToMass(mass));
        public void SetMuscleFlex(int muscleIndex, float flex) => SetMuscleFlex(muscleIndex, MuscleState.FloatToMass(flex));
        public void SetMusclePump(int muscleIndex, float pump) => SetMusclePump(muscleIndex, MuscleState.FloatToMass(pump));

        public void SetMuscleMass(int muscleIndex, MuscleState referenceState) => SetMuscleMass(muscleIndex, referenceState.mass);
        public void SetMuscleFlex(int muscleIndex, MuscleState referenceState) => SetMuscleFlex(muscleIndex, referenceState.flex);
        public void SetMusclePump(int muscleIndex, MuscleState referenceState) => SetMusclePump(muscleIndex, referenceState.pump);

        #endregion

        #endregion

        #region All Muscles

        #region Three Properties

        /// <summary>
        /// Sets the properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleState(ushort mass, ushort flex, ushort pump)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.mass = mass;
                    state.flex = flex;
                    state.pump = pump;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.mass = mass;
                    state.flex = flex;
                    state.pump = pump;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }
        /// <summary>
        /// Sets the properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleState(float mass, float flex, float pump) => SetGlobalMuscleState(MuscleState.FloatToMass(mass), MuscleState.FloatToFlex(flex), MuscleState.FloatToPump(pump));
        /// <summary>
        /// Sets the properties of all muscles in this Physique to that of the reference state.
        /// </summary>
        public void SetGlobalMuscleState(MuscleState referenceState) => SetGlobalMuscleState(referenceState.mass, referenceState.flex, referenceState.pump);

        #endregion

        #region Two Properties

        /// <summary>
        /// Sets the mass and flex properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleMassFlex(ushort mass, ushort flex)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.mass = mass;
                    state.flex = flex;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.mass = mass;
                    state.flex = flex;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }
        /// <summary>
        /// Sets the mass and pump properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleMassPump(ushort mass, ushort pump)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.mass = mass;
                    state.pump = pump;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.mass = mass;
                    state.pump = pump;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }
        /// <summary>
        /// Sets the flex and pump properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleFlexPump(ushort flex, ushort pump)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.flex = flex;
                    state.pump = pump;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.flex = flex;
                    state.pump = pump;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }

        /// <summary>
        /// Sets the mass and flex properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleMassFlex(float mass, float flex) => SetGlobalMuscleMassFlex(MuscleState.FloatToMass(mass), MuscleState.FloatToFlex(flex));
        /// <summary>
        /// Sets the mass and pump properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleMassPump(float mass, float pump) => SetGlobalMuscleMassFlex(MuscleState.FloatToMass(mass), MuscleState.FloatToFlex(pump));
        /// <summary>
        /// Sets the flex and pump properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleFlexPump(float flex, float pump) => SetGlobalMuscleMassFlex(MuscleState.FloatToMass(flex), MuscleState.FloatToFlex(pump));

        /// <summary>
        /// Sets the mass and flex properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleMassFlex(MuscleState referenceState) => SetGlobalMuscleMassFlex(referenceState.mass, referenceState.flex);
        /// <summary>
        /// Sets the mass and pump properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleMassPump(MuscleState referenceState) => SetGlobalMuscleMassFlex(referenceState.mass, referenceState.pump);
        /// <summary>
        /// Sets the flex and pump properties of all muscles in this Physique to the specified values.
        /// </summary>
        public void SetGlobalMuscleFlexPump(MuscleState referenceState) => SetGlobalMuscleMassFlex(referenceState.flex, referenceState.pump);


        #endregion

        #region One Property

        /// <summary>
        /// Sets the mass of all muscles in this Physique to the same value.
        /// </summary>
        public void SetGlobalMuscleMass(ushort mass)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.mass = mass;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.mass = mass;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }
        /// <summary>
        /// Sets the flex of all muscles in this Physique to the same value.
        /// </summary>
        public void SetGlobalMuscleFlex(ushort flex)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.flex = flex;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.flex = flex;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }
        /// <summary>
        /// Sets the pump of all muscles in this Physique to the same value.
        /// </summary>
        public void SetGlobalMusclePump(ushort pump)
        {
            if (muscleStates == null) return;
            if (OnStateChange == null)
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    state.pump = pump;
                    muscleStates[a] = state;
                }
            }
            else
            {
                for (int a = 0; a < muscleStates.Length; a++)
                {
                    var state = muscleStates[a];
                    var prevState = state;
                    state.pump = pump;
                    muscleStates[a] = state;
                    OnStateChange.Invoke(a, prevState, state);
                }
            }
        }

        /// <summary>
        /// Sets the mass of all muscles in this Physique to the same value.
        /// </summary>
        public void SetGlobalMuscleMass(float mass) => SetGlobalMuscleMass(MuscleState.FloatToMass(mass));
        /// <summary>
        /// Sets the flex of all muscles in this Physique to the same value.
        /// </summary>
        public void SetGlobalMuscleFlex(float flex) => SetGlobalMuscleFlex(MuscleState.FloatToMass(flex));
        /// <summary>
        /// Sets the pump of all muscles in this Physique to the same value.
        /// </summary>
        public void SetGlobalMusclePump(float pump) => SetGlobalMusclePump(MuscleState.FloatToMass(pump));

        /// <summary>
        /// Sets the mass of all muscles in this Physique to the same reference mass.
        /// </summary>
        public void SetGlobalMuscleMass(MuscleState referenceState) => SetGlobalMuscleMass(referenceState.mass);
        /// <summary>
        /// Sets the flex of all muscles in this Physique to the same reference flex.
        /// </summary>
        public void SetGlobalMuscleFlex(MuscleState referenceState) => SetGlobalMuscleFlex(referenceState.flex);
        /// <summary>
        /// Sets the pump of all muscles in this Physique to the same reference pump.
        /// </summary>
        public void SetGlobalMusclePump(MuscleState referenceState) => SetGlobalMusclePump(referenceState.pump);

        #endregion

        #endregion

        #endregion

    }

}
