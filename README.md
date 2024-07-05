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
    - [x] File Digest
3. Authentication
    - [x] Registration
    - [x] Login
    - [x] Token Renewal
    - [x] Token Disposal
    - [x] Email Confirmation
    - [x] Password Reset
    - [x] Login through Third-party Platforms
    - [ ] ~~Two-factor Authentication~~ (Not planned)
    - [ ] ~~Phone Number Confirmation~~ (Not planned)
4. TapTap
    - [x] Login & Account Binding

## Configuration

A template for both `./appsettings.Development.json` and `./appsettings.Production.json` is as follows.

```json
{
  "Secret": "YourSecretHere",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost:5432;Username=yourusername;Password=yourpassword;Database=yourdatabase"
  },
  "TapTapSettings": {
    "ClientId": "yourid",
    "ClientToken": "yourtoken",
    "TapApiUrl": "https://openapi.taptap.com",
    "FileStorageUrl": "https://oss.example.com"
  },
  "RabbitMQSettings": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "yourusername",
    "Password": "yourpassword"
  },
  "FeishuSettings": {
    "ApiUrl": "https://open.feishu.cn",
    "AppId": "yourappid",
    "AppSecret": "yourappsecret",
    "Cards": [
      "songcard",
      "chartcard",
      "petanswercard"
    ],
    "Chats": [
      "contentreviewal",
      "qualificationreviewal",
      "recruitmentreviewal"
    ]
  },
  "MessengerSettings": {
    "ApiUrl": "https://msgapi.example.com",
    "ClientId": "yourid",
    "ClientSecret": "yoursecret"
  },
  "AuthProviders": [
    {
      "Name": "GitHub",
      "ApplicationId": "yourguid",
      "ClientId": "yourid",
      "ClientSecret": "yoursecret",
      "AvatarUrl": "https://res.example.com/github-mark.png",
      "IllustrationUrl": "https://res.example.com/github.png"
    }
  ],
  "RedisConnection": "localhost:6379,password:yourpassword", 
  "Proxy": "http://this-is-an-optional-field:1080"
}

```

A template for `./Resources/resources.json` is as follows.

```json
{
  "ProhibitedWords": [
    "word1",
    "word2",
    "word3"
  ]
}
```

## Data Processing

For details on processing an image on the OSS,
see [Qiniu Developer Docs](https://developer.qiniu.com/dora/3683/img-directions-for-use).