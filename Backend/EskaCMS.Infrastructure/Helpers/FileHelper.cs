using MimeDetective;
using Newtonsoft.Json;
using NUglify;
using NUglify.Css;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers
{
    public static class FileHelper
    {
        
        public static bool UpsertFile(string Content, string Name, string Path,string type)
        {
            try
            {

                CreateIfMissing(Path);

                var FileName = Path + $"/{Name}" + $".{type.ToLower()}";

                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }

                StringBuilder Builder = new StringBuilder();

                Builder.Append(Content.TrimStart('\"').TrimEnd('\"'));

                byte[] bytes = Encoding.UTF8.GetBytes(Builder.ToString());

                using (var stream = File.Create(FileName))
                {

                    stream.Write(bytes, 0, bytes.Length);


                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private static void CreateIfMissing(string path)
        {
            bool folderExists = Directory.Exists(path);
            if (!folderExists)
                Directory.CreateDirectory(path);
        }

        public static string MinifyCssFile(string cssContent)
        {
            try
            {
                cssContent = JsonConvert.DeserializeObject(cssContent).ToString().Replace("\\r", " ").Replace("/", "").Replace(" ", "");

            }
            catch (Exception)
            {


            }

            var settings = new CssSettings();
            UglifyResult result = Uglify.Css(cssContent, settings);
            return result.Code;
        }

        public static FileExtension GetFileExtension(string file)
        {
            try
            {
                

                
                byte[] fileBytes = Convert.FromBase64String(file);

                var Inspector = new ContentInspectorBuilder()
                {
                    Definitions = MimeDetective.Definitions.Default.All()
                }.Build();
                var Results = Inspector.Inspect(fileBytes);

                var type = Results[0].Definition.File.Extensions[0];


                var fileExtension = new FileExtension(type);
                return fileExtension;
              
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        
    }

    #region Helper Models

    public class FileExtension
    {
        public FileExtension()
        {

        }
        public FileExtension(string extension)
        {
            this.Extension = extension;
           
        }
        public string Extension { get; set; }
      
    }
    #endregion
}
