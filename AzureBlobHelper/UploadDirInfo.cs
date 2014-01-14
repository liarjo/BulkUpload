using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace AzureBlobHelper
{
    public class UploadDirInfo
    {
        public int TotalFiles { get; set; }
        public bool IsRunning { get; set;}
        public Hashtable UploadFilesInfo;
        public UploadDirInfo()
        {
            IsRunning = false;
            UploadFilesInfo = new Hashtable();
        }
    }
}
