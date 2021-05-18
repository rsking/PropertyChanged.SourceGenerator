﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using PropertyChanged.SourceGenerator.Analysis;

namespace PropertyChanged.SourceGenerator
{
    public class Generator
    {
        private readonly IndentedTextWriter writer = new(new StringWriter());
        private readonly EventArgsCache eventArgsCache;

        public static string FileHeader { get; } = @$"// <auto-generated>
//     Auto-generated by PropertyChanged.SourceGenerator {typeof(Generator).Assembly.GetName().Version}
// </auto-generated>";

        public Generator(EventArgsCache eventArgsCache)
        {
            this.eventArgsCache = eventArgsCache;
         
            this.writer.WriteLine(FileHeader);
        }

        public void Generate(TypeAnalysis typeAnalysis)
        {
            // SG'd files default to 'disable'
            if (typeAnalysis.NullableContext != NullableContextOptions.Disable)
            {
                this.writer.WriteLine(NullableContextToComment(typeAnalysis.NullableContext));
            }

            if (typeAnalysis.TypeSymbol.ContainingNamespace is { IsGlobalNamespace: false } @namespace)
            {
                this.writer.WriteLine($"namespace {@namespace.ToDisplayString(SymbolDisplayFormats.Namespace)}");
                this.writer.WriteLine("{");
                this.writer.Indent++;

                this.GenerateType(typeAnalysis);

                this.writer.Indent--;
                this.writer.WriteLine("}");
            }
            else
            {
                this.GenerateType(typeAnalysis);
            }
        }

        private void GenerateType(TypeAnalysis typeAnalysis)
        {
            this.writer.Write($"partial {typeAnalysis.TypeSymbol.ToDisplayString(SymbolDisplayFormats.TypeDeclaration)}");
            if (!typeAnalysis.HasInpcInterface)
            {
                this.writer.Write(" : global::System.ComponentModel.INotifyPropertyChanged");
            }
            this.writer.WriteLine();
            
            this.writer.WriteLine("{");
            this.writer.Indent++;

            if (typeAnalysis.RequiresEvent)
            {
                string nullable = typeAnalysis.NullableContext.HasFlag(NullableContextOptions.Annotations) ? "?" : "";
                this.writer.WriteLine($"public event global::System.ComponentModel.PropertyChangedEventHandler{nullable} PropertyChanged;");
            }

            foreach (var member in typeAnalysis.Members)
            {
                this.GenerateMember(typeAnalysis, member);
            }

            if (typeAnalysis.RequiresRaisePropertyChangedMethod)
            {
                Trace.Assert(typeAnalysis.RaisePropertyChangedMethodSignature == RaisePropertyChangedMethodSignature.PropertyChangedEventArgs);
                this.writer.WriteLine($"protected virtual void {typeAnalysis.RaisePropertyChangedMethodName}(global::System.ComponentModel.PropertyChangedEventArgs eventArgs)");
                this.writer.WriteLine("{");
                this.writer.Indent++;

                this.writer.WriteLine("this.PropertyChanged?.Invoke(this, eventArgs);");

                this.writer.Indent--;
                this.writer.WriteLine("}");
            }

            this.writer.Indent--;
            this.writer.WriteLine("}");
        }

        private void GenerateMember(TypeAnalysis type, MemberAnalysis member)
        {
            string backingMemberReference = "this." + member.BackingMember.ToDisplayString(SymbolDisplayFormats.SymbolName);

            if (member.NullableContextOverride is { } context)
            {
                this.writer.WriteLine(NullableContextToComment(context));
            }

            var (propertyAccessibility, getterAccessibility, setterAccessibility) = CalculateAccessibilities(member);

            this.writer.WriteLine($"{propertyAccessibility}{member.Type.ToDisplayString(SymbolDisplayFormats.MethodOrPropertyReturnType)} {member.Name}");
            this.writer.WriteLine("{");
            this.writer.Indent++;

            this.writer.WriteLine($"{getterAccessibility}get => {backingMemberReference};");
            this.writer.WriteLine($"{setterAccessibility}set");
            this.writer.WriteLine("{");
            this.writer.Indent++;

            this.writer.WriteLine("if (!global::System.Collections.Generic.EqualityComparer<" +
                member.Type.ToDisplayString(SymbolDisplayFormats.TypeParameter) +
                $">.Default.Equals(value, {backingMemberReference}))");
            this.writer.WriteLine("{");
            this.writer.Indent++;

            this.writer.WriteLine($"{backingMemberReference} = value;");

            this.GenerateRaiseEvent(type, member.Name);
            foreach (string? alsoNotify in member.AlsoNotify.OrderBy(x => x))
            {
                this.GenerateRaiseEvent(type, alsoNotify);
            }

            this.writer.Indent--;
            this.writer.WriteLine("}");
            this.writer.Indent--;
            this.writer.WriteLine("}");
            this.writer.Indent--;
            this.writer.WriteLine("}");

            if (member.NullableContextOverride != null)
            {
                this.writer.WriteLine(NullableContextToComment(type.NullableContext));
            }
        }

