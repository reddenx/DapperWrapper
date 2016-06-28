using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	class Program
	{
		static void Main()
		{
			var connectionString = @"";//removed for security purposes

			var sql1 = @"SELECT 
                            SurveyQuestionAnswerId AS Id
                            ,SurveyQuestionId AS QuestionId
                            ,AnswerText AS Text
                            ,Sequence
                            ,Score
                            ,AllowTextComment AS AllowComment
                            FROM SurveyQuestionAnswers
                            WHERE SurveyQuestionAnswerId = @Id";
			var parameters1 = new { id = 104 };

			var sql2 = @"SELECT
                            SurveyQuestionId AS Id
                            ,Questiontext AS Text
                            ,Sequence
                            ,NumberAllowedResponses
                            FROM SurveyQuestions
                            WHERE SurveyId = @SurveyId";
			var parameters2 = new { surveyId = 3 };


			var results = DapperWrapper
				.Cmd<AnswerModel>(sql1, parameters1)
				.Cmd<QuestionModel>(sql2, parameters2)
				.Cmd<QuestionModel>(sql2, parameters2)
				.Execute(connectionString);

			var results2 = DapperWrapper
				.Cmd<AnswerModel>(sql1, parameters1)
				.Execute(connectionString);

			Console.ReadLine();

		}

		public class QuestionModel
		{
			public int? Id { get; set; }
			public string Text { get; set; }
			public uint NumberAllowedResponses { get; set; }

			public int Sequence { get; set; }
			public List<AnswerModel> Answers { get; set; }

			public List<int> DependentAnswers { get; set; }

			public QuestionModel()
			{
				this.Answers = new List<AnswerModel>();
				this.DependentAnswers = new List<int>();
			}
		}

		public class AnswerModel
		{
			public int? Id { get; set; }

			public int? QuestionId { get; set; }

			public int Sequence { get; set; }
			public string Text { get; set; }
			public decimal? Score { get; set; }
			public bool AllowComment { get; set; }
		}
	}
}
