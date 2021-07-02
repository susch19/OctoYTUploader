using SharpDX.DirectWrite;
using SharpDX.IO;
using SharpDX;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.IO;
// use namespaces shortcuts to reduce typing and avoid the messing the same class names from different namespaces
using d2 = SharpDX.Direct2D1;
using d3d = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;
using wic = SharpDX.WIC;
using dw = SharpDX.DirectWrite;
using SharpDX.Direct3D11;
using SharpDX.Direct2D1.Effects;
using System.IO;

namespace OctoThumbnailGenerator
{
    internal class ImageHelper : IDisposable
    {
        private Guid wicPixelFormat;


        private readonly Device defaultDevice;
        private readonly Device1 d3dDevice;
        private readonly dxgi.Device dxgiDevice;
        private readonly d2.Device d2dDevice;
        private readonly wic.ImagingFactory2 imagingFactory;
        private readonly d2.DeviceContext d2dContext;
        private readonly Factory dwFactory;
        private readonly d2.PixelFormat d2PixelFormat;
        private readonly wic.PngBitmapDecoder decoder;
        private readonly wic.WICStream inputStream;
        private readonly wic.FormatConverter formatConverter;
        private readonly BitmapSource bitmapSourceEffect;
        private readonly TextFormat textFormat;
        private readonly d2.SolidColorBrush textBrush;
        private readonly d2.Bitmap1 d2dRenderTarget;
        private readonly int pixelWidth;
        private readonly wic.BitmapFrameDecode bitmapFrameDecode;
        private readonly int pixelHeight;

        public ImageHelper(string inputPath, string fontName)
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // INITIALIZATION ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // initialize the D3D device which will allow to render to image any graphics - 3D or 2D
            defaultDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                             d3d.DeviceCreationFlags.VideoSupport
                                                             | d3d.DeviceCreationFlags.BgraSupport
                                                             | d3d.DeviceCreationFlags.None); // take out the Debug flag for better performance

            d3dDevice = defaultDevice.QueryInterface<d3d.Device1>(); // get a reference to the Direct3D 11.1 device
            dxgiDevice = d3dDevice.QueryInterface<dxgi.Device>(); // get a reference to DXGI device

            d2dDevice = new d2.Device(dxgiDevice); // initialize the D2D device

            imagingFactory = new wic.ImagingFactory2(); // initialize the WIC factory

            // initialize the DeviceContext - it will be the D2D render target and will allow all rendering operations
            d2dContext = new d2.DeviceContext(d2dDevice, d2.DeviceContextOptions.None);

            dwFactory = new dw.Factory();

            // specify a pixel format that is supported by both D2D and WIC
            d2PixelFormat = new d2.PixelFormat(dxgi.Format.R8G8B8A8_UNorm, d2.AlphaMode.Premultiplied);
            // if in D2D was specified an R-G-B-A format - use the same for wic
            wicPixelFormat = wic.PixelFormat.Format32bppPRGBA;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // IMAGE LOADING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            decoder = new wic.PngBitmapDecoder(imagingFactory); // we will load a PNG image
            inputStream = new wic.WICStream(imagingFactory, inputPath, NativeFileAccess.Read); // open the image file for reading
            decoder.Initialize(inputStream, wic.DecodeOptions.CacheOnLoad);

            // decode the loaded image to a format that can be consumed by D2D
            formatConverter = new wic.FormatConverter(imagingFactory);
            bitmapFrameDecode = decoder.GetFrame(0);
            formatConverter.Initialize(bitmapFrameDecode, wicPixelFormat);

            // load the base image into a D2D Bitmap
            // inputBitmap = d2.Bitmap1.FromWicBitmap(d2dContext, formatConverter, new d2.BitmapProperties1(d2PixelFormat));

            // store the image size - output will be of the same size
            var inputImageSize = formatConverter.Size;
            pixelWidth = inputImageSize.Width;
            pixelHeight = inputImageSize.Height;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // EFFECT SETUP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // Effect 1 : BitmapSource - take decoded image data and get a BitmapSource from it
            bitmapSourceEffect = new d2.Effects.BitmapSource(d2dContext)
            {
                WicBitmapSource = formatConverter
            };

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // OVERLAY TEXT SETUP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            textFormat = new dw.TextFormat(dwFactory, fontName, 260f); // create the text format of specified font configuration

            // draw a long text to show the automatic line wrapping

            // create the text layout - this improves the drawing performance for static text
            // as the glyph positions are precalculated

            textBrush = new d2.SolidColorBrush(d2dContext, Color.LightGreen);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // RENDER TARGET SETUP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // create the d2d bitmap description using default flags (from SharpDX samples) and 96 DPI
            var d2dBitmapProps = new d2.BitmapProperties1(d2PixelFormat, 96, 96, d2.BitmapOptions.Target | d2.BitmapOptions.CannotDraw);

            // the render target
            d2dRenderTarget = new d2.Bitmap1(d2dContext, new Size2(pixelWidth, pixelHeight), d2dBitmapProps);
            d2dContext.Target = d2dRenderTarget; // associate bitmap with the d2d context
        }

        public void GenerateThumbnail(int version, Stream output)
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // DRAWING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // slow preparations - fast drawing:

            var textToDraw = "#" + version;

            using var textLayout = new dw.TextLayout(dwFactory, textToDraw, textFormat, 1714f, 1000f);
            d2dContext.BeginDraw();
            d2dContext.Clear(null);
            d2dContext.DrawImage(bitmapSourceEffect);
            d2dContext.DrawTextLayout(new Vector2(900, 50), textLayout, textBrush, d2.DrawTextOptions.EnableColorFont);
            d2dContext.EndDraw();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // IMAGE SAVING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


            // use the appropiate overload to write either to stream or to a file
            using var stream = new wic.WICStream(imagingFactory, output);

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

        public void Dispose()
        {
            defaultDevice?.Dispose();
            d3dDevice?.Dispose();
            dxgiDevice?.Dispose();
            d2dDevice?.Dispose();
            imagingFactory?.Dispose();
            d2dContext?.Dispose();
            dwFactory?.Dispose();
            decoder?.Dispose();
            inputStream?.Dispose();
            formatConverter?.Dispose();
            textFormat?.Dispose();
            textBrush?.Dispose();
            bitmapSourceEffect?.Dispose();
            d2dRenderTarget?.Dispose();
            bitmapFrameDecode?.Dispose();
        }

    }
}
