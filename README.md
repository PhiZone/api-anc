# PhiZone API

Based on ASP.NET Core.

## Configuration

File `appsettings.Development.json` is ignored by git using `.gitignore`. You should create it yourself.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost:5432;Username=yourusername;Password=yourpassword;Database=yourdatabase"
  },
  "JWT": {
    "ValidAudience": "http://localhost:4200",
    "ValidIssuer": "http://localhost:5000",
    "Secret": "YourRandomSecret"
  },
  "MailSettings": {
    "Server": "smtp.exmail.qq.com",
    "Port": 465,
    "SenderName": "PhiZone",
    "SenderAddress": "example@example.com",
    "UserName": "example@example.com",
    "Password": "yourpassword"
  },
  "FileStorageSettings": {
    "ClientId": "yourid",
    "ClientToken": "yourtoken",
    "ServerUrl": "https://example.com"
  }
}
```

1. Database: Configure your PostgreSQL server before filling in `DefaultConnection`.
2. File Storage: We use the file storage service provided by TapTap, courtesy of Phigrim. Ask anyone who has access to
   the service for credentials.