using System;

namespace FileUploader
{
	public class FileUploadException : Exception
	{
		public FileUploadException( string message ) : base(message)
		{
		}
	}
}