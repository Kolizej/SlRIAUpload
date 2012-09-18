
namespace SlRIAUpload.Web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using System.IO;


    [EnableClientAccess()]
    public class UploadDomainService : DomainService
    {     
        public bool UploadFilePart(string fileName, byte[] buffer)
        {
            try
            {
                string uploadPath = System.Configuration.ConfigurationManager.AppSettings.Get("uploadPath");
                FileStream fs = new FileStream(string.Format(@"{0}\{1}",uploadPath, fileName), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                BinaryWriter fileWriter = new BinaryWriter(fs);
                fileWriter.Write(buffer);
                fileWriter.Close();
                return true;
            }
            catch (IOException ee)
            {
                throw new IOException("Uploading Error");
            }
        }

        public bool FileIfExistsDelete(string fileName)
        {
            try
            {
                string uploadPath = System.Configuration.ConfigurationManager.AppSettings.Get("uploadPath");
                if (File.Exists(string.Format(@"{0}\{1}", uploadPath, fileName)))
                    File.Delete(string.Format(@"{0}\{1}", uploadPath, fileName));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}


