using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public static class TransactionExtensions
    {
        public class AsyncTransaction : IAsyncDisposable
        {
            private IDbContextTransaction? tx;

            public AsyncTransaction(IDbContextTransaction dbContextTransaction)
            {
                this.tx = dbContextTransaction;
            }

            public async ValueTask DisposeAsync()
            {
                if (tx != null)
                {
                    await tx.CommitAsync();
                }
            }

            public async ValueTask RollbackAsync()
            {
                await tx!.RollbackAsync();
                tx = null;
            }
        }

        public static async Task<AsyncTransaction> TransactionAsync(
            this DbContext context,
            CancellationToken token = default)
        {
            return new AsyncTransaction(await context.Database.BeginTransactionAsync(token));
        }
    }
}
