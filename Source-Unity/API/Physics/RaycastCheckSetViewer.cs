using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole
{

    [ExecuteAlways]
    public class RaycastCheckSetViewer : MonoBehaviour
    {

        public RaycastCheckSet checkSet;
        private RaycastCheckSetHandler handler;

        public bool drawHandlerResults;
        public bool drawHandlerPositions;

        public void OnDrawGizmos()
        {
            if (checkSet != null)
            {
                checkSet.DrawGizmos(transform.localToWorldMatrix);
            }

            if (handler != null) 
            {
                if (drawHandlerResults) handler.DrawResults(transform.localToWorldMatrix, Color.green);
                if (drawHandlerPositions) handler.DrawPositions(transform.localToWorldMatrix, Color.cyan);  
            }
        }

        public bool test;
        public bool Test()
        {
            if (checkSet == null) return false;

            if (handler == null) handler = new RaycastCheckSetHandler();
            handler.Initialize(checkSet);

            while(handler.ExecuteNext(transform.localToWorldMatrix))
            {
                Debug.Log($"Executed iteration {handler.Iteration} / {handler.IterationCount}"); 
            }

            return handler.CompletedSuccessfully; 
        }

        public void Update()
        {
            if (test)
            {
                test = false;
                if (Test())
                {
                    Debug.Log($"Check succeeded for {name}");
                }
                else
                {
                    Debug.Log($"Check failed for {name}");
                }
            }
        }

    }
}
