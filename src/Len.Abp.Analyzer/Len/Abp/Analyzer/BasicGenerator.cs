using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

public abstract class BasicGenerator<T>(string id, params HashSet<string> attributeFullyNames) : IIncrementalGenerator
{
    protected readonly HashSet<string> _attributeFullyNames = attributeFullyNames;
    private readonly string _id = id;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var version = context.GetVersion();

        foreach (var attributeFullyName in _attributeFullyNames)
        {
            context
                .SyntaxProvider.GetClasses(attributeFullyName)
                .Combine(version)
                .SelectAndReportExceptions(PrepareData, context, _id)
                .WhereNotNull()
                .SelectAndReportExceptions(context => new FileWithName(CreateFileName(context), CreateSourceCode(context)), context, _id)
                .AddSource(context);
        }
    }

    protected abstract string CreateFileName((ClassData Class, T Data, string Version) context);

    protected abstract string CreateSourceCode((ClassData Class, T Data, string Version) context);

    protected abstract (ClassData Class, T Data, string Version)? PrepareData((ClassContext, string) context);
}