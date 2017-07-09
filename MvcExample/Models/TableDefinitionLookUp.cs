using DynamicDataTableService;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MvcExample.Models
{
    public class TableDefinitionLookUp
    {
        public static readonly List<TableDefinition> Definitions = new List<TableDefinition>
        {
            // EXAMPLE 1 - START
            new TableDefinition{
                Identifier = "Example1",
                RootEntityName = "Product",
                ColumnDefinitions = new List<ColumnDefinition> {
                    new ColumnDefinition {
                        Identifier = "Id",
                        PropertyName = "ProductID",
                        PropertyType = typeof(int),
                        PrimaryKey = true
                    },
                    new ColumnDefinition {
                        Identifier = "Name",
                        PropertyName = "Name",
                        HeaderText = "Item Name",
                        PropertyType = typeof(string)
                    },
                     new ColumnDefinition {
                        Identifier = "Color",
                        PropertyName = "Color",
                        PropertyType = typeof(string)
                    },
                }
            },
            // EXAMPLE 1 - END

            // EXAMPLE 2 - START
            // Parent Entity getting category name
             new TableDefinition{
                Identifier = "Example2",
                RootEntityName = "Product",
                ColumnDefinitions = new List<ColumnDefinition> {
                    new ColumnDefinition {
                        Identifier = "Id",
                        PropertyName = "ProductID",
                        PropertyType = typeof(int),
                        PrimaryKey = true
                    },
                    new ColumnDefinition {
                        Identifier = "Name",
                        PropertyName = "Name",
                        HeaderText = "Item Name",
                        PropertyType = typeof(string)
                    },
                     new ColumnDefinition {
                        Identifier = "CategoryName",
                        PropertyName = "Name",
                        PropertyType = typeof(string),
                        PropertyPath = "ProductSubcategory"
                    },
                }
            },
            // EXAMPLE 2 - END

             // EXAMPLE 3 - START 
             // Aggregate Func from Children entity
            new TableDefinition{
                Identifier = "Example3",
                RootEntityName = "Product",
                ColumnDefinitions = new List<ColumnDefinition> {
                    new ColumnDefinition {
                        Identifier = "Id",
                        PropertyName = "ProductID",
                        PropertyType = typeof(int),
                        PrimaryKey = true
                    },
                    new ColumnDefinition {
                        Identifier = "Name",
                        PropertyName = "Name",
                        HeaderText = "Item Name",
                        PropertyType = typeof(string)
                    },
                     new ColumnDefinition {
                        Identifier = "OnHandQuantity",
                        PropertyName = "Quantity",
                        PropertyType = typeof(int),
                        PropertyPath = "ProductInventory",
                        Aggregate = DataTableAggregateEnum.Sum
                    },
                }
            },
            // EXAMPLE 3 - END

            // Different Types
            new TableDefinition{
                Identifier = "Example4",
                RootEntityName = "Product",
                ColumnDefinitions = new List<ColumnDefinition> {
                    new ColumnDefinition {
                        Identifier = "Id",
                        PropertyName = "ProductID",
                        PropertyType = typeof(int),
                        PrimaryKey = true
                    },
                    new ColumnDefinition {
                        Identifier = "Name",
                        PropertyName = "Name",
                        HeaderText = "Item Name",
                        PropertyType = typeof(string)
                    },
                    new ColumnDefinition {
                        Identifier = "Flagged",
                        PropertyName = "MakeFlag",
                        PropertyType = typeof(bool),
                    },
                    new ColumnDefinition {
                        Identifier = "DaysToManufacture",
                        PropertyName = "DaysToManufacture",
                        HeaderText = "Days To Manufacture",
                        PropertyType = typeof(DaysToManufacture),
                    },
                    new ColumnDefinition {
                        Identifier = "SellEndDate",
                        PropertyName = "SellEndDate",
                        HeaderText = "End Date",
                        PropertyType = typeof(DateTime),
                    },
                   
                }
            },


        };
    }
    public enum DaysToManufacture {
        None = 0,
        [Display(Name = "In One Day")]
        OneDay = 1,
        [Display(Name = "In Two Days")]
        TwoDays = 2,
        [Display(Name = "In Three Days")]
        ThreeDays = 3,
        [Display(Name = "In Four Days")]
        FourDays = 4,
    }
}