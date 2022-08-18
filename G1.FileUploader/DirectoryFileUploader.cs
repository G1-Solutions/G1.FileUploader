using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace FileUploader
{
	public class DirectoryFileUploader : IDisposable
	{
		private const int retryWaitPeriod = 5000;

		private readonly string _path;
		private FsRenamedFileWatcherDataMonitor _fileMonitor;
		private FtpUploader _uploader;
		private readonly ILogger<DirectoryFileUploader> _logger;

		public DirectoryFileUploader(string path, FtpUploader uploader, ILogger<DirectoryFileUploader> logger )
		{
			_path = path;
			_uploader = uploader;
			_logger = logger;
		}

		public void Start()
		{
			_fileMonitor = new FsRenamedFileWatcherDataMonitor( _path );
			
			_fileMonitor.FilesReceived += filenames =>
			{
				UploadUntilSuccessful(filenames[0], _uploader  );
			};

			_fileMonitor.Start();
		}

		public void Stop()
		{
			_fileMonitor?.Stop();
			_fileMonitor?.Dispose();
			_fileMonitor = null;
		}

		private void UploadUntilSuccessful( string filePath, FtpUploader uploader )
		{
			_logger.LogInformation( "Trying to upload '{FilePath}'... ", filePath );
					
			bool uploaded = Upload( filePath, uploader );

			while( !uploaded )
			{
				_logger.LogInformation( "\r\nRetrying in {RetryWaitPeriod} seconds... ", (retryWaitPeriod/1000).ToString() );
				
				Thread.Sleep( retryWaitPeriod );
				uploaded = Upload( filePath, uploader );
			}
			
			_logger.LogInformation( "Upload '{FilePath}' successful", filePath );
		}

		private bool Upload( string file, FtpUploader ftpUploader )
		{
			try
			{
				ftpUploader.Upload( file );

				return true;
			}
			catch( FileUploadException e )
			{
				_logger.LogError(e, "Unable to upload '{File}'", file);
			}
			catch( FileNotFoundException e )
			{
				_logger.LogError(e, "File '{File}'not found", file);
			}
			catch( Exception e )
			{
				_logger.LogError(e, "Unable to upload '{File}'", file);
			}

			return false;
		}

		public void Dispose()
		{
			_fileMonitor?.Dispose();
		}
	}
}