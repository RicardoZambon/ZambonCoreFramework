﻿using System;

namespace Zambon.Core.Database.ChangeTracker.Services
{
    /// <summary>
    /// Service used to store and retrieve the instance key in user's session.
    /// </summary>
    public interface IInstanceKeyService
    {
        /// <summary>
        /// Stores the key in user current session.
        /// </summary>
        /// <param name="databaseInstanceKey">Unique key to the user.</param>
        void StoreKey(Guid databaseInstanceKey);

        /// <summary>
        /// Retrieves the current user instance key.
        /// </summary>
        /// <returns>Returns an object of type InstanceKey.</returns>
        InstanceKey RetrieveKey();
    }
}