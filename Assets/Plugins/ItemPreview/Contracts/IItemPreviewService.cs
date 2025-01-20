using System;
using UnityEngine;

namespace ItemPreview.Contracts
{
    public interface IItemPreviewService
    {
        void InitializeAsync(Action onComplete, Action onFailed);
        
        void GetPreviewAsync(string objectAssetName,
                             Action<Sprite> onComplete,
                             Action onFailed);

        void ClearRuntimeCache();
    }
}