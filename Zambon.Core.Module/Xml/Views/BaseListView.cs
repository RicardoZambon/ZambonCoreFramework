﻿using Microsoft.EntityFrameworkCore;
using Zambon.Core.Database;
using Zambon.Core.Database.ChangeTracker.Extensions;
using Zambon.Core.Database.Extensions;
using Zambon.Core.Database.Interfaces;
using Zambon.Core.Module.ExtensionMethods;
using Zambon.Core.Module.Interfaces;
using Zambon.Core.Module.Services;
using Zambon.Core.Module.Xml.Views.ListViews.Columns;
using Zambon.Core.Module.Xml.Views.ListViews.Search;
using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Xml.Serialization;

namespace Zambon.Core.Module.Xml.Views
{
    /// <summary>
    /// Represents base properties for nodes of <DetailView></DetailView> or <ListView></ListView> from XML Application Model.
    /// </summary>
    public abstract class BaseListView : BaseView, ICriteria
    {
        /// <summary>
        /// The Criteria attribute from XML. Criteria to use when displaying the records, can reference arguments by @0, @1, etc.
        /// </summary>
        [XmlAttribute("Criteria")]
        public string Criteria { get; set; }

        /// <summary>
        /// The CriteriaArguments attribute from XML. Criteria arguments to use when displaying the records, should be separated by ",".
        /// </summary>
        [XmlAttribute("CriteriaArguments")]
        public string CriteriaArguments { get; set; }

        /// <summary>
        /// The FromSql attribute from XML. Custom SQL command to execute when returning the objects.
        /// </summary>
        [XmlAttribute("FromSql")]
        public string FromSql { get; set; }

        /// <summary>
        /// The Sort attribute from XML. Sort condition to apply to object, separate properties by using ",". If needed to show descent add DESC. Ex: ID desc, Name desc.
        /// </summary>
        [XmlAttribute("Sort")]
        public string Sort { get; set; }


        /// <summary>
        /// List all search properties should be possible to search.
        /// </summary>
        [XmlIgnore]
        public SearchProperty[] SearchProperties { get { return _SearchProperties?.SearchProperty; } }

        /// <summary>
        /// List all columns.
        /// </summary>
        [XmlIgnore]
        public Column[] Columns { get { return _Columns?.Column; } }


        /// <summary>
        /// The PaginationOptions element from XML.
        /// </summary>
        [XmlElement("SearchProperties"), Browsable(false)]
        public SearchPropertiesArray _SearchProperties { get; set; }

        /// <summary>
        /// The Columns element from XML.
        /// </summary>
        [XmlElement("Columns"), Browsable(false)]
        public ColumnsArray _Columns { get; set; }


        /// <summary>
        /// The active search options.
        /// </summary>
        [XmlIgnore]
        public SearchOptions SearchOptions { get; protected set; }

        /// <summary>
        /// The active list of items being displayed in this list view. Only available when listing the items in page.
        /// </summary>
        [XmlIgnore]
        public IQueryable ItemsCollection { get; protected set; }


        #region Overrides

        internal override void OnLoadingXml(Application app, CoreDbContext ctx)
        {
            base.OnLoadingXml(app, ctx);

            if (string.IsNullOrWhiteSpace(Sort))
                Sort = Entity.GetDefaultProperty();

            if (string.IsNullOrWhiteSpace(FromSql) && !string.IsNullOrWhiteSpace(Entity.FromSql))
                FromSql = Entity.FromSql;

            if ((SearchProperties?.Length ?? 0) > 0)
            {
                Array.Sort(SearchProperties);
                for (var s = 0; s < SearchProperties.Length; s++)
                    if (string.IsNullOrWhiteSpace(SearchProperties[s].DisplayName))
                        SearchProperties[s].DisplayName = Entity?.GetPropertyDisplayName(SearchProperties[s].PropertyName); ;
            }

            if ((Columns?.Length ?? 0) > 0)
            {
                Array.Sort(Columns);
                for (var s = 0; s < Columns.Length; s++)
                    if (string.IsNullOrWhiteSpace(Columns[s].DisplayName))
                        Columns[s].DisplayName = Entity?.GetPropertyDisplayName(Columns[s].PropertyName);
            }
        }

        #endregion


        #region Methods

