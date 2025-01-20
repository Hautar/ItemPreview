using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("AssetGuidMap.Editor")]

namespace AssetGuidMap
{
    [Serializable]
    public struct AssetGuid : IComparable<AssetGuid>, IEqualityComparer<AssetGuid>
    {
        public const uint Invalid  = uint.MinValue;
        public const uint MaxValue = uint.MaxValue;

        //guid range for automatic management
        public const uint ManualStart   = 1u;
        public const uint ManualEnd     = 1000000u;
        
        //guid range for manual management
        public const uint AutoStart = ManualEnd + 1u;
        public const uint AutoEnd   = uint.MaxValue;

        [SerializeField]
        public uint Value;

        public override int GetHashCode() => unchecked((int)Value);

        public static implicit operator uint(AssetGuid x) => x.Value;
        public static implicit operator AssetGuid(uint x) => new(){Value = x};
        public static implicit operator AssetGuid(string asset) => asset?.ToAssetGuid() ?? default;

        public int CompareTo(AssetGuid other) => Value.CompareTo(other.Value);

        public bool Equals(AssetGuid x, AssetGuid y) => x.Value == y.Value;

        public int GetHashCode(AssetGuid obj) => unchecked((int)obj.Value);

        public bool IsValid => Value != Invalid;

        public override string ToString() => this.ToAssetName();

        public static AssetGuid FromString(string assetName) => assetName.ToAssetGuid();
    }
}
