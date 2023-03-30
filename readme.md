# Data Access Generation

### Better SQL Server stored procedure calls from C#

#### Accurate - Builds data types in and out including nullable information

#### Understandable - Output code should be readable and visible to developers

#### Maintainable - Should be easy to use within a project on an ongoing basis. It is not just scaffolding

This will generate a series of files to call stored procedures in a SQL Server database. 

## Features

* Creates a repository method, parameter class, result class for every procedure for easy calls
* User Defined Table types are supported as parameters
* Uses nullable reference types
* Fake repositories may be generated for unit tests

## Examples

### Dependency Injection
In the startup class, add a line like the following.

```csharp
services.AddScoped<IDBORepository, DBORepository>(_ => new DBORepository(connectionString));
```

### Calling a Stored Procedure with a single result
Shorthand format, available with 3 or fewer parameters. This assumes the procedure is defined as a Single return type in the settings file.

```csharp
SalesByCategory_ResultSet result = await _repository.SalesByCategory("Beverages", "1998");
```

Full parameter passing. This can also be used with automapper. This assumes the procedure is defined as a Single return type
```csharp
var result = await _repository.SalesByCategory(new SalesByCategory_Parameters(){ CategoryName = "Beverages", OrdYear = "1998"});
```


### Calling a Stored Procedure with multiple results
This assumes the procedure is defined as a List return type
```csharp
var result = await _repository.SalesByCategory(new SalesByCategory_Parameters(){ CategoryName = "Beverages", OrdYear = "1998"});
```

### Output parameters
This assumes the procedure is defined as an Output return type. 
```csharp
var result = await _repository.SalesByCategory(new SalesByCategory_Parameters(){ CategoryName = "Beverages", OrdYear = "1998"});
```
This assumes the procedure is defined as a List return type but that you want to access the Output parameter "RecordCount" directly. It is then found on the parameters object.
```csharp
var parameters = new SalesByCategory_Parameters(){ CategoryName = "Beverages", OrdYear = "1998", RecordCount = null};
var records = await _repository.SalesByCategory(parameters);
var result = parameters.RecordCount;
```

### Calling multiple procedures in a repository within a transaction
```csharp
await _repo.RunTransaction(async (context) =>
{
    var insertResult = await context.Repository.CustOrder_Insert(new CustOrder_Insert_Parameters()
    {
      ProductName = "125-800",
      UnitPrice = 1000000,
      Quantity = 1,
      Discount = 0,
      ExtendedPrice = 1000000
    });
    var detailResult = await context.Repository.CustOrdersDetail(insertResult.OrderID);            
    
    return TransactionResult.Commit;
});
```

### Calling multiple procedures across repositories within a transaction
```csharp
await _repo.RunTransaction(async (context) =>
{    
    var insertResult = await context.Repository.CustOrder_Insert(new CustOrder_Insert_Parameters()
    {
        ProductName = "125-800",
        UnitPrice = 1000000,
          Quantity = 1,
          Discount = 0,
          ExtendedPrice = 1000000
    });
    var otherRepo = new NorthwindRepository.TransactionManaged(context.Connection, context.Transaction);
    var detailResult = await otherRepo.CustOrdersDetail(insertResult.OrderID);
    
    return TransactionResult.Commit;
});

```

### Adding test data

If the IncludesFakes setting is true, these options will be available.

#### Add a whole list to the data values.
```csharp
var fakeRepo = new FakeNorthwindRepository()
{
    SalesByCategory_Data = new List<SalesByCategory_Data_ResultSet>()
    {
        new SalesByCategory_Data_ResultSet()
        {
            ProductName = "Beverages",
            TotalPurchase = 12348
        },
        new SalesByCategory_Data_ResultSet()
        {
            ProductName = "Produce",
            TotalPurchase = 4648
        },
    }
};
```

#### Add an individual test item with WithData helper.
```csharp
var fakeRepo = new FakeNorthwindRepository()
    .WithData(new SalesByCategory_Data_ResultSet()
    {
        ProductName = "Beverages",
        TotalPurchase = 12348
    });
```

