using System.Collections.Generic;
using System.Linq;
using Fantasy.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1024
#pragma warning disable RS1035

namespace Fantasy.SourceGenerator.Generators
{
    /// <summary>
    /// DbSet 注册源生成器。
    /// 扫描当前程序集中所有标记了 [DbSet] 特性的类，
    /// 生成 IDbSetModelBuilderRegistrar 的实现，替代运行时的反射扫描 (ScanDbSetTypes)。
    /// </summary>
    [Generator]
    public sealed class DbSetRegistrationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var dbSetTypes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsDbSetCandidate(node),
                    transform: static (ctx, _) => GetDbSetTypeInfo(ctx))
                .Where(static info => info != null)
                .Collect()
                .Select(static (types, _) => types.Distinct().ToList());

            var compilationAndTypes = context.CompilationProvider.Combine(dbSetTypes);

            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) =>
            {
                if (CompilationHelper.IsSourceGeneratorDisabled(source.Left))
                    return;

                if (!CompilationHelper.HasFantasyDefine(source.Left))
                    return;

                if (source.Left.GetTypeByMetadataName("Fantasy.Assembly.IDbSetModelBuilderRegistrar") == null)
                    return;

                GenerateDbSetRegistrar(spc, source.Left, source.Right!);
            });
        }

        private static bool IsDbSetCandidate(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax classDecl)
                return false;

            return classDecl.AttributeLists.Count > 0;
        }

        private static DbSetTypeInfo? GetDbSetTypeInfo(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
                return null;

            if (symbol.IsAbstract || symbol.IsStatic || symbol.TypeKind != TypeKind.Class)
                return null;

            var dbSetAttrData = symbol.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "DbSetAttribute" &&
                a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Fantasy.Database.Attributes");

            if (dbSetAttrData == null)
                return null;

            var attrInfo = ExtractDbSetAttributeInfo(dbSetAttrData);
            bool inheritsFromEntity = InheritsFromEntity(symbol);

            return new DbSetTypeInfo(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.Name,
                symbol.ContainingNamespace?.ToDisplayString() ?? "",
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", ""),
                attrInfo,
                inheritsFromEntity
            );
        }

        private static DbSetAttrInfo ExtractDbSetAttributeInfo(AttributeData attrData)
        {
            var info = new DbSetAttrInfo();

            foreach (var named in attrData.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Name":
                        info.Name = named.Value.Value as string;
                        break;
                    case "WithNamespace":
                        info.WithNamespace = named.Value.Value is bool b && b;
                        break;
                    case "Comment":
                        info.Comment = named.Value.Value as string;
                        break;
                    case "DbSelection":
                        if (named.Value.Value is int dbVal)
                            info.DbSelection = dbVal;
                        break;
                    case "IsAsDocument":
                        info.IsAsDocument = named.Value.Value is bool d && d;
                        break;
                    case "IsEmbedded":
                        info.IsEmbedded = named.Value.Value is bool e && e;
                        break;
                    case "IsAsBytes":
                        info.IsAsBytes = named.Value.Value is bool b2 && b2;
                        break;
                    case "IsAsConfig":
                        info.IsAsConfig = named.Value.Value is bool c && c;
                        break;
                }
            }

            return info;
        }

        private static bool InheritsFromEntity(INamedTypeSymbol? symbol)
        {
            var current = symbol;
            while (current != null)
            {
                if (current.Name == "Entity" && current.Arity == 0)
                {
                    if (current.ContainingNamespace.ToDisplayString() == "Fantasy.Entitas")
                        return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static void GenerateDbSetRegistrar(
            SourceProductionContext context,
            Compilation compilation,
            IEnumerable<DbSetTypeInfo> dbSetTypes)
        {
            var typeList = dbSetTypes.ToList();
            var markerClassName = compilation.GetAssemblyName("DbSetModelBuilderRegistrar", out var assemblyName, out var _);
            var builder = new SourceCodeBuilder();

            builder.AppendLine(GeneratorConstants.AutoGeneratedHeader);
            builder.AddUsings(
                "System",
                "System.Collections.Generic",
                "Fantasy.Assembly",
                "Fantasy.Database",
                "Fantasy.Database.Attributes",
                "Fantasy.Database.DataTransfer",
                "Fantasy.Database.Helper",
                "Fantasy.Entitas",
                "Fantasy.Entitas.TypeMeta",
                "Fantasy.Helper",
                "Fantasy.Pool",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore.Metadata.Builders"
            );
            builder.AppendLine();

            builder.BeginDefaultNamespace();
            builder.AddXmlComment($"Auto-generated DbSet registration class for {assemblyName}");
            builder.BeginClass(markerClassName, "internal sealed", "global::Fantasy.Assembly.IDbSetModelBuilderRegistrar");

            // ===== RegisterToModelBuilder =====
            builder.AddXmlComment("Register all [DbSet] types to the EFCore ModelBuilder");
            builder.BeginMethod("public void RegisterToModelBuilder(global::Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder, bool isSessionForConfig, global::Fantasy.Helper.JsonSettings jsonSettings)");

            builder.AppendLine("const string defaultSchema = \"default\";");
            builder.AppendLine("modelBuilder.HasDefaultSchema(defaultSchema);");
            builder.AppendLine();

            foreach (var info in typeList)
            {
                GenerateTypeRegistration(builder, info);
            }

            builder.EndMethod();
            builder.AppendLine();

            // ===== GetDbSetTypeCache =====
            builder.AddXmlComment("Get compile-time generated TypeDbSetCache dictionary");
            builder.BeginMethod("public Dictionary<long, global::Fantasy.Entitas.TypeMeta.TypeDbSetCache> GetDbSetTypeCache()");

            if (typeList.Count > 0)
            {
                builder.AppendLine($"var dict = new Dictionary<long, global::Fantasy.Entitas.TypeMeta.TypeDbSetCache>({typeList.Count});");
                foreach (var info in typeList)
                {
                    GenerateTypeDbSetCacheEntry(builder, info);
                }
                builder.AppendLine("return dict;");
            }
            else
            {
                builder.AppendLine("return new Dictionary<long, global::Fantasy.Entitas.TypeMeta.TypeDbSetCache>();");
            }

            builder.EndMethod();
            builder.EndClass();
            builder.EndNamespace();

            context.AddSource($"{markerClassName}.g.cs", builder.ToString());
        }

        private static void GenerateTypeRegistration(SourceCodeBuilder builder, DbSetTypeInfo info)
        {
            var attr = info.AttrInfo;

            builder.AddComment($"---- {info.TypeSimpleName} ----");

            // DbSelection 位标志不含 PostgreSQL (bit 0) 则跳过
            if ((attr.DbSelection & 1) == 0)
            {
                builder.AddComment($"Skipped: DbSelection does not include PostgreSQL");
                builder.AppendLine();
                return;
            }

            // Embedded 类型跳过
            if (attr.IsEmbedded)
            {
                builder.AddComment("Embedded type, ignored in PgSQL ORM-Model");
                builder.AppendLine();
                return;
            }

            // 表名
            var tableName = !string.IsNullOrEmpty(attr.Name) ? attr.Name : info.TypeSimpleName;

            builder.AppendLine("{");
            builder.Indent();

            // Schema
            builder.AppendLine("string schemaStr = defaultSchema;");
            if (attr.WithNamespace && !string.IsNullOrEmpty(info.Namespace))
            {
                var schemaFromNs = info.Namespace.Replace(".", "_");
                builder.AppendLine($"schemaStr = \"{schemaFromNs}\";");
            }

            // Config 过滤
            if (attr.IsAsConfig)
            {
                builder.AppendLine("global::Fantasy.Database.PostgreSQL.ExistingAtLeastOneConfigDbSet = true;");
                builder.AppendLine("if (!isSessionForConfig)");
                builder.AppendLine("    return;");
                builder.AppendLine("if (schemaStr == defaultSchema)");
                builder.AppendLine("    schemaStr = \"Config\";");
                builder.AppendLine("else");
                builder.AppendLine("    schemaStr = \"Config_\" + schemaStr;");
            }
            else
            {
                builder.AppendLine("if (isSessionForConfig)");
                builder.AppendLine("    return;");
            }

            builder.AppendLine($"string tableName = \"{tableName}\";");
            builder.AppendLine($"global::Fantasy.Log.Debug(\"PgSQL ORM-Model Registering entities: {info.TypeFullNameNoGlobal} -> table \" + schemaStr + \".\" + tableName);");
            builder.AppendLine();

            if (attr.IsAsDocument)
            {
                // 文档建表
                var shadowName = $"{info.TypeFullNameNoGlobal}_Shadow";
                if (info.InheritsFromEntity)
                    builder.AppendLine($"var entityBuilder = modelBuilder.SharedTypeEntity<global::Fantasy.Database.DataTransfer.EntityDocumentDTC>(\"{shadowName}\");");
                else
                    builder.AppendLine($"var entityBuilder = modelBuilder.SharedTypeEntity<global::Fantasy.Database.DataTransfer.DocumentDTC>(\"{shadowName}\");");

                builder.AppendLine("entityBuilder.ToTable(tableName + \"_Doc\", schemaStr);");

                if (info.InheritsFromEntity)
                    builder.AppendLine("entityBuilder.Property<long>(\"Id\").ValueGeneratedNever();");
                else
                    builder.AppendLine("entityBuilder.Property<long>(\"Id\").UseIdentityColumn();");

                builder.AppendLine("entityBuilder.Property<object?>(global::Fantasy.Database.Attributes.DbSetProperty.DocAsJson).HasColumnType(\"jsonb\").IsRequired(false);");
            }
            else
            {
                // 实体建表
                builder.AppendLine($"var entityBuilder = modelBuilder.Entity(typeof({info.TypeFullName})).ToTable(tableName, schemaStr);");

                if (info.InheritsFromEntity)
                {
                    // 嵌入实体影子属性 - HasConversion
                    builder.AppendLine("entityBuilder.Property<global::Fantasy.DataStructure.Collection.ReuseList<global::Fantasy.Entitas.Entity>>(nameof(global::Fantasy.Entitas.Entity.EmbbededSingle)).HasColumnType(\"jsonb\")");
                    builder.Indent(2);
                    builder.AppendLine(".HasColumnName(global::Fantasy.Database.Attributes.DbSetProperty.JsonSingle)");
                    builder.AppendLine(".HasConversion(");
                    builder.Indent();
                    builder.AppendLine("entityList => entityList.ToJson(jsonSettings, true),");
                    builder.AppendLine("jsonStr => jsonStr.Deserialize<global::Fantasy.DataStructure.Collection.ReuseList<global::Fantasy.Entitas.Entity>>(jsonSettings, global::Fantasy.Helper.DetectMode.MustBeWrapper, true)");
                    builder.Unindent();
                    builder.AppendLine(")");
                    builder.AppendLine(".IsRequired(false);");
                    builder.Unindent();
                    builder.Unindent();

                    builder.AppendLine("entityBuilder.Property<global::Fantasy.DataStructure.Collection.ReuseList<global::Fantasy.Entitas.Entity>>(nameof(global::Fantasy.Entitas.Entity.EmbbededMulti)).HasColumnType(\"jsonb\")");
                    builder.Indent(2);
                    builder.AppendLine(".HasColumnName(global::Fantasy.Database.Attributes.DbSetProperty.JsonMulti)");
                    builder.AppendLine(".HasConversion(");
                    builder.Indent();
                    builder.AppendLine("entityList => entityList.ToJson(jsonSettings, true),");
                    builder.AppendLine("jsonStr => jsonStr.Deserialize<global::Fantasy.DataStructure.Collection.ReuseList<global::Fantasy.Entitas.Entity>>(jsonSettings, global::Fantasy.Helper.DetectMode.MustBeWrapper, true)");
                    builder.Unindent();
                    builder.AppendLine(")");
                    builder.AppendLine(".IsRequired(false);");
                    builder.Unindent();
                    builder.Unindent();
                }
            }

            // 父级 Type + Id 联合索引
            if (info.InheritsFromEntity)
            {
                builder.AppendLine("entityBuilder.Property<long>(global::Fantasy.Database.Attributes.DbSetProperty.ParentType);");
                builder.AppendLine("entityBuilder.Property<long>(global::Fantasy.Database.Attributes.DbSetProperty.ParentId);");
                builder.AppendLine("entityBuilder.HasIndex(global::Fantasy.Database.Attributes.DbSetProperty.ParentType, global::Fantasy.Database.Attributes.DbSetProperty.ParentId).IsUnique(false);");
            }

            builder.Unindent();
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private static void GenerateTypeDbSetCacheEntry(SourceCodeBuilder builder, DbSetTypeInfo info)
        {
            var attr = info.AttrInfo;

            // 构建 DbSetAttribute 实例化代码
            var attrParts = new List<string>();
            if (!string.IsNullOrEmpty(attr.Name))
                attrParts.Add($"Name = \"{attr.Name}\"");
            if (attr.WithNamespace)
                attrParts.Add("WithNamespace = true");
            if (!string.IsNullOrEmpty(attr.Comment))
                attrParts.Add($"Comment = \"{attr.Comment}\"");
            if (attr.DbSelection > 0 && attr.DbSelection != 3) // Skip if Any (3) since it's the default
                attrParts.Add($"DbSelection = (global::Fantasy.Database.DatabaseType){attr.DbSelection}");
            if (attr.IsAsDocument)
                attrParts.Add("IsAsDocument = true");
            if (attr.IsEmbedded)
                attrParts.Add("IsEmbedded = true");
            if (attr.IsAsBytes)
                attrParts.Add("IsAsBytes = true");
            if (attr.IsAsConfig)
                attrParts.Add("IsAsConfig = true");

            var attrInit = attrParts.Count > 0
                ? "new global::Fantasy.Database.Attributes.DbSetAttribute { " + string.Join(", ", attrParts) + " }"
                : "new global::Fantasy.Database.Attributes.DbSetAttribute()";

            builder.AddComment($"{info.TypeSimpleName}");
            builder.AppendLine($"dict.Add(global::Fantasy.Entitas.TypeMeta.TypeHashCache.GetHashCode(typeof({info.TypeFullName})), new global::Fantasy.Entitas.TypeMeta.TypeDbSetCache({attrInit}));");
        }
    }

    internal sealed class DbSetTypeInfo : IEquatable<DbSetTypeInfo>
    {
        public readonly string TypeFullName;
        public readonly string TypeSimpleName;
        public readonly string Namespace;
        public readonly string TypeFullNameNoGlobal; // 不含 global:: 前缀
        public readonly DbSetAttrInfo AttrInfo;
        public readonly bool InheritsFromEntity;

        public DbSetTypeInfo(
            string typeFullName,
            string typeSimpleName,
            string ns,
            string typeFullNameNoGlobal,
            DbSetAttrInfo attrInfo,
            bool inheritsFromEntity)
        {
            TypeFullName = typeFullName;
            TypeSimpleName = typeSimpleName;
            Namespace = ns;
            TypeFullNameNoGlobal = typeFullNameNoGlobal;
            AttrInfo = attrInfo;
            InheritsFromEntity = inheritsFromEntity;
        }

        public bool Equals(DbSetTypeInfo? other)
        {
            if (other is null) return false;
            return TypeFullName == other.TypeFullName;
        }

        public override bool Equals(object? obj) => Equals(obj as DbSetTypeInfo);
        public override int GetHashCode() => TypeFullName.GetHashCode();
    }

    internal sealed class DbSetAttrInfo
    {
        public string? Name;
        public bool WithNamespace;
        public string? Comment;
        public int DbSelection = 3; // DatabaseType.Any = PostgreSQL | MongoDB
        public bool IsAsDocument;
        public bool IsEmbedded;
        public bool IsAsBytes;
        public bool IsAsConfig;
    }
}
