﻿namespace ApiGsm.Models;

public class Otp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SmsContents { get; set; }
    public int SmsOpt { get; set; } = Random.Shared.Next(100000, 999999);
    public string NumberPhone { get; set; }
    public DateTime CreateOtp { get; set; } = DateTime.Now.AddMinutes(2);

    public Otp(string smsContents, string numberPhone)
    {
        SmsContents = string.Format(smsContents, SmsOpt);
        NumberPhone = numberPhone;
    }
}