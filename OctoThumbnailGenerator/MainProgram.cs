using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace OctoThumbnailGenerator
{
    public class MainProgram
    {
        public static void Main(string[] args)
        {
            Console.Write("Folgennummer von: ");
            var fromNumberInclusive = int.Parse(Console.ReadLine());
            Console.Write("Folgennummer bis: ");
            var toNumberInclusive = int.Parse(Console.ReadLine());
            var bitmap = Image.FromFile("octoawesome.png");
            if (!Directory.Exists("Fertige Thumbnails"))
                Directory.CreateDirectory("Fertige Thumbnails");
            Directory.SetCurrentDirectory("Fertige Thumbnails");

            var file = new FileInfo(@"D:\LP's\Stream\OctoAwesome\PNGFont\awesome.fdef");
            var folder = file.Directory;
            var defFile = JsonConvert.DeserializeObject<ImageFontDefiniton>(File.ReadAllText(file.FullName));

            var font = defFile.Mapping.Select(a => (a.Key[0], new FileInfo(Path.Combine(folder.FullName, a.Value))));
            var imageFont = new ImageBasedFont(font);

            Image newBmp;
            using (var b = new SolidBrush(Color.FromArgb(255, 254, 0, 0)))
            {
                for (; fromNumberInclusive <= toNumberInclusive; fromNumberInclusive++)
                {
                    newBmp = (Image)bitmap.Clone();
                    using (var g = Graphics.FromImage(newBmp))
                    {
                        imageFont.DrawFont(g, new Point(955,24), "#" + fromNumberInclusive, 0, rescale: size => RescaleCubic(size, height: size.Height - 194));
                    }
                    newBmp.Save($"octoawesome{fromNumberInclusive}.png");
                    Console.WriteLine($"octoawesome{fromNumberInclusive}.png saved");
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished. Press any key to close :)");
            Console.Read();
        }

        public static Image GenerateThumbnail(int version)
        {
            var newBmp = Image.FromFile("octoawesome.png");
            using (Brush b = new SolidBrush(Color.FromArgb(255, 254, 0, 0)))
            {
                using (var pen = new Pen(Color.Black, 10))
                {
                    using (var g = Graphics.FromImage(newBmp))
                    {
                        using (var p = new GraphicsPath())
                        {
                            p.AddString(
                                "#" + version,
                                FontFamily.Families.FirstOrDefault(f => f.Name == "Calibri"),
                                (int)FontStyle.Bold,
                                g.DpiY * 275 / 90,
                                new Point(540, 300),
                                new StringFormat());
                            g.DrawPath(pen, p);
                            g.FillPath(b, p);
                        }
                        return newBmp;
                    }
                }
            }
        }

        public static Size RescaleCubic(Size size, int? height = null, int? width = null)
        {
            var heightScale = height.HasValue ?  size.Height - height.Value : 0;
            var widhtScale = width.HasValue ? size.Width - width.Value : 0;

            if (heightScale == 0 && widhtScale != 0)
                heightScale = widhtScale;

            if (widhtScale == 0 && heightScale != 0)
                widhtScale = heightScale;

            return new Size(size.Width - widhtScale, size.Height - heightScale);
        }
    }
}
