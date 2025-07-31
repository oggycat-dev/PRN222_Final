using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace FinalProject.Pages.VNPay
{
    public class PaymentReturnModel : PageModel
    {
        private readonly IVNPayService _vnPayService;
        private readonly IAuthService _authService;
        private readonly ILogger<PaymentReturnModel> _logger;

        public PaymentReturnModel(
            IVNPayService vnPayService, 
            IAuthService authService,
            ILogger<PaymentReturnModel> logger)
        {
            _vnPayService = vnPayService;
            _authService = authService;
            _logger = logger;
        }

        public PaymentResultViewModel PaymentResult { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
                var vnpayData = _vnPayService.ParseReturnData(queryParams);

                if (vnpayData.Count == 0)
                {
                    PaymentResult = new PaymentResultViewModel 
                    { 
                        Success = false, 
                        Message = "Không nhận được dữ liệu từ VNPay" 
                    };
                    return Page();
                }

                var vnp_SecureHash = queryParams.GetValueOrDefault("vnp_SecureHash", "");
                var isValidSignature = _vnPayService.ValidateSignature(vnpayData, vnp_SecureHash);

                if (!isValidSignature)
                {
                    PaymentResult = new PaymentResultViewModel 
                    { 
                        Success = false, 
                        Message = "Chữ ký không hợp lệ" 
                    };
                    return Page();
                }

                var responseCode = vnpayData.GetValueOrDefault("vnp_ResponseCode", "");
                var transactionStatus = vnpayData.GetValueOrDefault("vnp_TransactionStatus", "");
                var orderRef = vnpayData.GetValueOrDefault("vnp_TxnRef", "");
                var amount = long.Parse(vnpayData.GetValueOrDefault("vnp_Amount", "0")) / 100; // Convert back from VNPay format

                if (responseCode == "00" && transactionStatus == "00")
                {
                    // Parse order reference to get user ID and coin amount
                    var orderParts = orderRef.Split('_');
                    if (orderParts.Length >= 3)
                    {
                        var userIdStr = orderParts[1];
                        var coinsStr = orderParts[2];

                        if (int.TryParse(userIdStr, out var userId) && int.TryParse(coinsStr, out var coins))
                        {
                            // Update user coins using AuthService for consistency
                            var userResult = await _authService.GetUserByIdAsync(userId);
                            if (userResult.Success && userResult.User != null)
                            {
                                var newCoins = userResult.User.Coins + coins;
                                var updateResult = await _authService.UpdateUserCoinsAsync(userId, newCoins);
                                
                                if (updateResult.Success)
                                {
                                    _logger.LogInformation($"VNPay payment successful: User {userId} received {coins} coins");

                                    // Update session with new coin balance for consistency
                                    HttpContext.Session.SetString("UserCoins", newCoins.ToString());

                                    PaymentResult = new PaymentResultViewModel 
                                    { 
                                        Success = true, 
                                        Message = $"Thanh toán thành công! Bạn đã nhận được {coins:N0} coins.",
                                        Amount = amount,
                                        Coins = coins,
                                        TransactionRef = orderRef
                                    };
                                    return Page();
                                }
                            }
                        }
                    }

                    PaymentResult = new PaymentResultViewModel 
                    { 
                        Success = false, 
                        Message = "Lỗi xử lý đơn hàng" 
                    };
                    return Page();
                }
                else
                {
                    var errorMessage = GetVNPayErrorMessage(responseCode);
                    PaymentResult = new PaymentResultViewModel 
                    { 
                        Success = false, 
                        Message = $"Thanh toán thất bại: {errorMessage}" 
                    };
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment return");
                PaymentResult = new PaymentResultViewModel 
                { 
                    Success = false, 
                    Message = "Có lỗi xảy ra khi xử lý thanh toán" 
                };
                return Page();
            }
        }

        private string GetVNPayErrorMessage(string responseCode)
        {
            return responseCode switch
            {
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => "Giao dịch thất bại"
            };
        }
    }

    public class PaymentResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long Amount { get; set; }
        public int Coins { get; set; }
        public string TransactionRef { get; set; } = "";
    }
}
