using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Dapper1.Model;
using Z.Dapper.Plus;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

class Program
{
    static async Task Main(string[] args)
    {

        try
        {
            #region Content
            //Basic();
            //MethodExecute();
            //MethodQuery();
            //Parameter();
            Result();
            //Utilities();
            //DapperTransaction();
            //DapperPlus();
            //DapperContrib();
            //CallFunctionSQL();
            #endregion
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public static async Task Basic()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connection = new SqlConnection(stringConnection);

        //select
        var people = await connection.QueryAsync<Branch>("select * from Branch");

        foreach (var person in people)
        {
            Console.WriteLine($"Hello from {person.nAMe}");
        }

        //insert
        var name = "CN Quận 1";
        var addr = "TB TPHCM";
        var phoneNum = "0123";
        var count = await connection.ExecuteAsync(
                    @"insert Branch(Name, address, phoneNumber) values (@name, @addr, @phoneNum)",
                    new { name, addr, phoneNum });


        Console.WriteLine($"Inserted {count} rows.");

        //delete
        var removed = await connection.ExecuteAsync(
                "delete from Branch where Name = @name",
                new { name });

        Console.WriteLine($"Removed {removed} rows.");
    }
    public static void MethodExecute()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connection = new SqlConnection(stringConnection);
        //Execute - Store Procedure Single
        string sql = "sp_Method_Execute";

        using (var conn = connection)
        {
            var affectedRows = connection.Execute(sql,
                commandType: CommandType.StoredProcedure);
        }

        //Execute - Store Procedure Many
        // string sql2 = "sp_Method_Execute";

        //using (var conn = connection)
        //{
        //    var affectedRows = connection.Execute(sql2,
        //        new[]
        //        {
        //                new {Kind = InvoiceKind.WebInvoice, Code = "Many_Insert_1"},
        //                new {Kind = InvoiceKind.WebInvoice, Code = "Many_Insert_2"},
        //                new {Kind = InvoiceKind.StoreInvoice, Code = "Many_Insert_3"}
        //        },
        //        commandType: CommandType.StoredProcedure
        //    );
        //}

        //Execute INSERT single
        string sql3 = "INSERT INTO Branch (Name) Values (@BranchName);";

        using (var conn = new SqlConnection(stringConnection))
        {
            var affectedRows = connection.Execute(sql3, new { BranchName = "Tai.Vo" });

            Console.WriteLine(affectedRows);

            var result = connection.Query<Branch>("Select * FROM Branch WHERE Name = 'Tai.Vo'").ToList();

            Console.WriteLine($"Hello {result.Select(d => d.nAMe).FirstOrDefault()}");
        }

        //Execute INSERT Many
        string sql4 = "INSERT INTO Branch (Name) Values (@BranchName);";

        using (var cons = new SqlConnection(stringConnection))
        {
            connection.Open();

            var affectedRows = connection.Execute(sql4,
            new[]
            {
                    new {BranchName = "Thang.Mai"},
                    new {BranchName = "Quang.Vo"},
                    new {BranchName = "Phat.Tran"}
            }
        );

            Console.WriteLine(affectedRows);
        }

        //Execute Reader
        using (var con = new SqlConnection(stringConnection))
        {
            var reader = connection.ExecuteReader("SELECT * FROM Employee;");

            DataTable table = new DataTable();
            table.Load(reader);

            //Helper.WriteTable(table);
        }

        //Execute Scalar
        using (var cons = new SqlConnection(stringConnection))
        {
            var name = connection.ExecuteScalar<string>("SELECT PositionName FROM Position");

            Console.WriteLine(name);
        }
    }
    public static void MethodQuery()
    {

        //Query Anonymous
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connection = new SqlConnection(stringConnection);

        string sql = "SELECT * FROM Employee";

        using (var conn = new SqlConnection(stringConnection))
        {
            var orderDetail = connection.Query(sql).FirstOrDefault();
            foreach (var item in orderDetail)
            {
                Console.WriteLine(item);
            }
        }

        //Query Strongly Typed
        using (var conn = new SqlConnection(stringConnection))
        {
            var orderDetails = connection.Query<Branch>(sql).ToList();

            Console.WriteLine(orderDetails.Count);
        }


        //Query Multi-Mapping (One to One)
        string sqljoin = "SELECT * FROM Employee AS A INNER JOIN Branch AS B ON A.BranchID = B.ID;";

        using (var conn = connection)
        {
            connection.Open();

            var invoices = connection.Query<Branch, Employee, Branch>(
                    sql,
                    (branchs, employees) =>
                    {
                        branchs.Employee = employees;
                        return branchs;
                    },
                    splitOn: "BranchID")
                .Distinct()
                .ToList();
            foreach (var item in invoices)
            {
                Console.WriteLine(item.nAMe);
            }
        }

        //Query Multi-Mapping (One to Many)
        string sqlMany = "SELECT * FROM Employee AS A INNER JOIN Branch AS B ON A.BranchID = B.ID;";

        using (var conn = new SqlConnection(stringConnection))
        {
            var orderDictionary = new Dictionary<int, Branch>();


            var list = conn.Query<Branch, Employee, Branch>(
            sql,
            (branchs, employees) =>
            {
                Branch branchEntry;

                if (!orderDictionary.TryGetValue(branchs.Id, out branchEntry))
                {
                    branchEntry = branchs;
                    branchEntry.empList = new List<Employee>();
                    orderDictionary.Add(branchEntry.Id, branchEntry);
                }

                branchEntry.empList.Add(employees);
                return branchEntry;
            },
            splitOn: "Id")
            .Distinct()
            .ToList();

            Console.WriteLine(list.Count);
            foreach (var item in list)
            {
                Console.WriteLine("Many to one: " + item.Id + "-" + item.nAMe + "-" + item.Address);
            }
        }
    }
    public static void Parameter()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        ////Param Anonymous Single
        //string sql = "INSERT INTO Position (PositionName) Values (@posiName);";

