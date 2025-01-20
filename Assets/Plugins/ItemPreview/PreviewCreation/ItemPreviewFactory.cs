using System;
using System.Collections.Generic;
using System.Linq;
using CommonExtensions;
using Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.Universal;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace ItemPreview.PreviewCreation
{
    public class ItemPreviewFactory
    {
        public const int  PreviewLayer  = 10;
        private const int RendererIndex = 0;
        
        private const string CameraObjectName = "Camera";
        private const string HolderObjectName = "Placeholder";
        private const string LightObjectName  = "Light";
        private const string SceneName        = "AutoRender";
        
        private bool          isInitialized;
        private Camera        camera;
        private Transform     viewPoint;
        private Transform     lightParent;
        private SceneInstance sceneInstance;
        private GameObject[]  rootObjects;
        
        private GameObject placeholder;
        private RenderTexture outputTexture;
        private CamCaptureProcessor camCaptureProcessor;

        private readonly List<ItemPreviewContainer> previewContainers = new();

        public int ResolutionX => outputTexture.width;
        public int ResolutionY => outputTexture.height;

        public void InitializeAsync(Action onComplete, Action onFailed)
        {
            SequentialAction
                .Create("Initialize Item Preview Scene Async", OnComplete, OnFailed)
                .Then(LoadSceneAsync)
                    .WithThreadOptions(ThreadOptions.MonoBehaviourFixedUpdate)
                .Then(ParseSceneContentAsync)
                .Then(PrepareObjects)
                .Then(LoadRenderTextureAsync)
                .Then(LoadPlaceholder)
                    .WithThreadOptions(ThreadOptions.MonoBehaviourFixedUpdate)
                .Then(() => camCaptureProcessor = new CamCaptureProcessor(camera, outputTexture))
                    .WithThreadOptions(ThreadOptions.MonoBehaviourFixedUpdate)
                .Run();

            void LoadRenderTextureAsync(Action onComplete, Action onFailed)
            {
                var handle = Resources.LoadAsync<RenderTexture>("ItemPreview/RT_ItemPreviewCamOutput");
                handle.completed += _ =>
                {
                    if (!handle.isDone || handle.asset == null)
                    {
                        onFailed();
                        return;
                    }

                    outputTexture = (RenderTexture)handle.asset;
                    onComplete();
                };
            }

            void LoadPlaceholder(Action onComplete, Action onFailed)
            {
                var handle = Resources.LoadAsync<GameObject>("Prefabs/Pref_CubePrimitive");
                handle.completed += _ =>
                {
                    if (!handle.isDone || handle.asset == null)
                    {
                        onFailed();
                        return;
                    }

                    placeholder = UnityEngine.Object.Instantiate((GameObject)handle.asset);
                    placeholder.SetupLayerRecursive(PreviewLayer);
                    placeholder.SetActive(false);
                    SceneManager.MoveGameObjectToScene(placeholder, sceneInstance.Scene);
                    onComplete();
                };
            }
            
            void OnComplete()
            {
                onComplete();
                isInitialized = true;
            }

            void OnFailed()
            {
                if (sceneInstance.Scene.isLoaded)
                {
                    var operation = SceneManager.UnloadSceneAsync(sceneInstance.Scene);
                    operation.completed += _ => onFailed?.Invoke();
                    return;
                }
                
                onFailed?.Invoke();
            }
        }

        public void Update()
        {
            if (!isInitialized)
                return;
            
            // in process
            foreach (var container in previewContainers)
            {
                if (container.State == ContainerState.Loading)
                    continue;
                
                switch (container.State)
                {
                    case ContainerState.Created:
                    {
                        container.LoadAssetsAsync();
                        break;
                    }
                    case ContainerState.Ready:
                    {
                        container.CreatePreview(viewPoint);
                        break;
                    }
                }
            }
            
            // finished
            for (int k = previewContainers.Count - 1; k > -1; k--)
            {
                var container = previewContainers[k];
                
                if (container.State == ContainerState.Loading)
                    continue;
                
                switch (container.State)
                {
                    case ContainerState.Processed:
                    {
                        container.Complete(container.Texture2D);
                        previewContainers.RemoveAt(k);
                        break;
                    }
                    case ContainerState.Failed:
                    {
                        container.Failed();
                        container.ClearAsset();
                        container.ClearTexture();
                        previewContainers.RemoveAt(k);
                        break;
                    }
                }
            }
        }

        public void SchedulePreviewCreation(PreviewDescription previewDescription,
                                            Action<Texture2D> onComplete, Action onFailed)
        {
            if (camCaptureProcessor == null)
            {
                Debug.LogError($"Service is not initialized!");
                return;
            }
            
            var previewContainer = new ItemPreviewContainer(camCaptureProcessor,
                                                            placeholder,
                                                            previewDescription,
                                                            onComplete,
                                                            onFailed);            
            previewContainers.Add(previewContainer);
        }
        
        private void LoadSceneAsync(Action onComplete, Action onFailed)
        {
            var loadSceneOperation = Addressables.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            loadSceneOperation.Completed += (operationHandle) =>
            {
                if (loadSceneOperation.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load scene: {SceneName}");
                    onFailed?.Invoke();
                    return;
                }

                sceneInstance = operationHandle.Result;
                onComplete?.Invoke();
            };
        }

        private void ParseSceneContentAsync(Action onComplete, Action onFailed)
        {
            rootObjects = sceneInstance.Scene.GetRootGameObjects();

            var potentialCameraObject = rootObjects
                .FirstOrDefault(t => t.name.Equals(CameraObjectName, StringComparison.InvariantCultureIgnoreCase));
            
            var potentialHolderObject = rootObjects
                .FirstOrDefault(t => t.name.Equals(HolderObjectName, StringComparison.InvariantCultureIgnoreCase));

            var potentialLightObject = rootObjects
                .FirstOrDefault(t => t.name.Equals(LightObjectName, StringComparison.InvariantCultureIgnoreCase));

            if (potentialCameraObject == null || potentialHolderObject == null || potentialLightObject == null)
            {
                Debug.LogError($"Failed to find objects: {CameraObjectName}, {HolderObjectName}, {LightObjectName}");
                onFailed?.Invoke();
                return;
            }

            camera = potentialCameraObject.GetComponent<Camera>();
            viewPoint = potentialHolderObject.GetComponent<Transform>();
            lightParent = potentialLightObject.GetComponent<Transform>();

            if (camera == null)
            {
                Debug.LogError($"Failed to get camera component on {potentialCameraObject.name}");
                onFailed?.Invoke();
                return;
            }
            
            onComplete?.Invoke();
        }

        private void PrepareObjects()
        {
            // layers
            foreach (var gameObject in rootObjects)
            {
                gameObject.SetupLayerRecursive(PreviewLayer);
            }
            
            // camera settings
            camera.cullingMask     = 1 << PreviewLayer;
            camera.depth           = 10; // render priority (bigger => worse)
            camera.clearFlags      = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            
            // set renderer
            var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            additionalCameraData.SetRenderer(RendererIndex); 
            additionalCameraData.renderPostProcessing = false;
            
            // light settings
            var lights = lightParent.GetComponentsInChildren<Light>();
            foreach (var light in lights)
            {
                light.cullingMask = 1 << PreviewLayer;
            }
        }
    }
}