using System;
using System.Collections;
using System.IO;
using System.Threading;


namespace AzureBlobHelper
{
    public class UploadHelpDir
    {
        private UploadDirInfo myInfo=new UploadDirInfo();
        private Hashtable workers;
        private int maxfileParallel;
        public event EventHandler<UploadFileInfo> onFileProgress;
        public event EventHandler<UploadDirInfo> onDirProgress;
        public event EventHandler<UploadFileInfo> onFinishFile;
        public event EventHandler<object> onFinishAll;

        private void engine_onProgress(object sender, UploadFileInfo e)
        {
            if (myInfo.UploadFilesInfo.Contains(e.BlobName))
            {
                myInfo.UploadFilesInfo.Add(e.BlobName, e);
            }
            else
            {
                myInfo.UploadFilesInfo[e.BlobName] = e;
            }
            if (onDirProgress != null)
            {
                onDirProgress(this, myInfo);
            }
            if (onFileProgress != null)
            {
                try
                {
                    onFileProgress(this, e);
                }
                catch (Exception x)
                {

                    throw x;
                }

            }
        }
        void worker_onFinish(object sender, UploadFileInfo e)
        {
            UploadHelpFile worker = (UploadHelpFile)sender;
            if (worker.IsComplete)
            {
                System.Diagnostics.Trace.WriteLine("File: {0} Finish", worker.File2Upload);
                workers[worker.File2Upload] = null;
                workers.Remove(worker.File2Upload);
                ParallelAdvance();
            }
            if (onFinishFile != null)
            {
                try
                {
                    onFinishFile(this, e);
                }
                catch (Exception X)
                {
                    
                    throw X;
                }
                
            }
            if ((workers.Count == 0)&& (onFinishAll!=null))
            {
                try
                {
                    onFinishAll(null, null);
                }
                catch (Exception X)
                {

                    throw X;
                }
            }
        }
        void worker_onError(object sender, UploadFileInfo e)
        {
            UploadHelpFile worker = (UploadHelpFile)sender;
            workers[worker.File2Upload] = null;
            workers.Remove(worker.File2Upload);
            workers.Add(worker.File2Upload, new Thread((worker.ParallelUploadBlob)));
            System.Diagnostics.Trace.WriteLine("Recreate Worker: " + worker.File2Upload);
        }
        private int CurrentWorkers()
        {
            int acc = 0;
            foreach (string fileName in workers.Keys)
            {
                if ((workers[fileName] as Thread).IsAlive)
                {
                    acc += 1;
                }
            }
            return acc;
        }
        private void ParallelAdvance()
        {
            int currentWorkers = CurrentWorkers();
            foreach (string fileName in workers.Keys)
            {
                if (currentWorkers < maxfileParallel)
                {
                    if (!(workers[fileName] as Thread).IsAlive)
                    {
                        //start
                        (workers[fileName] as Thread).Start();
                        currentWorkers += 1;
                    }
                }
            }
        }
        public void UploadDir(string path, string storageConn, string storageContainer, int MaxFileParallel, int MaxThreadParallel)
        {
            myInfo.IsRunning = true;
            maxfileParallel = MaxFileParallel;
            workers = new Hashtable();
            string[] filesList = Directory.GetFiles(path);
            myInfo.TotalFiles = filesList.Length;
            foreach (var file in filesList)
            {
                UploadHelpFile worker = new UploadHelpFile(storageConn);
                worker.ThreadMax = MaxThreadParallel;
                worker.BlobContainer = storageContainer;
                worker.File2Upload = file;
                worker.onProgress += this.engine_onProgress;
                worker.onFinish += worker_onFinish;
                worker.onError += worker_onError;
                workers.Add(worker.File2Upload, new Thread((worker.ParallelUploadBlob)));
            }
            ParallelAdvance();
            System.Diagnostics.Trace.WriteLine("all file ready to start upload");

        }
    }
}
