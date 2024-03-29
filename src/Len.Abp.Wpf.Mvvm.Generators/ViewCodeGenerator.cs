﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Len.Abp.Wpf.Mvvm.Generators;

internal class ViewCodeGenerator
{
    internal static ViewCodeGenerator Instance { get; } = new();

    public void Excute(ImmutableArray<ViewInfo> viewInfos, SourceProductionContext context, string version)
    {
        foreach (var item in viewInfos)
        {
            string code;
            if (item.ViewModelFullyQualifiedName is not null)
            {
                code = $$"""
                    // <auto-generated />

                    namespace {{item.Namespace}};

                    #nullable enable

                    partial class {{item.Name}} :
                        global::Len.Abp.Wpf.Mvvm.IView<{{item.ViewModelFullyQualifiedName}}>, 
                        global::Volo.Abp.DependencyInjection.{{GetLifetime(item.LifetimeValue)}}
                    {
                        ///<inheritdoc/>
                        [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(ViewCodeGenerator)}}", "{{version}}")]
                        public required {{item.ViewModelFullyQualifiedName}} ViewModel { get; set; }
                    }
                    """;
            }
            else
            {
                code = $$"""
                    // <auto-generated />

                    namespace {{item.Namespace}};

                    #nullable enable

                    partial class {{item.Name}} :
                        global::Len.Abp.Wpf.Mvvm.IView,
                        global::Volo.Abp.DependencyInjection.{{GetLifetime(item.LifetimeValue)}}
                    {
                    }
                    """;
            }

            context.AddSource($"{item.FullName}.g.cs", code);
        }
    }

    /// <summary>
    /// Gets the lifetime string based on the lifetime value.
    /// </summary>
    ///<param name="lifetimeValue">The lifetime value.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private string GetLifetime(int lifetimeValue) =>
        lifetimeValue switch
        {
            0 => "ISingletonDependency",
            1 => "IScopedDependency",
            2 => "ITransientDependency",
            _ => throw new ArgumentException("Invalid lifetime value")
        };
}
