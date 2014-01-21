using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TED.Sample.AzureBlobHelper
{
    public class UploadFileInfo
    {
        public bool IsComplete { get; set; }
        public int TotalBlock { get; set; }
        public int ProgressBlock { get; set; }
        public DateTime StartTime { get; set; }
        public string BlobName { get; set; }
        public string ErrorMessage { get; set; }
        public double Progress
        {
            get
            {
                double adv = 0;
                if (ProgressBlock > 0)
                {
                    adv = Convert.ToDouble(ProgressBlock) / Convert.ToDouble(TotalBlock);
                }
                return adv;
            }
        }
        public double Speed
        {
            get
            {
                double aux = 0;
                double sec = DateTime.Now.Subtract(StartTime).TotalSeconds;
                aux = TotalBlock * ProgressBlock / sec / 1000;
                return aux;
            }
        }
        public UploadFileInfo(int totalBlock, int progressBlock)
        {
            TotalBlock = totalBlock;
            ProgressBlock = progressBlock;
        }
        public UploadFileInfo()
        {
            IsComplete = false;
        }
    }
}
