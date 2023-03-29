using System.Text.Json;
using System.Windows.Threading;

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
                        count++;
                    }
                }
            }

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(15);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            _ = GetSmsOtp();
        }

        private List<Otp> _otps = new();
        private readonly string _urlOtp = "https://otp.ole777nhacai.com/api/v1/otp/all";
        private bool _isGetSmsOtp = false;

        private async Task GetSmsOtp()
        {
            if (_isGetSmsOtp) return;
            _isGetSmsOtp = true;
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, _urlOtp);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var data = JsonSerializer.Deserialize<List<Otp>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (data != null && data.Any())
                {
                    _otps.AddRange(data);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                _isGetSmsOtp = false;
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            rec += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            rec2 = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            _recport[sp.PortName] += rec;
        }

        public void SendSms(SerialPort sp, string numnerPhone, string Content)
        {
            int count = _numberPort[sp.PortName];
            string status = "";
            bool isPdu = false;
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
                    status = "CMS ERROR";
                    Content = "Please check again";
                    break;
                }
                if (_recport[sp.PortName].Contains("CMGS:"))
                {
                    status = "Success";
                    break;
                }
                if (_recport[sp.PortName].Contains("CME ERROR"))
                {
                    status = "CME ERROR";
                    Content = _recport[sp.PortName];
                    break;
                }
                Thread.Sleep(1000);
                if (CheckSMS < 2)
                {
                    CheckSMS++;
                    continue;
                }
                status = "Not response";
                Content = _recport[sp.PortName];
                break;
            }
            _recport[sp.PortName] = "";
            if (isPdu)
            {
                Content = prevContent;
            }
        }
    }
}