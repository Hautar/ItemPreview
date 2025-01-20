using System;
using AssetLoadingManager;
using CommonExtensions;
using Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ItemPreview.PreviewCreation
{
    public class ItemPreviewContainer
    {
        // in
        public readonly Action<Texture2D>    Complete;
        public readonly Action               Failed;
        private readonly CamCaptureProcessor captureProcessor;
        private readonly PreviewDescription  previewDescription;
        private readonly GameObject          placeHolder;
        
        // external control
        public ContainerState State;
        
        // self
        private GameObject  asset;
        
        // out
        public Texture2D  Texture2D;

        // const bounds
        private static readonly Vector3 BoundsCenterOffset = new(-0.1f, 0.6f, 0);
        private static readonly float3  MaxBoundsSize = new(1.2f, 2f, 1.2f);
        private static readonly float3  MinBoundsSize = MaxBoundsSize * 0.75f;

        public ItemPreviewContainer(CamCaptureProcessor captureProcessor,
                                    GameObject placeHolder,
                                    PreviewDescription previewDescription,
                                    Action<Texture2D> complete,
                                    Action failed)
        {
            this.captureProcessor   = captureProcessor;
            this.previewDescription = previewDescription;
            this.placeHolder        = placeHolder;
            
            Complete = complete;
            Failed = failed;

            State = ContainerState.Created;
        }

        public void LoadAssetsAsync()
        {
            Debug.Assert(State == ContainerState.Created, "State == ContainerState.Ready");

            State = ContainerState.Loading;
            MonoBehaviourRunner.EnqueueFixedUpdate(LoadAsset);

            void LoadAsset()
            {
                AssetManager.Instance.LoadAssetAsync<GameObject>(previewDescription.AssetGuid, gameObject =>
                {
                    asset = gameObject;
                    asset.SetActive(false);
                    asset.SetupLayerRecursive(ItemPreviewFactory.PreviewLayer);
                    State = ContainerState.Ready;
                }, () => State = ContainerState.Failed);
            }
        }
        
        public void CreatePreview(Transform viewPoint)
        {
            Debug.Assert(State == ContainerState.Ready, "State == ContainerState.Ready");

            if (asset == null)
            {
                Debug.LogError("Asset == null");
                State = ContainerState.Failed;
                return;
            }
            
            // determine if should use placeHolder instead of original object
            var targetAsset = IsAnyRendererEnabled(asset) ? asset : placeHolder;
            var otherAsset = targetAsset == asset ? placeHolder : asset;
            
            // disable not used object and enable used object
            targetAsset.SetActive(true);
            otherAsset.SetActive(false);
            
            // fit targetAsset to viewPoint
            FitGameObjectToView(targetAsset, viewPoint);

            // capture screenshot
            Texture2D = captureProcessor.CaptureScreenshotWithTransparency();
            
            // cleanup
            targetAsset.SetActive(false);
            UnityEngine.Object.DestroyImmediate(asset);
            
            // progress further
            State = Texture2D != null ? ContainerState.Processed : ContainerState.Failed;
        }

        private static bool IsAnyRendererEnabled(GameObject asset)
        {
            if (asset == null)
                return false;
            
            asset.SetActive(true);
            var meshRenderers = asset.GetComponentsInChildren<MeshRenderer>();

            var isAnyRendererEnabled = false;
            foreach (var meshRenderer in meshRenderers)
            {
                if (!meshRenderer.enabled)
                    continue;

                isAnyRendererEnabled = true;
                break;
            }

            asset.SetActive(false);
            return isAnyRendererEnabled;
        }

        private static void FitGameObjectToView(GameObject asset, Transform viewPoint)
        {
            var meshRenderers = asset.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length <= 0)
                return;

            asset.transform.localPosition = Vector3.zero;
            asset.transform.SetParent(viewPoint);
            
            var camCenter = viewPoint.position + BoundsCenterOffset;
            var transform = asset.transform;
            transform.position = camCenter;
            
            RenderExtensions.FitTransform(meshRenderers,
                                          MinBoundsSize,
                                          MaxBoundsSize,
                                          out var scale,
                                          out var offset);
            
            transform.localScale = new float3(scale);
            transform.position = camCenter + (Vector3) offset;
        }

        public void ClearAsset()
        {
            if (asset == null)
                return;
            
            UnityEngine.Object.Destroy(asset);
        }

        public void ClearTexture()
        {
            if (Texture2D == null)
                return;
            
            UnityEngine.Object.Destroy(Texture2D);
        }
    }
}