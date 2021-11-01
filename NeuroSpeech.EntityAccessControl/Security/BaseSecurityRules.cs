using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace NeuroSpeech.EntityAccessControl
{

    public abstract class BaseSecurityRules<TC>
    {
        private RulesDictionary select = new RulesDictionary();
        private RulesDictionary insert = new RulesDictionary();
        private RulesDictionary update = new RulesDictionary();
        private RulesDictionary delete = new RulesDictionary();

        internal IQueryable<T> Apply<T>(IQueryable<T> ts, TC client) where T : class
        {
            return select.As<T, TC>()(ts, client);
        }

        internal IQueryable<T> ApplyInsert<T>(IQueryable<T> q, TC client) where T : class
        {
            return insert.As<T, TC>()(q, client);
        }

        internal IQueryable<T> ApplyDelete<T>(IQueryable<T> q, TC client) where T : class
        {
            return delete.As<T, TC>()(q, client);
        }

        internal IQueryable<T> ApplyUpdate<T>(IQueryable<T> q, TC client) where T : class
        {
            return update.As<T,TC>()(q, client);
        }

        protected void SetAllFunc<T>(
            Func<IQueryable<T>, TC, IQueryable<T>>? select = null,
            Func<IQueryable<T>, TC, IQueryable<T>>? insert = null,
            Func<IQueryable<T>, TC, IQueryable<T>>? update = null,
            Func<IQueryable<T>, TC, IQueryable<T>>? delete = null)
        {
            this.select.SetFunc<T, TC>(select);
            this.insert.SetFunc<T, TC>(insert);
            this.update.SetFunc<T, TC>(update);
            this.delete.SetFunc<T, TC>(delete);
        }

        public void SetFilterForAll<T>(
            Func<IQueryable<T>, TC, IQueryable<T>> all)
        {
            SetAllFunc<T>(all, all, all, all);
        }


        public static IQueryable<T> Allow<T>(IQueryable<T> q, TC c) => q;

        public static IQueryable<T> Unauthorized<T>(IQueryable<T> q, TC c)
                   where T : class
                   => throw new UnauthorizedAccessException();
    }
}