        private void GenerateRaiseEvent(TypeAnalysis type, string? propertyName)
        {
            switch (type.RaisePropertyChangedMethodSignature)
            {
                case RaisePropertyChangedMethodSignature.PropertyChangedEventArgs:
                    string cacheName = this.eventArgsCache.GetOrAdd(propertyName);
                    this.writer.WriteLine($"this.{type.RaisePropertyChangedMethodName}(global::PropertyChanged.SourceGenerator.Internal.PropertyChangedEventArgsCache.{cacheName});");
                    break;

                case RaisePropertyChangedMethodSignature.String:
                    this.writer.WriteLine($"this.{type.RaisePropertyChangedMethodName}({EscapeString(propertyName)});");
                    break;
            }
        }

        private static (string property, string getter, string setter) CalculateAccessibilities(MemberAnalysis member)
        {
            string property;
            string getter = "";
            string setter = "";

            if (member.GetterAccessibility >= member.SetterAccessibility)
            {
                property = AccessibilityToString(member.GetterAccessibility);
                setter = member.GetterAccessibility == member.SetterAccessibility
                    ? ""
                    : AccessibilityToString(member.SetterAccessibility);
            }
            else
            {
                property = AccessibilityToString(member.SetterAccessibility);
                getter = AccessibilityToString(member.GetterAccessibility);
            }

            return (property, getter, setter);
        }

        private static string NullableContextToComment(NullableContextOptions context)
        {
            return context switch
            {
                NullableContextOptions.Disable => "#nullable disable",
                NullableContextOptions.Warnings => "#nullable enable warnings",
                NullableContextOptions.Annotations => "#nullable enable annotations",
                NullableContextOptions.Enable => "#nullable enable"
            };
        }

        private static string AccessibilityToString(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public ",
                Accessibility.ProtectedOrInternal => "protected internal ",
                Accessibility.Internal => "internal ",
                Accessibility.Protected => "protected ",
                Accessibility.ProtectedAndInternal => "private protected ",
                Accessibility.Private => "private ",
                _ => throw new ArgumentException("Unknown member", nameof(accessibility)),
            };
        }

        private static string EscapeString(string? str)
        {
            return str == null
                ? "null"
                : "@\"" + str.Replace("\"", "\"\"") + "\"";
        }

        public void GenerateNameCache()
        {
            this.writer.WriteLine("namespace PropertyChanged.SourceGenerator.Internal");
            this.writer.WriteLine("{");
            this.writer.Indent++;

            this.writer.WriteLine("internal static class PropertyChangedEventArgsCache");
            this.writer.WriteLine("{");
            this.writer.Indent++;

            foreach (var (cacheName, propertyName) in this.eventArgsCache.GetEntries().OrderBy(x => x.cacheName))
            {
                string backingFieldName = "_" + cacheName;
                for (int i = 0; this.eventArgsCache.ContainsCacheName(backingFieldName); i++)
                {
                    backingFieldName = $"_{cacheName}{i}";
                }
                this.writer.WriteLine($"private static global::System.ComponentModel.PropertyChangedEventArgs {backingFieldName};");
                this.writer.WriteLine($"public static global::System.ComponentModel.PropertyChangedEventArgs {cacheName} => " +
                    $"{backingFieldName} ??= new global::System.ComponentModel.PropertyChangedEventArgs({EscapeString(propertyName)});");
            }

            this.writer.Indent--;
            this.writer.WriteLine("}");

            this.writer.Indent--;
            this.writer.WriteLine("}");
        }

        public override string ToString() => this.writer.InnerWriter.ToString();
    }
}
