﻿namespace Zambon.Core.Database.Domain.Interfaces
{
    /// <summary>
    /// Represents database classes with int primary key.
    /// </summary>
    public interface IKeyed
    {

        /// <summary>
        /// Primary key of the entity
        /// </summary>
        int ID { get; set; }

    }
}