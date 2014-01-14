BulkUpload
=========
BulkUpload is a C# sample project to show how you can upload all directory's content to Windows Azure Blob Storage, using parallel upload of files and blocks to optimize the upload's time. it's a sample of Bulk load to Azure blob Storage.

The examples has the capacity to resume the upload of files if you have any problems, saving time. You could check the upload task's porgress.

The configuration keys:
1. storageAccount: storage account name
2. storageKey: azure stoarge key
3. MaxFileParallel: how many files you will upload at time
4. MaxThreadParallel: hay many thread by file you will use to upload the each file.

This is a example and it's in beta.

