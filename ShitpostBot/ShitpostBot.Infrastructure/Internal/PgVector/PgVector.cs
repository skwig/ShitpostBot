using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.Internal;
using Npgsql.Internal.Postgres;
using Npgsql.TypeMapping;
using ShitpostBot.Domain;

// Copied from https://github.com/pgvector/pgvector-dotnet/tree/master/src/Pgvector.EntityFrameworkCore in order to avoid a Npgsql dependency in Domain
namespace ShitpostBot.Infrastructure.PgVector;

public static class VectorDbContextOptionsBuilderExtensions
{
    public static NpgsqlDbContextOptionsBuilder UseVector(this NpgsqlDbContextOptionsBuilder optionsBuilder)
    {
        // not ideal, but how Npgsql.EntityFrameworkCore.PostgreSQL does it
#pragma warning disable CS0618
        NpgsqlConnection.GlobalTypeMapper.UseVector();
#pragma warning restore CS0618

        var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;

        var extension = coreOptionsBuilder.Options.FindExtension<VectorDbContextOptionsExtension>()
                        ?? new VectorDbContextOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }
}

public class VectorDbContextOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public virtual DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        new EntityFrameworkRelationalServicesBuilder(services)
            .TryAdd<IMethodCallTranslatorPlugin, VectorDbFunctionsTranslatorPlugin>();

        services.AddSingleton<IRelationalTypeMappingSourcePlugin, VectorTypeMappingSourcePlugin>();
    }

    public void Validate(IDbContextOptions options) { }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

        private new VectorDbContextOptionsExtension Extension
            => (VectorDbContextOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using vector ";

        public override int GetServiceProviderHashCode()
            => 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Pgvector.EntityFrameworkCore:UseVector"] = "1";
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => true;
    }
}

public static class VectorDbFunctionsExtensions
{
    public static double L2Distance(this Vector a, Vector b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(L2Distance)));

    public static double MaxInnerProduct(this Vector a, Vector b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(MaxInnerProduct)));

    public static double CosineDistance(this Vector a, Vector b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(CosineDistance)));
}

public class VectorDbFunctionsTranslatorPlugin : IMethodCallTranslatorPlugin
{
    public VectorDbFunctionsTranslatorPlugin(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource
    )
    {
        Translators = new[]
        {
            new VectorDbFunctionsTranslator(sqlExpressionFactory, typeMappingSource),
        };
    }

    public virtual IEnumerable<IMethodCallTranslator> Translators { get; }

    private class VectorDbFunctionsTranslator : IMethodCallTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        private static readonly MethodInfo _methodL2Distance = typeof(VectorDbFunctionsExtensions)
            .GetRuntimeMethod(nameof(VectorDbFunctionsExtensions.L2Distance), new[]
            {
                typeof(Vector),
                typeof(Vector),
            })!;

        private static readonly MethodInfo _methodMaxInnerProduct = typeof(VectorDbFunctionsExtensions)
            .GetRuntimeMethod(nameof(VectorDbFunctionsExtensions.MaxInnerProduct), new[]
            {
                typeof(Vector),
                typeof(Vector),
            })!;

        private static readonly MethodInfo _methodCosineDistance = typeof(VectorDbFunctionsExtensions)
            .GetRuntimeMethod(nameof(VectorDbFunctionsExtensions.CosineDistance), new[]
            {
                typeof(Vector),
                typeof(Vector),
            })!;

        public VectorDbFunctionsTranslator(
            ISqlExpressionFactory sqlExpressionFactory,
            IRelationalTypeMappingSource typeMappingSource
        )
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _typeMappingSource = typeMappingSource;
        }

#pragma warning disable EF1001
        public SqlExpression? Translate(
            SqlExpression? instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger
        )
        {
            var vectorOperator = method switch
            {
                _ when ReferenceEquals(method, _methodL2Distance) => "<->",
                _ when ReferenceEquals(method, _methodMaxInnerProduct) => "<#>",
                _ when ReferenceEquals(method, _methodCosineDistance) => "<=>",
                _ => null
            };

            if (vectorOperator != null)
            {
                var resultTypeMapping = _typeMappingSource.FindMapping(method.ReturnType)!;

                return new PgUnknownBinaryExpression(
                    left: _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]),
                    right: _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    binaryOperator: vectorOperator,
                    type: resultTypeMapping.ClrType,
                    typeMapping: resultTypeMapping
                );
            }

            return null;
        }
#pragma warning restore EF1001
    }
}

public class VectorTypeMapping : RelationalTypeMapping
{
    public VectorTypeMapping(string storeType) : base(storeType, typeof(Vector)) { }

