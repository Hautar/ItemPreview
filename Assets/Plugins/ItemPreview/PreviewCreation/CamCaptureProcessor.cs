using Tasks;
using UnityEngine;

namespace ItemPreview.PreviewCreation
{
    public class CamCaptureProcessor
    {
        private int Width => outputTexture.width;
        private int Height => outputTexture.height;

        private readonly Camera camera;
        private readonly RenderTexture outputTexture;
        
        private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGBA32;

        public CamCaptureProcessor(Camera camera, RenderTexture outputTexture)
        {
            this.camera = camera;
            this.outputTexture = outputTexture;
            this.camera.targetTexture = outputTexture;
        }

        public Texture2D CaptureScreenshotWithTransparency()
        {
            Debug.Assert(MainThreadRunner.IsMainThread(), "MainThreadRunner.IsMainThread()");
            
            if (camera == null)
                return null;
            
            var defaultRenderTexture = RenderTexture.active;

            // set texture as active and clear it
            RenderTexture.active = outputTexture;
            GL.Clear(true, true, Color.clear);
            
            // render to outputTexture
            camera.Render();
            
            // copy render result to new texture
            var resultTexture = new Texture2D(Width, Height, TextureFormat, false);
            resultTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            resultTexture.Apply();

            // revert to default render texture
            RenderTexture.active = defaultRenderTexture;

            return resultTexture;
        }
    }
}