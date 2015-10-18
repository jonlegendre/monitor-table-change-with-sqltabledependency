﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.IntegrationTest.Models;
using TableDependency.Mappers;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    [TestClass]
    public class EventForSpecificColumnsTestSqlServer
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["SqlServerConnectionString"].ConnectionString;
        private const string TableName = "Check_Model";
        private static int _counter;
        private static Dictionary<string, Tuple<Check_Model, Check_Model>> _checkValues = new Dictionary<string, Tuple<Check_Model, Check_Model>>();

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText =
                        $"CREATE TABLE [{TableName}]( " +
                        "[Id][int] IDENTITY(1, 1) NOT NULL, " +
                        "[First Name] [nvarchar](50) NOT NULL, " +
                        "[Second Name] [nvarchar](50) NOT NULL, " +
                        "[Born] [datetime] NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestInitialize()]
        public void TestInitialize()
        {
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestMethod]
        public void EventForSpecificColumnsTest()
        {
            SqlTableDependency<Check_Model> tableDependency = null;
            string naming = null;

            try
            {
                var mapper = new ModelToTableMapper<Check_Model>();
                mapper.AddMapping(c => c.Name, "FIRST name").AddMapping(c => c.Surname, "Second Name");

                tableDependency = new SqlTableDependency<Check_Model>(
                    _connectionString, 
                    TableName, 
                    mapper, new List<string>() { "second name" });
                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.Start();
                naming = tableDependency.DataBaseObjectsNamingConvention;

                Thread.Sleep(5000);

                var t = new Task(ModifyTableContent);
                t.Start();
                t.Wait(20000);
            }
            finally
            {
                tableDependency?.Dispose();
            }

            Assert.AreEqual(_counter, 2);
            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Name, _checkValues[ChangeType.Insert.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Surname, _checkValues[ChangeType.Insert.ToString()].Item1.Surname);

            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Name, _checkValues[ChangeType.Delete.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Surname, _checkValues[ChangeType.Delete.ToString()].Item1.Surname);
            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(_connectionString, naming));
        }

        private static void TableDependency_Changed(object sender, RecordChangedEventArgs<Check_Model> e)
        {
            _counter++;

            switch (e.ChangeType)
            {
                case ChangeType.Insert:
                    _checkValues[ChangeType.Insert.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Insert.ToString()].Item2.Surname = e.Entity.Surname;
                    break;
                case ChangeType.Delete:
                    _checkValues[ChangeType.Delete.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Delete.ToString()].Item2.Surname = e.Entity.Surname;
                    break;
            }
        }

        private static void ModifyTableContent()
        {
            _checkValues.Add(ChangeType.Insert.ToString(), new Tuple<Check_Model, Check_Model>(new Check_Model { Name = "Christian", Surname = "Del Bianco" }, new Check_Model()));
            _checkValues.Add(ChangeType.Update.ToString(), new Tuple<Check_Model, Check_Model>(new Check_Model { Name = "Velia" }, new Check_Model()));
            _checkValues.Add(ChangeType.Delete.ToString(), new Tuple<Check_Model, Check_Model>(new Check_Model { Name = "Velia", Surname = "Del Bianco" }, new Check_Model()));

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('{_checkValues[ChangeType.Insert.ToString()].Item1.Name}', '{_checkValues[ChangeType.Insert.ToString()].Item1.Surname}')";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(1000);

                    sqlCommand.CommandText = $"UPDATE [{TableName}] SET [First Name] = '{_checkValues[ChangeType.Update.ToString()].Item1.Name}'";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(1000);

                    sqlCommand.CommandText = $"DELETE FROM [{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(1000);
                }
            }
        }
    }
}