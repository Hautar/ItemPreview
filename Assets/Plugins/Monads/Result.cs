using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monads
{
    public static class Result
    {
        public const int ErrorCode = 400;
        public const int WarningCode = 304;
        
        public static Result<T> Ok<T>(T value, string message = "")
        {
            Result<T> result = default;
            
            result.IsSuccess = true;
            result.Value = value;
            result.Code = 200;
            result.Message = message;

            return result;
        }

        public static Result<T> Error<T>(string message = "", int code = ErrorCode)
        {
            Result<T> result = default;
            
            result.IsSuccess = false;
            result.Value = default;
            result.Message = message;
            result.Code = code;

            return result;
        }

        public static Result<T> Warning<T>(string message = "", int code = WarningCode)
        {
            Result<T> result = default;

            result.IsSuccess = false;
            result.Value = default;
            result.Message = message;
            result.Code = code;

            return result;
        }

        public static Result<T> Error<T>(Error error) => Error<T>(error.Message, error.Code);

        public static Result<IEnumerable<T>> CombineAll<T>(IEnumerable<Result<T>> results)
        {
            var list = results.ToList();
            
            var allSuccess = list.All(x => x.IsSuccess);
            var allValues = list.Select(x => x.Value);
            var allMessage = string.Join("\n", list
                .Where(x => !string.IsNullOrWhiteSpace(x.Message))
                .Select(x => x.Message));

            return allSuccess ? Ok(allValues, allMessage) : Error<IEnumerable<T>>(allMessage);
        }
        
        public static Result<bool> Ok(string message = "") => Ok(true, message);

        public static Result<bool> Warning(string message = "", int code = WarningCode) => Warning<bool>(message, code);

        public static Result<bool> Error(string message = "", int code = ErrorCode) => Error<bool>(message, code);

        public static Result<bool> Error(Error error) => Error<bool>(error.Message, error.Code);

        public static Result<bool> Validate(bool result, string message = "") => result ? Ok(message) : Error(message);
    }

    public struct Result<T>
    {
        public bool IsSuccess { get; internal set; }
        public T Value { get; internal set; }
        
        public string Message { get; internal set; }
        public int Code { get; internal set; }

        public bool TryGet(out T result)
        {
            if (!IsSuccess)
            {
                result = default;
                return false;
            }
            
            result = Value;
            return true;
        }
        
        public bool TryGetError(out Error result)
        {
            if (IsSuccess)
            {
                result = default;
                return false;
            }
            
            result = new Error(Code, Message);
            return true;
        }
    }

    public readonly struct Error
    {
        public readonly int Code;
        public readonly string Message;

        public Error(int code, string message)
        {
            Code = code;
            Message = message;
        }
        
        public override string ToString() => $"Code: {Code} Message: {Message}";
    }

    public static class ErrorExtension
    {
        public static bool IsWarning(this Error error) => error.Code == Result.WarningCode;

        public static void LogError(this ILogger source, Error error, string prefix = "")
        {
            switch (error.Code)
            {
                case Result.WarningCode:
                    source.Log(LogType.Warning, $"{prefix} {error}");
                    break;
                
                default:
                    source.Log(LogType.Error, $"{prefix} {error}");
                    break;
            }
        }
    }
}
