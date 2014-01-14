using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using System.Globalization;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections;

namespace AzureBlobHelper
{
    public class UploadHelpFile
    {
        private int MaxBlockSize = 4000000;
        private UploadFileInfo myinfo;
        private int intTotalBlock;
        private int intProgressAcc;
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobclient;
        private CloudBlockBlob myblob;
        private bool isComplete = false;
        private bool isactive = false;
        public event EventHandler<UploadFileInfo> onProgress;
        public event EventHandler<UploadFileInfo> onFinish;
        public event EventHandler<UploadFileInfo> onError;
        public string File2Upload { get; set; }
        public string BlobContainer { get; set; }
        public int ThreadMax { get; set; }
        public bool IsComplete { get { return isComplete; } }
        private void OnProgressCall(UploadFileInfo e)
        {
            myinfo.IsComplete = isComplete;
            if (isComplete)
            {
                if (onFinish != null)
                {
                    onFinish(this, e);
                }
            }
            else
            {
                if (onProgress != null)
                {
                    onProgress(this, e);
                }
            }
        }
        private void OnErrorCall(UploadFileInfo e)
        {
            if (onError != null)
            {
                onError(this, e);
            }
        }
        public UploadHelpFile(string StorageConn)
        {
            storageAccount = CloudStorageAccount.Parse(StorageConn);
            blobclient = storageAccount.CreateCloudBlobClient();
            myinfo = new UploadFileInfo(0, 0);
        }
        private List<IAsyncResult> WaitPutBlock(List<IAsyncResult> workList, int TargetThreads)
        {
            do
            {
                for (int index = 0; index < workList.Count; index++)
                {
                    IAsyncResult result = workList[index];
                    if (result.IsCompleted)
                    {
                        UploadState blockdata = (UploadState)result.AsyncState;

                        workList.RemoveAt(index);
                        try
                        {
                            lock (this)
                            {
                                myblob.EndPutBlock(result);
                                blockdata.Info.Dispose();
                                intProgressAcc += 1;
                                myinfo.ProgressBlock = intProgressAcc;
                            }

                            this.OnProgressCall(myinfo);
                        }
                        catch (Exception X)
                        {
                            System.Diagnostics.Trace.WriteLine("WaitPutBlock Error: " + X.Message);
                            System.Diagnostics.Trace.WriteLine("Retraying : " + blockdata.InfoIde + "of " + myblob.Name);
                            workList.Add(myblob.BeginPutBlock(blockdata.InfoIde, blockdata.Info, null, null, blockdata));
                        }
                        index--;
                    }
                }
                System.Threading.Thread.Sleep(1000);
            } while (workList.Count > TargetThreads);
            return workList;
        }
        private IEnumerable<ListBlockItem> GetBlockListUncomitted(CloudBlockBlob myblob)
        {
            IEnumerable<ListBlockItem> uCommitted = null;
            try
            {
                uCommitted = myblob.DownloadBlockList(BlockListingFilter.Uncommitted);
            }
            catch (Exception)
            {
                System.Diagnostics.Trace.WriteLine("GetBlockListUncomitted: Not uncomitted blocks in " + myblob.Name);
            }
            return uCommitted;

        }
        public void ParallelUploadBlob()
        {
            if (isactive)
            {
                System.Diagnostics.Trace.WriteLine("ParallelUploadBlob is already running ");
            }
            else
            {
                myinfo.BlobName = Path.GetFileName(File2Upload);
                FileInfo fi = new FileInfo(File2Upload);

                this.intTotalBlock = Convert.ToInt32(Math.Ceiling((fi.Length) / Convert.ToDouble(MaxBlockSize)));

                CloudBlobContainer container = blobclient.GetContainerReference(BlobContainer);
                container.CreateIfNotExists();
                //CloudBlockBlob blob = container.GetBlockBlobReference(myinfo.BlobName);
                myblob = container.GetBlockBlobReference(myinfo.BlobName);

                HashSet<string> blocklist = new HashSet<string>();
                List<IAsyncResult> asyncResultsPutBlock = new List<IAsyncResult>();
                //Obtain Block allredy uploaded
                IEnumerable<ListBlockItem> uCommitted = GetBlockListUncomitted(myblob);

                int blockId = 0;
                string idBlock;
                bool swBlockReady = false;
                intProgressAcc = 0;
                myinfo.StartTime = DateTime.Now;
                myinfo.TotalBlock = this.intTotalBlock;

                using (var fs = new FileStream(File2Upload, FileMode.Open, FileAccess.Read))
                {
                    byte[] chunk = new byte[MaxBlockSize];
                    int bytesRead = 0;

                    while ((bytesRead = fs.Read(chunk, 0, chunk.Length)) > 0)
                    {
                        idBlock = Convert.ToBase64String(System.BitConverter.GetBytes(blockId));
                        //Exist this block in storage?
                        if (uCommitted != null)
                        {
                            IEnumerable<ListBlockItem> x =
                                from ListBlockItem
                                    in uCommitted
                                where ListBlockItem.Name == idBlock
                                select ListBlockItem;
                            swBlockReady = (x.Count() == 1);
                        }
                        if (!swBlockReady)
                        {
                            //Last Block diferent size
                            if (bytesRead != MaxBlockSize)
                            {
                                System.Diagnostics.Trace.WriteLine(bytesRead);
                                byte[] chunkBuffer = new byte[bytesRead];
                                Array.Copy(chunk, chunkBuffer, bytesRead);
                                chunk = new byte[bytesRead];
                                chunk = chunkBuffer;
                            }
                            //Start Block upload
                            MemoryStream BlockContent = new MemoryStream(chunk, false);

                            IAsyncResult asyncresult = myblob.BeginPutBlock(
                               idBlock, BlockContent, null, null, new UploadState { InfoIde=idBlock,Info= BlockContent });

                            asyncResultsPutBlock.Add(asyncresult);
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine("Block " + idBlock + "already in Blob Storage");
                            this.intProgressAcc++;
                        }
                        blocklist.Add(idBlock);
                        //Check current thread uploading max
                        if (asyncResultsPutBlock.Count > ThreadMax)
                        {
                            //wait
                            System.Diagnostics.Trace.WriteLine("threadMax: waiting, current " + asyncResultsPutBlock.Count + " file: " + myblob.Name);
                            asyncResultsPutBlock = WaitPutBlock(asyncResultsPutBlock, ThreadMax);

                        }
                        blockId++;
                    }
                    System.Diagnostics.Trace.WriteLine("Total waiting");
                    if (asyncResultsPutBlock.Count > 0)
                    {
                        asyncResultsPutBlock = WaitPutBlock(asyncResultsPutBlock, 0);
                    }
                    try
                    {
                        myblob.PutBlockList(blocklist);
                        isComplete = true;
                        this.OnProgressCall(myinfo);
                    }
                    catch (Exception X)
                    {
                        System.Diagnostics.Trace.WriteLine("ParallelUploadBlob Error: " + X.Message);
                        myinfo.ErrorMessage = X.Message;

                    }
                }
            }

        }
    }
}
