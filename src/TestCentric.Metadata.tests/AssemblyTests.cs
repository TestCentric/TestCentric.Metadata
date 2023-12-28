// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Framework;
using System;
using System.Reflection;

namespace TestCentric.Metadata
{
    public class AssemblyTests
    {
        static string THIS_ASSEMBLY = Assembly.GetExecutingAssembly().Location;

        AssemblyDefinition _assemblyDef;


        [OneTimeSetUp]
        public void CreateAssemblyDefinition()
        {
            _assemblyDef = AssemblyDefinition.ReadAssembly(THIS_ASSEMBLY);
        }

        [Test]
        public void CheckAttribute()
        {
            var titleAttr = GetCustomAttribute("System.Reflection.AssemblyTitleAttribute");

            Assert.NotNull(titleAttr, "Title Attribute not found");

            var args = titleAttr.ConstructorArguments;
            Assert.That(args.Count, Is.EqualTo(1));
            Assert.That(args[0].Value, Is.EqualTo("TestCentric.Metadata Tests"));
        }

        [TestCase("nunit.framework")]
        [TestCase("TestCentric.Metadata")]
        public void HasAssemblyReference(string name)
        {
            Assert.That(HasReferenceTo(name));
        }

        [Test]
        public void GetFrameworkName()
        {
#if NET35
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(null));
#elif NET40
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETFramework,Version=v4.0"));
#elif NET45
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETFramework,Version=v4.5"));
#elif NETCOREAPP2_1
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETCoreApp,Version=v2.1"));
#elif NETCOREAPP3_1
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETCoreApp,Version=v3.1"));
#elif NET5_0
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETCoreApp,Version=v5.0"));
#elif NET6_0
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETCoreApp,Version=v6.0"));
#elif NET7_0
            Assert.That(_assemblyDef.GetFrameworkName(), Is.EqualTo(".NETCoreApp,Version=v7.0"));
#else
            Assert.Fail($"Untested target runtime: {_assemblyDef.GetFrameworkName()}");
#endif
        }

        [Test]
        public void GetRuntimeVersion()
        {
#if NET35
            Assert.That(_assemblyDef.GetRuntimeVersion(), Is.EqualTo(new Version(2, 0, 50727)));
#else
            Assert.That(_assemblyDef.GetRuntimeVersion(), Is.EqualTo(new Version(4, 0, 30319)));
#endif
        }

        private CustomAttribute GetCustomAttribute(string fullName)
        {
            foreach (var attr in _assemblyDef.CustomAttributes)
                if (attr.AttributeType.FullName == fullName)
                    return attr;

            return null;
        }

        private bool HasReferenceTo(string name)
        {
            foreach (var reference in _assemblyDef.MainModule.AssemblyReferences)
                if (reference.Name == name)
                    return true;

            return false;
        }
    }
}