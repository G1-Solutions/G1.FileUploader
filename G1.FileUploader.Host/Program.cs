using System.IO;
using G1.FileUploader.HostedService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace G1.FileUploader.Host
{
	internal static class Program
	{
		public static void Main( string[] args )
		{
			using IHost host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder( args )
								.UseConsoleLifetime()
								.ConfigureHostConfiguration(
										configHost =>
										{
											configHost.SetBasePath( Directory.GetCurrentDirectory() );
											configHost.AddCommandLine( args );
											
											configHost.AddJsonFile( "appsettings.json", optional: true );
											// configHost.AddEnvironmentVariables( prefix: "PREFIX_" );
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

			string runAsServiceSettingString = host.Services.GetService<IConfiguration>()["asService"];

			if( !string.IsNullOrWhiteSpace( runAsServiceSettingString ) && bool.TryParse( runAsServiceSettingString, out bool runAsService ) &&
				runAsService )
				host.Start();
			else
				host.Run();
		}
	}
}