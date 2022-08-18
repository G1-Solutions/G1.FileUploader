using System;
using System.Threading;
using System.Threading.Tasks;
using FileUploader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace G1.FileUploader.HostedService
{
	public class FileUploaderService : IHostedService, IDisposable
	{
		private readonly ILoggerFactory _loggerFactory;
		private readonly DirectoryFileUploader _uploader;

		public FileUploaderService( string monitoredLocation, Uri uri, bool useSsl, bool usePassiveMode, ILoggerFactory loggerFactory )
		{
			_loggerFactory = loggerFactory;
			
			FtpUploader ftpUploader = new FtpUploader( uri, usePassiveMode, useSsl );
			_uploader = new DirectoryFileUploader( monitoredLocation, ftpUploader, loggerFactory.CreateLogger<DirectoryFileUploader>() );

		}
		
		public Task StartAsync( CancellationToken cancellationToken )
		{
			_uploader.Start();

			return Task.CompletedTask;
		}

		public Task StopAsync( CancellationToken cancellationToken )
		{
			_uploader.Stop();
			
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_uploader?.Dispose();
		}
	}
}