        /// <summary>
        /// Return the base list view contents.
        /// </summary>
        /// <typeparam name="T">The type of the ListView.</typeparam>
        /// <param name="app">The application service.</param>
        /// <param name="ctx">The CoreDbContext service.</param>
        /// <param name="searchOptions">If applyng search, otherwise null.</param>
        /// <returns>Returns the IQueryable list.</returns>
        protected IQueryable<T> GetPopulatedView<T>(ApplicationService app, CoreDbContext ctx, SearchOptions searchOptions = null) where T : class
        {
            //Todo: Validate if still needed. GC.Collect();
            SearchOptions = searchOptions;
            var list = GetItemsList<T>(app, ctx);

            if (searchOptions?.HasSearch() ?? false)
            {
                var searchProperty = Array.Find(SearchProperties, x => x.PropertyName == searchOptions.SearchProperty);
                if (searchProperty != null)
                {
                    searchOptions.SearchType = searchProperty.Type;
                    searchOptions.ComparisonType = searchProperty.Comparison;
                    list = searchOptions.SearchList(list);
                }
            }

            if (!string.IsNullOrWhiteSpace(Sort))
                list = list.OrderBy(Sort);

            return list;
        }


        /// <summary>
        /// Get the value of a property.
        /// </summary>
        /// <param name="app">The application service.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>Returns the property value.</returns>
        public object GetCellValue(ApplicationService app, string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(propertyName) && (Columns?.Length ?? 0) > 0)
            {
                var column = Array.Find(Columns, x => x.PropertyName == propertyName);
                if (column != null)
                    return GetCellValue(app, column);
            }
            return null;
        }
        /// <summary>
        /// Get the value of a column.
        /// </summary>
        /// <param name="app">The application service.</param>
        /// <param name="column">The column to get the value.</param>
        /// <returns>Returns the column value.</returns>
        public object GetCellValue(ApplicationService app, Column column)
        {
            return GetType().GetMethods().FirstOrDefault(x => x.Name == nameof(GetCellValue) && x.IsGenericMethod).MakeGenericMethod(Entity.GetEntityType()).Invoke(this, new object[] { app, column, null });
        }
        /// <summary>
        /// Get the value of a column.
        /// </summary>
        /// <typeparam name="T">The type of the ListView.</typeparam>
        /// <param name="app">The application service.</param>
        /// <param name="column">The column to get the value.</param>
        /// <param name="customProperty">If the column references a sub property should pass in this parameter the sub property name.</param>
        /// <returns>Returns the column value.</returns>
        public object GetCellValue<T>(ApplicationService app, Column column, string customProperty = null) where T : class
        {
            object value = (new[] { (T)CurrentObject }).AsQueryable().Select(customProperty ?? column.PropertyName).FirstOrDefault();

            if (value is Enum enumValue)
                return enumValue.GetEnumDisplayName();
            else
            {
                value = TryGetDefaultPropertyValue(app, value);

                if (!string.IsNullOrWhiteSpace(column.IsNullValue) && value == null)
                    value = column.IsNullValue;
            }
            return !string.IsNullOrWhiteSpace(column.FormatType) ? string.Format(column.FormatType, value ?? "") : value;
        }

        private object TryGetDefaultPropertyValue(ApplicationService app, object value)
        {
            if (value != null && (value.GetType().ImplementsInterface<IEntity>() || value.GetType().ImplementsInterface<IQuery>()))
            {
                var valueEntity = app.GetDefaultProperty(value.GetUnproxiedType().FullName);
                if (!string.IsNullOrWhiteSpace(valueEntity))
                    return TryGetDefaultPropertyValue(app, value.GetType().GetProperty(valueEntity).GetValue(value));
            }
            return value;
        }


        /// <summary>
        /// Return the base list view contents.
        /// </summary>
        /// <typeparam name="T">The type of the ListView.</typeparam>
        /// <param name="app">The application service.</param>
        /// <param name="ctx">The CoreDbContext service.</param>
        /// <returns>Returns the IQueryable list.</returns>
        protected IQueryable<T> GetItemsList<T>(ApplicationService app, CoreDbContext ctx) where T : class
        {
            IQueryable<T> list;

            if (typeof(T).ImplementsInterface<IEntity>())
            {
                list = ctx.Set<T>().AsQueryable();

                if (!string.IsNullOrEmpty(FromSql))
                    list = list.FromSql(FromSql);
            }
            else if (typeof(T).ImplementsInterface<IQuery>())
            {
                if (!string.IsNullOrWhiteSpace(FromSql))
                    list = ctx.Set<T>().FromSql(FromSql);
                else
                    throw new ApplicationException($"The View \"{ViewId}\" has an entity \"{Entity}\" that implements the IQuery interface and is mandatory inform the attribute FromSql in ListView or Entity definition.");
            }
            else
                throw new ApplicationException($"The View \"{ViewId}\" entity \"{Entity}\" does not have implemented the interface IEntity not IQuery.");

            if (!string.IsNullOrWhiteSpace(Criteria))
                list = list.Where(app.GetExpressionsService(), this);

            if (!string.IsNullOrWhiteSpace(Sort))
                list = list.OrderBy(Sort);

            return list;
        }

        #endregion
    }
}