        //using (var connection = connections)
        //{
        //    var affectedRows = connection.Execute(sql, new { posiName = "Director" });

        //    Console.WriteLine(affectedRows);

        //    // Only for see the Insert.
        //    var postition = connection.Query<Position>("Select * FROM Position WHERE PositionName = 'Director'").ToList();
        //    foreach(var item in postition)
        //    {
        //        Console.WriteLine(item.PositionName);
        //    }
        //}

        ////Param Anonymous Many
        //string sqlMany = "INSERT INTO Position (PositionName) Values (@posiName);";

        //using (var connection = new SqlConnection(stringConnection))
        //{
        //    var affectedRows = connection.Execute(sqlMany,
        //    new[]
        //    {
        //        new {posiName = DateTime.Now.ToString()},
        //        new {posiName = DateTime.Now.ToString()},
        //        new {posiName = DateTime.Now.ToString()}
        //    });
        //    Console.WriteLine(affectedRows);
        //}

        ////Dynamic Single
        //var sqlDynamic = "sp_Insert_Position";

        //using (var connection =connections)
        //{
        //    connection.Open();

        //    DynamicParameters parameter = new DynamicParameters();

        //    parameter.Add("@PositionName", "Loc sharing Dapper: " + DateTime.Now.ToString(), DbType.String, ParameterDirection.Input);

        //    connection.Execute(sqlDynamic,
        //        parameter,
        //        commandType: CommandType.StoredProcedure);

        //    //int rowCount = parameter.Get<int>("@RowCount");
        //}

        //Dynamic Many
        //var sqlDynamicMany = "sp_Insert_Position";
        //var parameters = new List<DynamicParameters>();
        //for (var i = 0; i < 3; i++)
        //{
        //    var p = new DynamicParameters();
        //    p.Add("@PositionName", "Loc sharing Dapper: " + DateTime.Now.ToString() + " Loop: " + i, DbType.String, ParameterDirection.Input);
        //    parameters.Add(p);
        //}
        //using (var connection = connections)
        //{
        //    connection.Open();
        //    connection.Execute(sqlDynamicMany,
        //        parameters,
        //        commandType: CommandType.StoredProcedure);
        //}

        //List
        var sqlList = "SELECT * FROM Employee WHERE BranchId IN @BranchId;";

        using (var connection = connections)
        {
            connection.Open();

            var lstEmp = connection.Query<Employee>(sqlList, new { BranchId = new[] { "1", "2" } }).ToList();
            foreach (var item in lstEmp)
            {
                Console.WriteLine(item.nAMe + " " + item.BranchId);
            }
        }

