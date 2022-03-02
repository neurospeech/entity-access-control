﻿using Microsoft.AspNetCore.Mvc;
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
using System.Text.Json;
using System.Threading.Tasks;
using NeuroSpeech.EntityAccessControl.Internal;
using System.Threading;
using System.Text.Json.Nodes;
using NeuroSpeech.EntityAccessControl.Parser;
using Microsoft.AspNetCore.Http;

namespace NeuroSpeech.EntityAccessControl
{

    public abstract class BaseEntityController : BaseController
    {

        public BaseEntityController(ISecureQueryProvider db) : base(db)
        {
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
import type IClrEntity from ""@web-atoms/entity/dist/models/IClrEntity"";
import type { ICollection, IModel } from ""@web-atoms/entity/dist/services/BaseEntityService"";
export class Model<T extends IClrEntity> {
    constructor(public name: string) {}
}");

            var enumTypes = new List<Type>();

            foreach (var e in db.Model.GetEntityTypes())
            {

                var name = ModelName(e.Name);
                var b = e.BaseType == null ? "ClrEntity" : ModelName(e.BaseType.Name);
                i.WriteLine($"export interface I{name} extends I{b} {{");
                i.Indent++;
                foreach (var p in e.GetDeclaredProperties())
                {
                    var clrType = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                    if (clrType.IsEnum) {
                        if (!enumTypes.Contains(clrType))
                            enumTypes.Add(clrType);
                        var typeName = $"IEnum{clrType.Name}";
                        if (p.IsNullable)
                        {
                            typeName += " | null";
                        }
                        i.WriteLine($"{naming.ConvertName(p.Name)}?: {typeName};");
                        continue;
                    }
                    var type = p.ClrType.ToTypeScript();
                    if (p.IsNullable)
                    {
                        type += " | null";
                    }
                    i.WriteLine($"{naming.ConvertName(p.Name)}?: {type};");
                }
                foreach (var np in e.GetDeclaredNavigations())
                {
                    var npName = $"I{ModelName(np.TargetEntityType.Name)}";
                    if (np.IsCollection)
                    {
                        i.WriteLine($"{naming.ConvertName(np.Name)}?: ICollection<{npName}>;");
                        continue;
                    }
                    i.WriteLine($"{naming.ConvertName(np.Name)}?: {npName};");
                }
                i.Indent--;
                i.WriteLine("}");
                i.WriteLine();

                if (e.IsOwned())
                    continue;
                i.WriteLine($"export const {name}: IModel<I{name}> = new Model<I{name}>(\"{e.Name}\");");
            }

            foreach (var enumType in enumTypes)
            {
                var names = string.Join(" | ", enumType.GetEnumNames().Select(x => $"\"{x}\""));
                i.WriteLine($"export type IEnum{enumType.Name} = {names};");
            }
            i.WriteLine();

            return Content(sw.ToString(), "text/plain");
        }

        [HttpPut]
        [HttpPost]
        public async Task<IActionResult> Save(
            [FromBody] JsonElement body
            )
        {
            try
            {
                db.EnforceSecurity = true;
                if (body.ValueKind == JsonValueKind.Array)
                    return await SaveMultiple(body);
                if (!body.TryGetStringProperty("$type", out var typeName))
                    throw new KeyNotFoundException($"$type not found");
                var t = FindEntityType(typeName);
                var e = await LoadOrCreateAsync(t.ClrType, body);
                await db.SaveChangesAsync();
                return Json(Serialize(e));
            }catch (EntityAccessException eae)
            {
                if (eae.StackTrace!= null)
                    eae.ErrorModel.Add("Stack", eae.StackTrace);
                return this.UnprocessableEntity(eae.ErrorModel);
            }
        }


