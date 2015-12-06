using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WPFMandelbrot.Engine;

namespace WPFMandelbrot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMandelbrotWindow
    {
        private readonly GraphicsEngine graphicsEngine;
        private readonly Action emptyDelegate;

        public MainWindow()
        {
            InitializeComponent();

            emptyDelegate = delegate { };

            graphicsEngine = new GraphicsEngine(this, (int)ImageControl.Width, (int)ImageControl.Height);
            ImageControl.Source = graphicsEngine.bitmap;

#if DEAD_CODE
            // Initialize the WriteableBitmap with size 512x512 and set it as source of an Image control
            //WriteableBitmap writeableBmp = BitmapFactory.New(512, 512);
            WriteableBitmap writeableBmp = graphicsEngine.bitmap;

            //ImageControl.Source = writeableBmp;

            using (writeableBmp.GetBitmapContext())
            {
                // Clear the WriteableBitmap with white color
                //writeableBmp.Clear(Colors.White);
                writeableBmp.Clear(Colors.Black);

                // Set the pixel at P(10, 13) to black
                writeableBmp.SetPixel(10, 13, Colors.Black);

                // Red filled rectangle from the point P1(128, 128) that is 256px wide and 256px high
                writeableBmp.FillRectangle(1, 1, 511, 510, Colors.White);
                writeableBmp.FillRectangle(256, 256, 257, 256, Colors.Black);
            }
#endif

            graphicsEngine.renderDefaultView();
        }

        public void SetZoomMessage(int zoomExponent)
        {
            tbZoomMessage.Text = "Zoom factor: 2 to the power of " + zoomExponent.ToString();
        }

        public void RedrawImage()
        {
            ImageControl.Dispatcher.Invoke(emptyDelegate, DispatcherPriority.Render);
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            graphicsEngine.zoomOut();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            graphicsEngine.renderDefaultView();
        }

        private void ImageControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(ImageControl);

            graphicsEngine.onCanvasClick((int)position.X, (int)position.Y);
        }
    }
}
