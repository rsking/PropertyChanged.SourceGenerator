﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyChanged.SourceGenerator;

public static class StringConstants
{
    public static string FileHeader { get; } = @$"// <auto-generated>
//     Auto-generated by PropertyChanged.SourceGenerator {typeof(Generator).Assembly.GetName().Version}
// </auto-generated>";

    public static string Attributes { get; } = FileHeader + @"

namespace PropertyChanged.SourceGenerator
{
    /// <summary>
    /// Specifies the accessibility of a generated property getter
    /// </summary>
    internal enum Getter
    {
        Public = 6,
        ProtectedInternal = 5,
        Internal = 4,
        Protected = 3,
        PrivateProtected = 2,
        Private = 1,
    }

    /// <summary>
    /// Specifies the accessibility of a generated property getter
    /// </summary>
    internal enum Setter
    {
        Public = 6,
        ProtectedInternal = 5,
        Internal = 4,
        Protected = 3,
        PrivateProtected = 2,
        Private = 1,
    }

    /// <summary>
    /// Instruct PropertyChanged.SourceGenerator to generate a property which implements INPC using this backing field
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = false)]
    [global::System.Diagnostics.Conditional(""DEBUG"")]
    internal class NotifyAttribute : global::System.Attribute
    {
        /// <summary>
        /// Generate a property whose name is derived from the name of this field, with a public getter and setter
        /// </summary>
        public NotifyAttribute() { }

        /// <summary>
        /// Generate a property with the given name, and optionally the given getter and setter accessibilities
        /// </summary>
        /// <param name=""name"">Name of the generated property</param>
        /// <param name=""get"">Accessibility of the generated getter</param>
        /// <param name=""set"">Accessibility of the generated setter</param>
        public NotifyAttribute(string name, Getter get = Getter.Public, Setter set = Setter.Public) { }

        /// <summary>
        /// Generate a property whose name is derived from the name of this field, with the given getter and optionally setter accessibilities
        /// </summary>
        /// <param name=""get"">Accessibility of the generated getter</param>
        /// <param name=""set"">Accessibility of the generated setter</param>
        public NotifyAttribute(Getter get, Setter set = Setter.Public) { }

        /// <summary>
        /// Generate a property whose name is derived from the name of this field, with a public getter and the given setter accessibility
        /// </summary>
        /// <param name=""set"">Accessibility of the generated setter</param>
        public NotifyAttribute(Setter set) { }
    }

    /// <summary>
    /// Instruct PropertyChanged.SourceGenerator to also raise INPC notifications for the named properties whenever the property this is applied to changes
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = true)]
    [global::System.Diagnostics.Conditional(""DEBUG"")]
    internal class AlsoNotifyAttribute : global::System.Attribute
    {
        /// <summary>
        /// Raise INPC notifications for the given properties when the property generated for this backing field changes
        /// </summary>
        /// <param name=""otherProperties"">Other properties to raise INPC notifications for</param>
        public AlsoNotifyAttribute(params string[] otherProperties) { }
    }

    /// <summary>
    /// Instruct PropertyChanged.SourceGenerator to raise INPC notifications for this property whenever one of the named generated properties is changed
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = false)]
    [global::System.Diagnostics.Conditional(""DEBUG"")]
    internal class DependsOnAttribute : global::System.Attribute
    {
        /// <summary>
        /// Raise an INPC notification for this property whenever one of the named properties is changed
        /// </summary>
        /// <param name=""dependsOn"">Other properties this property depends on</param>
        public DependsOnAttribute(params string[] dependsOn) { }
    }

    /// <summary>
    /// Instruct PropertyChanged.SourceGenerator to assign true to this boolean property whenver any generated member changes
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = true)]
    [global::System.Diagnostics.Conditional(""DEBUG"")]
    internal class IsChangedAttribute : global::System.Attribute
    {
    }
}";
}