    protected VectorTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new VectorTypeMapping(parameters);
}

public class VectorTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    public RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        => mappingInfo.ClrType == typeof(Vector)
            ? new VectorTypeMapping(mappingInfo.StoreTypeName ?? "vector")
            : null;
}

public class VectorConverter : PgStreamingConverter<Vector>
{
    public override Vector Read(PgReader reader)
    {
        if (reader.ShouldBuffer(2 * sizeof(ushort)))
            reader.Buffer(2 * sizeof(ushort));

        var dim = reader.ReadUInt16();
        var unused = reader.ReadUInt16();
        if (unused != 0)
            throw new InvalidCastException("expected unused to be 0");

        var vec = new double[dim];
        for (var i = 0; i < dim; i++)
        {
            if (reader.ShouldBuffer(sizeof(double)))
                reader.Buffer(sizeof(double));
            vec[i] = reader.ReadDouble();
        }

        return new Vector(vec);
    }

    public override async ValueTask<Vector> ReadAsync(PgReader reader, CancellationToken cancellationToken = default)
    {
        if (reader.ShouldBuffer(2 * sizeof(ushort)))
            await reader.BufferAsync(2 * sizeof(ushort), cancellationToken).ConfigureAwait(false);

        var dim = reader.ReadUInt16();
        var unused = reader.ReadUInt16();
        if (unused != 0)
            throw new InvalidCastException("expected unused to be 0");

        var vec = new double[dim];
        for (var i = 0; i < dim; i++)
        {
            if (reader.ShouldBuffer(sizeof(double)))
                await reader.BufferAsync(sizeof(double), cancellationToken).ConfigureAwait(false);
            vec[i] = reader.ReadFloat();
        }

        return new Vector(vec);
    }

    public override Size GetSize(SizeContext context, Vector value, ref object? writeState)
        => sizeof(ushort) * 2 + sizeof(float) * value.ToArray().Length;

    public override void Write(PgWriter writer, Vector value)
    {
        if (writer.ShouldFlush(sizeof(ushort) * 2))
            writer.Flush();

        var span = value.Memory.Span;
        var dim = span.Length;
        writer.WriteUInt16(Convert.ToUInt16(dim));
        writer.WriteUInt16(0);

        for (int i = 0; i < dim; i++)
        {
            if (writer.ShouldFlush(sizeof(double)))
                writer.Flush();
            writer.WriteDouble(span[i]);
        }
    }

    public override async ValueTask WriteAsync(
        PgWriter writer, Vector value, CancellationToken cancellationToken = default)
    {
        if (writer.ShouldFlush(sizeof(ushort) * 2))
            await writer.FlushAsync(cancellationToken);

        var memory = value.Memory;
        var dim = memory.Length;
        writer.WriteUInt16(Convert.ToUInt16(dim));
        writer.WriteUInt16(0);

        for (int i = 0; i < dim; i++)
        {
            if (writer.ShouldFlush(sizeof(float)))
                await writer.FlushAsync(cancellationToken);
            writer.WriteDouble(memory.Span[i]);
        }
    }
}

public static class VectorExtensions
{
    public static INpgsqlTypeMapper UseVector(this INpgsqlTypeMapper mapper)
    {
        mapper.AddTypeInfoResolverFactory(new VectorTypeInfoResolverFactory());
        return mapper;
    }
}

public class VectorTypeInfoResolverFactory : PgTypeInfoResolverFactory
{
    public override IPgTypeInfoResolver CreateResolver() => new Resolver();
    public override IPgTypeInfoResolver CreateArrayResolver() => new ArrayResolver();

    class Resolver : IPgTypeInfoResolver
    {
        TypeInfoMappingCollection? _mappings;
        protected TypeInfoMappingCollection Mappings => _mappings ??= AddMappings(new());

        public PgTypeInfo? GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
            => Mappings.Find(type, dataTypeName, options);

        static TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
        {
            mappings.AddType<Vector>("vector",
                static (options, mapping, _) => mapping.CreateInfo(options, new VectorConverter()), isDefault: true);
            return mappings;
        }
    }

    sealed class ArrayResolver : Resolver, IPgTypeInfoResolver
    {
        TypeInfoMappingCollection? _mappings;
        new TypeInfoMappingCollection Mappings => _mappings ??= AddMappings(new(base.Mappings));

        public new PgTypeInfo? GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
            => Mappings.Find(type, dataTypeName, options);

        static TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
        {
            mappings.AddArrayType<Vector>("vector");
            return mappings;
        }
    }
}