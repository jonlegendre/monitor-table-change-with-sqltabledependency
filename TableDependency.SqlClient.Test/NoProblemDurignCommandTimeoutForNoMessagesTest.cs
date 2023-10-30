﻿using System;
using Microsoft.Data.SqlClient;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;

namespace TableDependency.SqlClient.Test
{
    public class NoProblemDurignCommandTimeoutForNoMessagesSqlServerModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime Born { get; set; }
        public int Quantity { get; set; }
    }

    [TestClass]
    public class DatabaseObjectCleanUpNoProblemDurignCommandTimeoutForNoMessagesSqlServer : Base.SqlTableDependencyBaseTest
    {
        private static string _dbObjectsNaming;
        private static readonly string TableName = typeof(NoProblemDurignCommandTimeoutForNoMessagesSqlServerModel).Name;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText = $"CREATE TABLE [{TableName}]([Id][int] IDENTITY(1, 1) NOT NULL, [First Name] [NVARCHAR](50) NOT NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void Test()
        {
            var domaininfo = new AppDomainSetup { ApplicationBase = Environment.CurrentDirectory };
            var adevidence = AppDomain.CurrentDomain.Evidence;
            var domain = AppDomain.CreateDomain("TableDependencyDomaing", adevidence, domaininfo);
            var otherDomainObject = (RunsInAnotherAppDomainNoMessage) domain.CreateInstanceAndUnwrap(typeof (RunsInAnotherAppDomainNoMessage).Assembly.FullName, typeof (RunsInAnotherAppDomainNoMessage).FullName);
            _dbObjectsNaming = otherDomainObject.RunTableDependency(ConnectionStringForTestUser, tableName: TableName);
            Thread.Sleep(4*60*1000);
            var status = otherDomainObject.GetTableDependencyStatus();

            AppDomain.Unload(domain);
            Thread.Sleep(3 * 60 * 1000);

            Assert.IsTrue(status != TableDependencyStatus.StopDueToError && status != TableDependencyStatus.StopDueToCancellation);
            Assert.IsTrue(base.AreAllDbObjectDisposed(_dbObjectsNaming));
            Assert.IsTrue(base.CountConversationEndpoints(_dbObjectsNaming) == 0);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public class RunsInAnotherAppDomainNoMessage : MarshalByRefObject
        {
            private SqlTableDependency<NoProblemDurignCommandTimeoutForNoMessagesSqlServerModel> _tableDependency = null;

            public TableDependencyStatus GetTableDependencyStatus()
            {
                return this._tableDependency.Status;
            }

            public string RunTableDependency(string connectionString, string tableName)
            {
                var mapper = new ModelToTableMapper<NoProblemDurignCommandTimeoutForNoMessagesSqlServerModel>();
                mapper.AddMapping(c => c.Name, "First Name");

                this._tableDependency = new SqlTableDependency<NoProblemDurignCommandTimeoutForNoMessagesSqlServerModel>(connectionString, tableName: tableName, mapper: mapper);
                this._tableDependency.OnChanged += (o, args) => { };
                this._tableDependency.Start(60, 120);

                return this._tableDependency.DataBaseObjectsNamingConvention;
            }
        }
    }
}