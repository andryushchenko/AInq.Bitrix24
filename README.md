# AInq.Bitrix24

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/andryushchenko/AInq.Bitrix24)](https://github.com/andryushchenko/AInq.Bitrix24/releases) [![GitHub](https://img.shields.io/github/license/andryushchenko/AInq.Bitrix24)](LICENSE)

![AInq](https://raw.githubusercontent.com/andryushchenko/AInq.Bitrix24/main/AInq.png)

## What is it?

Client for [Bitrix24 Rest API](https://dev.1c-bitrix.ru/rest_help/)

## Project status

Unfortunately, at the moment the development of the project is suspended. In the near future, only bug fixes and minor technical updates will be released. There is a plan to create a new strongly typed API client (primarily CRM and tasks), but there are no specific deadlines due to the workload on the main project.

## Статус проекта

К сожалению, в настоящий момент развитие проекта приостановлено. В ближайшее время будут выходить только исправления ошибок и небольшие технические обновления. Существует план создания нового строго типизированного клиента API (в первую очередь CRM и задач), но никаких конкретных сроков нет по причине загруженности на основном проекте.

## New in 2.0

Split interface, implementation and high-level method calls (in future) to separate packages

## Packages description
#### [![Nuget](https://img.shields.io/nuget/v/AInq.Bitrix24.Abstraction)](https://www.nuget.org/packages/AInq.Bitrix24.Abstraction/) AInq.Bitrix24.Abstraction

`IBitrix24Client` interface.

#### [![Nuget](https://img.shields.io/nuget/v/AInq.Bitrix24)](https://www.nuget.org/packages/AInq.Bitrix24/) AInq.Bitrix24

Base client implementation with GET/POST method calls, authentication and request-per-second limit (only in `Bitrix24ClientTimeoutBase`).
Provided client are abstract: methods to interact with some persistent storage (for access and refresh tokens) and to obtain OAuth authentication code should be implemented.

#### [![Nuget](https://img.shields.io/nuget/v/AInq.Bitrix24.Background)](https://www.nuget.org/packages/AInq.Bitrix24.Background/) AInq.Bitrix24.Background

Integration with [AInq.Background](https://github.com/andryushchenko/AInq.Background) conveyor.
DO NOT use `IBitrix24Client` implementations with internal timeout control with this package.

#### [![Nuget](https://img.shields.io/nuget/v/AInq.Bitrix24.Api)](https://www.nuget.org/packages/AInq.Bitrix24.Background/) AInq.Bitrix24.Api

High-level API methods will be added here.

**Currently available:**
- Get, List, Update, Create, Delete methods for basic CRM entities (lead, deal, contact, company)

There is currently no plan to implement all API methods, first priority is [CRM](https://dev.1c-bitrix.ru/rest_help/crm/index.php) methods implementation.

***Дальнейшая разработка***

Данный пакет будет дополняться методами общего назначения по мере того, как их реализации будут стабилизироваться и тестироваться в реальном приложении, находящемся сейчас в активной разработке. Разработка ведется с учетом реально встречающегося странного поведения API Битрикс24 (например отдача значения `false` в необязательных пользовательских полях числовых типов). Функциональность иногда приносится в жертву надежности, API может отличаться от официального. Таков путь.

## Documentation

As for now documentation is provided in this document and by XML documentation inside packages.

## Contribution

If you find a bug, have a question or something else - you are friendly welcome to open an issue.

## License
Copyright © 2021 Anton Andryushchenko. AInq.Bitrix24 is licensed under [Apache License 2.0](LICENSE)
