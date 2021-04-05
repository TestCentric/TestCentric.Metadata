# TestCentric Metadata

The **testcentric.engine.metadata** library is used for accessing assembly metadata
without loading the target assembly and without use of reflection. Most of the code
is taken from **Mono.Cecil** on which it is based. The metadata may be examined but
modification is not modified and there is no provision for accessing the code.

The library was created as part of the TestCentric engine for loading and running tests,
but is suitable for use by any application, which needs to examine assembly metadata.
It has no dependencies other than the appropriate Microsoft SDKs for each build.

Currently, the library is built for .NET Framework 2.0 and 4.0 and .NET Standard 1.6 and 2.0.
It is available as nuget package TestCentric.Metadata on nuget.org. The package adds
a reference to the library to any project in which it is installed.

## Versioning

**TestCentric.Metadata** follows semantic versioning. Versions through 1.6.2 were distributed
as part of the **TestCentric GUI** project. Beginning with version 1.7, it became an
independent project, making it easier for the library to be used in multiple projects.

## Licensing

**TestCentric.Metadata** is Open Source software, released under the MIT / X11 license.
See LICENSE in the root of the distribution for a copy of the license.
