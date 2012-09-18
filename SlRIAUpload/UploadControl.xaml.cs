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
        private bool retryRead = true;
        private Stream fs = null;
        private long currentSeekPosition = 0;
        private byte[] currentBuffer = null;
        private bool IsPaused = false;

        public UploadControl()
        {
            InitializeComponent();
            _context = new UploadDomainContext();
            btnPause.IsEnabled = false;
            btnCancel.IsEnabled = false;
        }

        private void ClearUploadingValues()
        {
            btnSelect.IsEnabled = true;
            btnPause.IsEnabled = false;
            btnCancel.IsEnabled = false;
            uploadProgress.Value = 0;
            currentProgressIterator = 0;
            progressIterator = 0;
            txtProgress.Text = "0%";
        }

        private void UploadFile()
        {
            fs = _finfo.OpenRead();
            br = new BinaryReader(fs);
            numChuncks = Convert.ToInt32(fs.Length / bufferSize) + 1;
            progressIterator = 100 / Convert.ToDouble(numChuncks);
            btnSelect.IsEnabled = false;
            btnPause.IsEnabled = true;
            btnCancel.IsEnabled = true;
            byte[] buffer = readNextBuffer();
            writeNextBuffer(buffer);
        }

        private byte[] readNextBuffer()
        {
            txtStatus.Text = "Uploading...";
            retryRead = true;
            byte[] buffer = new byte[bufferSize];
            //while (retryRead)
            {
                try
                {
                    buffer = br.ReadBytes(bufferSize);
                    currentSeekPosition = br.BaseStream.Position;
                    retryRead = false;
                }
                catch (IOException ee)
                {
                    Resume(ee);
                }
            }

            if (buffer.Length == 0)
            {
                MessageBox.Show("Upload completed");
                btnSelect.IsEnabled = true;
                ClearUploadingValues();
                txtStatus.Text = "Completed";
                return null;
            }


            currentBuffer = buffer;
            return buffer;
        }

        private void Resume(IOException ee)
        {
                Thread.Sleep(5000);
                try
                {
                    retryRead = true;
                    _finfo.Refresh();
                    fs = _finfo.OpenRead();
                    fs.Seek(currentSeekPosition, SeekOrigin.Begin);
                    br = new BinaryReader(fs);
                }
                catch (IOException e)
                {
                    if (MessageBox.Show(string.Format("Error occured while reading source file: {0} Try read again?", ee.Message), "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        Resume(e);
                    }
                    else
                    {
                        retryRead = false;
                        _context.FileIfExistsDelete(_finfo.Name);
                    }
                }
           
        }

        private void writeNextBuffer(byte[] buffer)
        {
            if (buffer != null)
            {
                op_upload = _context.UploadFilePart(_finfo.Name, buffer);
                op_upload.Completed += new EventHandler(op_upload_Completed);
            }
        }

        void op_upload_Completed(object sender, EventArgs e)
        {
            if (IsPaused)
                return;

            if(!op_upload.HasError)
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
                ClearUploadingValues();
                _finfo = fd.File;
                txtFilename.Text = _finfo.Name;
                InvokeOperation op_delete = _context.FileIfExistsDelete(_finfo.Name);
                op_delete.Completed += new EventHandler(op_delete_Completed);               
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (IsPaused)
            {
                btnPause.Content = "Pause";
                IsPaused = false;
                writeNextBuffer(currentBuffer);
                txtStatus.Text = "Uploading...";
            }
            else
            {
                btnPause.Content = "Resume";
                txtStatus.Text = "Paused";
                IsPaused = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsPaused = true;
            InvokeOperation op_deleteOnCancel = _context.FileIfExistsDelete(_finfo.Name);
            op_deleteOnCancel.Completed += new EventHandler(op_deleteOnCancel_Completed);
        }

        void op_deleteOnCancel_Completed(object sender, EventArgs e)
        {
            ClearUploadingValues();
            btnSelect.IsEnabled = true;
            btnPause.IsEnabled = false;
            btnCancel.IsEnabled = false;
            txtStatus.Text = "Canceled";
            IsPaused = false;
        }        
    }
}
