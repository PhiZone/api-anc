# PhiZone API

Based on ASP.NET Core.

## Features

1. Data Presentation
    - [x] Sorting
    - [x] Pagination
    - [ ] Searching
    - [ ] Filtering
    - [ ] ETag
2. Data Persistence
    - [ ] Inheritance
    - [ ] File Digest
3. Authentication
    - [x] Registration
    - [x] Login
    - [x] Token Renewal
    - [ ] Token Disposal
    - [ ] Email Confirmation
    - [ ] Password Reset
    - [ ] Two-factor Authentication
    - [ ] Phone Number Confirmation
4. User
    - [x] Model Creation
    - [ ] Retrieval
    - [x] Update
    - [ ] Deletion
5. User Relation
    - [x] Model Creation
    - [ ] Retrieval
    - [ ] Deletion
    - [ ] Count
6. Region
    - [x] Model Creation
    - [ ] Retrieval
    - [ ] Update
    - [ ] Deletion

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
  },
  "RabbitMQSettings": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "yourusername",
    "Password": "yourpassword"
   },
  "RedisConnection": "localhost:6379,password:yourpassword"
}
```

1. Database: Configure your own PostgreSQL server before filling in `DefaultConnection`.
2. File Storage: We use the file storage service provided by TapTap, courtesy of Phigrim. Ask anyone who has access to
   the service for credentials before filling in `FileStorageSettings`.
3. RabbitMQ: Setup your own RabbitMQ server before filling in `RabbitMQSettings`.

## Data Processing

For details on processing an image in the File Storage,
see [Qiniu Developer Docs](https://developer.qiniu.com/dora/3683/img-directions-for-use).