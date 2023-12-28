# TestCentric Metadata

The `TestCentric.Metadata` library is used for accessing assembly metadata
without loading the target assembly and without use of reflection. Most of
the code is taken from `Mono.Cecil` on which it is based. The metadata may
be examined but modification is not supported and there is no provision for
accessing the generated code.

This library was created as part of the TestCentric engine for loading and
running tests, but is suitable for use by any application, which needs to
examine assembly metadata. It has no dependencies other than the appropriate
Microsoft SDKs for each build.

Currently, the library is built for .NET Framework 2.0 and 4.0 and .NET
Standard 1.6 and 2.0. It is available as nuget package TestCentric.Metadata
on nuget.org. The package adds a reference to the library to any project in
which it is installed.

## History

When the TestCentric GUI project was created in 2018, it used `Mono.Cecil` 
to examine assemblies, just as NUnit did. Use of `Mono.Cecil`, however, came
to present a number of problems for both projects. One was that the library
was larger than we needed, containing a great deal of unused functionality.
Another was that there seemed to be a risk in using it to examine assemblies,
which themselves depended on `Mono.Cecil`.

Those two problems were relatively small. More important was the fact that
`Mono.Cecil` was moving forward, dropping support for older platforms. This
presented a problem for a test framework committed to continued support for
tests using those platforms. While alternative solutions were possible, we
eventually settled on building a subset of `Mono.Cecil` and modifying the 
code as needed so that a single build could be used by all our agents,
including those which ran tests on legacy platforms.

Initially, this subset was incorporated in the TestCentric engine itself, which
was separated from the GUI as a project in 2020. In the same year, we made
`TestCentric.Metadata` a separate project and package and it began to be used
by NUnit as well as TestCentric.

## Versioning

`TestCentric.Metadata` follows semantic versioning. Versions through 1.6.2 were
distributed as part of the TestCentric GUI project. Beginning with version 1.7,
it became an independent package, making it easier for the library to be used 
in multiple projects.

## Licensing

**TestCentric.Metadata** is Open Source software, released under the MIT / X11 
license. See LICENSE in the root of the distribution for a copy of the license.
