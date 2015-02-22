using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Tkinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _kinect = null;

        //Size for the RGB pixel in bitmap
        private readonly int _bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        //FrameReader for our coloroutput
        private ColorFrameReader _colorReader = null;

        //Array of color pixels
        private byte[] _colorPixels = null;

        //Color WriteableBitmap linked to our UI
        private WriteableBitmap _colorBitmap = null;

        //FrameReader for our depth output
        private DepthFrameReader _depthReader = null;

        //Array of depth values
        private ushort[] _depthData = null;

        //Array of depth pixels used for the output
        private byte[] _depthPixels = null;

        //Depth WriteableBitmap linked to our UI
        private WriteableBitmap _depthBitmap = null;

        //FrameReader for our coloroutput(Infrared Mode)
        private InfraredFrameReader _infraReader = null;
 
        //Array of infrared data
        private ushort[] _infraData = null;

        //Array of infrared pixels used for the output
        private byte[] _infraPixels = null;

        //Infrared WriteableBitmap linked to our UI
        private WriteableBitmap _infraBitmap = null;

        public MainWindow()
        {
            InitializeComponent();

            InitializeKinect();

            //Close Kinect when closing app
            Closing += OnClosing;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Close Kinect
            if (_kinect != null) _kinect.Close();
        }

        private void InitializeKinect()
        {
            _kinect = KinectSensor.GetDefault();
            if (_kinect == null)
                return;

            _kinect.Open();
            InitializeCamera();
            InitializeDepth();
            InitializeInfrared();
            InitializeBody();

        }

        private void InitializeBody()
        {
        }

        private void InitializeInfrared()
        {
            if (_kinect == null) return;

            //Get frame description for the color output
            FrameDescription desc = _kinect.InfraredFrameSource.FrameDescription;

            //Get the frameReader for color
            _infraReader = _kinect.InfraredFrameSource.OpenReader();

            //Allocate pixel array
            _infraData = new ushort[desc.Width * desc.Height];
            _infraPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            //Create new WriteableBitmap
            _infraBitmap = new WriteableBitmap(desc.Width,desc.Height,96,96,PixelFormats.Bgr32,null);

            //Link WBMP to UI
            InfraredImage.Source = _infraBitmap;

            //Hook-up event
            _infraReader.FrameArrived += OnInfraredFrameArrived;
        }

        private void OnInfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            //Reference to infrared frame
            InfraredFrameReference refer = e.FrameReference;

            if (refer == null) return;

            //Get infrared frame
            InfraredFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            //Process it
            using (frame)
            {
                //Get the description
                FrameDescription frameDesc = frame.FrameDescription;

                if(((frameDesc.Width * frameDesc.Height) == _infraData.Length) &&
                    (frameDesc.Width == _infraBitmap.Width) && (frameDesc.Height == _infraBitmap.Height)){
                    //Copy data
                    frame.CopyFrameDataToArray(_infraData);

                    int colorPixelIndex = 0;

                    for(int i = 0; i < _infraData.Length; ++i){
                        //Get infrared value;
                        ushort ir = _infraData[i];

                        //Bitshift
                        byte intensity = (byte)(ir >> 8);

                        //Assign infrared intensity
                        _infraPixels[colorPixelIndex++] = intensity;
                        _infraPixels[colorPixelIndex++] = intensity;
                        _infraPixels[colorPixelIndex++] = intensity;

                        ++colorPixelIndex;
                    }

                    //Copy output to bitmap
                    _infraBitmap.WritePixels(new Int32Rect(0,0,frameDesc.Width,frameDesc.Height),_infraPixels,frameDesc.Width * _bytePerPixel,0);
                }
            }
        }

        private void InitializeDepth()
        {
            if (_kinect == null) return;

            //Get frame description for the color output
            FrameDescription desc = _kinect.DepthFrameSource.FrameDescription;

            //Get the framerader for Depth
            _depthReader = _kinect.DepthFrameSource.OpenReader();

            //Allocate pixel array
            _depthData = new ushort[desc.Width * desc.Height];
            _depthPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            //Create new WriteableBitmap
            _depthBitmap = new WriteableBitmap(desc.Width,desc.Height,96,96,PixelFormats.Bgr32,null);

            //Link WBMP to UI
            DepthImage.Source = _depthBitmap;

            //Hook-up event
            _depthReader.FrameArrived += OnDepthFrameArrived;
        }

        private void OnDepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            DepthFrameReference refer = e.FrameReference;
            
            if (refer == null) return;

            DepthFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                FrameDescription frameDesc = frame.FrameDescription;
                if (((frameDesc.Width * frameDesc.Height) == _depthData.Length) && (frameDesc.Width == _depthBitmap.PixelWidth) && (frameDesc.Height == _depthBitmap.PixelHeight))
                {
                    //Copy Depth frame
                    frame.CopyFrameDataToArray(_depthData);

                    //Get min & max depth
                    ushort minDepth = frame.DepthMinReliableDistance;
                    ushort maxDepth = frame.DepthMaxReliableDistance;

                    //Adjust visualisation
                    int colorPixelIndex = 0;
                    for (int i = 0; i < _depthData.Length; ++i)
                    {
                        //Get Depth value
                        ushort depth = _depthData[i];

                        if (depth == 0)
                        {
                            _depthPixels[colorPixelIndex++] = 41;
                            _depthPixels[colorPixelIndex++] = 239;
                            _depthPixels[colorPixelIndex++] = 242;
                        }
                        else if (depth < minDepth || depth > maxDepth)
                        {
                            _depthPixels[colorPixelIndex++] = 25;
                            _depthPixels[colorPixelIndex++] = 0;
                            _depthPixels[colorPixelIndex++] = 255;
                        }
                        else
                        {
                            double gray = (Math.Floor((double)depth / 250) * 12.75);

                            _depthPixels[colorPixelIndex++] = (byte)gray;
                            _depthPixels[colorPixelIndex++] = (byte)gray;
                            _depthPixels[colorPixelIndex++] = (byte)gray;
                        }

                        //Increment
                        ++colorPixelIndex;
                    }

                    //Copy output to bitmap
                    _depthBitmap.WritePixels(new Int32Rect(0,0,frameDesc.Width,frameDesc.Height),_depthPixels,frameDesc.Width * _bytePerPixel,0);
                }
            }
        }   

        private void InitializeCamera()
        {
            if (_kinect == null) return;

            //Get frame description for the color output
            FrameDescription desc = _kinect.ColorFrameSource.FrameDescription;

            //Get the framereader for Color
            _colorReader = _kinect.ColorFrameSource.OpenReader();

            //Allocate pixel array
            _colorPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            //Create new WriteableBitmap
            _colorBitmap = new WriteableBitmap(desc.Width,desc.Height,96,96,PixelFormats.Bgr32,null);

            //Link WBMP to UI
            CameraImage.Source = _colorBitmap;

            //Hook-up event
            _colorReader.FrameArrived += OnColorFrameArrived;
        }

        private void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            //Get the reference to the color frame
            ColorFrameReference colorRef = e.FrameReference;

            if (colorRef == null) return;

            //Acquire frame for specific reference
            ColorFrame frame = colorRef.AcquireFrame();

            //It's possible that we skipped a frame or it is already gone
            if (frame == null) return;

            using (frame)
            {
                //Get frame description
                FrameDescription frameDesc = frame.FrameDescription;

                //Check if width/height matches
                if (frameDesc.Width == _colorBitmap.PixelWidth && frameDesc.Height == _colorBitmap.PixelHeight)
                {
                    //Copy data to array based on image format
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToArray(_colorPixels);
                    }
                    else frame.CopyConvertedFrameDataToArray(_colorPixels,ColorImageFormat.Bgra);

                    //Copy output to bitmap
                    _colorBitmap.WritePixels(new Int32Rect(0,0,frameDesc.Width,frameDesc.Height),_colorPixels,frameDesc.Width * _bytePerPixel,0);
                }
            }
        }

        private void OnToggleCamera(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Camera");
        }
        private void OnToggleDepth(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Depth");
        }
        private void OnToggleInfrared(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Infrared");
        }

        private void ChangeVisualMode(string mode)
        {
            CameraGrid.Visibility = Visibility.Collapsed;
            DepthGrid.Visibility = Visibility.Collapsed;
            InfraredGrid.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case "Camera":
                    CameraGrid.Visibility = Visibility.Visible;
                    break;
                case "Depth":
                    DepthGrid.Visibility = Visibility.Visible;
                    break;
                case "Infrared":
                    InfraredGrid.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
   
}
