using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NexusForever.Database.EntityFramework
{
    public static class DatabaseModelBuilderExtensions
    {
        private static readonly Regex MySqlUnsignedColumnTypeRegex = new(@"\s+unsigned\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly HashSet<Type> SqliteIntegerTypes =
        [
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        ];

        public static ModelBuilder UseProviderCompatibility(this ModelBuilder modelBuilder, string providerName)
        {
            if (string.Equals(providerName, Extensions.SqliteProviderName, StringComparison.Ordinal))
                UseSqliteCompatibility(modelBuilder);

            return modelBuilder;
        }

        private static void UseSqliteCompatibility(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                RemoveProviderAnnotations(entityType);

                foreach (var property in entityType.GetProperties())
                {
                    RemoveProviderAnnotations(property);

                    string columnType = property.GetColumnType();
                    if (!string.IsNullOrWhiteSpace(columnType))
                    {
                        string sqliteColumnType = MySqlUnsignedColumnTypeRegex.Replace(columnType, string.Empty).Trim();
                        if (!string.Equals(sqliteColumnType, columnType, StringComparison.Ordinal))
                            property.SetColumnType(sqliteColumnType);
                    }

                    if (IsSqliteAutoIncrementKey(property))
                        property.SetColumnType("INTEGER");

                    string defaultValueSql = property.GetDefaultValueSql();
                    if (string.Equals(defaultValueSql, "current_timestamp()", StringComparison.OrdinalIgnoreCase))
                        property.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }

                foreach (var key in entityType.GetKeys())
                    RemoveProviderAnnotations(key);

                foreach (var index in entityType.GetIndexes())
                    RemoveProviderAnnotations(index);

                foreach (var foreignKey in entityType.GetForeignKeys())
                    RemoveProviderAnnotations(foreignKey);
            }
        }

        private static void RemoveProviderAnnotations(IMutableAnnotatable annotatable)
        {
            foreach (var annotation in annotatable.GetAnnotations().Where(a => a.Name.StartsWith("MySql:", StringComparison.Ordinal)).ToArray())
                annotatable.RemoveAnnotation(annotation.Name);
        }

        private static bool IsSqliteAutoIncrementKey(IMutableProperty property)
        {
            Type clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            return property.IsPrimaryKey()
                && property.ValueGenerated == ValueGenerated.OnAdd
                && SqliteIntegerTypes.Contains(clrType);
        }
    }
}
