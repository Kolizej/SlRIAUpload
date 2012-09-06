using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using SlRIAUpload.Web;
using System.ComponentModel;
using System.Threading;
using System.ServiceModel.DomainServices.Client;

namespace SlRIAUpload
{
    public partial class UploadControl : UserControl
    {
        private UploadDomainContext _context;
        private double currentProgressIterator = 0;
        private double progressIterator = 0;
        private FileInfo _finfo = null;
        private InvokeOperation<bool> op_upload = null;
        private BinaryReader br = null;
        private int numChuncks = 0;
        private const int bufferSize = 4096;
        private bool retryWrite = true;
        private bool retryRead = true;
        private Stream fs = null;
        private long currentSeekPosition = 0;
        private byte[] currentBuffer = null;

        public UploadControl()
        {
            InitializeComponent();
            _context = new UploadDomainContext();
        }

        private void UploadFile()
        {
            fs = _finfo.OpenRead();
            br = new BinaryReader(fs);
            numChuncks = Convert.ToInt32(fs.Length / bufferSize) + 1;
            progressIterator = 100 / Convert.ToDouble(numChuncks);
            byte[] buffer = readNextBuffer();
            writeNextBuffer(buffer);
        }

        private byte[] readNextBuffer()
        {
            retryRead = true;
            byte[] buffer = new byte[bufferSize];
            while (retryRead)
            {
                try
                {
                    buffer = br.ReadBytes(bufferSize);
                    currentSeekPosition = br.BaseStream.Position;
                    retryRead = false;
                }
                catch (IOException ee)
                {
                    if (MessageBox.Show(string.Format("Error occured while reading source file: {0} Try read again?", ee.Message), "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        retryRead = true;
                        _finfo.Refresh();
                        fs = _finfo.OpenRead();
                        fs.Seek(currentSeekPosition, SeekOrigin.Begin);
                        br = new BinaryReader(fs);
                    }
                    else
                    {
                        retryRead = false;
                        _context.FileIfExistsDelete(_finfo.Name);
                        return null;
                    }
                }
            }

            if (buffer.Length == 0)
            {
                MessageBox.Show("Upload completed");
                return null;
            }

            currentBuffer = buffer;
            return buffer;
        }

        private void writeNextBuffer(byte[] buffer)
        {
            op_upload = _context.UploadFilePart(_finfo.Name, buffer);
            op_upload.Completed += new EventHandler(op_upload_Completed);
        }

        void op_upload_Completed(object sender, EventArgs e)
        {
            if (op_upload.Value)
            {
                currentProgressIterator += progressIterator;

                if (Dispatcher.CheckAccess())
                {
                    uploadProgress.Value = Convert.ToInt32(currentProgressIterator);
                    txtProgress.Text = uploadProgress.Value.ToString() + "%";
                }
                else
                {
                    Dispatcher.BeginInvoke((EventHandler)op_upload_Completed, sender, e);
                }

                byte[] buffer = readNextBuffer();
                writeNextBuffer(buffer);
            }
            else
            {
                if (MessageBox.Show("Error occured while uploading file. Try to resume upload?", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    writeNextBuffer(currentBuffer);
                }
                else
                {
                    return;
                }
            }
        }

        void op_delete_Completed(object sender, EventArgs e)
        {
            UploadFile();
        }        

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            if (fd.ShowDialog().Value)
            {
                _finfo = fd.File;
                txtFilename.Text = _finfo.Name;
                InvokeOperation op_delete = _context.FileIfExistsDelete(_finfo.Name);
                op_delete.Completed += new EventHandler(op_delete_Completed);               
            }
        }        
    }
}
