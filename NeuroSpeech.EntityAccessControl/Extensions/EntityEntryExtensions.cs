using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public static class EntityEntryExtensions
    {

        public class Selection<T, T2>
        {
            public T? Left { get; set; }  

            public IEnumerable<T2>? Right { get; set; }
        }

        public readonly struct QueryBuilder<T, TR>
            where T : class
            where TR : class
        {
            private readonly EntityEntry<T> entry;
            private readonly IQueryable<TR> rs;

            public QueryBuilder(EntityEntry<T> entry, IQueryable<TR> rs)
            {
                this.entry = entry;
                this.rs = rs;
            }

            public QueryBuilder<T, TR> With(Func<IQueryable<TR>,IQueryable<TR>> func)
            {
                return new QueryBuilder<T, TR> (entry, func(rs) );
            }

            public QueryBuilder<T,Selection<TR2,TR>> JoinReference<TR2>(
                Expression<Func<T,TR2?>> reference)
                where TR2 : class
            {
                var rs = this.rs;
                return new QueryBuilder<T, Selection<TR2, TR>>(entry,
                    entry.Reference(reference).Query()
                    .Select(x => new Selection<TR2, TR> {
                        Left = x
                        , Right = rs.ToList()
                    })
                );
            }

            public QueryBuilder<T, Selection<TR2, TR>> JoinCollection<TR2>(
                Expression<Func<T, IEnumerable<TR2>>> collection)
                where TR2 : class
            {
                var rs = this.rs;
                return new QueryBuilder<T, Selection<TR2, TR>>(entry,
                    entry.Collection(collection).Query()
                    .Select(x => new Selection<TR2, TR>
                    {
                        Left = x
                        ,
                        Right = rs.ToList()
                    })
                );
            }


            public Task LoadAsync()
            {
                return rs.LoadAsync();
            }
        }

        public static QueryBuilder<T,TR> BuildReferenceQuery<T, TR>(
            this EntityEntry<T> entry, Expression<Func<T,TR?>> reference)
            where T : class
            where TR: class
        {
            return new QueryBuilder<T, TR>(entry, entry.Reference(reference).Query());
        }

        public static QueryBuilder<T, TR> BuildCollectionQuery<T, TR>(
            this EntityEntry<T> entry, Expression<Func<T, IEnumerable<TR>?>> collection)
            where T : class
            where TR : class
        {
            return new QueryBuilder<T, TR>(entry, entry.Collection(collection).Query());
        }
    }
}
