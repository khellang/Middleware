using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.StackTrace.Sources;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Hellang.Middleware.ProblemDetails
{
    internal static class DeveloperProblemDetailsExtensions
    {
        public static MvcProblemDetails WithExceptionDetails(this MvcProblemDetails problem, string propertyName, Exception error, IEnumerable<ExceptionDetails> details)
        {
            problem.Title ??= TypeNameHelper.GetTypeDisplayName(error.GetType());
            problem.Extensions[propertyName] = GetErrors(details).ToList();
            problem.Status ??= StatusCodes.Status500InternalServerError;
            problem.Instance ??= GetHelpLink(error);
            problem.Detail ??= error.Message;
            return problem;
        }

        private static IEnumerable<ErrorDetails> GetErrors(IEnumerable<ExceptionDetails> details)
        {
            foreach (var detail in details)
            {
                yield return new ErrorDetails(detail);
            }
        }

        private static string? GetHelpLink(Exception exception)
        {
            var link = exception.HelpLink;

            if (string.IsNullOrEmpty(link))
            {
                return null;
            }

            if (Uri.TryCreate(link, UriKind.Absolute, out var result))
            {
                return result.ToString();
            }

            return null;
        }

        public class ErrorDetails
        {
            public ErrorDetails(ExceptionDetails detail)
            {
                Raw = detail.Error?.ToString();
                Message = detail.ErrorMessage ?? detail.Error?.Message;
                Type = TypeNameHelper.GetTypeDisplayName(detail.Error);
                StackFrames = GetStackFrames(detail.StackFrames).ToList();
            }

            public string? Message { get; }

            public string? Type { get; }

            public string? Raw { get; }

            public IReadOnlyCollection<StackFrame> StackFrames { get; }

            private static IEnumerable<StackFrame> GetStackFrames(IEnumerable<StackFrameSourceCodeInfo> stackFrames)
            {
                foreach (var stackFrame in stackFrames)
                {
                    yield return new StackFrame
                    {
                        FilePath = stackFrame.File,
                        FileName = string.IsNullOrEmpty(stackFrame.File) ? null : Path.GetFileName(stackFrame.File),
                        Function = stackFrame.Function,
                        Line = GetLineNumber(stackFrame.Line),
                        PreContextLine = GetLineNumber(stackFrame.PreContextLine),
                        PreContextCode = GetCode(stackFrame.PreContextCode),
                        ContextCode = GetCode(stackFrame.ContextCode),
                        PostContextCode = GetCode(stackFrame.PostContextCode),
                    };
                }
            }

            private static int? GetLineNumber(int lineNumber)
            {
                if (lineNumber == 0)
                {
                    return null;
                }

                return lineNumber;
            }

            private static IReadOnlyCollection<string>? GetCode(IEnumerable<string> code)
            {
                var list = code.ToList();
                return list.Count > 0 ? list : null;
            }

            public class StackFrame
            {
                public string? FilePath { get; set; }

                public string? FileName { get; set; }

                public string? Function { get; set; }

                public int? Line { get; set; }

                public int? PreContextLine { get; set; }

                public IReadOnlyCollection<string>? PreContextCode { get; set; }

                public IReadOnlyCollection<string>? ContextCode { get; set; }

                public IReadOnlyCollection<string>? PostContextCode { get; set; }
            }
        }
    }
}