        //String
        var sqlString = "SELECT * FROM Employee WHERE EmpId = @EmpId;";

        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            var empInfo = connection.Query<Employee>(sqlString, new { EmpId = new DbString { Value = "1", IsFixedLength = false, Length = 9, IsAnsi = true } }).FirstOrDefault();

            Console.WriteLine(empInfo.nAMe);
        }

        //Table-Valued Parameter
        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();
            //create the table.
            connection.Execute(@"
                CREATE TABLE [Customer]
                (
                    [CustomerID] [INT] IDENTITY(1,1) NOT NULL,
                    [Code] [VARCHAR](20) NULL,
                    [Name] [VARCHAR](20) NULL,

                    CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
                    (
                        [CustomerID] ASC
                    )
                )
            ");

            // create the TVP type.
            connection.Execute(@"
                CREATE TYPE TVP_Customer AS TABLE
                (
                    [Code] [VARCHAR](20) NULL,
                    [Name] [VARCHAR](20) NULL
                )
            ");

            //create the stored procedure that will take the TVP type as a parameter.
            connection.Execute(@"
                CREATE PROCEDURE Customer_Seed
                    @Customers TVP_Customer READONLY
                AS
                BEGIN
                    INSERT INTO Customer (Code, Name)
                    SELECT Code, Name
                    FROM @Customers
                END
            ");

            //use a TVP parameter

            //create a DataTable with the same definition
            var dt = new DataTable();
            dt.Columns.Add("Code");
            dt.Columns.Add("Name");

            for (int i = 0; i < 5; i++)
            {
                dt.Rows.Add("Code_" + i, "Name_" + i);
            }

            //use the AsTableValuedParameter with the TVP type name in parameter to execute the Stored Procedure
            connection.Execute("Customer_Seed", new { Customers = dt.AsTableValuedParameter("TVP_Customer") }, commandType: CommandType.StoredProcedure);
        }

    }
    public static void Result()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        ////Query
        //string sql = "SELECT TOP 10 * FROM Employee";

        //using (var connection = new SqlConnection(stringConnection))
        //{
        //    var resultQuery = connection.QueryFirstOrDefault(sql);

        //    Console.WriteLine(resultQuery);
        //}

        //string sqlF = "SELECT * FROM Employee WHERE EmpId = @EmpId;";

        //using (var connection = new SqlConnection(stringConnection))
        //{
        //    var resultQueryF = connection.QueryFirst(sqlF, new { EmpId = 1 });
        //    var resultQueryFOD = connection.QueryFirstOrDefault(sqlF, new { EmpId = 1 });
        //    var resultQueryS = connection.QuerySingle(sqlF, new { EmpId = 1 });
        //    var resultQuerySOD = connection.QuerySingleOrDefault(sqlF, new { EmpId = 1 });
        //    Console.WriteLine(resultQueryF);
        //}

        ////Strongly Typed
        //string sqlST = "SELECT TOP 10 * FROM Branch";

        //using (var connection = new SqlConnection(stringConnection))
        //{
        //    var orderDetails = connection.Query<Branch>(sqlST).ToList();

        //    Console.WriteLine(orderDetails.Count);
        //}

        string sqlSTQ = "SELECT * FROM Employee WHERE BranchId = @BranchId;";

        using (var connection = new SqlConnection(stringConnection))
        {
            var resultF = connection.QueryFirst<Employee>(sqlSTQ, new { BranchId = 1 });
            //var resultFOD = connection.QueryFirstOrDefault<Employee>(sqlSTQ, new { BranchId = 1 });
            //var resultS = connection.QuerySingle<Employee>(sqlSTQ, new { BranchId = 1 });
            //var resultSOD = connection.QuerySingleOrDefault<Employee>(sqlSTQ, new { BranchId = 1 });

            Console.WriteLine(new List<Employee>() { resultF });
        }

        //Query Multi-Mapping One-One
        string sql = "SELECT * FROM Employee AS A INNER JOIN Branch AS B ON A.BranchID = B.Id;";

        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            var mapping = connection.Query<Branch, Employee, Branch>(
                    sql,
                    (branchs, employees) =>
                    {
                        branchs.Employee = employees;
                        return branchs;
                    },
                    splitOn: "BranchID")
                .Distinct()
                .ToList();

        }

        //Query Multi-Mapping (One to Many)
        string sqMultiMapl = "SELECT TOP 10 * FROM [dbo].[Order] AS A INNER JOIN OrderDetail AS B ON A.OrderID = B.OrderID;";

        using (var connection = new SqlConnection(stringConnection))
        {
            var orderDictionary = new Dictionary<int, Order>();


            var list = connection.Query<Order, OrderDetail, Order>(
            sqMultiMapl,
            (order, orderDetail) =>
            {
                Order orderEntry;

                if (!orderDictionary.TryGetValue(order.OrderID, out orderEntry))
                {
                    orderEntry = order;
                    orderEntry.OrderDetails = new List<OrderDetail>();
                    orderDictionary.Add(orderEntry.OrderID, orderEntry);
                }

                orderEntry.OrderDetails.Add(orderDetail);
                return orderEntry;
            },
            splitOn: "OrderDetailID")
            .Distinct()
            .ToList();

            Console.WriteLine(list.Count);
            foreach (var item in list)
            {
                Console.WriteLine("Many to one: " + item.OrderID + "-" + item.CustomerID + "-" + item.OrderDate);
            }
        }

        //Multi result
        string sqlMultiR = "SELECT * FROM Employee WHERE EmpId = @Id; SELECT * FROM Branch WHERE Id = @Id;";

        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            //disadvan: select phải truyền theo thứ tự
            using (var multi = connection.QueryMultiple(sqlMultiR, new { Id = 1 }))
            {
                var invoice = multi.Read<Employee>().First();
                var invoiceItems = multi.Read<Branch>().ToList();
            }
        }

    }
    public static async Task Utilities()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);
        //Async
        string sqlAsync = "INSERT INTO Customer (Name) Values (@Name);";
        string sql = "SELECT * FROM Customer WHERE CustomerID = @CustomerId;";

        //try
        //{
        //    using (var connection = new SqlConnection(stringConnection))
        //    {
        //        connection.Open();


        //        var affectedRows = await connection.ExecuteAsync(sqlAsync, new { Name = "Loc"  }).ConfigureAwait(false);

        //        Console.WriteLine(affectedRows);

        //        var customers = await connection.QueryAsync<Customer>("Select * FROM CUSTOMER WHERE Name Like '%Loc%'").ConfigureAwait(false);

        //        Console.WriteLine(customers);

        //        var queryF = await connection.QueryFirstAsync<Customer>(sql, new { CustomerId = 1 }).ConfigureAwait(false);

        //        Console.WriteLine(queryF);

        //        var queryFOD = await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerId = 1 }).ConfigureAwait(false);

        //        Console.WriteLine(queryFOD);

        //        var queryS = await connection.QuerySingleAsync<Customer>(sql, new { CustomerId = 1 }).ConfigureAwait(false);

        //        Console.WriteLine(queryS);

        //        var querySOD = await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { CustomerId = 1 }).ConfigureAwait(false);

        //        Console.WriteLine(querySOD);
        //    }
        //}
        //catch (Exception ex)
        //{

        //    throw ex;
        //}

        //Buffered
        string sqlB = "SELECT * FROM Customer;";

        using (var connection = new SqlConnection(stringConnection))
        {
            var buffered = connection.Query<Customer>(sqlB, buffered: false).ToList();
            var lst = buffered.Take(3);
            foreach (var item in lst)
            {
                Console.WriteLine(item.Name);
            }

        }

        //stored single
        var sqlS = "sp_Buffered_single";

        //using (var connection = new SqlConnection(stringConnection))
        //{
        //    connection.Open();

        //    var affectedRows = connection.Execute(sqlS,
        //        new { Code = "BufferedCode", Name = "Loc Buffered" },
        //        commandType: CommandType.StoredProcedure);
        //}

        //Multi
        var sqlMulti = "sp_Buffered_single";

        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            var affectedRows = connection.Execute(sqlMulti,
                new[]
                {
            new {Code = "BufferedCode", Name = "Loc Buffered 1"},
            new {Code = "BufferedCode", Name = "Loc Buffered 2"},
            new {Code = "BufferedCode", Name = "Loc Buffered 3"}
                },
                commandType: CommandType.StoredProcedure
            );
        }
    }
    public static void DapperTransaction()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        //Begin a new transaction from the connection and pass it in the transaction optional parameter.
        string sql = "INSERT INTO Customer (Name) Values (@CustomerName);";

        //using (var connection = new SqlConnection(stringConnection))
        //{
        //    connection.Open();
        //    //create transaction
        //    using (var transaction = connection.BeginTransaction())
        //    {
        //        try
        //        {
        //            //pass the transaction
        //            var affectedRows = connection.Execute(sql, new { CustomerName = "Mark" }, transaction: transaction);
        //            //if it was successful, commit the transaction
        //            transaction.Commit();

        //            Console.WriteLine(affectedRows);
        //        }
        //        catch (Exception ex)
        //        {
        //            // roll the transaction back
        //            transaction.Rollback();
        //            throw;
        //        }
               
        //    }
        //}

        ////Begin a new transaction scope before starting the connection

        //using (var transaction = new TransactionScope())
        //{
        //    var sqlT = "sp_Buffered_single";

        //    using (var connection = new SqlConnection(stringConnection))
        //    {
        //        connection.Open();

        //        var affectedRows = connection.Execute(sqlT,
        //            new { Code = "BufferedCode", Name = "Loc Buffered" },
        //            commandType: CommandType.StoredProcedure);
        //    }
        //    //stop here and search query in SQL
        //    transaction.Complete();
        //}

        //Transaction Dapper

        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            using (var transactions = connection.BeginTransaction())
            {
                // Dapper
                var affectedRows1 = connection.Execute(sql, new { CustomerName = "Mark Trans" }, transaction: transactions);

                // Dapper Transaction
                var affectedRows2 = connection.Execute(sql, new { CustomerName = "Loc Trans" }, transaction: transactions);

                transactions.Commit();
            }
        }
    }
    public static List<Supplier> suppliers { get; set; }
    public static void DapperPlus()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        suppliers = new List<Supplier>();

        // STEP MAPPING
        StepMapping();


        // STEP BULKINSERT
        StepBulkInsert();

        //After Insert
        using (var connection = new SqlConnection(stringConnection))
        {
            var prod = connection.Query<Product>("select * from Products where ProductName like'%Bulk%'");
            Console.WriteLine("After Insert: ", prod);
        }

        // STEP BULKUPDATE
        StepBulkUpdate();

        //After Update
        using (var connection = new SqlConnection(stringConnection))
        {
            Console.WriteLine("After Update", connection.Query<Supplier>("Select A.SupplierID, A.SupplierName, A.ContactName FROM Suppliers as A where A.SupplierName = 'BULKUPDATE'"));

            Console.WriteLine("After Update", connection.Query<Product>("select * from Products where ProductName like'%Bulk%'"));
        }

        // STEP BULKMERGE
        StepBulkMerge();


        //After Merge 
        using (var connection = new SqlConnection(stringConnection))
        {
            Console.WriteLine("After Merge 1", connection.Query<Supplier>("Select A.SupplierID, A.SupplierName, A.ContactName FROM Suppliers as A where A.SupplierName = 'BULKUPDATE'"));
            Console.WriteLine("After Merge 2", connection.Query<Product>("select * from Products where ProductName like'%Bulk%'"));
        }

        // STEP BULKDELETE
        SteBulkDelete();

        //After Delete
        using (var connection = new SqlConnection(stringConnection))
        {
            Console.WriteLine("Suplier After Delete : " + connection.Query<Supplier>("Select A.SupplierID, A.SupplierName, A.ContactName FROM Suppliers as A where A.SupplierName = 'BULKUPDATE'").ToList().Count);

            Console.WriteLine("Product After Delete : " + connection.Query<Product>("select * from Products where ProductName = 'BULKUPDATE'").Count());
        }
    }
    public static void StepMapping()
    {
        DapperPlusManager.Entity<Supplier>().Table("Suppliers").Identity(x => x.SupplierID);
        DapperPlusManager.Entity<Product>().Table("Products").Identity(x => x.ProductID);
    }
    public static void StepBulkInsert()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        suppliers.Add(new Supplier()
        {
            SupplierName = "BulkInsert",
            ContactName = "BulkInsert",
            Products = new List<Product>
        { new Product() {ProductName = "BulkInsert", Unit = "BulkInsert"},new Product() {ProductName = "BulkInsert", Unit = "BulkInsert"} ,new Product() {ProductName = "BulkInsert", Unit = "BulkInsert"}  }
        });

        // STEP BULKINSERT
        using (var connection = new SqlConnection(stringConnection))
        {
            connection.BulkInsert(suppliers).ThenForEach(x => x.Products.ForEach(y => y.SupplierID = x.SupplierID)).ThenBulkInsert(x => x.Products);
        }
    }
    public static void StepBulkUpdate()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        suppliers.ForEach(x =>
        {
            x.SupplierName = "BULKUPDATE";
            x.Products.ForEach(y => y.ProductName = "BULKUPDATE");
        });


        // STEP BULKUPDATE
        using (var connection = new SqlConnection(stringConnection))
        {
            connection.BulkUpdate(suppliers, x => x.Products);
        }
    }
    public static void StepBulkMerge()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";

        suppliers.Add(new Supplier()
        {
            SupplierName = "BulkMerge",
            ContactName = "BulkMerge",
            Products = new List<Product>()
        { new Product() { ProductName = "BulkMerge", Unit = "BulkMerge"},  new Product() { ProductName = "BulkMerge" , Unit = "BulkMerge"} ,  new Product() { ProductName = "BulkMerge", Unit = "BulkMerge" }
        }
        });

        suppliers.ForEach(x =>
        {
            x.SupplierName = "BULKMERGE";
            x.Products.ForEach(y => y.ProductName = "BULKMERGE");
        });

        // STEP BULKMERGE
        using (var connection = new SqlConnection(stringConnection))
        {
            connection.BulkMerge(suppliers).ThenForEach(x => x.Products.ForEach(y => y.SupplierID = x.SupplierID)).ThenBulkMerge(x => x.Products);
        }

    }
    public static void SteBulkDelete()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        // STEP BULKDELETE
        using (var connection = new SqlConnection(stringConnection))
        {
            connection.BulkDelete(suppliers.SelectMany(x => x.Products)).BulkDelete(suppliers);
        }
    }
    public static void DapperContrib()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);

        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            var invoice = connection.GetAll<Employee>;
        }
        var invoices = connections.GetAll<Branch>().ToList();
        var identity = connections.Insert(new Position { PositionName = "Contrib" });
        var isSuccess = connections.Update(new Position { PositionId = 1, PositionName = "Contrib" });
        //var isSuccess = connections.Delete(new InvoiceContrib { InvoiceID = 1 });
        //var isSuccess = connections.DeleteAll<InvoiceContrib>();
    }
    public static void CallFunctionSQL()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);
        using (var connection = new SqlConnection(stringConnection))
        {
            connection.Open();

            var result = connections.Query<dynamic>("SELECT * from dbo.fnc_ExecuteReturnEmpTable(@PositionId)", new { PositionId = 3 }, commandType: CommandType.Text);
        }

    }
    public async Task<IEnumerable<int>> TempTable()
    {
        var stringConnection = "Server=LOCNGUYEN\\MSSQLSERVER02; Database=DapperSharing; Trusted_Connection=True;";
        using IDbConnection connections = new SqlConnection(stringConnection);
        //not work
        using (var conn = new SqlConnection(stringConnection))
        {
            await conn.ExecuteAsync(@"CREATE TABLE #tmpWidget(widgetId int);");

            // this will throw an error because the #tmpWidget table no longer exists
            await conn.ExecuteAsync(@"insert into #tmpWidget(WidgetId) VALUES (1);");

            return await conn.QueryAsync<int>(@"SELECT * FROM #tmpWidget;");
        }

        //working
        //opt 1:
        using (var conn = new SqlConnection(stringConnection))
        {
            // Here, everything is done in one statement, therefore the temp table
            // always stays within the scope of the connection
            return await conn.QueryAsync<int>(
              @"CREATE TABLE #tmpWidget(widgetId int);
                insert into #tmpWidget(WidgetId) VALUES (1);
                SELECT * FROM #tmpWidget;");
        }
        //opt 2:
        using (var conn = new SqlConnection(stringConnection))
        {
            // Here, everything is done in separate statements. To not loose the 
            // connection scope, we have to explicitly open it
            await conn.OpenAsync();

            await conn.ExecuteAsync(@"CREATE TABLE #tmpWidget(widgetId int);");
            await conn.ExecuteAsync(@"insert into #tmpWidget(WidgetId) VALUES (1);");
            return await conn.QueryAsync<int>(@"SELECT * FROM #tmpWidget;");
        }
    }

}