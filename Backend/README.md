# Welcome to EskaCMS.Core

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/ba7cbafe380b4c2796b731562c5166e0)](https://www.codacy.com/app/tidyui/EskaCMS.core?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=EskaCMSCMS/EskaCMS.core&amp;utm_campaign=Badge_Grade)
[![CodeFactor](https://www.codefactor.io/repository/github/EskaCMScms/EskaCMS.core/badge)](https://www.codefactor.io/repository/github/EskaCMScms/EskaCMS.core)
[![Total alerts](https://img.shields.io/lgtm/alerts/g/EskaCMSCMS/EskaCMS.core.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/EskaCMSCMS/EskaCMS.core/alerts/)
[![Language grade: JavaScript](https://img.shields.io/lgtm/grade/javascript/g/EskaCMSCMS/EskaCMS.core.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/EskaCMSCMS/EskaCMS.core/context:javascript)
[![Backers](https://opencollective.com/EskaCMScms/tiers/backer/badge.svg?label=backer&color=brightgreen)](Backers)
[![Gitter chat](https://badges.gitter.im/EskaCMSCMS/EskaCMS.png)](https://gitter.im/EskaCMSCMS/EskaCMS)

| Build server | Platform     | Build status |
|--------------|--------------|--------------|
| AppVeyor     | Windows      | [![Build status](https://ci.appveyor.com/api/projects/status/brw0cak0b5x4w17m?svg=true)](https://ci.appveyor.com/project/tidyui/EskaCMS-core) |
| Travis       | Linux / OS X | [![Build status](https://travis-ci.org/EskaCMSCMS/EskaCMS.core.svg?branch=master)](https://travis-ci.org/EskaCMSCMS/EskaCMS.core) |
| CoverAlls    |              | [![Coverage Status](https://coveralls.io/repos/github/EskaCMSCMS/EskaCMS.core/badge.svg?branch=master&service=github)](https://coveralls.io/github/EskaCMSCMS/EskaCMS.core?branch=master) |
| NuGet        |              | [![NuGet](https://img.shields.io/nuget/v/EskaCMS.svg)](https://www.nuget.org/packages/EskaCMS) |

## About

This is a **complete rewrite** of EskaCMS CMS for `NetStandard`. The goal of this rewrite 
is to create a version capable of targeting multiple platforms & frameworks with minimal
depenencies, but still provide a flexible & high performance CMS library.

EskaCMS is currently built for `NetStandard 2.0` and uses the following awesome packages:

* AutoMapper `7.0.1`
* Markdig `0.15.0`
* Microsoft.AspNetCore.App `2.1.5`
* Microsoft.EntityFrameworkCore `2.1.4`
* Newtonsoft.Json `11.0.2`

## Getting started

#### Prerequisites

* [.NET Core SDK 2.1.5](https://www.microsoft.com/net/core/) or later
* An IDE or Editor of your choice

#### Get the latest source code and get going:

    > git clone https://github.com/EskaCMSCMS/EskaCMS.core.git
    > cd EskaCMS.core
    > dotnet restore
    > dotnet build
    > cd examples/MvcWeb
    > dotnet run
    
#### Build and update javascript/css assets

    > cd EskaCMS.core/core/EskaCMS.Manager
    > npm install
    > bower install
    > cd ../../examples/MvcWeb
    > npm install
    > bower install

#### Visual Studio users

For people running Visual Studio 2017 almost all of the above steps will be handled by the IDE. Just get the source code, open the `.sln` file and you're good to go.

## Backers

Support EskaCMS CMS with a monthly donation and help us focus on giving you even more features and better support. [EskaCMS CMS @ Open Collective](https://opencollective.com/EskaCMScms)

<img src="https://opencollective.com/EskaCMScms/tiers/backer.svg?avatarHeight=36&width=600" />
