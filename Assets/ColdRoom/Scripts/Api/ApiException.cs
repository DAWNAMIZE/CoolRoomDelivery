using System;

namespace ColdRoom.Api
{
    public static class ApiErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string EmailTaken = "EMAIL_TAKEN";
        public const string IllegalTransition = "ILLEGAL_TRANSITION";
        public const string InsufficientStock = "INSUFFICIENT_STOCK";
    }

    /// <summary>An API failure with a stable machine-readable code for the UI.</summary>
    public sealed class ApiException : Exception
    {
        public ApiException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
