using System;
using System.Threading;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Leap
{
    /// <summary>
    /// Class to hold the static data about our leap device
    /// </summary>
    class DeviceInfo
    {
        public uint DeviceID;
        public Image.FormatType Format;
        public Image.ImageType Type;
        public int BytesPerPixel;
        public uint NumBytes;
        public int DistortionHeight;
        public int DistortionWidth;
        public int Height;
        public int Width;
        public uint ByteOffsetLeft;
        public uint ByteOffsetRight;

        public DeviceInfo(Image image)
        {
            DeviceID = image.DeviceID;
            Format = image.Format;
            Type = image.Type;
            BytesPerPixel = image.BytesPerPixel;
            NumBytes = image.NumBytes;
            DistortionHeight = image.DistortionHeight;
            DistortionWidth = image.DistortionWidth;
            Height = image.Height;
            Width = image.Width;
            ByteOffsetLeft = image.ByteOffset(Image.CameraType.LEFT);
            ByteOffsetRight = image.ByteOffset(Image.CameraType.RIGHT);
        }
    }

    class Program
    {
        public static bool firstCall = true;
        public static Controller controller;
        public static DeviceInfo deviceInfo;

        public static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public static byte[] latestImage;

        public static string window = "Video Feed";
        public static Mat mat;

        static void Main(string[] args)
        {
            controller = new Controller();

            // Set the policy to get images and wait for it to go through
            controller.PolicyChange += OnPolicyChange;
            controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            if (!WaitForEvent("Images policy was not set properly")) return;

            // Start fetching images and wait to fetch the first one
            controller.ImageReady += OnImageReady;
            if (!WaitForEvent("No image has been fetched")) return;

            CvInvoke.NamedWindow(window);
            mat = new Mat(deviceInfo.Width * 2, deviceInfo.Height, DepthType.Cv8U, 1);
            mat.SetTo(latestImage);
            CvInvoke.Imshow(window, mat);

            while (true)
            {
                var key = CvInvoke.WaitKey(16);
                RenderImage();
                if ((char)key == 27) // ESC
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Check that the policy has been updated to accept images
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public static void OnPolicyChange(object source, PolicyEventArgs args)
        {
            UInt64 policyToCheck = (ulong)Controller.PolicyFlag.POLICY_IMAGES;
            if ((args.currentPolicies & policyToCheck) == policyToCheck)
            {
                waitHandle.Set();
            }
        }

        public static void OnImageReady(object source, ImageEventArgs args)
        {
            // Push the image byte array to latestImage
            latestImage = args.image.Data(Image.CameraType.LEFT); // CameraType.LEFT seems to make no difference here

            // Set deviceInfo and print values if this is the first image we got
            if (firstCall)
            {
                deviceInfo = new DeviceInfo(args.image);
                Console.WriteLine(
                    "DeviceID: {0}\n" +
                    "Format: {1}\n" +
                    "Type: {2}\n" +
                    "BytesPerPixel: {3}\n" +
                    "NumBytes: {4}\n" +
                    "DistortionHeight: {5}\n" +
                    "DistortionWidth: {6}\n" +
                    "Height: {7}\n" +
                    "Width: {8}\n" +
                    "ByteOffsetLeft {9}\n" +
                    "ByteOffsetRight {10}",
                    deviceInfo.DeviceID,
                    deviceInfo.Format,
                    deviceInfo.Type,
                    deviceInfo.BytesPerPixel,
                    deviceInfo.NumBytes,
                    deviceInfo.DistortionHeight,
                    deviceInfo.DistortionWidth,
                    deviceInfo.Height,
                    deviceInfo.Width,
                    deviceInfo.ByteOffsetLeft,
                    deviceInfo.ByteOffsetRight);
                firstCall = false;
                Console.WriteLine("Setting wait");
                waitHandle.Set();
            }
        }

        /// <summary>
        /// Waits for the wait handle, prints an error message if wait time exceeeds 5000ms or given value
        /// </summary>
        /// <param name="errorMesage"> The error message to print if waitTime is exceeded </param>
        /// <param name="waitTime"> How long to wait before error (5000ms default) </param>
        /// <returns> False if waitTime is exceeded, True otherwise </returns>
        public static bool WaitForEvent(String errorMesage, int waitTime = 5000)
        {
            try
            {
                if (!waitHandle.WaitOne(waitTime))
                {
                    Console.WriteLine(errorMesage);
                    return false;
                }
            }
            catch
            {
                Console.WriteLine(errorMesage);
                return false;
            }
            return true;
        }

        public static void RenderImage()
        {
            if (mat != null)
            {
                mat.SetTo(latestImage);
                CvInvoke.Imshow(window, mat);
            }
        }
    }
}
