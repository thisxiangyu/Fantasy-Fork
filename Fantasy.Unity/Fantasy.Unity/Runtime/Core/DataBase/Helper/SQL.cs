using System.Linq;

namespace Fantasy.Database.Helper
{
    /// <summary>
    /// SQL 静态帮助类
    /// </summary>
    internal static class SQL
    {
        ///// <summary>
        ///// TODO 父带子还是不适合JOIN, JOIN适合查某组实体分别连带出不同的引用的数据, 即N:1的场景
        ///// 返回父级表与其JOIN的子级
        ///// </summary>
        ///// <param name="parentFullName"></param>
        ///// <param name="childFullName"></param>
        ///// <returns></returns>
        //public static string PARENT_JOIN_CHILD(string parentFullName, string childFullName)
        //{
        //    return $@"
        //            SELECT p.*, c.*
        //            FROM {parentFullName} p
        //            LEFT JOIN {childFullName} c 
        //                ON c.""ParentId"" = p.""Id""
        //            WHERE p.""Id"" = @ParentId;
        //        ";
        //}

        /// <summary>
        /// 父级查询语句。注意: 传入的 fullTableName 需要带上schema和双引号。
        /// <para>
        /// 可选: 传入 propertyNames 控制数据库仅返回部分属性。
        /// </para>
        /// </summary>
        public static string QUERY_BY_PARENT(string fullTableName, string[]? propertyNames = null)
        {
            string columns = (propertyNames is { Length: > 0 })
            ? string.Join(", ", propertyNames.Select(p => $@"""{p}"""))
            : "*"; //全选或只选择部分属性

            return $@"
            SELECT {columns}
            FROM {fullTableName}
            WHERE ""ParentType"" = @ParentType AND ""ParentId"" = @ParentId;
            ";
        }

    }
}
