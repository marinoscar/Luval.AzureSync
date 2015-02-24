using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.File;

namespace Luval.AzureSync
{
    public class DirectorySync
    {
        #region Variable Declaration

        private ILogger _logger;
        private SyncTaskSceduler _fileScheduler;
        private SyncTaskSceduler _directoryScheduler;
        private List<CloudFileDirectory> _cloudDirectories;
        private List<CloudFile> _cloudFiles;
        private List<string> _cloudFileNames;
        private List<string> _localFileNames;

        #endregion

        #region Constructors

        public DirectorySync(KeyData key, string cloudShareName, string directoryToSync)
            : this(key, cloudShareName, new DirectoryInfo(directoryToSync), new NoLogger())
        {

        }

        public DirectorySync(KeyData key, string cloudShareName, DirectoryInfo directoryToSync, ILogger logger)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(key.Account, key.PrivateKey), true);
            var fileClient = storageAccount.CreateCloudFileClient();
            var fileShare = fileClient.GetShareReference(cloudShareName);
            fileShare.CreateIfNotExists();
            Initialize(fileShare.GetRootDirectoryReference(), directoryToSync, logger);
        }

        public DirectorySync(CloudFileDirectory rootDirectory, DirectoryInfo directoryToSync, ILogger logger)
        {
            Initialize(rootDirectory, directoryToSync, logger);
        }

        private void Initialize(CloudFileDirectory rootDirectory, DirectoryInfo directoryToSync, ILogger logger)
        {
            CloudDirectory = rootDirectory;
            LocalDirectory = directoryToSync;
            _logger = logger;
            CloudDirectory.CreateIfNotExists();
            _fileScheduler = new SyncTaskSceduler();
            _directoryScheduler = new SyncTaskSceduler();
            _cloudDirectories = new List<CloudFileDirectory>();
            _cloudFiles = new List<CloudFile>();
        }

        #endregion

        #region Property Implemenation

        public DirectoryInfo LocalDirectory { get; private set; }
        public CloudFileDirectory CloudDirectory { get; private set; }
        public bool RunAsync { get; set; }

        #endregion

        #region Method Implementation

        private void LoadDirectoryMetadata()
        {
            _logger.WriteLine("Loading directory metadata {0}", CloudDirectory.Name);
            var items = CloudDirectory.ListFilesAndDirectories();
            foreach (var i in items)
            {
                if (i is CloudFile)
                {
                    var file = (CloudFile)i;
                    file.FetchAttributes();
                    _cloudFiles.Add(file);
                }
                else _cloudDirectories.Add((CloudFileDirectory)i);
            }
            _cloudFileNames = _cloudFiles.Where(i => i.Metadata.ContainsKey(Constants.LocalFileName)).Select(i => i.Metadata[Constants.LocalFileName].ToLowerInvariant()).ToList();
            _localFileNames =
                LocalDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                              .Select(i => i.FullName.ToLowerInvariant())
                              .ToList();
        }

        public void Sync()
        {
            _fileScheduler.RunAsync = RunAsync;
            var sw = Stopwatch.StartNew();
            LoadDirectoryMetadata();
            _logger.WriteLine("Syncing folder {0}", LocalDirectory.Name);
            var files = LocalDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            var dirs = LocalDirectory.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
            var missingLocalFiles = _cloudFileNames.Where(i => !_localFileNames.Contains(i)).ToList();
            foreach (var file in files)
            {
                _fileScheduler.Execute(new Task(() => { SyncFile(file); }));
            }
            foreach (var missingLocalFile in missingLocalFiles)
            {
                _fileScheduler.Execute(new Task(() => { SyncFromCloud(missingLocalFile); }));
            }
            _fileScheduler.WaitAll();
            sw.Stop();
            _logger.WriteLine("Finish syncing {0} in {1}", LocalDirectory.Name, sw.Elapsed);
            var missingLocalDirectories = _cloudDirectories.Where(i => !dirs.Select(j => j.Name).Contains(i.Name));
            foreach (var dir in dirs)
            {
                _directoryScheduler.Execute(new Task(() =>
                    {
                        var dirSync = new DirectorySync(CloudDirectory.GetDirectoryReference(dir.Name), dir, _logger);
                        dirSync.Sync();
                    }));
            }
            foreach (var cloudDir in missingLocalDirectories)
            {
                _directoryScheduler.Execute(new Task(() =>
                    {
                        var dirInfo = new DirectoryInfo(Path.Combine(LocalDirectory.FullName, cloudDir.Name));
                        dirInfo.Create();
                        var dirSync = new DirectorySync(cloudDir, dirInfo, _logger);
                        dirSync.Sync();
                    }));
            }
        }


        public void SyncFile(FileInfo file)
        {
            var fileSync = new FileSync(CloudDirectory, file, _logger);
            fileSync.Sync();
        }

        private void SyncFromCloud(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            var cloudFile = CloudDirectory.GetFileReference(fileInfo.Name);
            var fileSync = new FileSync(CloudDirectory, cloudFile, LocalDirectory, _logger);
            fileSync.Sync();
        }

        #endregion
    }
}
