using System;
using System.Collections.Generic;

namespace Swole
{

    [Serializable]
    public class GameWorld
    {

        protected List<Character> characters = new List<Character>();

        public int CharacterCount => characters.Count;
        public int NextCharacterID 
        {

            get => SwoleUtil.GetUniqueId(GetCharacterIdFromIndex, characters.Count);

        }

        protected int GetCharacterIdFromIndex(int index)
        {
            var character = characters[index];
            if (character == null) return -1;
            return character.ID;
        }

        public Character CreateNewCharacter()
        {

            var character = new Character(this);

            if (character.ID >= CharacterCount) characters.Add(character); else characters.Insert(character.ID, character);

            return character;

        }

    }

}
