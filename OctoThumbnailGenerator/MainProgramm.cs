using SharpDX;
using SharpDX.IO;
// use namespaces shortcuts to reduce typing and avoid the messing the same class names from different namespaces
using d2 = SharpDX.Direct2D1;
using d3d = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;
using wic = SharpDX.WIC;
using dw = SharpDX.DirectWrite;
using System;
using System.IO;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Microsoft.VisualBasic.CompilerServices;

namespace OctoThumbnailGenerator
{
    public class MainProgramm
    {
        const string inputPath = "octoawesome.png";
        const string fontName = "OctoSeason4FontOrangeBorder";

        static int Main(string[] args)
        {

            Console.Write("Folgennummer von: ");
            var fromNumberInclusive = int.Parse(Console.ReadLine());
            Console.Write("Folgennummer bis: ");
            var toNumberInclusive = int.Parse(Console.ReadLine());

            const string resultDirectory = "Fertige Thumbnails";
            if (!Directory.Exists(resultDirectory))
                Directory.CreateDirectory(resultDirectory);


            // input and output files are supposed to be in the program folder

            using var helper = new ImageHelper(inputPath, fontName);

            for (int i = fromNumberInclusive; i <= toNumberInclusive; i++)
            {

                var outputPath = Path.Combine(resultDirectory, $"{i}.png");

                // delete the output file if it already exists
                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                using (var str = File.OpenWrite(outputPath))
                    helper.GenerateThumbnail(i, str);


            }
            return 0;
        }

        public static void GenerateThumbnail(int version, Stream output)
        {
            using var helper = new ImageHelper(inputPath, fontName);
            helper.GenerateThumbnail(version, output);

        }
    }
}
