using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.ItemPreview.Initialization
{
    // todo:
    // uncomment when we go from openGL to vulkan on Android and remove all force main thread on texture creations
    internal static class InitializeTextureSettings
    {
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        // private static void Init()
        // {
        //     if (!IsThreadedCreationSupported)
        //         return;
        //     
        //     Texture.allowThreadedTextureCreation = true;
        // }

        public static bool IsThreadedCreationSupported => SystemInfo.graphicsDeviceType is
                                                          GraphicsDeviceType.Metal or
                                                          GraphicsDeviceType.Vulkan or
                                                          GraphicsDeviceType.Direct3D11 or
                                                          GraphicsDeviceType.Direct3D12;
    }
}