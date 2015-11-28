using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneFiftyOne.TearDrops.Repository
{
    /// <summary>
    /// Abstraction layer around Simple.Data.SimpleTransaction Represents a database transaction 
    /// </summary>
    public class RepositoryTransaction
    {
        protected Simple.Data.SimpleTransaction _transaction = null;

        /// <summary>
        /// Gets the Simple.Data transaction reference. Should only be used interal to the repositories.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        internal Simple.Data.SimpleTransaction Transaction
        {
            get
            {
                if (_transaction == null)
                    throw new Exception("Transaction must be created by calling BaseRepository.BeginTransaction()");

                return _transaction;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryTransaction"/> class.
        /// </summary>
        /// <param name="trans">The trans.</param>
        internal RepositoryTransaction(Simple.Data.SimpleTransaction trans)
        {
            _transaction = trans;
        }
    }
}
