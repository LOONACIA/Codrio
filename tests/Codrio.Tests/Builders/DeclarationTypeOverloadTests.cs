using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Codrio.Tests.Fixtures;
using Xunit;

namespace Codrio.Tests;

public class DeclarationTypeOverloadTests
{
    [Fact]
    public void BaseTypes_AllDeclarationKindsPreserveOrderAndReturnSameBuilder()
    {
        AssertBaseTypes(CSharp.Class("Sample"), "class");
        AssertBaseTypes(CSharp.Struct("Sample"), "struct");
        AssertBaseTypes(CSharp.Interface("Sample"), "interface");
        AssertBaseTypes(CSharp.Record("Sample"), "record", ";\n");
        AssertBaseTypes(CSharp.RecordStruct("Sample"), "record struct", ";\n");
    }

    private static void AssertBaseTypes<TSelf>(TSelf builder, string keyword, string ending = "\n{\n}\n")
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        builder.BaseType("IFirst");
        Assert.Same(builder, builder.BaseType(typeof(IDisposable)));
        builder.BaseType("IMiddle");
        Assert.Same(builder, builder.BaseType<IComparable<int>>());
        builder.BaseType("ILast");

        OutputAssert.Equal(keyword + " Sample : IFirst, global::System.IDisposable, IMiddle, global::System.IComparable<int>, ILast" + ending,
            builder.ToString());
    }

    [Fact]
    public void BaseTypes_ComposeWithPrimaryParametersTypeParametersAndConstraints()
    {
        OutputAssert.Equal("class Widget : global::System.Collections.ObjectModel.Collection<int>, global::System.IDisposable\n{\n}\n",
            CSharp.Class("Widget", c => c.BaseType<Collection<int>>().BaseType(typeof(IDisposable))).ToString());

        const string declaration = " Sample<T>(T value) : global::System.IDisposable where T : class, global::System.IComparable<int>, new()";
        OutputAssert.Equal("class" + declaration + "\n{\n}\n", CSharp.Class("Sample", c => c
            .PrimaryParameter("T", "value")
            .TypeParameter("T", t => t.Constraint("class").Constraint<IComparable<int>>().Constraint("new()"))
            .BaseType(typeof(IDisposable))).ToString());
        OutputAssert.Equal("struct" + declaration + "\n{\n}\n", CSharp.Struct("Sample", s => s
            .PrimaryParameter("T", "value")
            .TypeParameter("T", t => t.Constraint("class").Constraint(typeof(IComparable<int>)).Constraint("new()"))
            .BaseType<IDisposable>()).ToString());
        OutputAssert.Equal("record" + declaration + ";\n", CSharp.Record("Sample", r => r
            .PrimaryParameter("T", "value")
            .TypeParameter("T", t => t.Constraint("class").Constraint<IComparable<int>>().Constraint("new()"))
            .BaseType(typeof(IDisposable))).ToString());
        OutputAssert.Equal("record struct" + declaration + ";\n", CSharp.RecordStruct("Sample", r => r
            .PrimaryParameter("T", "value")
            .TypeParameter("T", t => t.Constraint("class").Constraint(typeof(IComparable<int>)).Constraint("new()"))
            .BaseType<IDisposable>()).ToString());
    }

    [Fact]
    public void BaseTypes_StringTypeAndGenericOverloadsRenderSameNestedType()
    {
        const string typeName = "global::Codrio.Tests.Fixtures.Outer<int>.Leaf";
        const string expected = "class Derived : " + typeName + "\n{\n}\n";
        OutputAssert.Equal(expected, CSharp.Class("Derived").BaseType(typeName).ToString());
        OutputAssert.Equal(expected, CSharp.Class("Derived").BaseType(typeof(Outer<int>.Leaf)).ToString());
        OutputAssert.Equal(expected, CSharp.Class("Derived").BaseType<Outer<int>.Leaf>().ToString());
    }

    [Fact]
    public void UnderlyingTypes_StringTypeAndGenericOverloadsRenderSameAlias()
    {
        const string expected = "enum Level : byte\n{\n    Low\n}\n";
        OutputAssert.Equal(expected, CSharp.Enum("Level", e => e.UnderlyingType("byte").Member("Low")).ToString());
        OutputAssert.Equal(expected, CSharp.Enum("Level", e => e.UnderlyingType(typeof(byte)).Member("Low")).ToString());
        OutputAssert.Equal(expected, CSharp.Enum("Level", e => e.UnderlyingType<byte>().Member("Low")).ToString());
    }

    [Fact]
    public void UnderlyingTypes_LastCallWinsAndReturnsSameBuilder()
    {
        var builder = CSharp.Enum("Level").UnderlyingType("short").Member("Low");
        Assert.Same(builder, builder.UnderlyingType(typeof(long)));
        OutputAssert.Equal("enum Level : long\n{\n    Low\n}\n", builder.ToString());
        Assert.Same(builder, builder.UnderlyingType<byte>());
        OutputAssert.Equal("enum Level : byte\n{\n    Low\n}\n", builder.ToString());
        builder.UnderlyingType("uint");
        OutputAssert.Equal("enum Level : uint\n{\n    Low\n}\n", builder.ToString());
    }

    [Fact]
    public void Constraints_AllOwnersPreserveOrderAndReturnSameBuilder()
    {
        const string clause = " where T : class, global::System.IDisposable, global::System.IComparable<int>, new()";
        OutputAssert.Equal("class Repo<T>" + clause + "\n{\n}\n",
            CSharp.Class("Repo", c => c.TypeParameter("T", ConfigureConstraints)).ToString());
        OutputAssert.Equal("void Use<T>()" + clause + ";\n",
            CSharp.Method("Use", m => m.TypeParameter("T", ConfigureConstraints)).ToString());
        OutputAssert.Equal("delegate void Use<T>()" + clause + ";\n",
            CSharp.Delegate("Use", d => d.TypeParameter("T", ConfigureConstraints)).ToString());
    }

    private static void ConfigureConstraints(TypeParameterBuilder builder)
    {
        builder.Constraint("class");
        Assert.Same(builder, builder.Constraint(typeof(IDisposable)));
        Assert.Same(builder, builder.Constraint<IComparable<int>>());
        builder.Constraint("new()");
    }

    [Fact]
    public void Constraints_StringTypeAndGenericOverloadsRenderSameType()
    {
        var configurations = new Action<TypeParameterBuilder>[]
        {
            t => t.Constraint("global::System.IComparable<int>"),
            t => t.Constraint(typeof(IComparable<int>)),
            t => t.Constraint<IComparable<int>>()
        };
        foreach (var configure in configurations)
        {
            OutputAssert.Equal("class Repo<T> where T : global::System.IComparable<int>\n{\n}\n",
                CSharp.Class("Repo").TypeParameter("T", configure).ToString());
            OutputAssert.Equal("void Use<T>() where T : global::System.IComparable<int>;\n",
                CSharp.Method("Use").TypeParameter("T", configure).ToString());
            OutputAssert.Equal("delegate void Use<T>() where T : global::System.IComparable<int>;\n",
                CSharp.Delegate("Use").TypeParameter("T", configure).ToString());
        }
    }

    [Fact]
    public void BaseTypes_InvalidTypesPreserveAllDeclarationKinds()
    {
        AssertInvalidBaseTypes(CSharp.Class("Sample"));
        AssertInvalidBaseTypes(CSharp.Struct("Sample"));
        AssertInvalidBaseTypes(CSharp.Interface("Sample"));
        AssertInvalidBaseTypes(CSharp.Record("Sample"));
        AssertInvalidBaseTypes(CSharp.RecordStruct("Sample"));
    }

    private static void AssertInvalidBaseTypes<TSelf>(TSelf builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        builder.BaseType("IExisting");
        var before = builder.ToString();
        Assert.Throws<ArgumentException>("type", () => builder.BaseType(typeof(List<>)));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.BaseType((Type)null!));
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void UnderlyingTypes_InvalidTypesPreservePreviousType()
    {
        var builder = CSharp.Enum("Level").UnderlyingType<byte>().Member("Low");
        var before = builder.ToString();
        Assert.Throws<ArgumentException>("type", () => builder.UnderlyingType(typeof(List<>)));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.UnderlyingType((Type)null!));
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void Constraints_InvalidTypesPreserveAllOwners()
    {
        AssertInvalidConstraints(configure => CSharp.Class("Repo").TypeParameter("T", configure));
        AssertInvalidConstraints(configure => CSharp.Method("Use").TypeParameter("T", configure));
        AssertInvalidConstraints(configure => CSharp.Delegate("Use").TypeParameter("T", configure));
    }

    private static void AssertInvalidConstraints(Func<Action<TypeParameterBuilder>, object> createOwner)
    {
        TypeParameterBuilder? parameter = null;
        var owner = createOwner(t =>
        {
            parameter = t;
            t.Constraint("class");
        });

        Assert.NotNull(parameter);
        var before = owner.ToString()!;
        Assert.Throws<ArgumentException>("type", () => parameter.Constraint(typeof(List<>)));
        OutputAssert.Equal(before, owner.ToString()!);
        Assert.Throws<ArgumentNullException>("type", () => parameter.Constraint((Type)null!));
        OutputAssert.Equal(before, owner.ToString()!);
    }

    [Fact]
    public void DeclarationTypes_DoNotValidateTypeKinds()
    {
        OutputAssert.Equal("class Sample : string, int\n{\n}\n",
            CSharp.Class("Sample").BaseType(typeof(string)).BaseType<int>().ToString());
        OutputAssert.Equal("enum Level : string\n{\n}\n", CSharp.Enum("Level").UnderlyingType(typeof(string)).ToString());
        OutputAssert.Equal("enum Level : object\n{\n}\n", CSharp.Enum("Level").UnderlyingType<object>().ToString());
        OutputAssert.Equal("class Repo<T> where T : object, int\n{\n}\n",
            CSharp.Class("Repo").TypeParameter("T", t => t.Constraint(typeof(object)).Constraint<int>()).ToString());
    }

    [Fact]
    public void CompleteFile_WithDeclarationTypeOverloads_Compiles()
    {
        var file = CSharp.File().Using("System").Namespace("App", n => n
            .Class("Derived", c => c.BaseType(typeof(Outer<int>.Leaf)).BaseType<IDisposable>()
                .Method("Dispose", m => m.Public().Body()))
            .Interface("IResource", i => i.BaseType<IDisposable>())
            .Enum("Level", e => e.UnderlyingType<byte>().Member("Low"))
            .Enum("WideLevel", e => e.UnderlyingType(typeof(long)).Member("High", "2147483648"))
            .Class("Repo", c => c.TypeParameter("T", ConfigureConstraints)
                .Method("Use", m => m.TypeParameter("U", ConfigureConstraints).Body()))
            .Delegate("Factory", d => d.TypeParameter("T", ConfigureConstraints).Returns("T")));

        CompilationAssert.NoErrors(file.ToString() + "\n" + TypeFixtures.Source);
    }
}
