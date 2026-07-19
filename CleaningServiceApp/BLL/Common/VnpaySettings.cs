namespace Cleaning.BLL.Common;

public class VnpaySettings
{
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnUrl { get; set; } = "";
}
