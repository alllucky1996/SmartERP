using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using FluentMigrator;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Domain;

namespace Smartstore.PriceBuilder.Migrations
{
    [MigrationVersion("2022-06-12 15:30:00", "Price builder: Initial")]
    internal class Initial : Migration
    {
        //const string blogPost = "BlogPost";
        //const string blogComment = "BlogComment";
        List<Type> types = new List<Type>()
        {
            typeof(string),
            typeof(int),
            typeof(DateTime)
        };

        List<string> orderTables = new List<string>()
        {
            nameof(BlogPost),
            nameof(BlogComment),
            nameof(PriceType),
            nameof(PriceAttribuite),
            nameof(PriceCompute),
            nameof(PriceApplyToProduct)
        };
        public override void Up()
        {

            const string id = nameof(BaseEntity.Id);
            var entities = typeof(BlogPost).GetTypeInfo().Assembly.GetTypes().Where(e => e.IsSubclassOf(typeof(BaseEntity)) && !e.IsAbstract && e.IsClass).ToList();
            var classEntities = entities.OrderBy(o => orderTables.IndexOf(o.Name)).ToList();
            foreach (var entity in classEntities)
            {
                var tableFix = entity.GetCustomAttribute(typeof(TableAttribute), false);
                var tableName = (tableFix as TableAttribute)?.Name ?? entity.Name;
                if (!Schema.Table(tableName).Exists())
                {
                    var props = entity
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(e =>
                                    e.Name != id
                                    && types.Contains(e.PropertyType)
                                    //    && e.GetCustomAttribute(typeof(ForeignKeyAttribute), false) == null
                                    || Nullable.GetUnderlyingType(e.PropertyType) != null
                                    ).ToList();

                    var p_foreignKeys = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(e => e.GetCustomAttribute(typeof(ForeignKeyAttribute), false) != null)
                        .ToList();
                    var dic_foreignKeys = new Dictionary<string, string>(); //id, table
                    if (p_foreignKeys != null && p_foreignKeys.Count() > 0)
                    {
                        foreach (var p in p_foreignKeys)
                        {
                            var key = p.GetCustomAttribute(typeof(ForeignKeyAttribute), false);
                            var name = (key as ForeignKeyAttribute)?.Name ?? "";
                            if (name.HasValue())
                            {
                                dic_foreignKeys.Add(name, p.PropertyType.Name);
                            }
                        }
                    }

                    var ctable = Create.Table(tableName).WithIdColumn();
                    foreach (var p in props)
                    {
                        var x = ctable.WithColumn(p.Name);
                        var _nullable_type = Nullable.GetUnderlyingType(p.PropertyType);
                        if (_nullable_type != null)
                        {
                            if (_nullable_type == typeof(int))
                            {
                                if (dic_foreignKeys.TryGetValue(p.Name, out var totableName))
                                {
                                    x.AsInt32().Nullable()
                                            .Indexed($"IX_{p.Name}")
                                            .ForeignKey(totableName, id).OnDelete(Rule.None);
                                }
                                else
                                {
                                    x.AsInt32().Nullable();
                                }
                            }
                            if (_nullable_type == typeof(DateTime))
                            {
                                x.AsDateTime2().Nullable();
                            }
                        }
                        else
                        {
                            if (p.PropertyType == typeof(string))
                            {
                                var requireds = p.GetCustomAttribute(typeof(RequiredAttribute), false);
                                if (requireds != null)
                                {
                                    x.AsString().NotNullable();
                                }
                                else
                                {
                                    x.AsString().Nullable();
                                }
                            }
                            if (p.PropertyType == typeof(int))
                            {
                                if (dic_foreignKeys.TryGetValue(p.Name, out var totableName))
                                {
                                    x.AsInt32().NotNullable()
                                           .Indexed($"IX_{p.Name}")
                                           .ForeignKey(totableName, id).OnDelete(Rule.None);
                                }
                                else
                                {
                                    x.AsInt32().NotNullable();
                                }
                            }
                            if (p.PropertyType == typeof(DateTime))
                            {
                                x.AsDateTime2().NotNullable();
                            }
                        }
                    }
                }
            }


            //foreach (var entity in classEntities)
            //{
            //    var tableFix = entity.GetCustomAttribute(typeof(TableAttribute), false);
            //    var tableName = (tableFix as TableAttribute)?.Name ?? entity.Name;

            //    var p_foreignKeys =  entity.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            //                   .Where(e => e.GetCustomAttribute(typeof(ForeignKeyAttribute), false) != null)
            //                   .ToList();

            //    if (p_foreignKeys != null && p_foreignKeys.Count() > 0)
            //    {
            //        foreach (var p in p_foreignKeys)
            //        {
            //            var key = p.GetCustomAttribute(typeof(ForeignKeyAttribute), false);
            //            var name = (key as ForeignKeyAttribute)?.Name ?? "";
            //            if (name.HasValue())
            //            {
            //                Create.ForeignKey().FromTable(tableName).ForeignColumn(name).ToTable(p.Name).PrimaryColumn(id);
            //            }
            //        }
            //    }
            //}
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave blog schema as it is or ask merchant to delete it.
            //const string blogPost = "BlogPost";
            //const string blogComment = "BlogComment";

            //if (Schema.Table(blogComment).Exists())
            //{
            //    Delete.Table(blogComment);
            //}
            //if (Schema.Table(blogPost).Exists())
            //{
            //    Delete.Table(blogPost);
            //}
            for (int i = orderTables.Count-1; i >= 0; i--)
            {
                if (Schema.Table(orderTables[i]).Exists())
                {
                    Delete.Table(orderTables[i]);
                }
            }
        }
    }
}
