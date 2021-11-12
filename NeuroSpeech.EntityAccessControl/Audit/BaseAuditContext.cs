using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class IgnoreAuditAttribute: Attribute
    {

    }

    public abstract class BaseAuditContext {

        public BaseAuditContext()
        {

        }

        public void Register(DbContext db)
        {
            List<AuditEntry>? changes = new List<AuditEntry>();
            db.SavingChanges += (s, e) => {
                Begin(changes, db.ChangeTracker.Entries());
            };
            db.SavedChanges += async (s, e) =>
            {
                try
                {
                    SetKeys(changes!, db.ChangeTracker.Entries());
                    var copy = changes;
                    changes = new List<AuditEntry>();
                    await SaveAsync(copy!);
                } catch (Exception ex)
                {
                    OnError(ex);
                }
            };
        }

        protected abstract void OnError(Exception ex);
        protected abstract Task SaveAsync(List<AuditEntry> auditEntries);

        private static void SetKeys(List<AuditEntry> entries, IEnumerable<EntityEntry> enumerable)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var e in entries)
            {
                if (e.State == EntityState.Added)
                {
                    e.UpdateValues();
                    e.LastUpdate = now;
                }
            }
        }

        private List<AuditEntry> Begin(List<AuditEntry> changes, IEnumerable<EntityEntry> entries)
        {
            var now = DateTimeOffset.UtcNow;
            foreach(var e in entries)
            {
                switch (e.State)
                {
                    case EntityState.Added:
                    case EntityState.Modified:
                    case EntityState.Deleted:

                        if (e.Entity.GetType().GetCustomAttribute<IgnoreAuditAttribute>() != null)
                            continue;

                        var entry = New(e);
                        entry.LastUpdate = now;
                        changes.Add(entry);
                        break;
                }
            }
            return changes;
        }

        protected abstract AuditEntry New(EntityEntry e);
    }
    
}
