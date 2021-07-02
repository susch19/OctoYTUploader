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

namespace OctoThumbnailGenerator
{
    public class Program
    {
        static int Main(string[] args)
        {

            Console.Write("Folgennummer von: ");
            var fromNumberInclusive = int.Parse(Console.ReadLine());
            Console.Write("Folgennummer bis: ");
            var toNumberInclusive = int.Parse(Console.ReadLine());

            const string resultDirectory = "Fertige Thumbnails";
            if (!Directory.Exists(resultDirectory))
                Directory.CreateDirectory(resultDirectory);


            const string fontName = "OctoSeason4FontOrangeBorder";
            // input and output files are supposed to be in the program folder
            var inputPath = "octoawesome.png";

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // INITIALIZATION ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // initialize the D3D device which will allow to render to image any graphics - 3D or 2D
            using var defaultDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                              d3d.DeviceCreationFlags.VideoSupport
                                                              | d3d.DeviceCreationFlags.BgraSupport
                                                              | d3d.DeviceCreationFlags.None); // take out the Debug flag for better performance

            using var d3dDevice = defaultDevice.QueryInterface<d3d.Device1>(); // get a reference to the Direct3D 11.1 device
            using var dxgiDevice = d3dDevice.QueryInterface<dxgi.Device>(); // get a reference to DXGI device

            using var d2dDevice = new d2.Device(dxgiDevice); // initialize the D2D device

            using var imagingFactory = new wic.ImagingFactory2(); // initialize the WIC factory

            // initialize the DeviceContext - it will be the D2D render target and will allow all rendering operations
            using var d2dContext = new d2.DeviceContext(d2dDevice, d2.DeviceContextOptions.None);

            using var dwFactory = new dw.Factory();

            // specify a pixel format that is supported by both D2D and WIC
            var d2PixelFormat = new d2.PixelFormat(dxgi.Format.R8G8B8A8_UNorm, d2.AlphaMode.Premultiplied);
            // if in D2D was specified an R-G-B-A format - use the same for wic
            var wicPixelFormat = wic.PixelFormat.Format32bppPRGBA;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // IMAGE LOADING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            using var decoder = new wic.PngBitmapDecoder(imagingFactory); // we will load a PNG image
            using var inputStream = new wic.WICStream(imagingFactory, inputPath, NativeFileAccess.Read); // open the image file for reading
            decoder.Initialize(inputStream, wic.DecodeOptions.CacheOnLoad);

            // decode the loaded image to a format that can be consumed by D2D
            using var formatConverter = new wic.FormatConverter(imagingFactory);
            formatConverter.Initialize(decoder.GetFrame(0), wicPixelFormat);

            // load the base image into a D2D Bitmap
            //using var inputBitmap = d2.Bitmap1.FromWicBitmap(d2dContext, formatConverter, new d2.BitmapProperties1(d2PixelFormat));

            // store the image size - output will be of the same size
            var inputImageSize = formatConverter.Size;
            var pixelWidth = inputImageSize.Width;
            var pixelHeight = inputImageSize.Height;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // EFFECT SETUP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // Effect 1 : BitmapSource - take decoded image data and get a BitmapSource from it
            using var bitmapSourceEffect = new d2.Effects.BitmapSource(d2dContext);
            bitmapSourceEffect.WicBitmapSource = formatConverter;



            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // OVERLAY TEXT SETUP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            using var textFormat = new dw.TextFormat(dwFactory, fontName, 260f); // create the text format of specified font configuration

            // draw a long text to show the automatic line wrapping

            // create the text layout - this improves the drawing performance for static text
            // as the glyph positions are precalculated

            using var textBrush = new d2.SolidColorBrush(d2dContext, Color.LightGreen);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // RENDER TARGET SETUP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // create the d2d bitmap description using default flags (from SharpDX samples) and 96 DPI
            var d2dBitmapProps = new d2.BitmapProperties1(d2PixelFormat, 96, 96, d2.BitmapOptions.Target | d2.BitmapOptions.CannotDraw);

            // the render target
            using var d2dRenderTarget = new d2.Bitmap1(d2dContext, new Size2(pixelWidth, pixelHeight), d2dBitmapProps);
            d2dContext.Target = d2dRenderTarget; // associate bitmap with the d2d context

            for (int i = fromNumberInclusive; i <= toNumberInclusive; i++)
            {


                var outputPath = Path.Combine(resultDirectory, $"{i}.png");
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // DRAWING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // slow preparations - fast drawing:

                var textToDraw = "#" + i;

                using var textLayout = new dw.TextLayout(dwFactory, textToDraw, textFormat, 1714f, 1000f);
                d2dContext.BeginDraw();
                d2dContext.Clear(null);
                d2dContext.DrawImage(bitmapSourceEffect);
                d2dContext.DrawTextLayout(new Vector2(900, 50), textLayout, textBrush, d2.DrawTextOptions.EnableColorFont);
                d2dContext.EndDraw();

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // IMAGE SAVING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                // delete the output file if it already exists
                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                // use the appropiate overload to write either to stream or to a file
                using var stream = new wic.WICStream(imagingFactory, outputPath, NativeFileAccess.Write);

                // select the image encoding format HERE
                using var encoder = new wic.PngBitmapEncoder(imagingFactory);
                encoder.Initialize(stream);

                using var bitmapFrameEncode = new wic.BitmapFrameEncode(encoder);
                bitmapFrameEncode.Initialize();
                bitmapFrameEncode.SetSize(pixelWidth, pixelHeight);
                bitmapFrameEncode.SetPixelFormat(ref wicPixelFormat);

                // this is the trick to write D2D1 bitmap to WIC
                using var imageEncoder = new wic.ImageEncoder(imagingFactory, d2dDevice);
                imageEncoder.WriteFrame(d2dRenderTarget, bitmapFrameEncode, new wic.ImageParameters(d2PixelFormat, 96, 96, 0, 0, pixelWidth, pixelHeight));

                bitmapFrameEncode.Commit();
                encoder.Commit();

            }
            return 0;
        }
    }
}
