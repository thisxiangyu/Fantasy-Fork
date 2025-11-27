using System.Text;
using System.Xml;
using Fantasy.Assembly;
using Fantasy.Entitas;
using Fantasy.Helper;
using Fantasy.Platform.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Fantasy.Database
{
    /// <summary>
    /// 开发时DbContext工厂类, 这个类主要用于执行数据库迁移命令。
    /// Win平台, 在PowerShell输入 : 
    /// dotnet ef migrations add MigrationTest --project "F:\Unity\Fantasy\Fantasy-Fork\Fantasy.Packages\Fantasy.Net" --startup-project "F:\Unity\Fantasy\Fantasy-Fork\examples\Server\Entity" --context PgSession
    /// (MigrationTest 是迁移名称, 可替换为自定义命名) 执行以上命令，EFCore 将会自动反射检测到本工厂内含方法，并生成迁移脚本到 Migrations 文件夹。
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PgSession>
    {
        /// <summary>
        /// 创建 PgSession (项目的PgSQL数据库上下文)
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public PgSession CreateDbContext(string[] args)
        {
            // 初始化程序集
            AssemblyHelper.Initialize();
            // 初始化日志
            Log.Initialize();

            // 找到 Fantasy.Config 并解析 ConnectionString
            var doc = new XmlDocument();
            var configText = File.ReadAllText("../Entity/Fantasy.config", Encoding.UTF8);
            doc.LoadXml(configText);
            var root = doc.DocumentElement;

            if (root?.LocalName != "fantasy")
            {
                throw new InvalidOperationException("Invalid Fantasy config file format");
            }

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("f", "http://fantasy.net/config");

            var serverNode = root.SelectSingleNode("f:server", nsManager);

            if (serverNode == null)
            {
                throw new InvalidOperationException("Missing server configuration in Fantasy config file");
            }

            List<WorldConfig> worldList = ConfigLoader.LoadWorldConfig(serverNode, nsManager);
            string ? ConnectionString = default;

            foreach (var worldConfig in worldList)
            {
                for (int i = 0; i < worldConfig.DbType.Length; i++)
                {
                    string db = worldConfig.DbType[i].ToLower();
                    switch (db)
                    {
                        case "postgresql":
                        case "postgres":
                        case "pgsql":
                        case "pg":
                            if (!string.IsNullOrWhiteSpace(worldConfig.DbConnection[i]))
                            {
                                ConnectionString = worldConfig.DbConnection[i];
                            }
                            break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new Exception("在 Fantasy.Config 中未能找到有效的 ConnectionString配置");
            }

            // 创建PgSession并返回
            var optionsBuilder = new DbContextOptionsBuilder();
            PostgreSQL.ConfigurePgSession(optionsBuilder, NpgsqlDataSource.Create(ConnectionString));
            var pgSession = new PgSession(optionsBuilder.Options);
            pgSession.Database.OpenConnection();

            var entityTypes = pgSession.Model.GetEntityTypes();
            Log.Info("PgSession 中识别到的实体与表：");
            foreach (var entityType in entityTypes)
            {
                var schema = entityType.GetSchema();       // schema
                var tableName = entityType.GetTableName(); // 表名

                Log.Info($"- {entityType.ClrType.Name} -> {schema}.{tableName}");
            }

            if (!entityTypes.Any())
            {
                Log.Info("❌ 未识别到任何实体！检查模型构建！");
            }

            return pgSession;
        }
    }
}