        [HttpPut("multiple")]
        [HttpPost("multiple")]
        public async Task<IActionResult> SaveMultiple(
            [FromBody] JsonElement model
            )
        {
            try
            {
                db.EnforceSecurity = true;
                List<object> results = new List<object>();
                foreach (var body in model.EnumerateArray())
                {
                    if (!body.TryGetStringProperty("$type", out var typeName))
                        throw new KeyNotFoundException($"$type not found");
                    var t = FindEntityType(typeName);
                    var result = await LoadOrCreateAsync(t.ClrType, body);
                    results.Add(result);
                }
                await db.SaveChangesAsync();
                return Json(SerializeList(results));
            }catch (EntityAccessException ex)
            {
                if (ex.StackTrace != null)
                    ex.ErrorModel.Add("Stack", ex.StackTrace);
                return this.UnprocessableEntity(ex.ErrorModel);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(
            [FromBody] JsonElement entity
            )
        {
            try
            {
                db.EnforceSecurity = true;
                var t = FindEntityType(entity);
                var d = await db.FindByKeysAsync(t, entity);
                if (d != null)
                {
                    db.Remove(d);
                }
                await db.SaveChangesAsync();
                return Ok(new { });
            } catch( EntityAccessException ex)
            {
                if (ex.StackTrace != null)
                    ex.ErrorModel.Add("Stack", ex.StackTrace);
                return this.UnprocessableEntity(ex.ErrorModel);
            }
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> BulkDelete(
            [FromBody] BulkOperation model,
            CancellationToken cancellation
            )
        {
            db.EnforceSecurity = true;
            if (model.Keys == null)
                return BadRequest();
            try
            {
                foreach (var item in model.Keys)
                {
                    var t = FindEntityType(item);
                    var entity = await db.FindByKeysAsync(t, item, cancellation);
                    if (entity == null)
                    {
                        if (model.ThrowWhenNotFound)
                        {
                            return BadRequest();
                        }
                        continue;
                    }
                    db.Remove(entity);
                }

                await db.SaveChangesAsync();
                return Ok(new { });
            }catch (EntityAccessException ex)
            {
                if (ex.StackTrace != null)
                    ex.ErrorModel.Add("Stack", ex.StackTrace);
                return UnprocessableEntity(ex.ErrorModel);
            }
        }

        public class BulkOperation
        {
            public JsonElement[]? Keys { get; set; }
            public JsonElement Update { get; set; }

            public bool ThrowWhenNotFound { get; set; }
        }

        [HttpPut("bulk")]
        public async Task<IActionResult> BulkSave(
            [FromBody] BulkOperation model,
            CancellationToken cancellation
            )
        {
            db.EnforceSecurity = true;
            if (model.Keys == null
                || model.Update.ValueKind == JsonValueKind.Undefined
                || model.Update.ValueKind == JsonValueKind.Null)
                return BadRequest();

            try
            {
                foreach (var item in model.Keys)
                {
                    var t = FindEntityType(item);
                    var entity = await db.FindByKeysAsync(t, item, cancellation);
                    if (entity == null)
                    {
                        if (model.ThrowWhenNotFound)
                        {
                            return BadRequest();
                        }
                        continue;
                    }
                    await LoadPropertiesAsync(entity, t, model.Update);
                }
                await db.SaveChangesAsync();
                return Ok(new { });
            }catch (EntityAccessException eae)
            {
                if (eae.StackTrace != null)
                    eae.ErrorModel.Add("Stack", eae.StackTrace);
                return UnprocessableEntity(eae.ErrorModel);
            }
        }

        public class MethodOptions
        {
            public string? Methods { get; set; }
            public int Start { get; set; } = 0;
            public int Size { get; set; } = 200;
            public bool SplitInclude { get; set; } = true;
            public bool Trace { get; set; }

            public int CacheSeconds { get; set; }
        }

        [HttpGet("methods/{entity}")]
        public Task<IActionResult> Methods(
            [FromRoute] string entity,
            [FromQuery] string methods,
            [FromQuery] int start = 0,
            [FromQuery] int size = 200,
            [FromQuery] bool splitInclude = true,
            [FromQuery] bool trace = false,
            CancellationToken cancellationToken = default
            )
        {
            return PostMethod(entity, new MethodOptions { 
                Methods = methods,
                Start = start,
                Size = size,
                SplitInclude = splitInclude,
                Trace = trace,
            }, cancellationToken);
        }

        [HttpGet("cached/{entity}/{methods}/{start}/{size}/{splitInclude}/{trace}/{cacheSeconds}")]
        public Task<IActionResult> CachedMethods(
            [FromRoute] string entity,
            [FromRoute] string methods,
            [FromRoute] int start = 0,
            [FromRoute] int size = 200,
            [FromRoute] bool splitInclude = true,
            [FromRoute] bool trace = false,
            [FromRoute] int cacheSeconds = 0,
            CancellationToken cancellationToken = default
        )
        {
            return PostMethod(entity, new MethodOptions
            {
                Methods = methods,
                Start = start,
                Size = size,
                SplitInclude = splitInclude,
                Trace = trace,
                CacheSeconds = cacheSeconds
            }, cancellationToken);
        }

        protected virtual void TraceQuery(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }


        [HttpPost("methods/{entity}")]
        public async Task<IActionResult> PostMethod(
            [FromRoute] string entity,
            [FromBody] MethodOptions model,
            CancellationToken cancellationToken = default
            )
        {
            var methods = model.Methods;
            var t = FindEntityType(entity);
            var hasSelect = false;
            var hasInclude = false;
            var options = new LinqMethodOptions();
            List<LinqMethod> methodList = new List<LinqMethod>();
            options.Methods = methodList;
            options.Start = model.Start;
            options.Size = model.Size;
            options.SplitInclude = model.SplitInclude;
            if (model.Trace)
            {
                options.Trace = TraceQuery;
            }

            var root = JsonDocument.Parse(methods).RootElement;

            foreach(var method in root.EnumerateArray())
            {
                LinqMethod lm = new LinqMethod();

                if (method.ValueKind != JsonValueKind.Array)
                {
                    throw new NotSupportedException($"Each method should be an array with format [name, query, ... parameters ]");
                }

                int n = method.GetArrayLength();
                if (n < 2)
                {
                    throw new ArgumentException($"Method Siganture must have two elements atleast");
                }

                string name = method[0].GetString()!;

                lm.Expression = method[1].GetString()!;

                for (int i = 2; i < n; i++)
                {
                    lm.Parameters.Add(new QueryParameter(method[i]));
                }

                switch (name)
                {
                    case "select":
                        lm.Method = "Select";
                        hasSelect = true;
                        break;
                    case "where":
                        lm.Method = "Where";
                        break;
                    case "orderBy":
                        lm.Method = "OrderBy";
                        break;
                    case "orderByDescending":
                        lm.Method = "OrderByDescending";
                        break;
                    case "thenBy":
                        lm.Method = "ThenBy";
                        break;
                    case "thenByDescending":
                        lm.Method = "ThenByDescending";
                        break;
                    case "include":
                        lm.Method = "Include";
                        hasInclude = true;
                        if (!lm.Expression.Contains("=>"))
                            lm.Expression = System.Text.Json.JsonSerializer.Serialize(lm.Expression);
                        break;
                    case "thenInclude":
                        lm.Method = "ThenInclude";
                        if (!lm.Expression.Contains("=>"))
                            lm.Expression = System.Text.Json.JsonSerializer.Serialize(lm.Expression);
                        break;
                    default:
                        continue;
                }

                methodList.Add(lm);
            }
            if (options.SplitInclude)
            {
                options.SplitInclude = hasInclude && !hasSelect;
            }
            var result = await this.GetInstanceGenericMethod(nameof(InvokeAsync), t.ClrType)
                .As<Task<IActionResult>>()
                .Invoke(options);
            if (model.CacheSeconds > 0) {
                Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue { 
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(model.CacheSeconds)
                };
            }
            return result;
        }

        public async Task<IActionResult> InvokeAsync<T>(
            LinqMethodOptions options)
            where T : class
        {
            var q = new QueryContext<T>(db, db.Query<T>()!, new ErrorModel());
            var result = await MethodParser.Instance.Parse<T>(q, options);
            var json = SerializeList(result.Items.ToList());
            return Json(new { 
                items = json,
                total = result.Total
            });
        }

        [HttpGet("query/{entity}")]
        public async Task<IActionResult> Query(
            [FromRoute] string entity,
            [FromQuery] string? select = null,
            [FromQuery] string? selectParameters = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? parameters = null,
            [FromQuery] string? keys = null,
            [FromQuery] string? include = null,
            [FromQuery] string? orderBy = null,
            [FromQuery] bool splitInclude = true,
            [FromQuery] int start = 0,
            [FromQuery] int size = 200
            )
        {

            var t = FindEntityType(entity);

            //this.GetInstanceGenericMethod(nameof(ListAsync), t.ClrType)
            //    .As<Task<IActionResult>>().Invoke()

            var r = this.GetType()
                .GetMethod(nameof(ListAsync))!
                .MakeGenericMethod(t.ClrType)
                .Invoke(this, new object?[] {
                    t,
                    select,
                    selectParameters,
                    filter,
                    parameters,
                    keys,
                    include,
                    orderBy,
                    splitInclude,
                    start,
                    size }) as Task<IActionResult>;
            return await r!;
        }

        private static readonly ParsingConfig config = new()
        {
            ResolveTypesBySimpleName = true
        };


        public async Task<IActionResult> ListAsync<T>(
            IEntityType entityType,
            string? select,
            string? selectParameters,
            string? filter, 
            string? parameters, 
            string? keys,
            string? include,
            string? orderBy, 
            bool splitInclude,
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
                    var scope = entityType;
                    while (x.Length > 0)
                    {
                        int index = x.IndexOf('.');
                        if (index == -1)
                        {
                            if (!scope.GetNavigations().TryGetFirst(n => n.Name.EqualsIgnoreCase(x), out var np))
                                throw new KeyNotFoundException($"No navigation property {x} found in {scope.Name}");
                            key += np.Name;
                            includeKeys.Add(key);
                            break;
                        }
                        var left = x.Substring(0, index);
                        x = x.Substring(index + 1);
                        if (!scope.GetNavigations().TryGetFirst(n => n.Name.EqualsIgnoreCase(left), out var leftProperty))
                            throw new KeyNotFoundException($"No navigation property {x} found in {scope.Name}");
                        scope = leftProperty.TargetEntityType;
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
                    if(!type.GetProperties().TryGetFirst(x => x.Name.EqualsIgnoreCase(key.Name), out var property))
                        throw new KeyNotFoundException($"No navigation property {key.Name} found in {type.Name}");
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
                    var parameterArray = JsonDocument.Parse(parameters ?? "[]").RootElement;
                    for (int i = 0; i < filters.Length; i++)
                    {
                        filter = filters[i];
                        q = await q.WhereLinqAsync(filter, parameterArray[i]);
                    }
                }
                else
                {
                    q = await q.WhereLinqAsync(filter, JsonDocument.Parse(parameters ?? "[]").RootElement);
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

                if (splitInclude)
                {
                    q = q.AsSplitQuery();
                }
            }

            var json = new JsonArray();
            if (!string.IsNullOrWhiteSpace(select))
            {
                var qc = new QueryContext<T>(db, q);
                var dl = await qc.SelectLinqAsync(select, JsonDocument.Parse(selectParameters ?? "[]").RootElement);
                foreach(var item in dl)
                {
                    json.Add(item);
                }
            }
            else
            {
                var list = await q.ToListAsync(this.HttpContext?.RequestAborted ?? default);
                json = SerializeList(list);
            }

            return Json(new
            {
                items = json,
                total
            });
        }
    }
}

