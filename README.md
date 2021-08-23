# AInq.Bitrix24

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/andryushchenko/AInq.Bitrix24)](https://github.com/andryushchenko/AInq.Bitrix24/releases) [![netstandard 2.0](https://img.shields.io/badge/netstandard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) [![netstandard 2.1](https://img.shields.io/badge/netstandard-2.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) [![net 5.0](https://img.shields.io/badge/net-5.0-blue.svg)](https://dotnet.microsoft.com/learn/dotnet/what-is-dotnet) [![net 6.0](https://img.shields.io/badge/net-6.0-blue.svg)](https://dotnet.microsoft.com/learn/dotnet/what-is-dotnet) [![GitHub](https://img.shields.io/github/license/andryushchenko/AInq.Bitrix24)](LICENSE)

## What is it?

Client for [Bitrix24 Rest API](https://dev.1c-bitrix.ru/rest_help/)

## New in 2.0

Split interface, implementation and high-level method calls (in future) to separate packages

## Packages description
#### [![Nuget](https://img.shields.io/nuget/v/AInq.Bitrix24.Abstraction)](https://www.nuget.org/packages/AInq.Bitrix24.Abstraction/) AInq.Bitrix24.Abstraction

`IBitrix24Client` interface.

#### [![Nuget](https://img.shields.io/nuget/v/AInq.Bitrix24)](https://www.nuget.org/packages/AInq.Bitrix24/) AInq.Bitrix24

Base client implementation with GET/POST method calls, authentication and request-per-second limit (only in `Bitrix24ClientTimeoutBase`).
Provided client are abstract: methods to interact with some persistent storage (for access and refresh tokens) and to obtain OAuth authentication code should be implemented.

## Future development

High-level API methods will be added as extension methods for `IBitrix24Client` interface in separate packages.
There is currently no plan to implement all API methods, first priority is [CRM](https://dev.1c-bitrix.ru/rest_help/crm/index.php) methods implementation.

## Documentation

As for now documentation is provided in this document and by XML documentation inside packages.

## Contribution

If you find a bug, have a question or something else - you are friendly welcome to open an issue.

## License
Copyright © 2021 Anton Andryushchenko. AInq.Bitrix24 is licensed under [Apache License 2.0](LICENSE)
