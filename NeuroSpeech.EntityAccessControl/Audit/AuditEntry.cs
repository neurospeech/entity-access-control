using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl
{
    public class AuditEntry
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AuditEntry()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {

        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AuditEntry(EntityEntry e)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            State = e.State;
            this.e = e;
            Values = new List<AuditEntryPair>();
            SetValues(e.State == EntityState.Deleted);
            Table = e.Metadata.Name;
        }

        public string Table { get; set; }

        public string Key { get; set; }

        public DateTimeOffset LastUpdate { get; set; }

        public EntityState State { get; set; }
        public List<AuditEntryPair>? Values { get; set; }

        private EntityEntry? e;

        private void SetValues(bool setKeysOnly)
        {
            foreach (var key in e!.Properties)
            {
                var v = key.CurrentValue;
                if (setKeysOnly && !key.Metadata.IsKey())
                    continue;
                if (State == EntityState.Added)
                {
                    if (v == null)
                        continue;
                }
                var ov = key.OriginalValue;
                if (ov == null)
                {
                    if (v == null)
                        continue;
                }
                else
                {
                    if (v != null && ov.Equals(v))
                        continue;
                }
                Values!.Add(new AuditEntryPair {
                    Name = key.Metadata.Name,
                    Value = v,
                    OldValue = ov
                });
            }
        }

        internal void UpdateValues()
        {
            foreach(var key in e!.Properties)
            {
                var v = key.CurrentValue;
                if (key.Metadata.IsKey() && key.Metadata.GetValueGenerationStrategy() == Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.IdentityColumn)
                {
                    Values!.Insert(0, new AuditEntryPair { 
                        Name = key.Metadata.Name,
                        Value = v
                    });
                    continue;
                }
                if (key.CurrentValue != key.OriginalValue)
                {
                    Values!.Add(new AuditEntryPair
                    {
                        Name = key.Metadata.Name,
                        Value = v
                    });
                }
            }
        }
    }
    
}
