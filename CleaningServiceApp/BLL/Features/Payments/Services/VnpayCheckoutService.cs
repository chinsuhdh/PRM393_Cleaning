using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Cleaning.BLL.Common;
using Microsoft.Extensions.Options;

namespace Cleaning.BLL.Features.Payments;

public class VnpayCheckoutService : IVnpayCheckoutService
{
    private readonly VnpaySettings _settings;

    public VnpayCheckoutService(IOptions<VnpaySettings> options)
    {
        _settings = options.Value;
    }

    public VnpayCheckoutLink CreatePaymentUrl(decimal amount, string orderInfo, string ipAddress)
    {
        var txnRef = GenerateTxnRef();
        var now = DateTime.UtcNow.AddHours(7);

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)Math.Round(amount, MidpointRounding.AwayFromZero) * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss")
        };

        var hashData = BuildHashData(parameters);
        var secureHash = Sign(hashData);

        var query = string.Join("&", parameters.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
        var paymentUrl = $"{_settings.BaseUrl}?{query}&vnp_SecureHash={secureHash}";

        return new VnpayCheckoutLink(txnRef, paymentUrl);
    }

    public VnpayCallbackResult VerifyCallback(IReadOnlyDictionary<string, string> queryParams)
    {
        var receivedHash = queryParams.GetValueOrDefault("vnp_SecureHash", "");

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParams)
        {
            if (kv.Key is "vnp_SecureHash" or "vnp_SecureHashType") continue;
            parameters[kv.Key] = kv.Value;
        }

        var hashData = BuildHashData(parameters);
        var expectedHash = Sign(hashData);
        var signatureValid = string.Equals(expectedHash, receivedHash, StringComparison.OrdinalIgnoreCase);

        var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
        var transactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "");
        var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "");
        var amount = queryParams.TryGetValue("vnp_Amount", out var amountStr) &&
                     long.TryParse(amountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amountRaw)
            ? amountRaw / 100m
            : 0m;
        var transactionNo = queryParams.GetValueOrDefault("vnp_TransactionNo");

        var success = signatureValid && responseCode == "00" && transactionStatus == "00";

        return new VnpayCallbackResult(signatureValid, success, txnRef, amount, transactionNo, responseCode);
    }

    private static string BuildHashData(SortedDictionary<string, string> parameters) =>
        string.Join("&", parameters.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));

    private string Sign(string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_settings.HashSecret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateTxnRef() =>
        DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..8];
}
