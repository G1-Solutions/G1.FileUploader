using System.IO;
using G1.FileUploader.HostedService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace G1.FileUploader.Cli
{
	internal static class Program
	{
		public static void Main( string[] args )
		{
			using IHost host = Host.CreateDefaultBuilder( args )
									.UseConsoleLifetime()
									.ConfigureHostConfiguration(
											configHost =>
											{
												configHost.SetBasePath( Directory.GetCurrentDirectory() );
												configHost.AddJsonFile( "appsettings.json", optional: true );
												// configHost.AddEnvironmentVariables( prefix: "PREFIX_" );
												configHost.AddCommandLine( args );
											}
										)
									.UseSerilog(
											( context, configuration ) => configuration.ReadFrom.Configuration( context.Configuration )
										)
									.ConfigureServices(
											( context, collection ) =>
											{
												collection.AddHostedService(
														sp => HostedServiceHelpers.CreateFileUploaderService( sp, context ) );
												
											}
										)
									.Build();
			
			host.Run();
		}
	}
}