using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.File;

namespace Luval.AzureSync
{
    public class FileSync
    {
        #region Variable Declaration

        private readonly ILogger _logger;
        private CloudFile _cloudFile;
        private DirectoryInfo _localDirectory;

        #endregion

        #region Constructors

        public FileSync(CloudFileDirectory cloudDirectory, FileInfo file)
            : this(cloudDirectory, file, new NoLogger())
        {

        }

        public FileSync(CloudFileDirectory cloudDirectory, FileInfo file, ILogger logger)
        {
            Directory = cloudDirectory;
            File = file;
            _logger = logger;
        }

        public FileSync(CloudFileDirectory cloudDirectory, CloudFile cloudFile, DirectoryInfo localDirectory, ILogger logger)
        {
            _localDirectory = localDirectory;
            Directory = cloudDirectory;
            File = null;
            _logger = logger;
            _cloudFile = cloudFile;
            _cloudFile.FetchAttributes();
        }

        #endregion

        #region Property Implementation

        public CloudFileDirectory Directory { get; private set; }
        public FileInfo File { get; private set; }
        public CloudFile CloudFile
        {
            get
            {
                if (_cloudFile == null)
                {
                    _cloudFile = Directory.GetFileReference(File.Name);
                    if (_cloudFile.Exists()) _cloudFile.FetchAttributes();
                }
                return _cloudFile;
            }
        }
        public DirectoryInfo LocalDirectory
        {
            get
            {
                if (_localDirectory != null) return _localDirectory;
                _localDirectory = File.Directory;
                return _localDirectory;
            }
        }

        #endregion

        #region Method Implementation

        public void Sync()
        {
            if (File == null)
            {
                TryDownload();
                return;
            }
            if (IsSynced())
            {
                _logger.WriteLine(string.Format("File {0} is up to date", File.Name));
                return;
            }
            if (IsLocalNewer())
            {
                TryUpload();
                return;
            }
            TryDownload();
        }

        public void TryDownload()
        {
            try
            {
                Download();
            }
            catch (Exception ex)
            {

                _logger.WriteLine("Failed to download {0}", CloudFile.Name);
            }
        }

        public void Download()
        {
            var sw = Stopwatch.StartNew();
            var fileName = Path.Combine(LocalDirectory.FullName, CloudFile.Name);
            _logger.WriteLine("Downloading {0} ", CloudFile.Name);
            CloudFile.DownloadToFile(fileName, FileMode.OpenOrCreate);
            sw.Stop();
            _logger.WriteLine("Finish {0} download in {1}", File.Name, sw.Elapsed);
        }

        public void Upload()
        {
            var sw = Stopwatch.StartNew();
            _logger.WriteLine("Uploading {0} {1} KB", File.Name, GetFileSizeInKb().ToString("N2"));
            CloudFile.DeleteIfExists();
            using (var stream = File.OpenRead())
            {
                CloudFile.UploadFromStream(stream);
            }
            SetMetadata();
            sw.Stop();
            _logger.WriteLine("Finish {0} upload in {1}", File.Name, sw.Elapsed);
        }


        public void TryUpload()
        {
            try
            {
                Upload();
            }
            catch (Exception ex)
            {
                _logger.WriteLine("Failed to upload {0} with exception:\n\n{1}", File.Name, ex.Message);
            }
        }

        private double GetFileSizeInKb()
        {
            var size = ((double)File.Length / 1024);
            return Math.Round(size, 2);
        }

        private void SetMetadata()
        {
            foreach (var i in GetLocalFileMetadata())
            {
                CloudFile.Metadata[i.Key] = i.Value;
            }
            CloudFile.SetMetadata();
        }

        private IDictionary<string, string> GetLocalFileMetadata()
        {
            var metaData = new Dictionary<string, string>()
                {
                    {Constants.LocalFileName, File.FullName},
                    {Constants.LocalRelativeFileName, GetRelativeName()},
                    {Constants.LocalLastModifiedOn, File.LastWriteTimeUtc.Ticks.ToString()},
                    {Constants.LocalMachineName, Environment.MachineName},
                    {Constants.LocalFileSize, File.Length.ToString()},
                    {Constants.LocalOS, Environment.OSVersion.ToString()}
                };
            return metaData;
        }

        private string GetRelativeName()
        {
            return string.Format(@"{0}\{1}", Directory.Name, CloudFile.Name);
        }

        public bool IsSynced()
        {
            var local = GetLocalFileMetadata();
            var cloud = CloudFile.Metadata;
            return IsSameFile(local, cloud) && Convert.ToInt64(local[Constants.LocalLastModifiedOn]) == Convert.ToInt64(cloud[Constants.LocalLastModifiedOn]);
        }

        public bool IsLocalNewer()
        {
            var local = GetLocalFileMetadata();
            var cloud = CloudFile.Metadata;
            return IsSameFile(local, cloud) && Convert.ToInt64(local[Constants.LocalLastModifiedOn]) > Convert.ToInt64(cloud[Constants.LocalLastModifiedOn]);
        }

        public bool IsCloudNewer()
        {
            var local = GetLocalFileMetadata();
            var cloud = CloudFile.Metadata;
            return IsSameFile(local, cloud) && Convert.ToInt64(local[Constants.LocalLastModifiedOn]) < Convert.ToInt64(cloud[Constants.LocalLastModifiedOn]);
        }

        private static bool IsSameFile(IDictionary<string, string> local, IDictionary<string, string> cloud)
        {
            return cloud != null &&
                   cloud.ContainsKey(Constants.LocalRelativeFileName) &&
                   cloud[Constants.LocalRelativeFileName] == local[Constants.LocalRelativeFileName] &&
                   cloud.ContainsKey(Constants.LocalLastModifiedOn);
        }

        #endregion


    }
}
