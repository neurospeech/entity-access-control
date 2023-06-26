using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using NeuroSpeech.EntityAccessControl.Internal;
using System.Collections.Concurrent;
using System.Threading;
using System.Text.Json.Nodes;
using NetTopologySuite.Geometries;

namespace NeuroSpeech.EntityAccessControl
{
    public abstract class BaseController: Controller
    {

        public static bool IsDeleted(in JsonElement v)
        {
            return v.TryGetPropertyCaseInsensitive("$deleted", out var v1) && v1.ValueKind == JsonValueKind.True;
        }

        protected static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";

        protected readonly ISecureQueryProvider db;

        public BaseController(ISecureQueryProvider db)
        {
            this.db = db;
            db.EnforceSecurity = true;
        }

        //public virtual IQueryable<T?> GetQuery<T>() 
        //    where T: class
        //    => db.Query<T>();

        //protected IQueryable? GetQueryFor(IEntityType type)
        //{
        //    return typeof(BaseEntityController)
        //        .GetMethod(nameof(GetQuery))!
        //        .MakeGenericMethod(type.ClrType)!.Invoke(this, null) as IQueryable;
        //}

        private Dictionary<int, object> objects = new Dictionary<int, object>();

        protected virtual async Task<object> LoadOrCreateAsync(Type? type,
            JsonElement body, 
            bool isChild = false,
            CancellationToken cancellationToken = default)
        {
            IEntityType entityType;

            if (body.TryGetInt32Property("$id", out var id))
            {
                if(objects.TryGetValue(id, out var v))
                {
                    return v;
                }
            }

            if (body.TryGetStringProperty("$type", out var typeName))
            {
                if (typeName.EqualsIgnoreCase("null"))
                    return null!;
                entityType = FindEntityType(typeName);
                if (type == null || type.IsAssignableFrom(entityType.ClrType))
                {
                    // type = entityType.ClrType;
                }
                else
                {
                    // we can ignore $type in case of specialized one to one mapping
                    entityType = db.Model.FindEntityType(type);
                }
            }
            else
            {
                if (type == null)
                {
                    throw new EntityAccessException("No $type provided");
                }
                entityType = db.Model.FindEntityType(type);
            }



            var (e, exists) = await db.BuildOrLoadAsync(entityType, body, cancellationToken);
            if (!(exists || isChild))
            {
                db.Add(e);
            }
            
            await LoadPropertiesAsync(e, entityType, body);

            if (exists && IsDeleted(body))
            {
                db.Remove(e);
            }
            if (id > 0)
            {
                objects[id] = e;
            }
            return e;

        }

        protected virtual async Task LoadPropertiesAsync(object entity, IEntityType entityType, JsonElement model)
        {
            var clrType = entityType.ClrType;
            var readOnlyProperties = db.GetReadonlyProperties(clrType);
            foreach (var p in model.EnumerateObject())
            {
                var property = clrType.StaticCacheGetOrCreate(p.Name,
                    () => clrType.GetProperties().FirstOrDefault(x => x.Name.EqualsIgnoreCase(p.Name)));
                if (readOnlyProperties.Contains(property))
                    continue;
                if (property == null)
                {
                    continue;
                }
                if (p.Value.ValueKind == JsonValueKind.Null)
                {
                    property.SetValue(entity, null);
                    continue;
                }
                if(typeof(Geometry).IsAssignableFrom(property.PropertyType))
                {
                    property.SaveJsonOrValue(entity, p.Value);
                    continue;
                }
                if (
                    p.Value.ValueKind != JsonValueKind.Array
                    && p.Value.ValueKind != JsonValueKind.Object)
                {
                    property.SaveJsonOrValue(entity, p.Value);
                    continue;
                }
                var navProperty = clrType.StaticCacheGetOrCreate((p.Name, p.Name),
                    () => entityType.GetNavigations().FirstOrDefault(x => x.Name.EqualsIgnoreCase(p.Name)));

                PropertyInfo navPropertyInfo = navProperty.PropertyInfo;
                if (!navProperty.IsCollection)
                {
                    navPropertyInfo.SetValue(entity, await LoadOrCreateAsync(navPropertyInfo.PropertyType, p.Value, true));
                    continue;
                }

                // what to do in collection...
                var pt = navProperty.TargetEntityType.ClrType;

                // get or create...
                var coll = (navPropertyInfo.GetOrCreateCollection(entity, pt) as System.Collections.IList)!;
                // this will be an array..
                if (p.Value.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"{p.Name} should be an Array");
                }
                foreach (var item in p.Value.EnumerateArray())
                {
                    var child = await LoadOrCreateAsync(pt, item, true);
                    if (coll.IndexOf(child) == -1)
                    {
                        if (!IsDeleted(item))
                        {
                            coll.Add(child);
                        }
                    }

                }
            }
        }

        protected static readonly ConcurrentDictionary<string, IEntityType> entityTypes = new();

        protected IEntityType FindEntityType(in JsonElement e)
        {
            if (!e.TryGetStringProperty("$type", out var p))
                throw new ArgumentException($"$type is missing in the object");
            return FindEntityType(p);
        }

        protected IEntityType FindEntityType(string? type)
        {
            if (type == null)
                throw new ArgumentNullException($"Type cannot be null");
            var t = db.GetType();
            var e = t.StaticCacheGetOrCreate(type, () => db.Model.GetEntityTypes().FirstOrDefault(x => x.Name.EqualsIgnoreCase(type)));
            if (e == null)
                throw new ArgumentOutOfRangeException($"Entity {type} not found");
            return e;
        }


        protected virtual IActionResult Serialize(object? e)
        {
            if (e == null)
            {
                return Json(null);
            }
            var options = new EntitySerializationSettings(db).Options;
            return Json(e, options);
        }

        //protected virtual JsonArray? SerializeList<T>(List<T> items)
        //{
        //    var serializer = new EntityJsonSerializer(new EntitySerializationSettings
        //    {
        //        GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!),
        //        NamingPolicy = JsonNamingPolicy.CamelCase,
        //        GetIgnoreCondition = db.GetIgnoreCondition
        //    });
        //    var result = new JsonArray();
        //    foreach(var item in items)
        //    {
        //        result.Add(serializer.Serialize(item));
        //    }
        //    return result;
        //}

        protected static readonly object[] Empty = new object[0];

        protected static readonly object EmptyResult = new
        {
            items = Empty,
            total = 0
        };

        protected static readonly List<string> EmptyStringArray = new();
    }
}

