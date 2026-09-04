using OnSteroidsApi.Domain.Models.Common.BaseModels.Responses;
using System.Net;

namespace OnSteroidsApi.Application.Features.Helpers
{
    public static class BaseResponseHelpers
    {
        public static Tuple<int, string, string, string> SuccessData = new((int)HttpStatusCode.OK, "Operation Successful", "01", "Request is successful");
        public static Tuple<int, string, string, string> ValidationSyntaxErrorData = new((int)HttpStatusCode.BadRequest, "Validation Error", "03", "Invalid request, please check your request");
        public static Tuple<int, string, string, string> ValidationSemanticErrorData = new((int)HttpStatusCode.UnprocessableEntity, "Validation Error", "04", "Invalid request, please check your request");
        public static Tuple<int, string, string, string> ServerErrorData = new((int)HttpStatusCode.InternalServerError, "Error Occured", "08", "Something went wrong with your request, please try again later");



        /// <summary>
        /// This helper returns success response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static (int StatusCode, BaseSuccessResponse<T> Data) ReturnSuccess<T>(string? message, T? data)
        {
            return (
                SuccessData.Item1,
                new BaseSuccessResponse<T>(SuccessData.Item2, SuccessData.Item3, message ?? SuccessData.Item4, data)
            );
        }


        /// <summary>
        /// This helper returns validation syntax of bad request error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static (int StatusCode, BaseErrorResponse Error) ReturnValidationSyntaxError(string? message, IEnumerable<string>? errors)
        {
            return (
                ValidationSyntaxErrorData.Item1,
                new BaseErrorResponse(ValidationSyntaxErrorData.Item2, ValidationSyntaxErrorData.Item3, message ?? ValidationSyntaxErrorData.Item4, errors)
            );
        }


        /// <summary>
        /// This helper returns validation semantic of unprocessable entity error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static (int StatusCode, BaseErrorResponse Error) ReturnValidationSemanticErrorData(string? message, IEnumerable<string>? errors)
        {
            return (
                ValidationSemanticErrorData.Item1,
                new BaseErrorResponse(ValidationSemanticErrorData.Item2, ValidationSemanticErrorData.Item3, message ?? ValidationSemanticErrorData.Item4, errors)
            );
        }


        /// <summary>
        /// This helper returns internal server error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static (int StatusCode, BaseErrorResponse Error) ReturnServerErrorData(string? message, IEnumerable<string>? errors)
        {
            return (
                ServerErrorData.Item1,
                new BaseErrorResponse(ServerErrorData.Item2, ServerErrorData.Item3, message ?? ServerErrorData.Item4, errors)
            );
        }

    }
}
