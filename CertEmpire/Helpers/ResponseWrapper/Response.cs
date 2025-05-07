using System.Text.Json.Serialization;

namespace CertEmpire.Helpers.ResponseWrapper
{
    public class Response<TData>
    {
        #region Constructors

        public Response()
        {
        }

        public Response(bool success, string message, string error, TData? data)
        {
            this.Success = success;
            this.Message = message;
            this.Error = error;
            this.Data = data;
        }

        #endregion Constructors

        #region Properties

        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("Error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("Data")]
        public TData? Data { get; set; } // ✅ Allow null values

        #endregion Properties
    }
}