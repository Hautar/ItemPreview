using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ItemPreview.DataRecognition;
using Tasks;
using UnityEngine;

namespace ItemPreview
{
    public class ItemPreviewCache
    {
        private readonly ConcurrentDictionary<int[], string> imageNameOverPreviewData = new();
        private readonly ConcurrentDictionary<int[], Sprite> spriteOverPreviewData    = new();
        
        private readonly ItemPreviewSettings settings;
        private readonly IHashScheme hashScheme;
        
        private int resolutionX;
        private int resolutionY;
        
        public ItemPreviewCache(IHashScheme hashScheme)
        {
            this.hashScheme = hashScheme;
        }

        public void Initialize(int resolutionX, int resolutionY)
        {
            Debug.Assert(resolutionX > 0 && resolutionY > 0, "width > 0 && height > 0");
            
            this.resolutionX = resolutionX;
            this.resolutionY = resolutionY;
            ParseSourceDirectory();
        }

        private void ParseSourceDirectory()
        {
            var sourceDirectory = new DirectoryInfo(SourceDirectory);

            // all okay
            if (!sourceDirectory.Exists)
            {
                sourceDirectory.Create();
                return;
            }

            // cleanup nested directories
            var nestedDirectories = sourceDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var nestedDirectory in nestedDirectories)
            {
                Task.Factory.StartNew(path => Directory.Delete((string)path, true), nestedDirectory.FullName);
            }

            var fileInfos = sourceDirectory.GetFiles("*.png", SearchOption.TopDirectoryOnly);
            
            // validate dimensions
            foreach (var fileInfo in fileInfos)
            {
                if (hashScheme.TryGetResolution(fileInfo, out var resolution) &&
                    Math.Abs(resolution.Width - resolutionX) < 0.1 && Math.Abs(resolution.Height - resolutionY) < 0.1)
                    continue;
                
                Task.Factory.StartNew(() => fileInfo.Delete());
                // Debug.Log($"Preview has invalid resolution: {fileInfo.Name}");
            }
            
            // collect fileInfos
            foreach (var fileInfo in fileInfos)
            {
                if (!fileInfo.Exists)
                    continue;

                // validate hash is correct
                if(!hashScheme.TryParseFileInfo(fileInfo, out var hash))
                {
                    Task.Factory.StartNew(() => fileInfo.Delete());
                    // Debug.Log($"Preview has invalid name: {fileInfo.Name}");
                    continue;
                }

                imageNameOverPreviewData[hash] = fileInfo.Name;
            }            
        }
        
        public void AddSprite(int[] hash, Sprite sprite)
        {
            Debug.Assert(!spriteOverPreviewData.ContainsKey(hash), "!spriteOverPreviewData.ContainsKey(hash)");
            spriteOverPreviewData[hash] = sprite;
        }

        public void SaveAsync(int[] hash, Texture2D texture2D, Action onComplete, Action onFailed)
        {
            if (HasPreviewInLocalStorage(hash))
            {
                onComplete();
                return;
            }

            if (texture2D == null)
            {
                onFailed();
                return;
            }
            
            var fileName = hashScheme.GenerateFileName(hash);
            var filePath = GetFullPath(fileName);
            
            try
            {
                var byteArray = texture2D.EncodeToPNG();
                File.WriteAllBytes(filePath, byteArray);
                imageNameOverPreviewData[hash] = fileName;
                onComplete();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.ToString());
                onFailed();
            }
        }
        
        public bool TryGetSpriteFromRunTimeCache(int[] hash, out Sprite sprite) =>
            spriteOverPreviewData.TryGetValue(hash, out sprite);

        public bool HasPreviewInLocalStorage(int[] hash) => imageNameOverPreviewData.ContainsKey(hash);

        public void GetSpriteFromStorageAsync(int[] hash, Action<Sprite> onComplete, Action onFailed)
        {
            if (spriteOverPreviewData.TryGetValue(hash, out var sprite))
            {
                onComplete?.Invoke(sprite);
                return;
            }

            if (!imageNameOverPreviewData.TryGetValue(hash, out var previewName))
            {
                onFailed?.Invoke();
                return;
            }

            byte[] byteArray = null;
            Sprite newSprite = null;

            SequentialAction
                .Create("Load Image From Disk Async", () => onComplete?.Invoke(newSprite), onFailed)
                .DontLogErrors()
                .Then((partialComplete, partialFailed) =>
                {
                    try
                    {
                        byteArray = File.ReadAllBytes(GetFullPath(previewName));
                        partialComplete?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(exception.ToString());
                        partialFailed?.Invoke();
                    }
                })
                    .WithThreadOptions(ThreadOptions.ForceNewThread)
                .Then((partialComplete, partialFailed) =>
                {
                    if (!TryCreateSprite(byteArray, out newSprite))
                    {
                        partialFailed?.Invoke();
                        return;
                    }

                    spriteOverPreviewData[hash] = newSprite;
                    partialComplete?.Invoke();
                })
                    .WithThreadOptions(ThreadOptions.MonoBehaviourFixedUpdate)
                .Run();
        }
        
        private static bool TryCreateSprite(byte[] byteArray, out Sprite sprite)
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                Debug.LogError($"Can't create textures not from main thread!");
                sprite = null;
                return false;
            }
            
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(byteArray);
            texture.Apply();

            var rect = new Rect(0, 0, texture.width, texture.height);
            var pivot = new Vector2(0.5f, 0.5f);
            sprite = Sprite.Create(texture, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
            return true;
        }

        public void ClearRuntimeCache()
        {
            foreach (var (_, sprite) in spriteOverPreviewData)
            {
                if (sprite != null && sprite.texture != null)
                    UnityEngine.Object.Destroy(sprite.texture);

                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
            }

            spriteOverPreviewData.Clear();
        }

        // kostyl for code sample
        private static string cachePath;
        private static string SourceDirectory
        {
            get
            {
                cachePath ??= Path.Combine(Application.persistentDataPath, "ItemPreview");
                return cachePath;
            }
        }
        
        private static string GetFullPath(string imageName) => Path.Combine(SourceDirectory, imageName);
    }
}