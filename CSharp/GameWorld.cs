using System;
using System.Collections.Generic;

namespace Swolescript
{

    [Serializable]
    public class GameWorld
    {

        protected List<Character> characters = new List<Character>();

        public int CharacterCount => characters.Count;
        public int NextCharacterID 
        { 
        
            get
            {

                List<int> available = new List<int>();
                List<int> restricted = new List<int>();

                void AddAvailableID(int id)
                {

                    if (id >= 0 && !restricted.Contains(id)) available.Add(id);

                }

                for (int ind = 0; ind < CharacterCount; ind++) 
                {

                    var character = characters[ind];
                    if (character == null) continue;

                    restricted.Add(character.ID);
                    available.RemoveAll(i => i == character.ID);

                    AddAvailableID(character.ID - 1);
                    AddAvailableID(character.ID + 1);

                }

                if (available.Count > 1)
                {
                    available.Sort();
                    return available[0];
                }
                else if (available.Count == 1)
                {
                    return available[0];
                }

                return CharacterCount;

            }
        
        }

        public Character CreateNewCharacter()
        {

            var character = new Character(this);

            if (character.ID >= CharacterCount) characters.Add(character); else characters.Insert(character.ID, character);

            return character;

        }

    }

}
