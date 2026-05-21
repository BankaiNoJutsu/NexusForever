using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexusForever.Database.EntityFramework
{
    public static class MySqlPropertyBuilderExtensions
    {
        public static PropertyBuilder<uint> HasUnsignedIntColumn(this PropertyBuilder<uint> property, string columnName, uint? defaultValue = null)
        {
            property
                .HasColumnName(columnName)
                .HasColumnType("int(10) unsigned");

            if (defaultValue.HasValue)
                property.HasDefaultValue(defaultValue.Value);

            return property;
        }

        public static PropertyBuilder<float> HasFloatColumn(this PropertyBuilder<float> property, string columnName, float? defaultValue = null)
        {
            property
                .HasColumnName(columnName)
                .HasColumnType("float");

            if (defaultValue.HasValue)
                property.HasDefaultValue(defaultValue.Value);

            return property;
        }

        public static PropertyBuilder<TEnum> HasUnsignedTinyEnumColumn<TEnum>(this PropertyBuilder<TEnum> property, string columnName, byte? defaultValue = null)
            where TEnum : struct, Enum
        {
            property
                .HasColumnName(columnName)
                .HasColumnType("tinyint(3) unsigned")
                .HasConversion<EnumToNumberConverter<TEnum, byte>>();

            if (defaultValue.HasValue)
                property.HasDefaultValue((TEnum)Enum.ToObject(typeof(TEnum), defaultValue.Value));

            return property;
        }
    }
}
