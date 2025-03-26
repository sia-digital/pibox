using System.Globalization;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Testing.Assertions
{
    public record DbConfiguredModel(IModel Model)
    {
        public DbEntity<T> Get<T>() => new(Model.FindEntityType(typeof(T))!);

        public static DbConfiguredModel GetModel<TDbContextType>() where TDbContextType : DbContext
        {
            var modelBuilder = new ModelBuilder();

            var dbContext = Activator.CreateInstance(typeof(TDbContextType), new DbContextOptions<TDbContextType>()).As<TDbContextType>();
#pragma warning disable EF1001
            new ModelCustomizer(new()).Customize(modelBuilder, dbContext);
#pragma warning restore EF1001

            return new(modelBuilder.FinalizeModel());
        }
    }

    // ReSharper disable once UnusedTypeParameter
#pragma warning disable S2326
    public record DbEntity<T>(IEntityType EntityType);
#pragma warning restore S2326
    public static class DbAssertions
    {
        private static string ToUnderscoreCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? $"_{x}" : x.ToString()))
                .ToLower(CultureInfo.InvariantCulture);
        }

        public static void AssertTable<T>(this DbEntity<T> entity, string table, string schema = null)
        {
            entity.EntityType.GetTableName().Should().Be(table, "Table name does not match");
            if (!string.IsNullOrEmpty(schema))
                entity.EntityType.GetSchema().Should().Be(schema, "Table schema does not match");
        }

        public static void AssertPk<T>(this DbEntity<T> entity, Expression<Func<T, object>> selector)
        {
            var members = selector.GetMemberAccessList().Select(x => x.Name).ToList();
            var pk = entity.EntityType.FindPrimaryKey()!;
            pk.Should().NotBeNull("Entity has no primary key");
            pk.Properties.Should().HaveCount(members.Count, "Properties for primary key does not match");
            foreach (var member in members)
                pk.Properties.Should().Contain(x => x.Name == member, $"Member {member} does not exist in primary key");
        }

        public static void AssertIndex<T>(this DbEntity<T> entity, Expression<Func<T, object>> selector, bool isUnique = false)
        {
            var indexes = entity.EntityType.GetIndexes().ToList();
            var indexMembers = selector.GetMemberAccessList().Select(x => x.Name).ToList();
            var relatedIndex = indexes.First(index => indexMembers.All(i => index.Properties.Select(x => x.Name).Contains(i)));
            relatedIndex.Should().NotBeNull($"Could not find matching index for members {string.Join(",", indexMembers)}");
            relatedIndex.IsUnique.Should().Be(isUnique, $"Index does not match the unique predicate for members {string.Join(",", indexMembers)}");
        }

        public static void AssertProperty<T>(this DbEntity<T> entity,
            Expression<Func<T, object>> selector,
            bool isRequired = true,
            ValueGenerated valueGenerated = ValueGenerated.Never,
            int? maxLength = null,
            string columnName = null)
        {
            var propertyInfo = selector.GetPropertyAccess();
            propertyInfo.Should().NotBeNull("Could not find the property given by the selector");
            var dbProperty = entity.EntityType.GetProperty(propertyInfo.Name);
            dbProperty.Should().NotBeNull($"Could not find member {propertyInfo.Name} on the entity");
            dbProperty.ClrType.Should().Be(propertyInfo.PropertyType, $"{propertyInfo.Name} has different types");
            dbProperty.IsNullable.Should().Be(!isRequired, $"{propertyInfo.Name} does not match the required check");
            dbProperty.GetColumnName().Should().NotBeNull($"{propertyInfo.Name} has invalid or no name");
            dbProperty.GetColumnName().Should().Be(columnName ?? propertyInfo.Name.ToUnderscoreCase(), $"{propertyInfo.Name} has invalid or no name");
            dbProperty.ValueGenerated.Should().Be(valueGenerated, $"{propertyInfo.Name} does not match on the value generation");
            if (maxLength is not null)
                dbProperty.GetMaxLength().Should().Be(maxLength, $"{propertyInfo.Name} does mismatch on the max length");
        }

        public static void AssertFk<T, TForeignType>(this DbEntity<T> entity, Expression<Func<T, object>> fkPropertySelector,
            Expression<Func<TForeignType, object>> pkPropertySelector)
        {
            var keyPropertyName = fkPropertySelector.GetPropertyAccess().Name;
            var foreignKeyPropertyName = pkPropertySelector.GetPropertyAccess().Name;
            var fk = entity.EntityType.GetForeignKeys().First(x => x.DeclaringEntityType.ClrType == typeof(T) && x.PrincipalEntityType.ClrType == typeof(TForeignType))!;
            fk.Should().NotBeNull($"Could not find a matching foreign key for member {keyPropertyName}");
            fk.Properties.Should().HaveCount(1, $"Foreign key for {keyPropertyName} has multiple properties");
            fk.Properties[0].Name.Should().Be(keyPropertyName, $"Foreign key for {keyPropertyName} has different properties");
            fk.PrincipalKey.Properties.Should().HaveCount(1, $"Foreign key for {keyPropertyName} has an invalid property");
            fk.PrincipalKey.Properties[0].Name.Should().Be(foreignKeyPropertyName, $"Foreign key for {keyPropertyName} has an invalid property");
        }

        public static void AssertFk<T, TForeignType>(this DbEntity<T> entity, Expression<Func<T, object>> fkPropertySelector) where TForeignType : IGuidIdentifier =>
            AssertFk<T, TForeignType>(entity, fkPropertySelector, t => t.Id);

        public static void AssertData<T>(this DbEntity<T> entity, Expression<Func<T, object>> propertyForCheck, params object[] values)
        {
            var memberName = propertyForCheck.GetPropertyAccess().Name;
            var data = entity.EntityType.GetSeedData()
                .Where(x => x.ContainsKey(memberName) && x[memberName] != null)
                .Select(x => x[memberName])
                .ToList();
            data.Count.Should().Be(values.Length, "Expected Data does not match tested data in length");
            foreach (var value in values)
                data.Should().Contain(value, $"Could not find value in seed data for value {value}");
        }

        public static void AssertData<T>(this DbEntity<T> entity, params T[] values)
        {
            var expected = values.Select(v => typeof(T).GetProperties().ToDictionary(x => x.Name, x => x.GetValue(v))).ToList();
            var list = entity.EntityType.GetSeedData().ToList();
            expected.Count.Should().Be(list.Count, $"{typeof(T).Name} seed data and expected data does not match in length");
            for (var i = 0; i < expected.Count; i++)
            {
                var dict = expected[i];
                var seedData = list[i];
                dict.Should().BeEquivalentTo(seedData, new KvpStringObjectComparer(), $"{typeof(T).Name} has an invalid data entry");
            }
        }
    }
}
