using System.Collections.Generic;
using UnityEngine;

namespace AssetGuidMap
{
    [CreateAssetMenu(fileName = "AssetGuidBucketStorage", menuName = "ScriptableObjects/AssetGuidBucketStorage")]
    internal class BucketStorage : ScriptableObject
    {
        [SerializeField]
        internal List<Bucket> buckets = new List<Bucket>();
    }
}
