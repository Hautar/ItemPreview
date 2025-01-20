using System.IO;

namespace ItemPreview.DataRecognition
{
    public interface IHashScheme
    {
        public bool TryParseFileInfo(FileInfo fileInfo, out int[] hash);

        public string GenerateFileName(int[] hash);

        public bool TryGetResolution(FileInfo fileInfo, out (int Width, int Height) resolution);
    }
}