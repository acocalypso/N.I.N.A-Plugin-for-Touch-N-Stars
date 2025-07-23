using System;
using System.IO;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using TouchNStars.PHD2;
using System.Linq;

namespace TouchNStars.Server
{
    public class PHD2ImageService : IDisposable
    {
        private readonly PHD2Service phd2Service;
        private readonly object lockObject = new object();
        private readonly string cacheDirectory;
        private string currentImagePath;
        private DateTime lastImageTime = DateTime.MinValue;
        private readonly Timer refreshTimer;
        private string lastError;

        public string LastError => lastError;
        public bool HasCurrentImage => !string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath);
        public DateTime LastImageTime => lastImageTime;

        public PHD2ImageService(PHD2Service phd2Service)
        {
            this.phd2Service = phd2Service;
            
            // Create cache directory
            cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NINA", "TnsCache", "phd2_images"
            );
            Directory.CreateDirectory(cacheDirectory);

            // Start a timer to periodically refresh the image (every 5 seconds)
            refreshTimer = new Timer(RefreshImageCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private async void RefreshImageCallback(object state)
        {
            try
            {
                await RefreshImageAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in PHD2 image refresh: {ex}");
                lastError = ex.Message;
            }
        }

        public async Task<bool> RefreshImageAsync()
        {
            try
            {
                lock (lockObject)
                {
                    if (!phd2Service.IsConnected)
                    {
                        lastError = "PHD2 is not connected";
                        return false;
                    }
                }

                // Get image from PHD2
                string fitsFilePath = await phd2Service.SaveImageAsync();
                
                if (string.IsNullOrEmpty(fitsFilePath) || !File.Exists(fitsFilePath))
                {
                    lastError = "PHD2 did not return a valid image file";
                    return false;
                }

                // Convert FITS to JPG and cache it
                string jpgPath = await ConvertFitsToJpgAsync(fitsFilePath);
                
                if (!string.IsNullOrEmpty(jpgPath))
                {
                    lock (lockObject)
                    {
                        // Clean up old cached image
                        if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
                        {
                            try { File.Delete(currentImagePath); } catch { }
                        }
                        
                        currentImagePath = jpgPath;
                        lastImageTime = DateTime.Now;
                        lastError = null;
                    }
                    
                    // Clean up the FITS file
                    try { File.Delete(fitsFilePath); } catch { }
                    
                    Logger.Info($"PHD2 image cached successfully: {jpgPath}");
                    return true;
                }
                else
                {
                    // Clean up the FITS file
                    try { File.Delete(fitsFilePath); } catch { }
                    return false;
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Logger.Error($"Failed to refresh PHD2 image: {ex}");
                return false;
            }
        }

        private async Task<string> ConvertFitsToJpgAsync(string fitsFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Generate unique filename for the JPG
                    string jpgFileName = $"phd2_image_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                    string jpgPath = Path.Combine(cacheDirectory, jpgFileName);

                    // Read FITS file using basic binary reading
                    var fitsData = ReadBasicFitsFile(fitsFilePath);
                    if (fitsData == null)
                    {
                        lastError = "Failed to read FITS file";
                        return null;
                    }

                    // Convert FITS data to bitmap
                    using (var bitmap = new Bitmap(fitsData.Width, fitsData.Height, PixelFormat.Format24bppRgb))
                    {
                        ConvertFitsDataToBitmap(fitsData.ImageData, bitmap, fitsData.Width, fitsData.Height);
                        
                        // Save as JPG
                        var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                            .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                        
                        if (jpegEncoder != null)
                        {
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
                            bitmap.Save(jpgPath, jpegEncoder, encoderParams);
                        }
                        else
                        {
                            bitmap.Save(jpgPath, ImageFormat.Jpeg);
                        }
                    }

                    return jpgPath;
                }
                catch (Exception ex)
                {
                    lastError = $"Failed to convert FITS to JPG: {ex.Message}";
                    Logger.Error($"FITS conversion error: {ex}");
                    return null;
                }
            });
        }

        private class FitsImageData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public float[,] ImageData { get; set; }
        }

        private FitsImageData ReadBasicFitsFile(string fitsFilePath)
        {
            try
            {
                using (var fileStream = new FileStream(fitsFilePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fileStream))
                {
                    // FITS files have 2880-byte blocks and start with header cards
                    // This is a very basic implementation that assumes PHD2's FITS format
                    
                    // Skip to find dimensions in header
                    fileStream.Seek(0, SeekOrigin.Begin);
                    byte[] headerBlock = reader.ReadBytes(2880);
                    string headerText = System.Text.Encoding.ASCII.GetString(headerBlock);
                    
                    // Parse NAXIS1 and NAXIS2 (width and height)
                    int width = ParseFitsKeyword(headerText, "NAXIS1");
                    int height = ParseFitsKeyword(headerText, "NAXIS2");
                    
                    if (width <= 0 || height <= 0)
                    {
                        Logger.Error($"Invalid FITS dimensions: {width}x{height}");
                        return null;
                    }

                    // Find end of header (marked by "END" keyword)
                    long dataStart = 2880; // Start with first block
                    while (dataStart < fileStream.Length)
                    {
                        fileStream.Seek(dataStart - 2880, SeekOrigin.Begin);
                        byte[] block = reader.ReadBytes(2880);
                        string blockText = System.Text.Encoding.ASCII.GetString(block);
                        
                        if (blockText.Contains("END     "))
                        {
                            break;
                        }
                        dataStart += 2880;
                    }

                    // Read image data
                    fileStream.Seek(dataStart, SeekOrigin.Begin);
                    var imageData = new float[width, height];
                    
                    // PHD2 typically saves as 32-bit floats (BITPIX = -32)
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte[] bytes = reader.ReadBytes(4);
                            if (BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(bytes); // FITS is big-endian
                            }
                            imageData[x, y] = BitConverter.ToSingle(bytes, 0);
                        }
                    }

                    return new FitsImageData
                    {
                        Width = width,
                        Height = height,
                        ImageData = imageData
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading basic FITS file: {ex}");
                return null;
            }
        }

        private int ParseFitsKeyword(string headerText, string keyword)
        {
            try
            {
                string pattern = keyword + "=";
                int startIndex = headerText.IndexOf(pattern);
                if (startIndex < 0) return -1;
                
                startIndex += pattern.Length;
                int endIndex = headerText.IndexOf('/', startIndex);
                if (endIndex < 0) endIndex = startIndex + 20; // Default length
                
                string valueText = headerText.Substring(startIndex, endIndex - startIndex).Trim();
                if (int.TryParse(valueText, out int value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error parsing FITS keyword {keyword}: {ex}");
            }
            return -1;
        }

        private void ConvertFitsDataToBitmap(Array fitsData, Bitmap bitmap, int width, int height)
        {
            try
            {
                // Convert FITS data to image
                double minVal = double.MaxValue;
                double maxVal = double.MinValue;

                // Find min/max values for scaling
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double value = Convert.ToDouble(fitsData.GetValue(x, y));
                        if (!double.IsNaN(value) && !double.IsInfinity(value))
                        {
                            minVal = Math.Min(minVal, value);
                            maxVal = Math.Max(maxVal, value);
                        }
                    }
                }

                // Avoid division by zero
                if (Math.Abs(maxVal - minVal) < 1e-10)
                {
                    maxVal = minVal + 1.0;
                }

                // Convert to 8-bit grayscale and then to RGB
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double value = Convert.ToDouble(fitsData.GetValue(x, y));
                        
                        // Handle NaN/Infinity values
                        if (double.IsNaN(value) || double.IsInfinity(value))
                        {
                            value = minVal;
                        }

                        // Scale to 0-255
                        int intensity = (int)Math.Round(255.0 * (value - minVal) / (maxVal - minVal));
                        intensity = Math.Max(0, Math.Min(255, intensity));

                        // Set pixel (flip Y coordinate since FITS has origin at bottom-left)
                        Color color = Color.FromArgb(intensity, intensity, intensity);
                        bitmap.SetPixel(x, height - 1 - y, color);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error converting FITS data to bitmap: {ex}");
                throw;
            }
        }

        public async Task<byte[]> GetCurrentImageBytesAsync()
        {
            string imagePath;
            lock (lockObject)
            {
                if (string.IsNullOrEmpty(currentImagePath) || !File.Exists(currentImagePath))
                {
                    return null;
                }
                imagePath = currentImagePath;
            }

            try
            {
                return await File.ReadAllBytesAsync(imagePath);
            }
            catch (Exception ex)
            {
                lastError = $"Failed to read cached image: {ex.Message}";
                Logger.Error($"Error reading cached image: {ex}");
                return null;
            }
        }

        public void Dispose()
        {
            refreshTimer?.Dispose();
            
            lock (lockObject)
            {
                // Clean up cached image
                if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
                {
                    try { File.Delete(currentImagePath); } catch { }
                }
            }
        }
    }
}