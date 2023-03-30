namespace ToolGsm.Models;

public class GsmDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SerialPort SerialPort { get; set; }
    public string PortName { get; set; }
    public bool SimCardRealy { get; set; } = false;
    public bool IsBusy { get; set; } = false;
    public string DataReceived { get; set; } = String.Empty;
    public bool IsError { get; set; } = false;

    public GsmDevice(SerialPort serialPort, string portName)
    {
        SerialPort = serialPort;
        PortName = portName;
    }
}