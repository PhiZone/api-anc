# PhiZone API

Based on ASP.NET Core.

## Configurations

There are some secret files that are ignored using `.gitignore`. You should create them yourself.

1. ### `/appsettings.Development.json`
   Configure your PostgreSQL server before filling in `DefaultConnection`.
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
     }
   }

2. ### `/.env`
   Variables starting with `TAPOSS` are the ones provided by TapTap. Contact anyone who has access to TapTap Developer
   Services before entering them.
   ```env
   TAPOSS__CLIENT_ID=yourid
   TAPOSS__CLIENT_TOKEN=yourtoken
   TAPOSS__URL=yoururl