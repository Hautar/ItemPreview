using System.IO;
using System.Text;

namespace ItemPreview.DataRecognition
{
    public class HashScheme : IHashScheme
    {
        private const int SplitLength  = 4;
        private const char SplitSymbol = '_';

        private const int WidthIndex = 2;
        private const int HeightIndex = 3;

        private const string ImageExtension = ".png";

        public bool TryParseFileInfo(FileInfo fileInfo, out int[] hash)
        {
            hash = null;
            
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            var split = fileName.Split(SplitSymbol);

            if (split.Length != SplitLength)
                return false;

            hash = new int[split.Length];
            for (int k = 0; k < split.Length; k ++)
            {
                if (!int.TryParse(split[k], out int hashElement))
                    return false;

                hash[k] = hashElement;
            }
            
            return true;
        }

        public string GenerateFileName(int[] hash)
        {
            StringBuilder builder = new();
            
            foreach (var hashElement in hash)
            {
                builder.Append(hashElement.ToString() + SplitSymbol);
            }            
            builder.Remove(builder.Length - 1, 1);
            
            builder.Append(ImageExtension);
            return builder.ToString();
        }

        public bool TryGetResolution(FileInfo fileInfo, out (int Width, int Height) resolution)
        {
            resolution = (0, 0);
            
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            var split = fileName.Split();

            if (split.Length != SplitLength || split.Length < HeightIndex)
                return false;

            if (!int.TryParse(split[WidthIndex], out var width) || !int.TryParse(split[HeightIndex], out var height))
                return false;

            resolution = (width, height);
            return true;
        }
    }
}