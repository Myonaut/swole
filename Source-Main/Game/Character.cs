using System;

namespace Swole
{

    [Serializable]
    public class Character
    {

        protected int id;

        /// <summary>
        /// The unique identifier for the Character.
        /// </summary>
        public int ID => id;

        public Character(GameWorld world)
        {
            id = world.NextCharacterID;
        }

        public string name;

        public string middleName;
        public string lastName;

        public string alias;

        /// <summary>
        /// The default name to be displayed when referencing the Character.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(alias) ? (string.IsNullOrEmpty(name) ? (string.IsNullOrEmpty(lastName) ? (string.IsNullOrEmpty(middleName) ? "null" : middleName) : lastName) : name) : alias;

        public override string ToString() => DisplayName + $"[{ID}]";

    }

}
