using System;
using Unity.Mathematics;
using UnityEngine;

namespace CommonExtensions
{
    public static class RenderExtensions
    {
        private static readonly Vector3 BoundsCenter = new(-4.075f, -0.65f, 57.8f);
        private static readonly Vector3 BoundsSize   = new(3.38f, 5f, 3.38f);
        private static readonly Vector3 MINSize      = new(0.05f, 0.05f, 0.05f);

        public static float GetFittingScale(MeshRenderer[] targetRenderers, float3 center)
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                Debug.LogError("FittingScale. targetRenderers list is null or empty");
                return 1f;
            }

            Bounds fitInto = new Bounds(BoundsCenter, BoundsSize);
            Bounds fitAll  = new Bounds(center, MINSize);
            foreach (var r in targetRenderers)
            {
                fitAll.Encapsulate(r.bounds);
            }

            var before = fitAll.size.x;
            fitAll.SetMinMax(fitInto.min, fitInto.max);
            var after = fitAll.size.x;

            return after / before;
        }

        private static readonly float3 MaxBoundsSize = new(3.38f, 5f, 3.38f);
        private static readonly float3 MinBoundsSize = MaxBoundsSize * 0.75f;

        public static void FitTransform(Renderer[] meshRenderers,
                                        out float scale, out float3 offset)
        {
            FitTransform(meshRenderers, MinBoundsSize, MaxBoundsSize, out scale, out offset);
        }

        public static void FitTransform(Renderer[] meshRenderers,
                                        float3 minBoundSize,
                                        float3 maxBoundSize,
                                        out float scale,
                                        out float3 offset)
        {
            scale = 1f;
            offset = 0f;

            if (!CalculateRendererBounds(meshRenderers, out var rendererBounds))
                return;

            var rendererSize = (float3)rendererBounds.size;

            if (math.any(rendererSize > maxBoundSize))
                scale = math.cmin(maxBoundSize / rendererSize);
            else if (math.all(rendererSize < minBoundSize))
                scale = math.cmin(minBoundSize / rendererSize);

            offset = -(float3)rendererBounds.center * scale;
        }
        
        public static (float scale, float3 offset) FitTransform(MeshRenderer[] meshRenderers,
                                                                float3 position,
                                                                float3 minBoundSize,
                                                                float3 maxBoundSize)
        {
            Debug.Assert(meshRenderers is {Length: > 0}, "meshRenderers is {Length: > 0}");
            
            var scale  = 1f;
            var offset = float3.zero;

            CalculateRendererBounds(meshRenderers, out var rendererBounds);

            var rendererSize = (float3)rendererBounds.size;

            if (math.any(rendererSize > maxBoundSize))
                scale = math.cmin(maxBoundSize / rendererSize);
            else if (math.all(rendererSize < minBoundSize))
                scale = math.cmin(minBoundSize / rendererSize);

            offset = (position - (float3) rendererBounds.center) * scale;
            
            return (scale, offset);
        }

        private const int MinVertices = 100;
        private const int MaxVertices = 1000;

        private static bool CalculateRendererBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogError("FittingScale. renderers list is null or empty");
                return false;
            }

            bounds = renderers[0].localBounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].localBounds);

            return true;
        }

        public static bool CalculateMeshBounds(MeshFilter[] meshFilters, out Bounds bounds)
        {
            bounds = default;
            if (meshFilters == null || meshFilters.Length == 0 && meshFilters[0].sharedMesh.vertices.Length > 0)
            {
                Debug.LogError("FittingScale. meshFilters list is null or empty");
                return false;
            }

            var initialized = false;
            for (var i = 0; i < meshFilters.Length; i++)
            {
                var meshFilter = meshFilters[i];

                if (CalculateMeshBounds(meshFilter, out var meshBounds))
                {
                    if (!initialized)
                    {
                        initialized = true;
                        bounds = meshBounds;
                    }
                    else
                        bounds.Encapsulate(meshBounds);
                }
            }

            return initialized;
        }

        private static bool CalculateMeshBounds(MeshFilter meshFilter, out Bounds bounds)
        {
            var mesh = meshFilter.sharedMesh;

            if (mesh.vertices.Length == 0)
            {
                bounds = new Bounds();
                return false;
            }

            var vertexCount = (int)Mathf.Lerp(MinVertices, MaxVertices, Mathf.InverseLerp(MinVertices, MaxVertices, mesh.vertices.Length / 10f));
            var increment = (int)MathF.Ceiling((float)mesh.vertices.Length / vertexCount);

            var transform = meshFilter.transform;
            bounds = new Bounds(transform.TransformPoint(mesh.vertices[0]), Vector3.zero);

            var counter = 1;
            for (var i = 1; i < mesh.vertices.Length; i += increment)
            {
                counter++;

                var position = transform.TransformPoint(mesh.vertices[i]);

                var min = Vector3.Min(bounds.min, position);
                var max = Vector3.Max(bounds.max, position);
                bounds.SetMinMax(min, max);
            }

            // Debug.Log($"VertexCounter = {counter}");
            return true;
        }

    }
}