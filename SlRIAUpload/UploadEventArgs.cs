using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SlRIAUpload
{
    public class UploadEventArgs : EventArgs
    {
        private int PercentComplete;

        public UploadEventArgs(int Percent)
        {
            PercentComplete = Percent;
        }

        public int Completed
        {
            get
            {
                return PercentComplete;
            }
        }
    }
}
