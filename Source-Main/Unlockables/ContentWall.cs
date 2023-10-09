using System.Collections;
using System.Collections.Generic;

namespace Swole
{

    public class ContentWall : SingletonBehaviour<ContentWall>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public override bool DestroyOnLoad => false;

        public Dictionary<string, bool> unlocks;

        public static bool IsUnlocked(string id)
        {

            if (Instance.unlocks.TryGetValue(id, out bool unlocked)) return unlocked;

            return false;

        }

        public static void SetUnlocked(string id, bool unlocked)
        {

            Instance.unlocks[id] = unlocked;

        }

        public static void SetUnlockedAndSave(string id, bool unlocked)
        {

            Instance.unlocks[id] = unlocked;

            Save();

        }

        protected override void OnInit()
        {

            base.OnInit();

            unlocks = new Dictionary<string, bool>();

            unlocks.Add("test", false);


            Load();

        }

        public static void Save()
        {



        }

        public static void Load()
        {



        }

    }

}
