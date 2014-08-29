using System;
using System.Data.SQLite;

namespace Infiks.IPhone
{
    public class Transaction : IDisposable
    {
        private readonly SQLiteTransaction _base;
        private bool _disposed = false;

        public Transaction(SQLiteTransaction transaction)
        {
            if (_base == null)
                throw new ArgumentNullException("transaction");

            _base = transaction;
        }

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        public void Commit()
        {
            _base.Commit();
        }

        /// <summary>
        /// Rolls back the active transaction.
        /// </summary>
        public void Rollback()
        {
            _base.Rollback();
        }

        ~Transaction()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                _base.Dispose();

            _disposed = true;
        }
    }
}
