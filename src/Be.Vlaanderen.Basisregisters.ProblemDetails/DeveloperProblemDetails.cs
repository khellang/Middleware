namespace Be.Vlaanderen.Basisregisters.BasicApiProblem
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Internal;
    using Microsoft.Extensions.StackTrace.Sources;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;

    internal static class DeveloperProblemDetailsExtensions
    {
        public static ProblemDetails WithExceptionDetails(
            this ExceptionProblemDetails problem,
            IEnumerable<ExceptionDetails> details) =>
            new DeveloperProblemDetails(problem, details);
    }

    [DataContract(Name = "DeveloperProblemDetails", Namespace = "")]
    internal class DeveloperProblemDetails : StatusCodeProblemDetails
    {
        [JsonProperty("errors", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Default)]
        [DataMember(Name = "errors", Order = 600, EmitDefaultValue = false)]
        public IReadOnlyCollection<ErrorDetails> Errors { get; }

        // Here to make DataContractSerializer happy
        public DeveloperProblemDetails() : base(StatusCodes.Status500InternalServerError) { }

        public DeveloperProblemDetails(
            ExceptionProblemDetails problem,
            IEnumerable<ExceptionDetails> details)
            : base(problem.HttpStatus ?? StatusCodes.Status500InternalServerError)
        {
            Detail = problem.Detail ?? problem.Error.Message;
            Title = problem.Title ?? TypeNameHelper.GetTypeDisplayName(problem.Error.GetType());
            ProblemInstanceUri = problem.ProblemInstanceUri ?? GetHelpLink(problem.Error);

            if (!string.IsNullOrEmpty(problem.ProblemTypeUri))
                ProblemTypeUri = problem.ProblemTypeUri;

            Errors = GetErrors(details).ToList();
        }

        private static IEnumerable<ErrorDetails> GetErrors(IEnumerable<ExceptionDetails> details)
            => details.Select(detail => new ErrorDetails(detail));

        private static string GetHelpLink(Exception exception)
        {
            var link = exception.HelpLink;

            if (string.IsNullOrEmpty(link))
                return null;

            if (Uri.TryCreate(link, UriKind.Absolute, out var result))
                return result.ToString();

            return null;
        }

        public class ErrorDetails
        {
            public string Message { get; }

            public string Type { get; }

            public string Raw { get; }

            public IReadOnlyCollection<StackFrame> StackFrames { get; }

            public ErrorDetails(ExceptionDetails detail)
            {
                Raw = detail.Error.ToString();
                Message = detail.ErrorMessage ?? detail.Error.Message;
                Type = TypeNameHelper.GetTypeDisplayName(detail.Error.GetType());
                StackFrames = GetStackFrames(detail.StackFrames).ToList();
            }

            private static IEnumerable<StackFrame> GetStackFrames(IEnumerable<StackFrameSourceCodeInfo> stackFrames)
                => stackFrames
                    .Select(stackFrame => new StackFrame
                    {
                        FilePath = stackFrame.File,
                        FileName = string.IsNullOrEmpty(stackFrame.File) ? null : Path.GetFileName(stackFrame.File),
                        Function = stackFrame.Function,
                        Line = GetLineNumber(stackFrame.Line),
                        PreContextLine = GetLineNumber(stackFrame.PreContextLine),
                        PreContextCode = GetCode(stackFrame.PreContextCode),
                        ContextCode = GetCode(stackFrame.ContextCode),
                        PostContextCode = GetCode(stackFrame.PostContextCode),
                    });

            private static int? GetLineNumber(int lineNumber)
                => lineNumber == 0 ? (int?) null : lineNumber;

            private static IReadOnlyCollection<string> GetCode(IEnumerable<string> code)
            {
                var list = code.ToList();
                return list.Count > 0 ? list : null;
            }

            public class StackFrame
            {
                public string FilePath { get; set; }

                public string FileName { get; set; }

                public string Function { get; set; }

                public int? Line { get; set; }

                public int? PreContextLine { get; set; }

                public IReadOnlyCollection<string> PreContextCode { get; set; }

                public IReadOnlyCollection<string> ContextCode { get; set; }

                public IReadOnlyCollection<string> PostContextCode { get; set; }
            }
        }
    }
}
