using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using NeuroSpeech.EntityAccessControl.Internal;
using System.Collections.Concurrent;
using System.Threading;
using System.Text.Json.Nodes;

namespace NeuroSpeech.EntityAccessControl
{
    public abstract class BaseController: Controller
    {

        protected static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";

        protected readonly ISecureQueryProvider db;

        public BaseController(ISecureQueryProvider db)
        {
            this.db = db;
        }

        public virtual IQueryable<T?> GetQuery<T>() 
            where T: class
            => db.Query<T>();

        protected IQueryable? GetQueryFor(IEntityType type)
        {
            return typeof(BaseEntityController)
                .GetMethod(nameof(GetQuery))!
                .MakeGenericMethod(type.ClrType)!.Invoke(this, null) as IQueryable;
        }

        protected virtual async Task<object> LoadOrCreateAsync(Type type,
            JsonElement body, 
            bool isChild = false,
            CancellationToken cancellationToken = default)
        {
            IEntityType t;
            if (body.TryGetStringProperty("$type", out var typeName))
            {
                t = FindEntityType(typeName);
                if (type.IsAssignableFrom(t.ClrType))
                {
                    type = t.ClrType;
                }
                else
                {
                    // we can ignore $type in case of specialized one to one mapping
                    t = db.Model.FindEntityType(type);
                }
            }
            else
            {
                t = db.Model.FindEntityType(type);
            }

            if (typeName.EqualsIgnoreCase("null"))
                return null!;

            object? e = null;
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entry = null;
            e = await db.FindByKeysAsync(t, body, cancellationToken);
            bool insert = false;
            if (e == null)
            {
                e = Activator.CreateInstance(type)!;
                insert = true;
            }

            if (insert && !isChild)
            {
                db.Add(e);
            }
            
            await LoadPropertiesAsync(e, t, body);

            if (!body.TryGetProperty("$navigations", out var nav))
                return e!;
            //var clrType = t.ClrType;
            //if (!(nav is JsonElement je))
            //    return e!;
            //foreach (var n in je.EnumerateObject())
            //{
            //    var nv = n.Value;
            //    var name = n.Name;

            //    var np = clrType.GetProperties().FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));
            //    if (np == null)
            //    {
            //        throw new ArgumentOutOfRangeException($"{name} navigation property not found on {clrType}");
            //    }
            //    var navValue = np.GetValue(e);
            //    if (nv.TryGetPropertyCaseInsensitive("Add", out var add)) {
            //        if (navValue == null)
            //        {
            //            navValue = np.PropertyType.CreateClassInstance()!;
            //            np.SetValue(e, navValue);
            //        }
            //        foreach (var child in add.EnumerateArray())
            //        {
            //            navValue.InvokeMethod("Add", await LoadOrCreateAsync(child, true));
            //        }
            //        continue;
            //    }
            //    if(nv.TryGetPropertyCaseInsensitive("Remove", out var remove))
            //    {
            //        if (entry != null) {
            //            await entry.Navigation(np.Name).LoadAsync();
            //        }
            //        navValue = np.GetValue(e);
            //        if (navValue == null)
            //            continue;
            //        foreach (var child in nv.EnumerateArray())
            //        {
            //            navValue.InvokeMethod("Remove", await LoadOrCreateAsync(child, true));
            //        }
            //        continue;
            //    }
            //    if (nv.TryGetPropertyCaseInsensitive("Clear", out var clear)) {
            //        if (entry != null)
            //        {
            //            // load 
            //            await entry.Navigation(np.Name).LoadAsync();
            //        }
            //        np.SetValue(e, null);
            //        continue;
            //    }
            //    if(nv.TryGetPropertyCaseInsensitive("Set", out var @set))
            //    {
            //        np.SetValue(e, await LoadOrCreateAsync(@set, true));
            //    }
            //}
            return e!;

        }

        protected virtual async Task LoadPropertiesAsync(object entity, IEntityType entityType, JsonElement model)
        {
            var clrType = entityType.ClrType;
            foreach (var p in model.EnumerateObject())
            {
                var property = clrType.StaticCacheGetOrCreate(p.Name,
                    () => clrType.GetProperties().FirstOrDefault(x => x.Name.EqualsIgnoreCase(p.Name)));
                if (property == null)
                {
                    continue;
                }
                if (p.Value.ValueKind == JsonValueKind.Null)
                {
                    property.SetValue(entity, null);
                    continue;
                }
                if (p.Value.ValueKind != JsonValueKind.Array && p.Value.ValueKind != JsonValueKind.Object)
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
                    if(coll.IndexOf(child) == -1)
                        coll.Add(child);
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
            var options = new EntitySerializationSettings {
                GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? ( x.IsAnonymous() ? x.Name : x.FullName!),
                NamingPolicy = JsonNamingPolicy.CamelCase,
                GetIgnoreCondition = db.GetIgnoreCondition
            }.Options;
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

