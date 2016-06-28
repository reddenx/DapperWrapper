using System;
using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	static class DapperWrapper
	{
		public static ICommand<T> Cmd<T>(string sql, object parameters) where T : class
		{
			return new DwCommand<T>(sql, parameters);
		}
	}

	public interface ICommand<out T>
		where T : class
	{
		T[] Execute(string connectionString);
		ICommandMulti<Tnext> Cmd<Tnext>(string sql, object parameters) where Tnext : class;
	}
	internal interface ICommandAccessor<out T>
		where T : class
	{
		T[] Execute(string connectionString);
		ICommandMulti<Tnext> Cmd<Tnext>(string sql, object parameters) where Tnext : class;
		T[] ExecuteConnection(SqlConnection openConnection);
	}
	internal class DwCommand<T> : ICommand<T>, ICommandAccessor<T>
		where T : class
	{
		private readonly string Sql;
		private readonly object Parameters;

		internal DwCommand(string sql, object parameters)
		{
			this.Sql = sql;
			this.Parameters = parameters;
		}

		public ICommandMulti<Tnext> Cmd<Tnext>(string sql, object parameters) where Tnext : class
		{
			return new DwCommandMulti<Tnext>(sql, parameters, this);
		}

		public T[] ExecuteConnection(SqlConnection openConnection)
		{
			return openConnection.Query<T>(Sql, Parameters).ToArray();
		}

		public T[] Execute(string connectionString)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				return ExecuteConnection(connection);
			}
		}
	}


	public interface ICommandMulti<out T>
		where T : class
	{
		object[][] Execute(string connectionString);
		ICommandMulti<Tnext> Cmd<Tnext>(string sql, object parameters) where Tnext : class;
	}
	internal interface ICommandMultiAccessor
	{
		object[][] Execute(string connectionString);
		object[] ExecuteLocal(SqlConnection openConnection);
		ICommandAccessor<object> PreviousCommand { get; }
		ICommandMultiAccessor PreviousMultiCommand { get; }
	}
	internal class DwCommandMulti<T> : ICommandMulti<T>, ICommandMultiAccessor
		where T : class
	{
		private readonly string Sql;
		private readonly object Parameters;
		public ICommandAccessor<object> PreviousCommand { get; private set; }
		public ICommandMultiAccessor PreviousMultiCommand { get; private set; }

		internal DwCommandMulti(string sql, object parameters, ICommandAccessor<object> previousCommand)
		{
			this.Sql = sql;
			this.Parameters = parameters;
			this.PreviousCommand = previousCommand;
		}

		internal DwCommandMulti(string sql, object parameters, ICommandMultiAccessor previousCommand)
		{
			this.Sql = sql;
			this.Parameters = parameters;
			this.PreviousMultiCommand = previousCommand;
		}

		public ICommandMulti<Tnext> Cmd<Tnext>(string sql, object parameters) where Tnext : class
		{
			return new DwCommandMulti<Tnext>(sql, parameters, this);
		}

		public object[] ExecuteLocal(SqlConnection openConnection)
		{
			return openConnection.Query<T>(Sql, Parameters).ToArray();
		}

		public object[][] Execute(string connectionString)
		{
			var results = new List<object[]>();
			var executionStack = new Stack<ICommandMultiAccessor>();

			//unravel the stack of commands
			ICommandMultiAccessor stackIndex = this;
			//hack to execute that last single command....
			ICommandAccessor<object> firstCommand = null;
			while (stackIndex != null)
			{
				executionStack.Push(stackIndex);
				if (stackIndex.PreviousMultiCommand == null)
				{
					firstCommand = stackIndex.PreviousCommand;
				}
				stackIndex = stackIndex.PreviousMultiCommand;
			}

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				results.Add(firstCommand.ExecuteConnection(connection));


				while (executionStack.Count > 0)
				{
					results.Add(executionStack.Pop().ExecuteLocal(connection));
				}
			}

			return results.ToArray();
		}
	}

}
