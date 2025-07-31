using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.Payment
{
    public class ReturnModel : PageModel
    {
        private readonly IVNPayService _vnPayService;
        private readonly IAuthService _authService;
        private readonly ILogger<ReturnModel> _logger;

        public ReturnModel(
            IVNPayService vnPayService,
            IAuthService authService,
            ILogger<ReturnModel> logger)
        {
            _vnPayService = vnPayService;
            _authService = authService;
            _logger = logger;
        }

        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public int Coins { get; set; }
        public decimal NewBalance { get; set; }
        public string OrderRef { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get all query parameters from VNPay return
                var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
                
                _logger.LogInformation("VNPay return with params: {Params}", string.Join(", ", queryParams.Select(kv => $"{kv.Key}={kv.Value}")));

                // Parse VNPay return data
                var vnpayData = _vnPayService.ParseReturnData(queryParams);
                
                _logger.LogInformation("== BEGIN DETAILED VNPay DEBUG ==");
                _logger.LogInformation("Raw Query Params Count: {Count}", queryParams.Count);
                foreach (var kv in queryParams.OrderBy(x => x.Key))
                {
                    _logger.LogInformation("Query Param: {Key} = {Value}", kv.Key, kv.Value);
                }
                
                _logger.LogInformation("Parsed VNPay Data Count: {Count}", vnpayData.Count);
                foreach (var kv in vnpayData.OrderBy(x => x.Key))
                {
                    _logger.LogInformation("VNPay Data: {Key} = {Value}", kv.Key, kv.Value);
                }
                _logger.LogInformation("== END DETAILED VNPay DEBUG ==");
                
                // Get signature for validation
                if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash))
                {
                    _logger.LogError("Missing vnp_SecureHash in return data");
                    Success = false;
                    ErrorMessage = "Thiếu chữ ký bảo mật";
                    return Page();
                }

                // Validate signature
                if (!_vnPayService.ValidateSignature(vnpayData, vnpSecureHash))
                {
                    _logger.LogError("Invalid VNPay signature");
                    Success = false;
                    ErrorMessage = "Chữ ký không hợp lệ";
                    return Page();
                }

                // Check payment result (VNPay standard - check both codes)
                var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
                var transactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "");
                var orderRef = queryParams.GetValueOrDefault("vnp_TxnRef", "");
                var amount = queryParams.GetValueOrDefault("vnp_Amount", "0");
                
                _logger.LogInformation("VNPay payment result - Code: {Code}, Status: {Status}, OrderRef: {OrderRef}, Amount: {Amount}", 
                    responseCode, transactionStatus, orderRef, amount);

                OrderRef = orderRef;

                // Check both response code and transaction status for success (VNPay standard)
                if (responseCode == "00" && transactionStatus == "00")
                {
                    // Payment successful
                    var result = await ProcessSuccessfulPayment(orderRef, amount);
                    return result ?? Page();
                }
                else if (responseCode == "24")
                {
                    // Payment cancelled by user
                    Success = false;
                    ErrorMessage = "Giao dịch đã được hủy bởi người dùng";
                    _logger.LogInformation("VNPay payment cancelled - Code: {Code}, OrderRef: {OrderRef}", responseCode, orderRef);
                    return Page();
                }
                else
                {
                    // Payment failed
                    Success = false;
                    ErrorMessage = GetErrorMessage(responseCode);
                    _logger.LogWarning("VNPay payment failed - Code: {Code}, Status: {Status}, Message: {Message}", 
                        responseCode, transactionStatus, ErrorMessage);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                Success = false;
                ErrorMessage = "Có lỗi xảy ra khi xử lý thanh toán";
                return Page();
            }
        }

        private async Task<IActionResult?> ProcessSuccessfulPayment(string orderRef, string amountStr)
        {
            try
            {
                // Parse order reference to get user ID and coins
                // Format: COIN_{userId}_{coins}_{timestamp}
                var parts = orderRef.Split('_');
                if (parts.Length < 3 || parts[0] != "COIN")
                {
                    _logger.LogError("Invalid order reference format: {OrderRef}", orderRef);
                    Success = false;
                    ErrorMessage = "Mã đơn hàng không hợp lệ";
                    return null;
                }

                if (!int.TryParse(parts[1], out var userId) || !int.TryParse(parts[2], out var coins))
                {
                    _logger.LogError("Cannot parse user ID or coins from order ref: {OrderRef}", orderRef);
                    Success = false;
                    ErrorMessage = "Thông tin đơn hàng không hợp lệ";
                    return null;
                }

                // Get current user
                var userResult = await _authService.GetUserByIdAsync(userId);
                if (!userResult.Success || userResult.User == null)
                {
                    _logger.LogError("User not found for ID: {UserId}", userId);
                    Success = false;
                    ErrorMessage = "Không tìm thấy thông tin người dùng";
                    return null;
                }

                // Add coins to user account
                var newCoins = userResult.User.Coins + coins;
                var updateResult = await _authService.UpdateUserCoinsAsync(userId, newCoins);
                
                if (!updateResult.Success)
                {
                    _logger.LogError("Failed to update user coins - UserId: {UserId}, NewCoins: {NewCoins}, Error: {Error}", 
                        userId, newCoins, updateResult.Message);
                    Success = false;
                    ErrorMessage = "Có lỗi xảy ra khi cập nhật coins";
                    return null;
                }

                _logger.LogInformation("Successfully added {Coins} coins to user {UserId}. New balance: {NewBalance}", 
                    coins, userId, newCoins);

                // Update session if this is the current user
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == userId)
                {
                    HttpContext.Session.SetString("UserCoins", newCoins.ToString());
                }

                // Set success data
                Success = true;
                Coins = coins;
                NewBalance = newCoins;
                Message = $"Nạp thành công {coins:N0} coins! Số dư hiện tại: {newCoins:N0} coins.";
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing successful payment for order: {OrderRef}", orderRef);
                Success = false;
                ErrorMessage = "Có lỗi xảy ra khi xử lý thanh toán thành công";
                return null;
            }
        }

        private string GetErrorMessage(string responseCode)
        {
            return responseCode switch
            {
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch",
                "99" => "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)",
                _ => "Giao dịch không thành công"
            };
        }
    }
}
