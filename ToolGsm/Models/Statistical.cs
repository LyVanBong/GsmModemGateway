namespace ToolGsm.Models;

public class Statistical : BindableBase
{
    private int _totalSim;
    private int _errorSim;
    private int _sms;
    private int _smsSent;

    public int TotalSim
    {
        get => _totalSim;
        set => SetProperty(ref _totalSim, value);
    }

    public int ErrorSim
    {
        get => _errorSim;
        set => SetProperty(ref _errorSim, value);
    }

    public int Sms
    {
        get => _sms;
        set => SetProperty(ref _sms, value);
    }

    public int SmsSent
    {
        get => _smsSent;
        set => SetProperty(ref _smsSent, value);
    }
}