#### Counting method calls

If you want a count of how many times a method is called, it is possible to wrap the existing functionality in a new delegate. 
This allows you to add functionality before or after the existing logic.
```csharp
var fakeRepo = new FakeNorthwindRepository()
    .WithData(new SalesByCategory_Data_ResultSet()
    {
        ProductName = "Beverages",
        TotalPurchase = 12348
    });
var dictionary = new Dictionary<string, int>();

var inner = fakeRepo.SalesByCategoryDelegate;
fakeRepo.SalesByCategoryDelegate = async (a, b) =>
{
    dictionary["SalesByCategoryDelegate"] = dictionary.ContainsKey("SalesByCategoryDelegate") 
        ? dictionary["SalesByCategoryDelegate"] + 1 : 1;
    return await inner(a, b);
};

var accessRepo = fakeRepo as IDBORepository;
var data = await accessRepo.SalesByCategory("Beverages", "1998");

Assert.Equal("Beverages", data.First().ProductName);
Assert.Equal(1, dictionary["SalesByCategoryDelegate"]);
```

### Integration Tests
* Uses SQL Server 2019 Express Edition for testing purposes.
* Install SQL Server 2019 Express Edition
* Create a database called Northwind and run the Northwind/instnwnd.sql script in the database.
* Make sure the current user has access to the Northwind database.

### Changes
#### TBD (V1.20)
* Always including output parameters even if default value is defined.
* If the first 2 characters are uppercase, don't change the first character to lowercase for parameters.
* Adjusted parameter lookup to fix length of text type.
* Added a new shorthand method if there is a single parameter of a User Defined Type (UDT) and it has a single column. 

#### 2023-03-27 (V1.19)
* Added using statement for command objects to ensure they are disposed.

#### 2023-02-14 (V1.18)
* Corrected result assignments for output result types when no result set is returned.

#### 2022-12-13 (V1.17)
* Corrected name conversion for datetime2 types.

#### 2022-12-02 (V1.16)
* Adding integration testing through a northwind database.
* Accounted for stored procedures that could have spaces in the name
* Better examples in readme

#### 2022-12-01 (V1.15)
* Fixed bug around default value lookup
* Fixed error handling for exceptions thrown in parallel loop

#### 2022-11-18 (V1.14)
* Only include using statements in generated files if needed

#### 2022-11-11 (V1.13)
* Improved line breaks in output code

#### 2022-11-10 (V1.12)
* Created a fallback approach to loading result metadata. Requires calling the procedure with default values
* Added more system type mappings

#### 2022-10-06 (V1.11)
* Skipping result column lookup if None or Output return types are used.
* Skipping result column error lookup on result set if None or Output return types are used.

#### 2022-10-03 (V1.10)
* Allowing location to be passed in for support of shared release directories

#### 2022-09-22 (V1.9)
* User defined types with a different schema will correctly use the type's schema 

#### 2022-08-24 (V1.8)
* When the parameters have a default value and a null is provided, allowing default value to be used.

#### 2022-08-18 (V1.7)
* Made sure output type can initialize a new result object even if there are no result rows.

#### 2022-08-12 (V1.6)
* Removed dependency between the repositories and transaction managed classes

#### 2022-07-29 (V1.5)
* Added support for transactions

#### 2022-07-21 (V1.4)
* Returning errors generated by SQL for result definitions that couldn't be calculated

#### 2022-07-19 (V1.3)
* Changed verification process for datetime ranges to prevent errors if the datetime output parameter is never assigned in the procedure
* Workaround to case where user defined type of same name exists in multiple schemas

#### 2022-05-27 (V1.2)
* Updated file deletion process. Some files were not being correctly deleted.
* Handled DBNull values for the output parameters in Output return types

#### 2022-05-26 (V1.1)
* Changed output type to properly pull from the parameter value instead of trying to read a row

#### 2022-04-28 (V1.0)
* Changed ProcedureList to be a list of objects instead of strings. Requires changing the settings file. Please see settings for more details.

