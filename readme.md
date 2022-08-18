# G1.FileUploader

Monitors directory for new (renamed) file and upload to specified FTP location.

## Usage 

Example:

`g1.fileuploader.cli --directory \test --destination ftp://username:password@ftp.server.host/folder`

Other supported parameters:
- useSsl: whether to use tls/ssl, defaults to false
- usePassiveFtp: whether to use passive FTP mode, defaults to true

## Requirements
- .NET Core Runtime ( v3.1 or later )

## Not Supported
- Implicit TLS
- SFTP (FTP over SSH) 