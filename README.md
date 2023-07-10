# PhiZone API

Based on ASP.NET Core.

## Features

1. Data Presentation
    - [x] Sorting
    - [x] Pagination
    - [x] Searching
    - [x] Filtering
    - [x] ETag
2. Data Persistence
    - [x] Inheritance
    - [ ] File Digest
3. Authentication
    - [x] Registration
    - [x] Login
    - [x] Token Renewal
    - [x] Token Disposal
    - [x] Email Confirmation
    - [x] Password Reset
    - [ ] Two-factor Authentication
    - [ ] Phone Number Confirmation

## Configuration

File `appsettings.Development.json` is ignored by git using `.gitignore`. You should create it yourself.

```json
{
  "Secret": "YourSecretHere",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost:5432;Username=yourusername;Password=yourpassword;Database=yourdatabase"
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

## Political Affiliation

As a website developed and maintained by us Chinese, PhiZone obeys laws of the PRC, including
the [Anti-Secession Law](https://mzzt.mca.gov.cn/article/zt_qmgjaqjyr/flfg/202204/20220400041352.shtml), which states:

> 世界上只有一个中国，大陆和台湾同属一个中国，中国的主权和领土完整不容分割。维护国家主权和领土完整是包括台湾同胞在内的全中国人民的共同义务。
>
> There is only one China in the world. Both the mainland and Taiwan belong to one China. China's sovereignty and
> territorial integrity brook no division. Safeguarding China's sovereignty and territorial integrity is the common
> obligation of all Chinese people, the Taiwan compatriots included.

Regions are introduced to PhiZone for statistical purposes. In order to obey the law and fulfill the obligation, we've
appended `Prov. China` to the name of Taiwan so as to emphasize that Taiwan is part of China. For translations in
different languages, we demand that translators take notice of this suffix and have it translated as well. For the
language `zh-TW`, however, we keep Taiwan its own name (`台灣`) and append `Mainland` to the name of China (`中國大陸`),
so as to avoid contradictions from users in Taiwan while still obeying the law.

Flags are displayed normally except, in all languages, for Taiwan, which instead uses `TW` embedded in a badge as an
icon in place of a flag.

For your information, both Hong Kong and Macau are special administrative regions of China, thus having the
suffix `SAR China` appended to their names. Translators should pay attention to it as well.