#### 2022-04-07 (V0.9)
* Changed from System.Data.SqlClient to Microsoft.Data.SqlClient
* This may require adding Encrypt=false to the connection string since the default changes with this library.
* Breaking change: Switch the nuget package from System.Data.SqlClient to Microsoft.Data.SqlClient 

## Building

* Make sure .NET 6 is installed
* Run publish.bat to create a small set of output files
* There should be no more than 4 files with this approach. Supports Windows 64 bit
* [root]\DataAccessGeneration\bin\Release\net6.0\win-x64\publish
* Other build targets will require updating the publish.bat file

## Using

* There are two approaches to using this tool
* Option 1: Directly use the EXE in your project
  * Take the published files and copy into the project structure where it will be used
  * Edit the json file with specific procedures you desire and adjust other settings
  * Run the EXE from windows explorer. It should create the desired files
* Option 2: Use the BAT or PS1 file to call Data Access Generator from a shared location
  * Copy the published json file to a folder on your machine. Edit the json file with specific procedures you desire and adjust other settings  
  * Make sure the BAT or PS1 file points to the shared location of the EXE and that it points to the json settings file.
  * Run the BAT or PS1 file from the command line or run the BAT file from windows explorer. It should create the desired files. PS1 files can not be run directly by double clicking.
  * If pinning to a specific version is required, use the version path instead of latest path in the BAT or PS1 file.
* Option 2 is recommended as it allows the EXE to be updated in a shared location and have all projects use the latest version. It will also prevent commiting EXE files into source control.

## Known Limitations

* Does not support multiple result sets
* Parameter properties will always be marked as nullable even if they should be required. SQL Server does not provide this information.
* If a procedure is complex enough, the result properties may not always be determinable. It attempts two approaches to get the data. The second requires calling the procedure with stub parameters
* Calculating result definitions may not work for procedures with temp tables. It attempts to call the procedure with stub parameters which sometimes works.

## Settings

* ConnectionString: String. The connection string to the database. This should be accessible to the user running the program. The Database portion will be used.
* SchemaName: String. The DB schema to use in addition to the database defined in the connection string. Only 1 schema per setting is supported.
* Namespace: String. This namespace will be used as the base for all generated classes. Fake classes will be in a sub namespace.
* OutputRelativePath: String. The path to the output directory relative to the DataAccessGeneration executable
* ProcedureList: Array of objects. List of procedures to generate. If left null or empty, all procedures will be generated for that schema.
  * Proc - Required - Name in database.
  * Name - Optional - Name to use in C#. Defaults to use the Proc value.
  * Return - Enum Optional - Could be List, Single, Scalar, Output, None. Defaults to List.
    * List is a list of results.
    * Single would be a Single() style result for a single row. Expect exceptions if there isn't a single result.
    * SingleOrDefault would be a SingleOrDefault() style result for a single row. No results will return a null.
    * Scalar would be a single value and would only work if there is just a single column & single row result.
    * Output would take all output parameters and assign them to a new return type and return that as a single item.
    * None would not return any results.
* IncludesFakes: Bool. Include classes for testing and have the Fake prefix. They are placed in the Fake sub namespace.
* RepositoryName: String, Optional. If not defined, the Repository name will be {schema}Repository. Otherwise it will be as defined in this property.

## Project History
* Originally designed for OneSky Flight LLC as an internal project. 
* Used in production in a limited fashion in 2021 and more widely used in many internal projects in 2022.
* Transitioned to an open source project with permission in 2022.

## Project decisions and reasoning
* An executable instead of nuget package. Maintainable and easy to get running for most developers.
* Run manually instead of on build. Better control and didn't want to need CI systems to access the database.
* Using nullable reference types. More accurate information with null types.
* Fake creation. If used, generated fakes cover about 80% of our test use cases with just returning a defined set of data on a proc call. This makes that case very easy to use. Delegates can be made to cover most other cases with more effort.
* .NET 6. Current .NET version at the time of development. Intent is to keep project in a current .NET version and output code compatible with oldest version of .NET that Microsoft actively supports. (see https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)



