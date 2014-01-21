using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using TED.Sample.AzureBlobHelper;

namespace UploadDirContent
{
    class Program
    {
        static object waitConsole = new object();
        static UploadHelpDir myHelp = new UploadHelpDir();
        static string strconn = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";
        static DateTime tCero;
        static void Main(string[] args)
        {
            myHelp.onFileProgress += myHelp_onProgress;
            myHelp.onFinishFile += myHelp_onFinishFile;
            myHelp.onFinishAll += myHelp_onFinishAll;
            myHelp.onDirProgress += myHelp_onDirProgress;

            string path = ConfigurationManager.AppSettings["OriginPath"];
            if (path == null)
            {
                Console.WriteLine("Enter Path:");
                path = Console.ReadLine();
            }
            
            Console.WriteLine("Enter Container");
            string container=Console.ReadLine();

            string conn=string.Format(
                strconn,
                ConfigurationManager.AppSettings["storageAccount"],
                ConfigurationManager.AppSettings["storageKey"]);

            int maxFilesParallel = int.Parse(ConfigurationManager.AppSettings["MaxFileParallel"]);
            int maxThreadParallel = int.Parse(ConfigurationManager.AppSettings["MaxThreadParallel"]);

            tCero=DateTime.Now;
            Console.WriteLine("StartTime: " + tCero.ToShortTimeString());
            myHelp.UploadDir(path, conn, container, maxFilesParallel,maxThreadParallel );
        }

        static void myHelp_onDirProgress(object sender, UploadDirInfo e)
        {
            lock (waitConsole)
            {
                Console.Clear();
                foreach (UploadFileInfo fileUpdate in e.UploadFilesInfo.Values)
                {
                    Console.WriteLine("File {0}", fileUpdate.BlobName);
                    Console.WriteLine("Duration {0} ", DateTime.Now.Subtract(fileUpdate.StartTime).ToString());
                    Console.WriteLine(fileUpdate.ProgressBlock + " of " + fileUpdate.TotalBlock + " progress " + fileUpdate.Progress.ToString("P"));
                    Console.WriteLine("Speed {0} kbps", (fileUpdate.Speed * 1000).ToString("F"));
                    Console.WriteLine("");
                }
            }
        }

        static void myHelp_onFinishAll(object sender, object e)
        {
            lock (waitConsole)
            {
                Console.WriteLine("Finish all Files");
                Console.WriteLine("StartTime: " + tCero.ToShortTimeString());
                Console.WriteLine("Finish Time: " + DateTime.Now.ToShortTimeString());
                Console.ReadLine();
            }
        }

        static void myHelp_onFinishFile(object sender, UploadFileInfo e)
        {
            lock (waitConsole)
            {
                Console.WriteLine("File {0} finish", e.BlobName);
                myHelp_onProgress(null, e);
            }
        }

        

        static void myHelp_onProgress(object sender, UploadFileInfo e)
        {
            //Console.WriteLine("File {0}", e.BlobName);
            //Console.WriteLine("Duration {0} ", DateTime.Now.Subtract(e.StartTime).ToString());
            //Console.WriteLine(e.ProgressBlock + " of " + e.TotalBlock + " progress " + e.Progress.ToString("P"));
            //Console.WriteLine("Speed {0} kbps", (e.Speed * 1000).ToString("F"));
            //Console.WriteLine("");
        }
    }
}
