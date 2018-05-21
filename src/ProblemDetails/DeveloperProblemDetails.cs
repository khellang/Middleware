using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.StackTrace.Sources;

namespace Hellang.Middleware.ProblemDetails
{
    internal class DeveloperProblemDetails : StatusCodeProblemDetails
    {
        public DeveloperProblemDetails(ExceptionProblemDetails problem, IEnumerable<ExceptionDetails> details)
            : base(problem.Status ?? StatusCodes.Status500InternalServerError)
        {
            Detail = problem.Detail ?? problem.Error.Message;
            Title = problem.Title ?? TypeNameHelper.GetTypeDisplayName(problem.Error.GetType());
            Instance = problem.Instance ?? GetHelpLink(problem.Error);

            if (!string.IsNullOrEmpty(problem.Type))
            {
                Type = problem.Type;
            }

            Errors = GetErrors(details).ToList();
        }

        public IReadOnlyCollection<ErrorDetails> Errors { get; }

        private static IEnumerable<ErrorDetails> GetErrors(IEnumerable<ExceptionDetails> details)
        {
            foreach (var detail in details)
            {
                yield return new ErrorDetails(detail);
            }
        }

        private static string GetHelpLink(Exception exception)
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
                Message = detail.ErrorMessage ?? detail.Error.Message;
                Type = TypeNameHelper.GetTypeDisplayName(detail.Error.GetType());
                StackFrames = GetStackFrames(detail.StackFrames).ToList();
            }

            public string Message { get; }

            public string Type { get; }
            
            public IReadOnlyCollection<StackFrame> StackFrames { get; }
            
            private static IEnumerable<StackFrame> GetStackFrames(IEnumerable<StackFrameSourceCodeInfo> stackFrames)
            {
                foreach (var stackFrame in stackFrames)
                {
                    yield return new StackFrame
                    {
                        File = stackFrame.File,
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

            private static IReadOnlyCollection<string> GetCode(IEnumerable<string> code)
            {
                var list = code.ToList();
                return list.Count > 0 ? list : null;
            }

            public class StackFrame
            {
                public string File { get; set; }
                
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
