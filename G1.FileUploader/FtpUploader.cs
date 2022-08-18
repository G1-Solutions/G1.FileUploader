using System;
using System.IO;
using System.Net;
using System.Text;

namespace FileUploader
{
	public class FtpUploader
	{
		private readonly bool _usePassiveMode;
		private readonly bool _useSsl;
		private readonly string _hostname, _folderPath;
		private readonly int _port = 21;
		private NetworkCredential _networkCredential;

		public FtpUploader( Uri uri, bool usePassiveMode, bool useSsl )
		{
			ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
			
			string[] credentials = uri.UserInfo.Split( new[] { ':' }, StringSplitOptions.RemoveEmptyEntries );

			if( credentials.Length < 2 )
				throw new ArgumentException( "FTP username and password not specified" );

			_hostname = uri.DnsSafeHost;
			_port = uri.Port;

			string username = credentials[0];
			string password = credentials[1];
			_networkCredential = new NetworkCredential( username, password );
			_folderPath = uri.AbsolutePath.Trim( '/' );
			_usePassiveMode = usePassiveMode;
			_useSsl = useSsl;
		}

		public void Upload( string filePath )
		{
			if( !File.Exists( filePath ) )
				throw new FileNotFoundException( $"'{filePath}' does not exist" );

			FtpWebRequest ftpWebRequest = 
				(FtpWebRequest)WebRequest.Create( $"ftp://{_hostname}:{_port.ToString()}/{_folderPath}/{Path.GetFileName( filePath )}" );
			
			ftpWebRequest.KeepAlive = false;
			ftpWebRequest.Timeout = 45000;
			ftpWebRequest.Proxy = null;
			ftpWebRequest.Credentials = _networkCredential;
			ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
			ftpWebRequest.UsePassive = _usePassiveMode;
			ftpWebRequest.EnableSsl = _useSsl;
			ftpWebRequest.UseBinary = true;
			
			byte[] buffer;

			using( StreamReader sourceStream = new StreamReader( filePath ) )
			{
				buffer = Encoding.ASCII.GetBytes( sourceStream.ReadToEnd() );
			}

			ftpWebRequest.ContentLength = buffer.Length;

			using( Stream reqStream = ftpWebRequest.GetRequestStream() )
			{
				reqStream.Write( buffer, 0, buffer.Length );
				reqStream.Flush();
			}

			using( FtpWebResponse response = (FtpWebResponse)ftpWebRequest.GetResponse() )
			{
				if( !( response.StatusCode == FtpStatusCode.ClosingData || response.StatusCode == FtpStatusCode.FileActionOK ||
						response.StatusCode == FtpStatusCode.CommandOK ) )
					throw new FileUploadException( $"Could not upload '{filePath}': {response.StatusCode.ToString()} ({response.StatusDescription}");
			}
		}
	}
}