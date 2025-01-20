using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AssetGuidMap;
using ItemPreview.CallbackStore;
using ItemPreview.DataRecognition;
using ItemPreview.PreviewCreation;
using Tasks;
using UnityEngine;

namespace ItemPreview
{
    internal class ItemPreviewApi
    {
        private readonly ItemPreviewFactory factory;
        private readonly ItemPreviewCache cache;

        private readonly Dictionary<int[], PreviewDescription> toProcess = new();
        private readonly ConcurrentDictionary<int[], PreviewDescription> inProcess = new();

        private readonly CallbackStorage<int[], Sprite> callbackStorage = new();
        
        private readonly ItemPreviewSettings settings;

        public ItemPreviewApi(IHashScheme hashScheme, ItemPreviewSettings settings)
        {
            this.settings = settings;
            factory = new ItemPreviewFactory();
            cache   = new ItemPreviewCache(hashScheme);
        }

        public void Initialize(Action onComplete, Action onFailed)
        {
            SequentialAction
                .Create("Initialize AutoRenderApi", onComplete, onFailed)
                    .LogDuration()
                .Then(factory.InitializeAsync)
                .Then(() => cache.Initialize(factory.ResolutionX, factory.ResolutionY))
                .Run();
        }

        public void GetPreviewAsync(string objectAssetName,
                                    Action<Sprite> onComplete,
                                    Action onFailed = null)
        {
            // invalid guid => can't load
            if (!objectAssetName.TryGetAssetGuid(out AssetGuid objAssetGuid))
            {
                Debug.LogError($"Missing assetGuid for {objectAssetName}");
                onFailed?.Invoke();
                return;
            }

            // generate hash
            var intObjGuid = (int) (uint) objAssetGuid;
            var hash = new int[] { intObjGuid, factory.ResolutionX, factory.ResolutionY };

            // is in runtime cache?
            if (cache.TryGetSpriteFromRunTimeCache(hash, out var sprite))
            {
                onComplete?.Invoke(sprite);
                return;
            }

            // add to callback storage
            callbackStorage.Add(hash, onComplete, onFailed);

            // is already processing => don't schedule creation
            if (toProcess.ContainsKey(hash) || inProcess.ContainsKey(hash))
                return;

            toProcess.Add(hash, new PreviewDescription()
            {
                AssetGuid = objAssetGuid,
            });
        }

        public void Update()
        {
            factory.Update();
            
            // isBusy rn
            if (inProcess.Count >= settings.PicturesPerBatch)
                return;

            var descriptions = new Dictionary<int[], PreviewDescription>();

            // move from toProcess to inProcess
            var hashes = toProcess.Keys.ToList();
            for (int k = hashes.Count - 1; k > -1; k--)
            {
                if (inProcess.Count >= settings.PicturesPerBatch)
                    break;

                var hash = hashes[k];
                var previewDescription = toProcess[hash];

                toProcess.Remove(hash);
                inProcess.AddOrUpdate(hash, previewDescription, (_, _) => previewDescription);

                // schedule loading
                descriptions.Add(hash, previewDescription);
            }

            // nothing to schedule
            if (descriptions.Count == 0)
                return;

            // launch preview creation
            foreach (var keyValue in descriptions)
            {
                var hash = keyValue.Key;
                var previewDescription = keyValue.Value;

                if (cache.HasPreviewInLocalStorage(hash))
                {
                    ProcessCachedAsync(hash);
                }
                else
                {
                    ProcessNewAsync(hash, previewDescription);
                }
            }
        }

        private void ProcessCachedAsync(int[] hash)
        {
            cache.GetSpriteFromStorageAsync(hash, sprite =>
            {
                inProcess.TryRemove(hash, out var _);
                callbackStorage.Invoke(hash, sprite, true);
            }, () =>
            {
                inProcess.TryRemove(hash, out var _);
                callbackStorage.Invoke(hash, null, false);
            });
        }

        private void ProcessNewAsync(int[] hash, PreviewDescription previewDescription)
        {
            Texture2D texture2D = null;
            Sprite sprite       = null;
            
            SequentialAction
                .Create("Process New Item Async", null, () => ProcessCallback(false))
                .Then((complete, failed) =>
                {
                    factory.SchedulePreviewCreation(previewDescription, t =>
                    {
                        texture2D = t;
                        complete();
                    }, failed);
                })
                .Then(CreateSprite)
                    .WithThreadOptions(ThreadOptions.MonoBehaviourFixedUpdate)
                // fire callback before writing output to file to speed up a little bit
                .Then(() => ProcessCallback(true))
                .Then((complete, failed) =>
                {
                    // clearRuntimeCache was called during the sequence
                    if (sprite == null || sprite.texture == null)
                    {
                        complete();
                        return;
                    }

                    cache.SaveAsync(hash, sprite.texture, complete, failed);
                })
                    .WithThreadOptions(ThreadOptions.MonoBehaviourFixedUpdate)
                .Run();

            void ProcessCallback(bool result)
            {
                inProcess.TryRemove(hash, out _);
                callbackStorage.Invoke(hash, sprite, result);
            }
            
            void CreateSprite()
            {
                var rect = new Rect(0, 0, texture2D.width, texture2D.height);
                var pivot = new Vector2(0.5f, 0.5f);
                sprite = Sprite.Create(texture2D, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
                
                cache.AddSprite(hash, sprite);
            }
        }

        public void ClearRuntimeCache()
        {
            // clear scheduled callbacks
            callbackStorage.Clear();
            toProcess.Clear();
            inProcess.Clear();
            
            // clear textures and sprites
            cache.ClearRuntimeCache();
        }
    }
}