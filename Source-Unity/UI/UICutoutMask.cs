#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Swole.UI
{
    public class UICutoutMask : Image
    {

        private static Material cutoutMaterial;
        private static Material GetCutoutMaterial(Material baseMaterial)
        {
            if (cutoutMaterial != null) return cutoutMaterial;
            cutoutMaterial = new Material(baseMaterial);
            cutoutMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);  
            return cutoutMaterial;
        }

        public override Material materialForRendering => GetCutoutMaterial(base.materialForRendering);

    }
}

#endif