
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Swole
{

    /// <summary>
    /// Source: https://discussions.unity.com/t/ssgi-with-outdoor-areas-and-vegetation-noise-denoiser-and-performance/865161/11
    /// </summary>
    [ExecuteAlways]
    public class DualBlendReflectionProbe : MonoBehaviour
    {
        [Range(0.1f, 60)] public float interval = 2;
        public int importance = 1;

        Transform[] reflectionTrans = new Transform[2];
        //HDAdditionalReflectionData[] reflectionData = new HDAdditionalReflectionData[2];
        ReflectionProbe[] reflectionData = new ReflectionProbe[2];
        Transform camTrans;
        float[] updateTime = new float[2];
        float[] weight = new float[2];
        float intervalTwo;

        void OnValidate()
        {
            Setup();
        }

        void Start()
        {
            Setup();
        }

        void Update()
        {
            weight[1] = Mathf.Abs((updateTime[0] - Time.time) / intervalTwo * 2 - 1);
            weight[0] = 1 - weight[1];

            for (int i = 0; i < 2; i++)
            {
                //reflectionData[i].weight = weight[i];
                reflectionData[i].intensity = weight[i];
                if (Time.time >= updateTime[i])
                {
                    updateTime[i] += intervalTwo;
                    reflectionTrans[i].position = camTrans.position;
                    //reflectionData[i].RequestRenderNextUpdate();
                    reflectionData[i].RenderProbe();
                }
            }
        }

        void Setup()
        {
            for (int i = 0; i < 2; i++)
            {
                reflectionTrans[i] = transform.GetChild(i).GetComponent<Transform>();
                //reflectionData[i] = transform.GetChild(i).GetComponent<HDAdditionalReflectionData>();
                reflectionData[i] = transform.GetChild(i).GetComponent<ReflectionProbe>();
            }

            camTrans = Camera.main.transform;
            updateTime[0] = Time.time;
            updateTime[1] = updateTime[0] + interval;
            intervalTwo = interval * 2;
        }
    }
}
