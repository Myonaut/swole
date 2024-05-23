#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.Unity
{

    [System.Serializable]
    public class RNG
    {

        private static RNG _global;

        public static RNG Global
        {

            get
            {

                if (_global == null) _global = new RNG(Random.state);

                return _global;

            }

        }

        [SerializeField]
        private Random.State? initialState = null;

        public RNG(int seed)
        {

            this.seed = seed;

            Reset();

        }

        public RNG(Random.State initialState)
        {

            this.initialState = initialState;

            this.state = initialState;

        }

        public RNG(Random.State initialState, Random.State currentState)
        {

            this.initialState = initialState;

            this.state = currentState;

        }

        public RNG Reset()
        {

            if (initialState == null)
            {

                Random.InitState(seed);

                initialState = Random.state;

            }

            state = (Random.State)initialState;

            return this;

        }

        public RNG Fork
        {

            get
            {

                return initialState == null ? new RNG(state) : new RNG((Random.State)initialState, state);

            }

        }

        [SerializeField]
        private int seed;

        public int Seed => seed;

        [SerializeField]
        private Random.State state;

        public Random.State State => state;

        public float NextValue
        {

            get
            {

                Random.state = state;

                float val = Random.value;

                state = Random.state;

                return val;

            }

        }

        public bool NextBool
        {

            get
            {

                Random.state = state;

                bool val = Random.value > 0.5f;

                state = Random.state;

                return val;

            }

        }

        public Color NextColor
        {

            get
            {

                Random.state = state;

                Color col = Random.ColorHSV();

                state = Random.state;

                return col;

            }

        }

        public Quaternion NextRotation
        {

            get
            {

                Random.state = state;

                Quaternion rot = Random.rotation;

                state = Random.state;

                return rot;

            }

        }

        public Quaternion NextRotationUniform
        {

            get
            {

                Random.state = state;

                Quaternion rot = Random.rotationUniform;

                state = Random.state;

                return rot;

            }

        }

        public float Range(float minInclusive = 0, float maxInclusive = 1)
        {

            Random.state = state;

            float val = Random.Range(minInclusive, maxInclusive);

            state = Random.state;

            return val;

        }

        public int RangeInt(int minInclusive, int maxExclusive)
        {

            Random.state = state;

            int val = Random.Range(minInclusive, maxExclusive);

            state = Random.state;

            return val;

        }

    }

}

#endif