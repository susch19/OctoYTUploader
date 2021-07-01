using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoThumbnailGenerator
{
    class ImageBasedFont
    {
        private readonly Dictionary<char, FileInfo> imagePaths;

        public ImageBasedFont(IEnumerable<(char Key, FileInfo Path)> images)
        {
            imagePaths = images.ToDictionary(c => c.Key, c => c.Path);
        }

        public Bitmap GetFont(char value, int width, int height)
        {
            if (imagePaths.TryGetValue(value, out var path))
            {
                using (var original = Image.FromFile(path.FullName))
                {
                    var bitmap = new Bitmap(width, height);
                    var graphic = Graphics.FromImage(bitmap);
                    graphic.DrawImage(original, 0,0, width, height);
                    return bitmap;
                }
            };

            return null;
        }

        public void DrawFont(Graphics graphics, Point position, string text, int margin, Func<Size, Size> rescale = null)
        {
            var lastPos = position.X;
            for (int i = 0; i < text.Length; i++)
            {
                if (imagePaths.TryGetValue(text[i], out var path))
                {
                    using (var original = Image.FromFile(path.FullName))
                    {
                        var usedSize = rescale == null ? original.Size : rescale(original.Size);
                        graphics.DrawImage(original, new Rectangle(new Point(lastPos, position.Y), usedSize));
                        lastPos += usedSize.Width + margin;
                    }
                }
            }
        }

    }
}
