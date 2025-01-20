using UnityEngine;

namespace CommonExtensions
{
    public static class GameObjectExtensions
    {
        public static void SetupLayerRecursive(this GameObject go, int layer)
        {
            go.layer = layer;
            var count = go.transform.childCount;
            for (var i = 0; i < count; ++i)
            {
                SetupLayerRecursive(go.transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
