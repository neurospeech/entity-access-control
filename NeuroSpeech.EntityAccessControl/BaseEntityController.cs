using NeuroSpeech.EntityAccessControl.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NeuroSpeech;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using NeuroSpeech.EntityAccessControl.Internal;

namespace NeuroSpeech.EntityAccessControl
{
    public abstract class BaseEntityController: Controller
    {
        protected readonly ISecureRepository db;

        public BaseEntityController(ISecureRepository db)
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

        protected async Task<object> LoadOrCreateAsync(
            JsonElement body, bool isChild = false)
        {
            if(!body.TryGetStringProperty("$type", out var type))
            {
                throw new ArgumentNullException("$type cannot be null");
            }

            if (type.EqualsIgnoreCase("null"))
                return null!;

            object? e = null;
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entry = null;
            var t = FindEntityType(type);
            e = await db.FindByKeysAsync(t, body, HttpContext.RequestAborted);
            if(e != null)
            {
                entry = db.Entry(e);
            }
            bool insert = false;
            if (e == null)
            {
                e = Activator.CreateInstance(t.ClrType)!;
                insert = true;
            }
            var properties = t.GetProperties();
            var navProperties = t.GetNavigations();
            foreach(var p in body.EnumerateObject())
            {
                if(properties.TryGetFirst(p.Name, (x,name) => x.Name.EqualsIgnoreCase(name),out var property))
                {
                    property.PropertyInfo.SaveJsonOrValue(e, p.Value);
                    continue;
                }
                if(navProperties.TryGetFirst(p.Name, (x, name) => x.Name.EqualsIgnoreCase(name), out var navProperty))
                {
                    if(navProperty.IsCollection)
                    {
                        // what to do in collection...
                        // get or create...
                        var coll = (navProperty.PropertyInfo.GetOrCreate(e) as System.Collections.IList)!;
                        // this will be an array..
                        if(p.Value.ValueKind != JsonValueKind.Array)
                        {
                            throw new InvalidOperationException($"{p.Name} should be an Array");
                        }
                        foreach (var item in p.Value.EnumerateArray())
                        {
                            coll.Add(await LoadOrCreateAsync(item, true));
                        }
                        continue;
                    }
                    navProperty.PropertyInfo.SaveJsonOrValue(e, await LoadOrCreateAsync(p.Value, true));
                }
            }

            //foreach (var p in properties)
            //{
            //    if (p.IsKey())
            //    {
            //        if (body.TryGetPropertyCaseInsensitive(p.Name, out value))
            //        {
            //            p.PropertyInfo.SaveJsonOrValue(e, value);
            //        }
            //        continue;
            //    }
            //    if (body.TryGetPropertyCaseInsensitive(p.Name, out value))
            //    {
            //        p.PropertyInfo.SaveJsonOrValue(e, value);
            //    }
            //}
            //foreach(var np in t.GetNavigations())
            //{
            //    if (np.IsCollection)
            //        continue;
            //    if(body.TryGetPropertyCaseInsensitive(np.Name, out value))
            //    {
            //        var ce = await LoadOrCreateAsync(value, true);
            //        np.PropertyInfo.SetValue(e, ce);
            //    }
            //}
            if (insert && !isChild)
            {
                db.Add(e);
            }
            if(!body.TryGetProperty("$navigations", out var nav))  
                return e!;
            var clrType = t.ClrType;
            if (!(nav is JsonElement je))
                return e!;
            foreach (var n in je.EnumerateObject())
            {
                var nv = n.Value;
                var name = n.Name;

                var np = clrType.GetProperties().FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));
                if (np == null)
                {
                    throw new ArgumentOutOfRangeException($"{name} navigation property not found on {clrType}");
                }
                var navValue = np.GetValue(e);
                if (nv.TryGetPropertyCaseInsensitive("Add", out var add)) {
                    if (navValue == null)
                    {
                        navValue = np.PropertyType.CreateClassInstance()!;
                        np.SetValue(e, navValue);
                    }
                    foreach (var child in add.EnumerateArray())
                    {
                        navValue.InvokeMethod("Add", await LoadOrCreateAsync(child, true));
                    }
                    continue;
                }
                if(nv.TryGetPropertyCaseInsensitive("Remove", out var remove))
                {
                    if (entry != null) {
                        await entry.Navigation(np.Name).LoadAsync();
                    }
                    navValue = np.GetValue(e);
                    if (navValue == null)
                        continue;
                    foreach (var child in nv.EnumerateArray())
                    {
                        navValue.InvokeMethod("Remove", await LoadOrCreateAsync(child, true));
                    }
                    continue;
                }
                if (nv.TryGetPropertyCaseInsensitive("Clear", out var clear)) {
                    if (entry != null)
                    {
                        // load 
                        await entry.Navigation(np.Name).LoadAsync();
                    }
                    np.SetValue(e, null);
                    continue;
                }
                if(nv.TryGetPropertyCaseInsensitive("Set", out var @set))
                {
                    np.SetValue(e, await LoadOrCreateAsync(@set, true));
                }
            }
            return e!;

        }

        [HttpGet("model")]
        public IActionResult Entities()
        {
            var naming = JsonNamingPolicy.CamelCase;
            return Ok(db.Model.GetEntityTypes().Select(x => new {
                x.Name,
                Keys = x.GetProperties().Where(x => x.IsKey()).Select(p => new {
                    Name = naming.ConvertName(p.Name),
                    Type = (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType).FullName,
                    Identity = p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn
                }),
                Properties = x.GetProperties().Where(x => !x.IsKey()).Select(p => new {
                    Name = naming.ConvertName(p.Name),
                    Type = (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType).FullName,
                    p.IsNullable
                }),
                NavigationProperties = x.GetNavigations().Select(np => new {
                    Name = naming.ConvertName(np.Name),
                    np.IsCollection,
                    Type = np.TargetEntityType.ClrType.FullName
                })
            }));
        }

        [HttpGet("model.ts")]
        public IActionResult ModelDeclaration()
        {
            static string ModelName(string n)
            {
                return n.Split('.').Last();
            }

            var naming = JsonNamingPolicy.CamelCase;
            var sw = new System.IO.StringWriter();
            var i = new IndentedTextWriter(sw);

            i.WriteLine(@"import DateTime from ""@web-atoms/date-time/dist/DateTime"";
import IClrEntity from ""./entity/IClrEntity"";
export class Model<T extends IClrEntity> {
    constructor(public name: string) {}
}");

            foreach(var e in db.Model.GetEntityTypes())
            {

                var name = ModelName(e.Name);
                var b = e.BaseType == null ? "ClrEntity" : ModelName(e.BaseType.Name);
                i.WriteLine($"export interface I{name} extends I{b} {{");
                i.Indent++;
                foreach(var  p in e.GetDeclaredProperties())
                {
                    var type = p.ClrType.ToTypeScript();
                    if (p.IsNullable)
                    {
                        type += " | null";
                    }
                    i.WriteLine($"{naming.ConvertName(p.Name)}?: {type};");
                }
                foreach(var np in e.GetDeclaredNavigations())
                {
                    var npName = $"I{ModelName(np.TargetEntityType.Name)}";
                    if (np.IsCollection)
                    {
                        i.WriteLine($"{naming.ConvertName(np.Name)}?: {npName}[];");
                        continue;
                    } 
                    i.WriteLine($"{naming.ConvertName(np.Name)}?: {npName};");
                }
                i.Indent--;
                i.WriteLine("}");
                i.WriteLine();
                if (e.IsOwned())
                    continue;
                i.WriteLine($"export const {name} = new Model<I{name}>(\"{e.Name}\");");
            }


            return Content(sw.ToString(),"text/plain");
        }

        [HttpPut]
        [HttpPost]
        public async Task<IActionResult> Save(
            [FromBody] JsonElement body
            )
        {
            var e = await LoadOrCreateAsync(body);
            await db.SaveChangesAsync();
            return Ok(Serialize(e));
        }

        private static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";

        protected virtual object? Serialize(object? e, List<object> added = null!)
        {
            if (e == null)
                return null;
            added ??= new List<object>();
            var existingIndex = added.IndexOf(e);
            if(existingIndex!= -1)
            {
                return new Dictionary<string, object> {
                    { "$id", existingIndex }
                };
            }
            var index = added.Count;
            added.Add(e);
            var r = new Dictionary<string, object>() {
                { "$id", index }
            };
            var d = db.Entry(e);
            r["$type"] = d.Metadata.Name;
            foreach(var p in e.GetType().GetProperties())
            {
                var name = JsonNamingPolicy.CamelCase.ConvertName(p.Name);
                if (p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
                    continue;
                var v = p.GetValue(e);
                if (v == null)
                    continue;
                if (v is not string && v is System.Collections.IEnumerable coll) {
                    var list = new List<object>();
                    foreach(var c in coll)
                    {
                        var jc = this.Serialize(c, added);
                        if (jc != null)
                        {
                            list.Add(jc);
                        }
                    }
                    r[name] = list;
                    continue;
                }
                var t = p.PropertyType;
                t = Nullable.GetUnderlyingType(t) ?? t;
                if(t.IsValueType || t == typeof(string))
                {
                    switch(v)
                    {
                        case DateTime dt:
                            r[name] = dt.ToString(DateFormat);
                            continue;
                        case DateTimeOffset dto:
                            r[name] = dto.UtcDateTime.ToString(DateFormat);
                            continue;
                    }
                    r[name] = v;
                    continue;
                }
                var sv = Serialize(v, added);
                if (sv != null)
                {
                    r[name] = sv;
                }
            }
            return r;
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(
            [FromBody] JsonElement entity
            )
        {
            var t = FindEntityType(entity);
            var d = await db.FindByKeysAsync(t, entity);
            if (d != null)
            {
                db.Remove(d);
            }
            await db.SaveChangesAsync();
            return Ok();
        }

        private static readonly Dictionary<string, IEntityType> entityTypes = new();

        private IEntityType FindEntityType(in JsonElement e)
        {
            if (!e.TryGetStringProperty("$type", out var p))
                throw new ArgumentException($"$type is missing in the object");
            return FindEntityType(p);
        }

        private IEntityType FindEntityType(string? type)
        {
            if (type == null)
                throw new ArgumentNullException($"Type cannot be null");
            var e = entityTypes.GetOrCreate(type, type =>
             db.Model.GetEntityTypes().FirstOrDefault(x => x.Name.EqualsIgnoreCase(type)));
            if (e == null)
                throw new ArgumentOutOfRangeException($"Entity {type} not found");
            return e;
        }

        #region Not Used
        //[HttpPut("relation")]
        //public async Task<IActionResult> AddRelation(
        //    [FromBody] ClrEntityRelation relation
        //    )
        //{
        //    var pt = FindEntityType(relation.Entity.Type);
        //    var ct = FindEntityType(relation.NavigationValue.Type);
        //    var parent = await db.FindByKeysAsync(pt, relation.Entity.Keys);
        //    var child = await db.FindByKeysAsync(ct, relation.NavigationValue.Keys);
        //    var entry = db.Entry(parent);
        //    var n = entry.Navigations.FirstOrDefault(x => x.Metadata.Name == relation.NavigationName);
        //    if (n.Metadata.IsCollection)
        //    {

        //    }
        //    else
        //    {
        //        n.CurrentValue = child;
        //    }
        //    await db.SaveChangesAsync();
        //    return Ok();
        //}

        //[HttpDelete("relation")]
        //public async Task<IActionResult> RemoveRelation(
        //    [FromBody] ClrEntityRelation relation
        //    )
        //{
        //    var pt = db.Model.GetEntityTypes().FirstOrDefault(x => x.Name.EqualsIgnoreCase(relation.Entity.Type));
        //    var ct = db.Model.GetEntityTypes().FirstOrDefault(x => x.Name.EqualsIgnoreCase(relation.NavigationValue.Type));
        //    var parent = await db.FindByKeysAsync(pt, relation.Entity.Keys);
        //    var child = await db.FindByKeysAsync(ct, relation.NavigationValue.Keys);
        //    var entry = db.Entry(parent);
        //    var n = entry.Navigations.FirstOrDefault(x => x.Metadata.Name == relation.NavigationName);
        //    await n.LoadAsync();
        //    if (n.Metadata.IsCollection)
        //    {
        //        // remove...
        //        var coll = n.Metadata.PropertyInfo.GetValue(entry) as System.Collections.IList;
        //        coll.Remove(entry);
        //    }
        //    else
        //    {
        //        var prop = n.Metadata.PropertyInfo;
        //        if (prop.GetValue(entry) == child)
        //        {
        //            n.Metadata.PropertyInfo.SetValue(entry, null);
        //        }
        //    }
        //    await db.SaveChangesAsync();
        //    return Ok();
        //} 
        #endregion

        private static readonly object[] Empty = new object[0];

        private static readonly object EmptyResult = new { 
            items = Empty,
            total = 0
        };

        private static readonly List<string> EmptyStringArray = new();

        [HttpGet("query/{entity}")]
        public async Task<IActionResult> Query(
            [FromRoute] string entity,
            [FromQuery] string? select = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? parameters = null,
            [FromQuery] string? keys = null,
            [FromQuery] string? include = null,
            [FromQuery] string? orderBy = null,
            [FromQuery] int start = 0,
            [FromQuery] int size = 200
            )
        {

            var t = FindEntityType(entity);

            var r = this.GetType()
                .GetMethod(nameof(ListAsync))!
                .MakeGenericMethod(t.ClrType)
                .Invoke(this, new object?[] {
                    t,
                    select,
                    filter,
                    parameters,
                    keys,
                    include,
                    orderBy,
                    start,
                    size }) as Task<IActionResult>;
            return await r!;
        }

        private static readonly ParsingConfig config = new()
        {
            ResolveTypesBySimpleName = true
        };

        public async Task<IActionResult> ListAsync<T>(
            IEntityType t,
            string? select,
            string? filter, 
            string? parameters, 
            string? keys,
            string? include,
            string? orderBy, 
            int start, int size)
            where T : class
        {
            var q = db.Query<T>();

            var includeList = ParseInclude(include);
            List<string>? ParseInclude(string? tx)
            {
                tx = tx?.Trim('[', ' ', '\t', '\r', '\n', ']');
                if (string.IsNullOrWhiteSpace(tx))
                    return null;
                var includeKeys = new List<string>();
                foreach (var token in tx.Split(',', ';'))
                {
                    var x = token.Trim('"',' ', '\r', '\t' , '\n');
                    var key = "";
                    while (x.Length > 0)
                    {
                        int index = x.IndexOf('.');
                        if (index == -1)
                        {
                            key += t.GetNavigations().First(n => n.Name.EqualsIgnoreCase(x)).Name;
                            includeKeys.Add(key);
                            break;
                        }
                        var left = x.Substring(0, index);
                        x = x.Substring(index + 1);
                        var leftProperty = t.GetNavigations().First(x => x.Name.EqualsIgnoreCase(left));
                        t = leftProperty.TargetEntityType;
                        key += leftProperty.Name + ".";
                    }
                }
                return includeKeys;
            }

            if (keys != null)
            {
                var type = typeof(T);
                var pe = Expression.Parameter(type);
                Expression? body = null;
                foreach (var key in JsonSerializer.Deserialize<JsonElement>(keys).EnumerateObject())
                {
                    var property = type.GetProperties().FirstOrDefault(x => x.Name.EqualsIgnoreCase(key.Name));
                    var compare = Expression.Equal(
                            Expression.Property(pe, property),
                            Expression.Constant(key.Value.DeserializeJsonElement(property.PropertyType)));
                    body = body == null ? compare : Expression.AndAlso(body, compare);
                }
                q = q.Where(Expression.Lambda(body, pe));
            }
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (filter.StartsWith("["))
                {
                    var filters = JsonSerializer.Deserialize<string[]>(filter);
                    var parameterArray = JsonSerializer.Deserialize<object[]>(parameters ?? "[]").JsonToNative();
                    for (int i = 0; i < filters.Length; i++)
                    {
                        filter = filters[i];
                        q = q.Where(config, filter, parameterArray[i] as object[]);
                    }
                }
                else
                {
                    q = q.Where(config, filter, JsonSerializer.Deserialize<object[]>(parameters ?? "[]").JsonToNative());
                }
            }
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                q = q.OrderBy(orderBy);
            }
            var original = q;
            bool pageResult = false;
            if (start > 0)
            {
                q = q.Skip(start);
                pageResult = true;
            }
            if (size > 0)
            {
                q = q.Take(size);
                pageResult = true;
            }

            var total = 0;

            if (pageResult)
            {
                total = await original.CountAsync();

            }

            if (includeList != null)
            {
                foreach(var i in includeList)
                {
                    q = q.Include(i);
                }
            }

            var json = new List<object>();
            if (!string.IsNullOrWhiteSpace(select))
            {
                var dl = await q.Select(config, select).ToDynamicListAsync();
                foreach(var item in dl)
                {
                    json.Add(item);
                }
            }
            else
            {
                var list = await q.ToListAsync(this.HttpContext.RequestAborted);
                foreach (var item in list)
                {
                    var s = Serialize(item);
                    if (s == null)
                        continue;
                    json.Add(s);
                }
            }

            return Json(new
            {
                items = json,
                total
            });
        }
    }
}

