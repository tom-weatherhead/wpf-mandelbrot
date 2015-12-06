using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;             // In the assembly PresentationCore.  Also requires the assembly WindowsBase.
using System.Windows.Media.Imaging;     // In the assembly PresentationCore.  Also requires the assembly WindowsBase.
using System.Text;

namespace WPFMandelbrot.Engine
{
    public interface IMandelbrotWindow
    {
        void SetZoomMessage(int zoomExponent);
        void RedrawImage();
    }

    public class GraphicsEngine
    {
        public readonly IMandelbrotWindow window;
        public readonly WriteableBitmap bitmap = null;
        public readonly int canvasWidthInPixels;    // = 512;
        public readonly int canvasHeightInPixels;   // = 512;
        public const double defaultViewLeft = -2.25;
        public const double defaultViewTop = 1.5;
        public const double defaultViewWidth = 3.0;
        public const double defaultViewHeight = 3.0;
        public double viewLeft = 0.0;
        public double viewTop = 0.0;
        public double viewWidth = 0.0;
        public double viewHeight = 0.0;
        public int currentCanvasLeftInPixels = 0;
        public int currentCanvasTopInPixels = 0;
        public int currentCanvasWidthInPixels = 0;
        public double currentWidth = 0.0;
        public List<Color> palette = null;
        public const int maxRendersPerCall = 1000; //32;
        public const int nextCallDelay = 5; // Milliseconds
        public int globalRenderNumber = 0;
        public int zoomExponent = 0;

        public GraphicsEngine(IMandelbrotWindow window, int canvasWidthInPixels, int canvasHeightInPixels)
        {
            this.window = window;
            this.canvasWidthInPixels = canvasWidthInPixels;
            this.canvasHeightInPixels = canvasHeightInPixels;
            bitmap = BitmapFactory.New(this.canvasWidthInPixels, this.canvasHeightInPixels);

            palette = new List<Color>();

            for (var i = 0; i <= 255; i += 5)
            {
                var b = (byte)i;

                palette.Add(new Color() { A = 255, R = 255, G = b, B = 0 });// "rgb(255," + i + ",0)");   // Red to Yellow
                palette.Add(new Color() { A = 255, R = 0, G = 255, B = b });//"rgb(0,255," + i + ")");   // Green to Cyan/Aqua
                palette.Add(new Color() { A = 255, R = b, G = 0, B = 255 });//"rgb(" + i + ",0,255)");   // Blue to Magenta/Fuchsia
            }

            palette.Add(new Color() { A = 255, R = 0, G = 0, B = 0 } );     // Pixels within the Mandelbrot Set are coloured Black.
        }

        public void calculateAndFillSquare(double cr, double ci, int canvasSquareLeft, int canvasSquareTop, int canvasSquareWidth)
        {
            var maxNumIterations = palette.Count - 1;
            var zr = cr;
            var zi = ci;
            var i = 0;

            for (; i < maxNumIterations; ++i)
            {
                var zr2 = zr * zr;
                var zi2 = zi * zi;

                if (zr2 + zi2 >= 4.0)
                {
                    break;
                }

                var tempzr = zr2 - zi2 + cr;

                zi = 2.0 * zr * zi + ci;
                zr = tempzr;
            }

            //fillSquare(cxt, canvasSquareLeft, canvasSquareTop, canvasSquareWidth, palette[i]);
            //bitmap.FillRectangle(canvasSquareLeft, canvasSquareTop, canvasSquareLeft + canvasSquareWidth, canvasSquareTop + canvasSquareWidth, palette[i]);

            // ThAW 2012/10/10 : There seems to be an off-by-one bug in the WriteableBitmapEx library.  We must compensate.
            bitmap.FillRectangle(canvasSquareLeft, canvasSquareTop, canvasSquareLeft + canvasSquareWidth, canvasSquareTop + canvasSquareWidth - 1, palette[i]);
        }

