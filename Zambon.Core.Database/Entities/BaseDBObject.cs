﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zambon.Core.Database.ChangeTracker.Interfaces;
using Zambon.Core.Database.Domain.Interfaces;
using Zambon.Core.Database.Interfaces;

namespace Zambon.Core.Database.Entity
{
    /// <summary>
    /// Represents base database classes with primary key.
    /// </summary>
    public abstract class BaseDBObject : IDBObject, IConfigurableEntity, IValidatableObject, ITrackableEntity
    {
        /// <summary>
        /// Primary key of the entity.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }


        /// <summary>
        /// Called when executing OnConfiguring from CoreContext.
        /// </summary>
        /// <param name="entityBuilder">The object that can be used to configure a given entity type in the model.</param>
        public virtual void Configure(EntityTypeBuilder entityBuilder)
        {
            entityBuilder.Property<int>("ID").UseSqlServerIdentityColumn();

            entityBuilder.Property<int>("ID").ValueGeneratedOnAdd();
            entityBuilder.Property<int>("ID").Metadata.BeforeSaveBehavior = PropertySaveBehavior.Ignore;
        }

        /// <summary>
        /// Executed whether the object is being deleted.
        /// </summary>
        /// <param name="ctx">The current database context instance.</param>
        public virtual void OnDeleting(object[] args)
        {

        }

        /// <summary>
        /// Executed whether the object is being saved into database.
        /// </summary>
        /// <param name="ctx">The current database context instance.</param>
        public virtual void OnSaving(object[] args)
        {

        }

        /// <summary>
        /// Default validation method, gets called every time when submitting the object to the server. Determines whether the specified object is valid.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>Returns a list of invalid properties names and their respective errors descriptions.</returns>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new ValidationResult[] { };
        }

        /// <summary>
        /// Called when validating the object using the custom validation methods.
        /// </summary>
        /// <param name="ctx">The current database context instance.</param>
        /// <returns>Returns a list of invalid properties names and their respective errors descriptions.</returns>
        public virtual List<KeyValuePair<string, string>> ValidateData(object[] args)
        {
            return new List<KeyValuePair<string, string>>();
        }
    }
}