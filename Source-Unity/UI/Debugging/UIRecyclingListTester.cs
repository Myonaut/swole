using UnityEngine;

namespace Swole.UI.Debugging
{
    public class UIRecyclingListTester : MonoBehaviour
    {

        public bool autoTest = true;

        public UIRecyclingList list;

        public int dummyListMemberCount = 10;

        protected void Start()
        {
            if (autoTest) Test(); 
        }

        public void Test()
        {
            if (list == null) list = gameObject.GetComponentInChildren<UIRecyclingList>();

            if (list != null)
            {
                for (int a = 0; a < dummyListMemberCount; a++)
                {
                    list.AddNewMember($"Dummy Item {a}", null, false);
                }

                list.Refresh();
            }
        }

    }
}
