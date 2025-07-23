using System;
using System.IO;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using TouchNStars.PHD2;
using System.Linq;
using NINA.Image.Interfaces;
using NINA.Core.Enum;
using System.Windows.Media.Imaging;
using NINA.WPF.Base.Utility;

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
                        Logger.Warning("PHD2 refresh failed: PHD2 is not connected");
                        return false;
                    }
                }

                Logger.Info("Starting PHD2 image refresh...");

                // Get image from PHD2
                string fitsFilePath = await phd2Service.SaveImageAsync();
                
                Logger.Info($"PHD2 returned FITS file path: '{fitsFilePath}'");
                
                if (string.IsNullOrEmpty(fitsFilePath))
                {
                    lastError = "PHD2 returned empty file path";
                    Logger.Error("PHD2 returned empty file path");
                    return false;
                }

                if (!File.Exists(fitsFilePath))
                {
                    lastError = $"FITS file does not exist: {fitsFilePath}";
                    Logger.Error($"FITS file does not exist: {fitsFilePath}");
                    return false;
                }

                // Log file details
                var fileInfo = new FileInfo(fitsFilePath);
                Logger.Info($"FITS file exists: {fitsFilePath}, Size: {fileInfo.Length} bytes, Created: {fileInfo.CreationTime}");

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
                    Logger.Error("FITS to JPG conversion returned null path");
                    // Clean up the FITS file
                    try { File.Delete(fitsFilePath); } catch { }
                    return false;
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Logger.Error($"Failed to refresh PHD2 image: {ex}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<string> ConvertFitsToJpgAsync(string fitsFilePath)
        {
            try
            {
                // Generate unique filename for the JPG
                string jpgFileName = $"phd2_image_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                string jpgPath = Path.Combine(cacheDirectory, jpgFileName);

                // Check if NINA's ImageDataFactory is available
                if (TouchNStars.Mediators?.ImageDataFactory != null)
                {
                    try
                    {
                        // Use NINA's ImageDataFactory to load the FITS file
                        var imageDataFactory = TouchNStars.Mediators.ImageDataFactory;
                        IImageData imageData = await imageDataFactory.CreateFromFile(fitsFilePath, 16, false, RawConverterEnum.FREEIMAGE);
                        
                        if (imageData != null)
                        {
                            // Render the image
                            IRenderedImage renderedImage = imageData.RenderImage();
                            
                            // Apply basic stretching for better visibility
                            renderedImage = await renderedImage.Stretch(2.5, 0.1, false);

                            // Convert to bitmap and save as JPG
                            BitmapSource bitmapSource = renderedImage.Image;
                            
                            // Create JPG encoder
                            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                            encoder.QualityLevel = 95;
                            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                            // Save to file
                            using (FileStream fileStream = new FileStream(jpgPath, FileMode.Create))
                            {
                                encoder.Save(fileStream);
                            }

                            Logger.Info($"FITS converted to JPG using NINA: {jpgPath}");
                            return jpgPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"NINA ImageDataFactory failed: {ex.Message}, trying fallback method");
                    }
                }

                // Fallback: Try to read and convert FITS file manually
                Logger.Info("Using fallback method: manual FITS conversion");
                return await ConvertFitsManuallyAsync(fitsFilePath, jpgPath);
            }
            catch (Exception ex)
            {
                lastError = $"Failed to convert FITS to JPG: {ex.Message}";
                Logger.Error($"FITS conversion error: {ex}");
                return null;
            }
        }

        private async Task<string> ConvertFitsManuallyAsync(string fitsFilePath, string jpgPath)
        {
            try
            {
                Logger.Info($"Starting manual FITS conversion of: {fitsFilePath}");
                
                var fitsData = await ReadFitsFileAsync(fitsFilePath);
                if (fitsData == null)
                {
                    Logger.Warning("Manual FITS reading returned null, creating placeholder");
                    return await CreatePlaceholderImageAsync(jpgPath);
                }

                Logger.Info($"FITS data loaded successfully: {fitsData.Width}x{fitsData.Height}, BITPIX={fitsData.BitPix}");
                
                var result = await ConvertFitsDataToJpgAsync(fitsData, jpgPath);
                
                if (string.IsNullOrEmpty(result))
                {
                    Logger.Warning("FITS data to JPG conversion returned null, creating placeholder");
                    return await CreatePlaceholderImageAsync(jpgPath);
                }
                
                Logger.Info($"Manual FITS conversion completed successfully: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Manual FITS conversion failed with exception: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                Logger.Warning("Creating placeholder due to conversion failure");
                return await CreatePlaceholderImageAsync(jpgPath);
            }
        }

        private class SimpleFitsData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public float[,] ImageData { get; set; }
            public int BitPix { get; set; }
        }

        private async Task<SimpleFitsData> ReadFitsFileAsync(string fitsFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Logger.Info($"Opening FITS file for reading: {fitsFilePath}");
                    
                    using (var fileStream = new FileStream(fitsFilePath, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(fileStream))
                    {
                        Logger.Info($"FITS file stream opened, length: {fileStream.Length} bytes");
                        
                        // Read FITS header
                        int width = 0, height = 0, bitpix = 0;
                        long dataStart = 0;
                        int headerBlockCount = 0;

                        // Read header blocks (2880 bytes each)
                        while (dataStart < fileStream.Length)
                        {
                            fileStream.Seek(dataStart, SeekOrigin.Begin);
                            byte[] block = reader.ReadBytes(2880);
                            string headerText = System.Text.Encoding.ASCII.GetString(block);
                            headerBlockCount++;

                            Logger.Debug($"Reading header block {headerBlockCount} at position {dataStart}");

                            // Parse key FITS keywords from the entire header block
                            if (width == 0) width = ParseFitsKeyword(headerText, "NAXIS1");
                            if (height == 0) height = ParseFitsKeyword(headerText, "NAXIS2");
                            if (bitpix == 0) bitpix = ParseFitsKeyword(headerText, "BITPIX");

                            // Log all header lines for debugging FITS structure
                            Logger.Debug($"=== Header block {headerBlockCount} content ===");
                            for (int i = 0; i < headerText.Length; i += 80)
                            {
                                int lineEnd = Math.Min(i + 80, headerText.Length);
                                string line = headerText.Substring(i, lineEnd - i).TrimEnd('\0');
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    Logger.Debug($"Header line {(i/80)+1}: '{line}'");
                                }
                            }
                            Logger.Debug($"=== End header block {headerBlockCount} ===");

                            // Check for END keyword
                            if (headerText.Contains("END     "))
                            {
                                dataStart += 2880;
                                Logger.Info($"Found END keyword in header block {headerBlockCount}, data starts at position {dataStart}");
                                break;
                            }
                            dataStart += 2880;
                        }

                        Logger.Info($"FITS header parsing complete: width={width}, height={height}, bitpix={bitpix}");

                        if (width <= 0 || height <= 0)
                        {
                            Logger.Error($"Invalid FITS dimensions: {width}x{height}");
                            return null;
                        }

                        if (bitpix == 0)
                        {
                            Logger.Error("BITPIX not found in FITS header");
                            return null;
                        }

                        Logger.Info($"FITS file: {width}x{height}, BITPIX={bitpix}");

                        // Calculate expected data size
                        int bytesPerPixel = Math.Abs(bitpix) / 8;
                        long expectedDataSize = (long)width * height * bytesPerPixel;
                        long availableData = fileStream.Length - dataStart;
                        
                        Logger.Info($"Expected data size: {expectedDataSize} bytes, Available: {availableData} bytes");
                        
                        if (availableData < expectedDataSize)
                        {
                            Logger.Warning($"File appears truncated. Expected {expectedDataSize} bytes but only {availableData} available");
                        }

                        // Read image data
                        fileStream.Seek(dataStart, SeekOrigin.Begin);
                        var imageData = new float[width, height];

                        Logger.Info($"Reading image data with BITPIX={bitpix}");

                        if (bitpix == -32) // 32-bit float
                        {
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    byte[] bytes = reader.ReadBytes(4);
                                    if (bytes.Length < 4)
                                    {
                                        Logger.Error($"Unexpected end of file at pixel ({x},{y})");
                                        return null;
                                    }
                                    if (BitConverter.IsLittleEndian)
                                    {
                                        Array.Reverse(bytes); // FITS is big-endian
                                    }
                                    imageData[x, y] = BitConverter.ToSingle(bytes, 0);
                                }
                            }
                        }
                        else if (bitpix == 16) // 16-bit signed integer
                        {
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    byte[] bytes = reader.ReadBytes(2);
                                    if (bytes.Length < 2)
                                    {
                                        Logger.Error($"Unexpected end of file at pixel ({x},{y})");
                                        return null;
                                    }
                                    if (BitConverter.IsLittleEndian)
                                    {
                                        Array.Reverse(bytes);
                                    }
                                    imageData[x, y] = BitConverter.ToInt16(bytes, 0);
                                }
                            }
                        }
                        else if (bitpix == -64) // 64-bit double
                        {
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    byte[] bytes = reader.ReadBytes(8);
                                    if (bytes.Length < 8)
                                    {
                                        Logger.Error($"Unexpected end of file at pixel ({x},{y})");
                                        return null;
                                    }
                                    if (BitConverter.IsLittleEndian)
                                    {
                                        Array.Reverse(bytes);
                                    }
                                    imageData[x, y] = (float)BitConverter.ToDouble(bytes, 0);
                                }
                            }
                        }
                        else
                        {
                            Logger.Warning($"Unsupported BITPIX: {bitpix}, attempting 16-bit fallback");
                            // Fallback to 16-bit reading
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    byte[] bytes = reader.ReadBytes(2);
                                    if (bytes.Length < 2)
                                    {
                                        Logger.Error($"Unexpected end of file at pixel ({x},{y}) during fallback read");
                                        return null;
                                    }
                                    if (BitConverter.IsLittleEndian)
                                    {
                                        Array.Reverse(bytes);
                                    }
                                    imageData[x, y] = BitConverter.ToInt16(bytes, 0);
                                }
                            }
                        }

                        Logger.Info($"Successfully read image data: {width}x{height} pixels");

                        return new SimpleFitsData
                        {
                            Width = width,
                            Height = height,
                            ImageData = imageData,
                            BitPix = bitpix
                        };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error reading FITS file '{fitsFilePath}': {ex.Message}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                    return null;
                }
            });
        }

        private int ParseFitsKeyword(string headerText, string keyword)
        {
            try
            {
                // FITS keywords are at the start of 80-character lines
                // Format: "KEYWORD = value / comment" or "KEYWORD = value"
                string[] lines = new string[headerText.Length / 80];
                for (int i = 0; i < headerText.Length; i += 80)
                {
                    int lineEnd = Math.Min(i + 80, headerText.Length);
                    lines[i / 80] = headerText.Substring(i, lineEnd - i).TrimEnd('\0', ' ');
                }

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Check if this line starts with our keyword
                    if (line.StartsWith(keyword))
                    {
                        Logger.Debug($"Found line with keyword '{keyword}': '{line}'");
                        
                        // Look for the equals sign
                        int equalsIndex = line.IndexOf('=');
                        if (equalsIndex < 0) continue;
                        
                        // Extract value part (between = and / or end of meaningful content)
                        string valuePart = line.Substring(equalsIndex + 1);
                        
                        // Find comment separator or end
                        int commentIndex = valuePart.IndexOf('/');
                        if (commentIndex >= 0)
                        {
                            valuePart = valuePart.Substring(0, commentIndex);
                        }
                        
                        // Clean up the value
                        string valueText = valuePart.Trim();
                        Logger.Debug($"Parsing FITS keyword '{keyword}': raw value = '{valueText}'");
                        
                        if (int.TryParse(valueText, out int value))
                        {
                            Logger.Debug($"Successfully parsed '{keyword}' = {value}");
                            return value;
                        }
                        else
                        {
                            Logger.Warning($"Failed to parse '{keyword}' value: '{valueText}' from line: '{line}'");
                        }
                    }
                }
                
                Logger.Debug($"Keyword '{keyword}' not found in header");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error parsing FITS keyword {keyword}: {ex.Message}");
                return 0;
            }
        }

        private async Task<string> ConvertFitsDataToJpgAsync(SimpleFitsData fitsData, string jpgPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Find min/max values for scaling
                    float minVal = float.MaxValue;
                    float maxVal = float.MinValue;

                    for (int y = 0; y < fitsData.Height; y++)
                    {
                        for (int x = 0; x < fitsData.Width; x++)
                        {
                            float value = fitsData.ImageData[x, y];
                            if (!float.IsNaN(value) && !float.IsInfinity(value))
                            {
                                minVal = Math.Min(minVal, value);
                                maxVal = Math.Max(maxVal, value);
                            }
                        }
                    }

                    // Avoid division by zero
                    if (Math.Abs(maxVal - minVal) < 1e-10f)
                    {
                        maxVal = minVal + 1.0f;
                    }

                    Logger.Info($"FITS data range: {minVal} to {maxVal}");

                    // Create bitmap and convert
                    using (var bitmap = new Bitmap(fitsData.Width, fitsData.Height, PixelFormat.Format24bppRgb))
                    {
                        for (int y = 0; y < fitsData.Height; y++)
                        {
                            for (int x = 0; x < fitsData.Width; x++)
                            {
                                float value = fitsData.ImageData[x, y];
                                
                                // Handle NaN/Infinity values
                                if (float.IsNaN(value) || float.IsInfinity(value))
                                {
                                    value = minVal;
                                }

                                // Apply simple linear stretch
                                float normalized = (value - minVal) / (maxVal - minVal);
                                
                                // Apply gamma correction for better visibility
                                normalized = (float)Math.Pow(normalized, 0.5);
                                
                                int intensity = (int)Math.Round(255.0f * Math.Max(0, Math.Min(1, normalized)));

                                // Set pixel (flip Y coordinate since FITS has origin at bottom-left)
                                Color color = Color.FromArgb(intensity, intensity, intensity);
                                bitmap.SetPixel(x, fitsData.Height - 1 - y, color);
                            }
                        }
                        
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

                    Logger.Info($"FITS manually converted to JPG: {jpgPath}");
                    return jpgPath;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to convert FITS data to JPG: {ex}");
                    throw;
                }
            });
        }

        private async Task<string> CreatePlaceholderImageAsync(string jpgPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Create a 640x480 placeholder image
                    using (var bitmap = new Bitmap(640, 480, PixelFormat.Format24bppRgb))
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        // Fill with dark gray background
                        graphics.Clear(Color.FromArgb(32, 32, 32));
                        
                        // Add text indicating this is a placeholder
                        using (var font = new Font("Arial", 16, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.White))
                        {
                            string text = $"PHD2 Image\n{DateTime.Now:HH:mm:ss}\n\nFITS conversion not available\nPlaceholder image";
                            var textBounds = graphics.MeasureString(text, font);
                            float x = (bitmap.Width - textBounds.Width) / 2;
                            float y = (bitmap.Height - textBounds.Height) / 2;
                            
                            graphics.DrawString(text, font, brush, x, y);
                        }
                        
                        // Draw a simple border
                        using (var pen = new Pen(Color.Gray, 2))
                        {
                            graphics.DrawRectangle(pen, 10, 10, bitmap.Width - 20, bitmap.Height - 20);
                        }
                        
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

                    Logger.Info($"Placeholder image created: {jpgPath}");
                    return jpgPath;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create placeholder image: {ex}");
                    throw;
                }
            });
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