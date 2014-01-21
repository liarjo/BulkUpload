using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TED.Sample.AzureBlobHelper
{
    internal class FileBlock
    {
        public string Id
        {
            get;
            set;
        }

        public byte[] Content
        {
            get;
            set;
        }
    }
}
