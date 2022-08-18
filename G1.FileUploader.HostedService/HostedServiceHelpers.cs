using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace G1.FileUploader.HostedService
{
	public static class HostedServiceHelpers
	{
		public static FileUploaderService CreateFileUploaderService( IServiceProvider sp, HostBuilderContext context )
		{
			ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();

			ILogger logger = loggerFactory?.CreateLogger( "HostedServiceHelpers" );

			if( !TryGetMonitorLocation( context.Configuration["directory"], out string monitoredLocation, logger ) )
			{
				throw new ArgumentException( "Directory (--directory) not specified" );
			}

			if( !TryGetDestinationUri( context.Configuration["destination"], out Uri uri, logger ) )
				throw new ArgumentException( "Destination (--destination) not specified" );

			bool.TryParse( context.Configuration["useSsl"], out bool useSsl );

			if( !bool.TryParse( context.Configuration["usePassiveFtp"], out bool usePassiveMode ) )
				usePassiveMode = true;

			return new FileUploaderService(
				monitoredLocation, uri, useSsl, usePassiveMode, loggerFactory );
		}

		private static bool TryGetMonitorLocation( string location, out string result, ILogger logger )
		{
			result = null;
			
			if( string.IsNullOrWhiteSpace( location ) || !Directory.Exists( location ) )
			{
				logger?.LogError("Please specify valid file location (--directory) to monitor");
				return false;
			}

			location = Environment.ExpandEnvironmentVariables( location );
			
			if( !Directory.Exists( location ) )
			{
				logger?.LogError($"'{location}' is not a valid location to monitor");
				return false;
			}

			result = location;
			
			return true;
		}

		private static bool TryGetDestinationUri( string destinationString, out Uri uri, ILogger logger )
		{
			uri = null;
			
			if( string.IsNullOrWhiteSpace( destinationString ) )
			{
				logger?.LogError( "Please specify destination (--destination) URL, e.g. ftp://username:password@ftp.hostname/folder" );
				return false;
			}

			try
			{
				uri = new Uri( destinationString );
			}
			catch( UriFormatException )
			{
				logger?.LogError( "Please specify valid destination URL" );
				return false;
			}

			if( uri.Scheme == Uri.UriSchemeFtp ) return true;
			
			logger?.LogError( "Destination URL must use FTP, e.g. ftp://username:password@ftp.hostname/folder" );
			return false;

		}
	}
}