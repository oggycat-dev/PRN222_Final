using System.Collections.Generic;

namespace Services.Interfaces
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(long amount, string orderInfo, string orderRef, string returnUrl);
        bool ValidateSignature(Dictionary<string, string> vnpayData, string inputHash);
        Dictionary<string, string> ParseReturnData(Dictionary<string, string> queryParams);
    }
}
