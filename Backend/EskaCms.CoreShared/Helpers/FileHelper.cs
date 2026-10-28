using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace EskaCms.CoreShared.Helpers
{
    public class FileHelper
    {
        private string[] permittedExtensions = { ".jpg", ".jpeg", ".tiff", ".png", ".svg" };
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileHelper(IWebHostEnvironment webHostEnvironmen)
        {
            _webHostEnvironment = webHostEnvironmen;
        }

        public string PostFile(IFormFile File, long siteId)
        {
            try
            {
                string randomFileName = Guid.NewGuid().ToString() + Path.GetExtension(File.FileName);
                string directory = _webHostEnvironment.WebRootPath + "\\Resources\\UserImages\\site_" + siteId.ToString();
                string filePath = Path.Combine(directory, randomFileName);
                if (File.Length > 0)
                {
                    if (File.Length >= 5242880)
                    {
                        throw new ValidationException("File size is invalid, max file size is 5 MB");
                    }


                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                    {
                        throw new ValidationException("File extention is not valid");
                    }
                    var stream = System.IO.File.Create(filePath);
                    File.CopyTo(stream);
                    stream.Close();
                }

                return randomFileName;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public string UpdateFile(IFormFile File, string oldFileName, long siteId)
        {
            try
            {
                string directory = _webHostEnvironment.WebRootPath + "\\Resources\\UserImages\\site_" + siteId.ToString();
                if (!string.IsNullOrEmpty(oldFileName))
                {
                    string oldFilePath = Path.Combine(directory, oldFileName);
                    System.IO.File.Delete(oldFilePath);
                }
                string randomFileName = Guid.NewGuid().ToString() + Path.GetExtension(File.FileName);
                string filePath = Path.Combine(directory, randomFileName);
                if (File.Length > 0)
                {
                    if (File.Length >= 5242880)
                    {
                        throw new ValidationException("File size is invalid, max file size is 5 MB");
                    }


                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                    {
                        throw new ValidationException("File extention is not valid");
                    }
                    var stream = System.IO.File.Create(filePath);
                    File.CopyTo(stream);
                    stream.Close();

                }

                return randomFileName;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        public IFormFile ReturnFormFile(FileStream result, string fileName)
        {
            var ms = new MemoryStream();
            try
            {
                result.CopyTo(ms);
                return new FormFile(ms, 0, ms.Length, fileName, fileName);
            }
            catch (Exception e)
            {
                ms.Dispose();
                throw;
            }
            finally
            {
                ms.Dispose();
            }
        }
        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        public bool DeleteFile(string fileName)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath + "\\AppImages\\", fileName);
            if (File.Exists(path))
            {
                // If file found, delete it    
                File.Delete(Path.Combine(path));
            }
            return true;
        }
    }
}
