using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;

namespace Services.Implementations
{
    public class VNPayService : IVNPayService
    {
        public const string VERSION = "2.1.0";
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreatePaymentUrl(long amount, string orderInfo, string orderRef, string returnUrl)
        {
            var vnp_TmnCode = _configuration["VNPay:TmnCode"] ?? "";
            var vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? "";
            var vnpUrl = _configuration["VNPay:Url"];

            // Use SortedList with VnPayCompare for proper ordering (following VNPay standard)
            var vnpParams = new SortedList<string, string>(new VnPayCompare());
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

            // Add required parameters (following VNPay standard order)
            vnpParams.Add("vnp_Version", VERSION);
            vnpParams.Add("vnp_Command", "pay");
            vnpParams.Add("vnp_TmnCode", vnp_TmnCode);
            vnpParams.Add("vnp_Amount", (amount * 100).ToString());
            vnpParams.Add("vnp_CurrCode", "VND");
            vnpParams.Add("vnp_TxnRef", orderRef);
            vnpParams.Add("vnp_OrderInfo", orderInfo);
            vnpParams.Add("vnp_OrderType", "other");
            vnpParams.Add("vnp_Locale", "vn");
            vnpParams.Add("vnp_ReturnUrl", returnUrl);
            vnpParams.Add("vnp_IpAddr", "127.0.0.1");
            vnpParams.Add("vnp_CreateDate", createDate);
            vnpParams.Add("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            // Create payment URL using VNPay standard method
            var paymentUrl = CreateRequestUrl(vnpUrl, vnp_HashSecret, vnpParams);
            
            _logger.LogInformation("VNPay Payment URL created: {PaymentUrl}", paymentUrl);
            return paymentUrl;
        }

        public bool ValidateSignature(Dictionary<string, string> vnpayData, string inputHash)
        {
            var vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? "";

            _logger.LogInformation("== BEGIN VERIFY VNPay ==");
            _logger.LogInformation("Input SecureHash: {SecureHash}", inputHash);
            _logger.LogInformation("HashSecret: {HashSecret}", vnp_HashSecret);

            try
            {
                // Create SortedList with VnPayCompare for proper ordering
                var responseData = new SortedList<string, string>(new VnPayCompare());
                
                foreach (var param in vnpayData)
                {
                    if (!string.IsNullOrEmpty(param.Key) && param.Key.StartsWith("vnp_"))
                    {
                        responseData.Add(param.Key, param.Value);
                    }
                }

                // Remove hash parameters
                if (responseData.ContainsKey("vnp_SecureHashType"))
                {
                    responseData.Remove("vnp_SecureHashType");
                }
                if (responseData.ContainsKey("vnp_SecureHash"))
                {
                    responseData.Remove("vnp_SecureHash");
                }

                _logger.LogInformation("Filtered data for validation:");
                foreach (var kv in responseData)
                {
                    _logger.LogInformation("  {Key} = {Value}", kv.Key, kv.Value);
                }

                // Create response data string using VNPay standard method
                var data = new StringBuilder();
                foreach (var kv in responseData)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                    }
                }
                
                // Remove last '&'
                if (data.Length > 0)
                {
                    data.Remove(data.Length - 1, 1);
                }

                var hashData = data.ToString();
                _logger.LogInformation("Recreated hashData: {HashData}", hashData);

                // Compute hash using VNPay standard method
                var computedHash = VnPayUtils.HmacSHA512(vnp_HashSecret, hashData);
                _logger.LogInformation("Recalculated SecureHash: {ComputedHash}", computedHash);

                var isValid = computedHash.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
                _logger.LogInformation("VNPay Signature Valid: {IsValid}", isValid);
                _logger.LogInformation("== END VERIFY VNPay ==");

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating VNPay signature");
                return false;
            }
        }

        public Dictionary<string, string> ParseReturnData(Dictionary<string, string> queryParams)
        {
            var result = new Dictionary<string, string>();
            
            foreach (var param in queryParams)
            {
                if (param.Key.StartsWith("vnp_"))
                {
                    result[param.Key] = param.Value;
                }
            }

            return result;
        }

        #region VNPay Standard Library Methods

        /// <summary>
        /// Create payment URL following VNPay standard library
        /// </summary>
        private string CreateRequestUrl(string baseUrl, string hashSecret, SortedList<string, string> requestData)
        {
            var data = new StringBuilder();
            foreach (var kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            
            string queryString = data.ToString();
            baseUrl += "?" + queryString;
            
            string signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }
            
            string vnpSecureHash = VnPayUtils.HmacSHA512(hashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;
            
            return baseUrl;
        }

        #endregion
    }

    /// <summary>
    /// VNPay utilities following VNPay standard library
    /// </summary>
    public static class VnPayUtils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }

    /// <summary>
    /// VNPay comparer for proper parameter ordering following VNPay standard library
    /// </summary>
    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
