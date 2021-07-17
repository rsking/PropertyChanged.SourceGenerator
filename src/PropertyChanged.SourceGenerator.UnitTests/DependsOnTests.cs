﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PropertyChanged.SourceGenerator.UnitTests.Framework;

namespace PropertyChanged.SourceGenerator.UnitTests
{
    [TestFixture]
    public class DependsOnTests : TestsBase
    {
        [Test]
        public void NotifiesDependsOnProperty()
        {
            string input = @"
public partial class SomeViewModel
{
    [Notify]
    private string _foo;

    [DependsOn(""Foo"")]
    public string Bar { get; set; }
}";
            this.AssertNotifies(input, "SomeViewModel", "Foo", "Bar");
        }

        [Test]
        public void NotifiesGeneratedDependsOnProperty()
        {
            string input = @"
public partial class SomeViewModel
{
    [Notify]
    private string _foo;

    [Notify]
    [DependsOn(""Foo"")]
    private string _bar;
}";
            this.AssertNotifies(input, "SomeViewModel", "Foo", "Bar");
        }

        [Test]
        public void RaisesIfDependsOnPropertyDoesNotExist()
        {
            string input = @"
public partial class SomeViewModel
{
    [Notify(""Foo2"")]
    private string _foo;

    [DependsOn(""Foo"")]
    public string Bar { get; set; }
}";

            this.AssertThat(input, It.HasDiagnostics(
                // (7,6): Warning INPC010: Unable to find a property called 'Foo' on this type which was generated by PropertyChanged.SourceGenerator. Skipping
                // DependsOn("Foo")
                Diagnostic("INPC010", @"DependsOn(""Foo"")").WithLocation(7, 6)
            ));
        }

        [Test]
        public void RaisesIfDependsOnPropertyIsInBaseClass()
        {
            string input = @"
public partial class Base
{
    [Notify]
    private string _foo;
}
public partial class Derived : Base
{
    [DependsOn(""Foo"")]
    public string Bar { get; set; }
}";

            this.AssertThat(input, It.HasDiagnostics(
                // (9,6): Warning INPC010: Unable to find a property called 'Foo' on this type which was generated by PropertyChanged.SourceGenerator. Skipping
                // DependsOn("Foo")
                Diagnostic("INPC010", @"DependsOn(""Foo"")").WithLocation(9, 6)
            ));
        }

        [Test]
        public void RaisesIfAppliedToFieldWithoutNotify()
        {
            string input = @"
public partial class SomeViewModel
{
    [Notify]
    private string _foo;

    [DependsOn(""Foo"")]
    private string _bar;
}";

            this.AssertThat(input, It.HasDiagnostics(
                // (7,6): Warning INPC011: [DependsOn] must only be applied to fields which also have [Notify]
                // DependsOn("Foo")
                Diagnostic("INPC011", @"DependsOn(""Foo"")").WithLocation(7, 6)
            ));
        }

        [Test]
        public void HandlesEmptyAndNullNames()
        {
            string input = @"
public partial class SomeViewModel
{
    [DependsOn("""", null)]
    private string _foo;
}";
            this.AssertThat(input, It.HasDiagnostics(
                // (4,6): Warning INPC010: Unable to find a property called '' on this type which was generated by PropertyChanged.SourceGenerator. Skipping
                // DependsOn("", null)
                Diagnostic("INPC010", @"DependsOn("""", null)").WithLocation(4, 6),

                // (4,6): Warning INPC010: Unable to find a property called '' on this type which was generated by PropertyChanged.SourceGenerator. Skipping
                // DependsOn("", null)
                Diagnostic("INPC010", @"DependsOn("""", null)").WithLocation(4, 6)
            ));
        }
    }
}