        public void renderLoop(int localRenderNumber)
        {

            if (localRenderNumber != globalRenderNumber) {
                return;
            }

            //var done = false;

            //do
            for (; ; )
            {

                using (bitmap.GetBitmapContext())
                {
                    //var c = document.getElementById("myCanvas");
                    //var cxt = c.getContext("2d");
                    var nextCanvasWidthInPixels = currentCanvasWidthInPixels / 2;
                    var nextWidth = currentWidth / 2.0;

                    for (var renderNum = 0; renderNum < maxRendersPerCall; ++renderNum)
                    {
                        var cr = currentCanvasLeftInPixels * viewWidth / canvasWidthInPixels + viewLeft;
                        var ci = viewTop - currentCanvasTopInPixels * viewHeight / canvasHeightInPixels;

                        /*
                        // TAW 2011/05/30 : This next call should be unnecessary if the "progressive scan" algorithm is working properly.
                        calculateAndFillSquare(cxt, cr, ci,
                            currentCanvasLeftInPixels, currentCanvasTopInPixels,
                            nextCanvasWidthInPixels);
                         */

                        calculateAndFillSquare(cr + nextWidth, ci,
                            currentCanvasLeftInPixels + nextCanvasWidthInPixels, currentCanvasTopInPixels,
                            nextCanvasWidthInPixels);
                        calculateAndFillSquare(cr, ci - nextWidth,
                            currentCanvasLeftInPixels, currentCanvasTopInPixels + nextCanvasWidthInPixels,
                            nextCanvasWidthInPixels);
                        calculateAndFillSquare(cr + nextWidth, ci - nextWidth,
                            currentCanvasLeftInPixels + nextCanvasWidthInPixels, currentCanvasTopInPixels + nextCanvasWidthInPixels,
                            nextCanvasWidthInPixels);

                        currentCanvasLeftInPixels += currentCanvasWidthInPixels;

                        if (currentCanvasLeftInPixels >= canvasWidthInPixels)
                        {
                            currentCanvasLeftInPixels = 0;
                            currentCanvasTopInPixels += currentCanvasWidthInPixels;

                            if (currentCanvasTopInPixels >= canvasHeightInPixels)
                            {
                                currentCanvasTopInPixels = 0;
                                currentCanvasWidthInPixels = nextCanvasWidthInPixels;
                                currentWidth = nextWidth;

                                if (currentCanvasWidthInPixels <= 1)
                                {
                                    // Rendering is complete.
                                    return;
                                }

                                nextCanvasWidthInPixels /= 2;
                                nextWidth /= 2.0;
                                break;  // Allow the display of the rendered image at this level of chunkiness.
                            }
                        }
                    }
                }

                window.RedrawImage();
            }
            //while (!done);

            // Set the timer for the next call.
            //setTimeout("renderLoop(" + localRenderNumber + ")", nextCallDelay);
        }

        public void renderView()
        {
            //var c = document.getElementById("myCanvas");
            //var cxt = c.getContext("2d");

            using (bitmap.GetBitmapContext())
            {
                calculateAndFillSquare(viewLeft, viewTop, 0, 0, canvasWidthInPixels);  // Fill the entire canvas with the colour for the top left pixel.
            }

            //displayZoomExponent();
            window.SetZoomMessage(this.zoomExponent);

            currentCanvasLeftInPixels = 0;
            currentCanvasTopInPixels = 0;
            currentCanvasWidthInPixels = canvasWidthInPixels;
            currentWidth = viewWidth;
            ++globalRenderNumber;
            //setTimeout("renderLoop(" + globalRenderNumber + ")", nextCallDelay);
            renderLoop(globalRenderNumber);
        }

        public void renderDefaultView()
        {
            viewLeft = defaultViewLeft;
            viewTop = defaultViewTop;
            viewWidth = defaultViewWidth;
            viewHeight = defaultViewHeight;
            zoomExponent = 0;

            renderView();
        }

        public bool constrainView(double newViewLeft, double newViewTop, double newViewWidth, double newViewHeight, int newZoomExponent) 
        {

            if (newViewWidth > defaultViewWidth) {
                newViewWidth = defaultViewWidth;
            }

            if (newViewHeight > defaultViewHeight) {
                newViewHeight = defaultViewHeight;
            }

            if (newViewLeft < defaultViewLeft) {
                newViewLeft = defaultViewLeft;
            }

            var newViewRight = newViewLeft + newViewWidth;
            var defaultViewRight = defaultViewLeft + defaultViewWidth;

            if (newViewRight > defaultViewRight) {
                newViewLeft = defaultViewRight - newViewWidth;
            }

            if (newViewTop > defaultViewTop) {
                newViewTop = defaultViewTop;
            }

            var newViewBottom = newViewTop - newViewHeight;
            var defaultViewBottom = defaultViewTop - defaultViewHeight;

            if (newViewBottom < defaultViewBottom) {
                newViewTop = defaultViewBottom + newViewHeight;
            }

            if (newViewLeft == viewLeft && newViewTop == viewTop && newViewWidth == viewWidth && newViewHeight == viewHeight) {
                return false;
            }

            if (newZoomExponent < 0) {
                newZoomExponent = 0;
            }

            viewLeft = newViewLeft;
            viewTop = newViewTop;
            viewWidth = newViewWidth;
            viewHeight = newViewHeight;
            zoomExponent = newZoomExponent;

            return true;
        }

        public void onCanvasClick(int x, int y)
        {
            var cr = x * viewWidth / canvasWidthInPixels + viewLeft;
            var ci = viewTop - y * viewHeight / canvasHeightInPixels;

            var newViewWidth = viewWidth / 2.0;
            var newViewHeight = viewHeight / 2.0;

            if (newViewWidth <= 0.0 || newViewHeight <= 0.0) {
                //alert("The floating-point precision limit has been reached.");
                return;
            }

            var newViewLeft = cr - newViewWidth / 2.0;
            var newViewTop = ci + newViewHeight / 2.0;

            if (constrainView(newViewLeft, newViewTop, newViewWidth, newViewHeight, zoomExponent + 1)) {
                renderView();
            }
        }

        public void zoomOut() 
        {
            var cr = viewLeft + viewWidth / 2.0;
            var ci = viewTop - viewHeight / 2.0;

            var newViewWidth = viewWidth * 2.0;
            var newViewHeight = viewHeight * 2.0;
            var newViewLeft = cr - newViewWidth / 2.0;
            var newViewTop = ci + newViewHeight / 2.0;

            if (constrainView(newViewLeft, newViewTop, newViewWidth, newViewHeight, zoomExponent - 1)) {
                renderView();
            }
        }
    }
}
