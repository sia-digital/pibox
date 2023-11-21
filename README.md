# PiBox

<figure><img src=".gitbook/assets/pibox-logo.png" alt=""><figcaption></figcaption></figure>

![Framework](https://img.shields.io/badge/framework-net7.0-blueviolet?style=flat-square)
[![MIT License](https://img.shields.io/badge/license-MIT-%230b0?style=flat-square)](https://github.com/sia-digital/pibox/blob/main/LICENSE.txt)
![Nuget](https://img.shields.io/nuget/v/PiBox.Hosting.Webhost?style=flat-square)
![Nuget](https://img.shields.io/nuget/dt/PiBox.Hosting.Webhost?style=flat-square)

PiBox is a `service hosting framework` that allows `.net devs` to `decorate their services with behaviours or functionality (think of plugins) while only using minimal configuration`.

> "PiBox" because we want to support unlimited plugins within the api out of the box!

## Features

* logs
* metrics
* traces
* minimal api ready
* auto discovered plugins
* with performance in mind
* less memory allocations
* industry best practices applied security
* standard dotnet core patterns & APIs
* many plugins!

## Prerequisites

Before you begin, ensure you have met the following requirements:

* You have installed the latest version of `.net7 sdk`


## Get started

To start using PiBox components/plugins, you have to:

1. Install/add the PluginWebHost via nuget package `PiBox.Hosting.WebHost`
2. Install/add the source generator plugin via nuget package `PiBox.Hosting.Generator`
3. Rewrite the Entrypoint `Program.cs` to have following code:

```c#
using PiBox.Hosting.WebHost;
PluginWebHostBuilder.RunDefault(PiBox.Generated.PiBoxPluginTypes.All);
```

4. Install/add your plugins via nuget packages prefixed with `PiBox`

## Configuring PiBox

To check how a plugin can be configured, search for the Plugin directory and read the README.md

## Building PiBox

To build PiBox, follow these steps:

```sh
dotnet build ./PiBox.sln
```

To format the solution run:

```sh
dotnet format ./PiBox.sln
```

## Testing PiBox

We are using these frameworks for unit testing

* AutoBogus
* Bogus
* FluentAssertions
* Microsoft.NET.Test.Sdk
* NaughtyStrings.Bogus
* NSubstitute
* NUnit
* RichardSzalay.MockHttp
* WireMock.Net

To run unit tests for PiBox, follow these steps:

```
dotnet test ./PiBox.sln
```

## Breaking changes in versions

## Contributing to PiBox

To contribute to PiBox, follow these steps:

1. Clone this repository.
2. Create a branch: `git checkout -b <branch_name>`.
3. Check our used tools and frameworks to ensure you are using the same tools which are already in place.
4. Check if your changes are in line with our style settings from .editorconfig and run `dotnet format`.
5. Make your changes and commit them: `git commit -m '<commit_message>'`
6. Push to the original branch: `git push origin PiBox/<branch_name>`
7. Create the pull request.

## Development decisions

* Follow the code style which is configured via `.editorconfig`

### Disabled Compiler Errors

| Code | Reason                                      | Link                                                                                               |
| ---- | ------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| 1701 | Assembly Referencing                        | [1701](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1701) |
| 1702 | Assembly Referencing                        | [1702](https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs1702)                                 |
| 1591 | Disable XML Comments                        | [1591](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1591) |
