using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Gab.Shared.Base
{
    internal class ResultCommonLogic<TError>
    {
        public bool IsFailure { get; }
        public bool IsSuccess => !IsFailure;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly TError error;

        public TError Error
        {
            [DebuggerStepThrough]
            get
            {
                //if (IsSuccess)
                //    throw new InvalidOperationException("There is no error message for success.");

                return error;
            }
        }

        [DebuggerStepThrough]
        public ResultCommonLogic(bool isFailure, TError error)
        {
            if (isFailure)
            {
                if (error == null)
                    throw new ArgumentNullException(nameof(error), ResultMessages.ErrorObjectIsNotProvidedForFailure);
            }
            else
            {
                if (error != null)
                    throw new ArgumentException(ResultMessages.ErrorObjectIsProvidedForSuccess, nameof(error));
            }

            IsFailure = isFailure;
            this.error = error;
        }

        public void GetObjectData(SerializationInfo oInfo, StreamingContext oContext)
        {
            oInfo.AddValue("IsFailure", IsFailure);
            oInfo.AddValue("IsSuccess", IsSuccess);
            if (IsFailure)
            {
                oInfo.AddValue("Error", Error);
            }
        }
    }

    internal sealed class ResultCommonLogic : ResultCommonLogic<string>
    {
        [DebuggerStepThrough]
        public static ResultCommonLogic Create(bool isFailure, string error)
        {
            if (isFailure)
            {
                if (string.IsNullOrEmpty(error))
                    throw new ArgumentNullException(nameof(error), ResultMessages.ErrorMessageIsNotProvidedForFailure);
            }
            else
            {
                if (error != null)
                    throw new ArgumentException(ResultMessages.ErrorMessageIsProvidedForSuccess, nameof(error));
            }

            return new ResultCommonLogic(isFailure, error);
        }

        public ResultCommonLogic(bool isFailure, string error) : base(isFailure, error)
        {
        }
    }

    internal static class ResultMessages
    {
        public static readonly string ErrorObjectIsNotProvidedForFailure =
            "You have tried to create a failure result, but error object appeared to be null, please review the code, generating error object.";

        public static readonly string ErrorObjectIsProvidedForSuccess =
            "You have tried to create a success result, but error object was also passed to the constructor, please try to review the code, creating a success result.";

        public static readonly string ErrorMessageIsNotProvidedForFailure = "There must be error message for failure.";

        public static readonly string ErrorMessageIsProvidedForSuccess = "There should be no error message for success.";
    }

    public struct Result : ISerializable
    {
        static readonly Result okResult = new Result(false, null);

        void ISerializable.GetObjectData(SerializationInfo oInfo, StreamingContext oContext)
        {
            logic.GetObjectData(oInfo, oContext);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ResultCommonLogic logic;

        public bool IsFailure => logic.IsFailure;
        public bool IsSuccess => logic.IsSuccess;
        public string Error => logic.Error;

        [DebuggerStepThrough]
        [JsonConstructor]
        Result(bool isFailure, string error)
        {
            logic = ResultCommonLogic.Create(isFailure, error);
        }

        [DebuggerStepThrough]
        public static Result Ok()
        {
            return okResult;
        }

        [DebuggerStepThrough]
        public static Result Fail(string error)
        {
            return new Result(true, error);
        }

        [DebuggerStepThrough]
        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(false, value, null);
        }

        [DebuggerStepThrough]
        public static Result<T> Fail<T>(string error)
        {
            return new Result<T>(true, default(T), error);
        }

        [DebuggerStepThrough]
        public static Result<TValue, TError> Ok<TValue, TError>(TValue value) where TError : class
        {
            return new Result<TValue, TError>(false, value, default(TError));
        }

        [DebuggerStepThrough]
        public static Result<TValue, TError> Fail<TValue, TError>(TError error) where TError : class
        {
            return new Result<TValue, TError>(true, default(TValue), error);
        }

        /// <summary>
        /// Returns first failure in the list of <paramref name="results"/>. If there is no failure returns success.
        /// </summary>
        /// <param name="results">List of results.</param>
        [DebuggerStepThrough]
        public static Result FirstFailureOrSuccess(params Result[] results)
        {
            foreach (var result in results)
            {
                if (result.IsFailure)
                    return Fail(result.Error);
            }

            return Ok();
        }

        /// <summary>
        /// Returns failure which combined from all failures in the <paramref name="results"/> list. Error messages are separated by <paramref name="errorMessagesSeparator"/>. 
        /// If there is no failure returns success.
        /// </summary>
        /// <param name="errorMessagesSeparator">Separator for error messages.</param>
        /// <param name="results">List of results.</param>
        [DebuggerStepThrough]
        public static Result Combine(string errorMessagesSeparator, params Result[] results)
        {
            var failedResults = results.Where(x => x.IsFailure).ToList();

            if (!failedResults.Any())
                return Ok();

            var errorMessage = string.Join(errorMessagesSeparator, failedResults.Select(x => x.Error).ToArray());
            return Fail(errorMessage);
        }

        [DebuggerStepThrough]
        public static Result Combine(params Result[] results)
        {
            return Combine(", ", results);
        }

        [DebuggerStepThrough]
        public static Result Combine<T>(params Result<T>[] results)
        {
            return Combine(", ", results);
        }

        [DebuggerStepThrough]
        public static Result Combine<T>(string errorMessagesSeparator, params Result<T>[] results)
        {
            var untyped = results.Select(result => (Result)result).ToArray();
            return Combine(errorMessagesSeparator, untyped);
        }
    }

    public struct Result<T> : ISerializable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ResultCommonLogic logic;

        public bool IsFailure => logic.IsFailure;
        public bool IsSuccess => logic.IsSuccess;
        public string Error => logic.Error;

        void ISerializable.GetObjectData(SerializationInfo oInfo, StreamingContext oContext)
        {
            logic.GetObjectData(oInfo, oContext);

            if (IsSuccess)
            {
                oInfo.AddValue("Value", Value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly T value;

        public T Value
        {
            [DebuggerStepThrough]
            get
            {
                //if (!IsSuccess)
                //    throw new InvalidOperationException("There is no value for failure.");

                return value;
            }
        }

        [DebuggerStepThrough]
        [JsonConstructor]
        internal Result(bool isFailure, T value, string error)
        {
            if (!isFailure && value == null)
                throw new ArgumentNullException(nameof(value));

            logic = ResultCommonLogic.Create(isFailure, error);
            this.value = value;
        }

        public static implicit operator Result(Result<T> result)
        {
            return result.IsSuccess ? Result.Ok() : Result.Fail(result.Error);
        }
    }

    public struct Result<TValue, TError> : ISerializable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ResultCommonLogic<TError> logic;

        public bool IsFailure => logic.IsFailure;
        public bool IsSuccess => logic.IsSuccess;
        public TError Error => logic.Error;

        void ISerializable.GetObjectData(SerializationInfo oInfo, StreamingContext oContext)
        {
            logic.GetObjectData(oInfo, oContext);

            if (IsSuccess)
            {
                oInfo.AddValue("Value", Value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly TValue value;

        public TValue Value
        {
            [DebuggerStepThrough]
            get
            {
                //if (!IsSuccess)
                //    throw new InvalidOperationException("There is no value for failure.");

                return value;
            }
        }

        [DebuggerStepThrough]
        [JsonConstructor]
        internal Result(bool isFailure, TValue value, TError error)
        {
            if (!isFailure && value == null)
                throw new ArgumentNullException(nameof(value));

            logic = new ResultCommonLogic<TError>(isFailure, error);
            this.value = value;
        }

        public static implicit operator Result(Result<TValue, TError> result)
        {
            return result.IsSuccess ? Result.Ok() : Result.Fail(result.Error.ToString());
        }

        public static implicit operator Result<TValue>(Result<TValue, TError> result)
        {
            return result.IsSuccess ? Result.Ok(result.Value) : Result.Fail<TValue>(result.Error.ToString());
        }
    }
}
