namespace ApiGsm.Utils;

public static class DatabaseOtp
{
    private static List<Otp> _otps = new();

    public static int AddOtp(string numberPhone)
    {
        Otp otp = new Otp("Ma dang ky Ole777 cua ban la: {0}", numberPhone);
        _otps.Add(otp);
        return otp.SmsOpt;
    }

    public static List<Otp> GetAllOtp()
    {
        List<Otp> otp = new List<Otp>(_otps);
        DeleteOtp(otp);
        return otp;
    }

    private static int DeleteOtp(List<Otp> otps)
    {
        return _otps.RemoveAll(otp => otps.Contains(otp));
    }
}