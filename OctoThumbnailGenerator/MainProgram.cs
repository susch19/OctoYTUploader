using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

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
            Image bitmap = Image.FromFile("octoawesome.png");
            if (!Directory.Exists("Fertige Thumbnails"))
                Directory.CreateDirectory("Fertige Thumbnails");
            Directory.SetCurrentDirectory("Fertige Thumbnails");
            Image newBmp;
            Brush b = new SolidBrush(Color.FromArgb(255, 36, 163, 179));
            Pen pen = new Pen(Color.Black, 10);
            Console.ForegroundColor = ConsoleColor.White;
            for (fromNumberInclusive = fromNumberInclusive; fromNumberInclusive <= toNumberInclusive; fromNumberInclusive++)
            {
                newBmp = (Image)bitmap.Clone();
                using (Graphics g = Graphics.FromImage(newBmp))
                {
                    GraphicsPath p = new GraphicsPath();
                    p.AddString(
                        "#" + fromNumberInclusive,
                        FontFamily.GenericSansSerif,
                        (int)FontStyle.Bold,
                        g.DpiY * 275 / 72,
                        new Point(3, 58),
                        new StringFormat());
                    g.DrawPath(pen, p);
                    g.FillPath(b, p);
                }
                newBmp.Save($"octoawesome{fromNumberInclusive}.png");
                Console.WriteLine($"octoawesome{fromNumberInclusive}.png saved");
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished. Press any key to close :)");
            Console.Read();
        }

        public static Image GenerateThumbnail(int version)
        {
            Image newBmp = Image.FromFile("octoawesome.png");
            Brush b = new SolidBrush(Color.FromArgb(255, 36, 163, 179));
            Pen pen = new Pen(Color.Black, 10);
            using (Graphics g = Graphics.FromImage(newBmp))
            {
                GraphicsPath p = new GraphicsPath();
                p.AddString(
                    "#" + version,
                    FontFamily.GenericSansSerif,
                    (int)FontStyle.Bold,
                    g.DpiY * 275 / 72,
                    new Point(3, 58),
                    new StringFormat());
                g.DrawPath(pen, p);
                g.FillPath(b, p);
                return newBmp;
            }
        }

    }
}
