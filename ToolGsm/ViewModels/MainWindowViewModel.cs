using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ToolGsm.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private List<SerialPort> _serialPort = new List<SerialPort>();

        private Dictionary<string, string> _recport = new Dictionary<string, string>();

        private Dictionary<string, int> _numberPort = new Dictionary<string, int>();

        private Dictionary<string, bool> _recordIff = new Dictionary<string, bool>();
        private Dictionary<string, bool> _onoffSim = new Dictionary<string, bool>();
        private Dictionary<string, int> _keyidbyte = new Dictionary<string, int>();
        private List<string> _inRiff = new List<string>();
        private Dictionary<byte[], string> _recordValua = new Dictionary<byte[], string>();

        public MainWindowViewModel()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                string[] ports = SerialPort.GetPortNames();
                int countport = ports.Count();
                if (countport > 35)
                {
                    Array.Sort(ports, (string a, string b) => int.Parse(Regex.Replace(a, "[^0-9]", "")) - int.Parse(Regex.Replace(b, "[^0-9]", "")));
                }
                List<string> list = new List<string>(ports);
                int count = 0;
                IEnumerable<string> lstPortfullname = from p in searcher.Get().Cast<ManagementBaseObject>().ToList()
                                                      select p["Caption"].ToString();
                foreach (string port in list)
                {
                    if (lstPortfullname.FirstOrDefault((string s) => s.Contains("(" + port + ")") && s.Contains("XR21V1414")) != null)
                    {
                        SerialPort sp = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                        sp.Handshake = Handshake.None;
                        sp.DataReceived += SerialPort_DataReceived;
                        sp.ReadTimeout = 3000;
                        sp.WriteTimeout = 3000;
                        _recport.Add(sp.PortName, "");
                        _serialPort.Add(sp);
                        _numberPort.Add(sp.PortName, count);
                        _onoffSim.Add(sp.PortName, value: true);
                        _recordIff.Add(sp.PortName, value: false);
                        _keyidbyte.Add(sp.PortName, 1);
                        SendSms(sp, "0356977131", "Ban da nhan duoc 100 ti do la tu peter");
                        count++;
                    }
                }
            }
        }

        public void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string rec = "";
            string rec2 = "";
            SerialPort sp = (SerialPort)sender;
            byte[] buffer = new byte[sp.ReadBufferSize];
            int bytesRead = 0;
            try
            {
                bytesRead = sp.Read(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
            }
            rec += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            rec2 = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            _recport[sp.PortName] += rec;
            if (rec.Contains("CPIN: NOT READY"))
            {
                rec = "";
                int count = _numberPort[sp.PortName];
                _recport[sp.PortName] = "";
            }
            if (rec.Contains("Call Ready"))
            {
                rec = "";
                _recport[sp.PortName] = "";
            }
            if (_recport[sp.PortName].Contains("CMT"))
            {
            }

            if (!rec.Contains("+CMTI") && !rec.Contains("CMT"))
            {
            }

            if (_recport[sp.PortName].Contains("RING"))
            {
                _recport[sp.PortName] = "";
                sp.Write("ATA\r");
                Thread.Sleep(1000);
                sp.Write("AT+QFDEL=\"RAM:*\"\r");
                Thread.Sleep(1000);
                sp.Write("AT+QAUDRD=1,\"RAM:voice.wav\",13;\r");
                Thread.Sleep(2 * 1000);
                sp.Write("AT+QAUDRD=0;\r");
                Thread.Sleep(1000);
                sp.Write("ATH\r");
                Thread.Sleep(3000);
                sp.Write("AT+QFDWL=\"RAM:voice.wav\";\r");
            }
        }

        public void SendSms(SerialPort sp, string numnerPhone, string Content)
        {
            int count = _numberPort[sp.PortName];
            string Status = "";
            bool IsPDU = false;
            string prevContent = "";
            try
            {
                if (!sp.IsOpen)
                {
                    sp.Open();
                }
            }
            catch
            {
                return;
            }
            _recport[sp.PortName] = "";
            sp.Write("AT+CPIN?; \r");
            Thread.Sleep(500);
            if (!_recport[sp.PortName].Contains("CPIN: READY"))
            {
                return;
            }

            sp.Write("AT+CSCS=\"GSM\"; \r");
            Thread.Sleep(100);
            sp.Write("AT+CMGF=1; \r");
            Thread.Sleep(100);
            sp.Write("AT+CSMP=17,173,0,0; \r");
            Thread.Sleep(100);
            numnerPhone = "\"" + numnerPhone + "\"";

            sp.Write("AT+CMGS=" + numnerPhone + "\r");
            Thread.Sleep(1000);
            sp.Write(Content + "\u001a");
            Thread.Sleep(6000);
            int CheckSMS = 0;
            while (true)
            {
                if (_recport[sp.PortName].Contains("CMS ERROR"))
                {
                    Status = "CMS ERROR";
                    Content = "Please check again";
                    break;
                }
                if (_recport[sp.PortName].Contains("CMGS:"))
                {
                    Status = "Success";
                    break;
                }
                if (_recport[sp.PortName].Contains("CME ERROR"))
                {
                    Status = "CME ERROR";
                    Content = _recport[sp.PortName];
                    break;
                }
                Thread.Sleep(1000);
                if (CheckSMS < 2)
                {
                    CheckSMS++;
                    continue;
                }
                Status = "Not response";
                Content = _recport[sp.PortName];
                break;
            }
            _recport[sp.PortName] = "";
            if (IsPDU)
            {
                Content = prevContent;
            }
        }
    }
}