using System.IO;
using GazeFirst;
using Serilog;

namespace eyetuitive.NET_SampleClient
{
    /// <summary>
    /// Helps convert raw frame data to image bytes (BMP format)
    /// </summary>
    public static class FrameConverter
    {
        private static ILogger Logger = Log.Logger;

        public static byte[] ImageBytes { get; private set; }

        public static bool CreateImageBytesFromRawFrame(FrameArgs frame)
        {
            if (frame?.data == null || frame.data.Length == 0)
                return false;

            try
            {
                // For 8-bit grayscale images
                if (frame.channels == 1)
                {
                    // Convert grayscale to RGB
                    byte[] rgbData = new byte[frame.width * frame.height * 3];

                    for (int i = 0, j = 0; i < frame.data.Length; i++, j += 3)
                    {
                        byte gray = frame.data[i];
                        rgbData[j] = gray;     // R
                        rgbData[j + 1] = gray; // G
                        rgbData[j + 2] = gray; // B
                    }

                    // BMP file structure
                    using (MemoryStream ms = new MemoryStream())
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        // BMP Header
                        bw.Write((byte)'B');
                        bw.Write((byte)'M');
                        int fileSize = 54 + rgbData.Length; // Header (54 bytes) + pixel data
                        bw.Write(fileSize);
                        bw.Write(0); // Reserved
                        bw.Write(54); // Offset to pixel data

                        // DIB Header
                        bw.Write(40); // DIB header size
                        bw.Write(frame.width);
                        bw.Write(frame.height);
                        bw.Write((short)1); // Color planes
                        bw.Write((short)24); // Bits per pixel (RGB)
                        bw.Write(0); // No compression
                        bw.Write(rgbData.Length); // Image size
                        bw.Write(0); // X pixels per meter
                        bw.Write(0); // Y pixels per meter
                        bw.Write(0); // Colors in color table
                        bw.Write(0); // Important color count

                        // Write the rows bottom-to-top (BMP format requirement)
                        int stride = frame.width * 3;
                        int padding = (4 - (stride % 4)) % 4; // Each row must be padded to multiple of 4 bytes

                        byte[] paddingBytes = new byte[padding];

                        for (int row = frame.height - 1; row >= 0; row--)
                        {
                            int rowStart = row * stride;
                            bw.Write(rgbData, rowStart, stride);

                            // Add padding if needed
                            if (padding > 0)
                                bw.Write(paddingBytes);
                        }

                        // Reset stream position
                        ms.Position = 0;

                        ImageBytes = ms.ToArray();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating image ", ex);
            }
            return false;
        }
    }
}
