namespace ToolGsm.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "SMS OTP (Peter Ly)";
        private List<Otp> _otps = new();
        private readonly string _urlOtp = "https://otp.ole777nhacai.com/api/v1/otp/all";
        private bool _isGetSmsOtp = false;
        private List<GsmDevice> _devices = new();
        private bool _isSendSms = false;
        private bool _isInitialized = false;
        private Statistical _statistical = new();
        private ObservableCollection<Logging> _loggings = new();

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {
            _ = InitialSerialPort();

            _ = InitialTeleBot();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += DoWork;
            timer.Start();
        }

        private async Task InitialTeleBot()
        {
            var botClient = new TelegramBotClient("5807966733:AAHQWANZTzZFXkGEa0zBzVTg-UV6cxzAtko");

            using CancellationTokenSource cts = new();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cts.Cancel();
        }

        private async Task<string?> ChatGpt(string content)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Add("Authorization", "Bearer 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");
                var d = $@"{{""model"":""gpt-3.5-turbo"",""messages"":[{{""role"":""user"",""content"":""{content}""}}]}}";
                var body = new StringContent(d, null, "application/json");
                request.Content = body;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine(json);

                if (string.IsNullOrWhiteSpace(json))
                    return "";

                ChatGpt? data = JsonConvert.DeserializeObject<ChatGpt>(json);

                return data?.choices[0]?.message?.content;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

            var responseChat = await ChatGpt(messageText);

            if (string.IsNullOrWhiteSpace(responseChat)) return;
            // Echo received message text
            Telegram.Bot.Types.Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responseChat,
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        public Statistical Statistical
        {
            get => _statistical;
            set => SetProperty(ref _statistical, value);
        }

        public ObservableCollection<Logging> Loggings
        {
            get => _loggings;
            set => SetProperty(ref _loggings, value);
        }

        /// <summary>
        /// khởi tạo Com đang kết nỗi
        /// </summary>
        /// <returns></returns>
        private async Task InitialSerialPort()
        {
            _isInitialized = true;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                string[] ports = SerialPort.GetPortNames();
                int countport = ports.Count();
                if (countport > 35)
                {
                    Array.Sort(ports, (string a, string b) => int.Parse(Regex.Replace(a, "[^0-9]", "")) - int.Parse(Regex.Replace(b, "[^0-9]", "")));
                }
                List<string> list = new List<string>(ports);
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
                        _devices.Add(new GsmDevice(sp, sp.PortName));

                        try
                        {
                            if (!sp.IsOpen)
                            {
                                sp.Open();
                            }
                        }
                        catch
                        {
                        }
                        sp.Write("AT+CPIN?; \r");
                        await Task.Delay(TimeSpan.FromSeconds(3));
                    }
                }
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Gửi opt
        /// </summary>
        /// <returns></returns>
        private Task SendSms()
        {
            if (_isInitialized) return Task.CompletedTask;
            if (_isSendSms) return Task.CompletedTask;
            _isSendSms = true;
            try
            {
                var smsOtp = _otps.Where(x => !x.Status).ToList();
                if (!smsOtp.Any())
                {
                    _isSendSms = false;
                    return Task.CompletedTask;
                }
                var devices = _devices.Where(x => x.SimCardRealy && !x.IsBusy && !x.IsError).ToList();
                if (!devices.Any())
                {
                    _devices.ForEach(x => x.IsBusy = false);
                    _isSendSms = false;
                    return Task.CompletedTask;
                }

                var countDevice = devices.Count;
                var countSmsOpt = smsOtp.Count;

                int count = countDevice > countSmsOpt ? countSmsOpt : countDevice;

                for (int i = 0; i < count; i++)
                {
                    var device = devices[i];
                    var otp = smsOtp[i];
                    device.IsBusy = true;
                    otp.Status = true;
                    _ = Sms(device, otp);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            _isSendSms = false;
            return Task.CompletedTask;
        }

        private void DoWork(object sender, EventArgs e)
        {
            _ = GetSmsOtp();
            _ = SendSms();
            _ = StatisticalTotal();
            _ = CheckSimCards();
        }

        private Task CheckSimCards()
        {
            if (_isInitialized) return Task.CompletedTask;
            if (Statistical.TotalSim == 0) return Task.CompletedTask;
            if (Statistical.TotalSim == Statistical.ErrorSim)
                MessageBox.Show("Tất cả sim đang lỗi vui lòng khởi động lại phần mềm", "Thông báo", MessageBoxButton.OK);
            return Task.CompletedTask;
        }

        private void Logging(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Thực hiện thay đổi trên CollectionView tại đây
                Loggings.Add(new Logging(message));
            });
        }

        private Task StatisticalTotal()
        {
            Statistical.TotalSim = _devices.Where(x => x.SimCardRealy).Count();
            Statistical.ErrorSim = _devices.Where(x => x.SimCardRealy && x.IsError).Count();
            Statistical.Sms = _otps.Where(x => !x.Status).Count();
            Statistical.SmsSent = _otps.Where(x => x.Status).Count();
            return Task.CompletedTask;
        }

        /// <summary>
        /// lấy ds otp
        /// </summary>
        /// <returns></returns>
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

                Logging($"Lấy được {data?.Count} otp");
                if (data != null && data.Any())
                {
                    _otps.AddRange(data);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            _isGetSmsOtp = false;
        }

        /// <summary>
        /// Phản hồi từ thiết bị Gsm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            byte[] buffer = new byte[sp.ReadBufferSize];
            int bytesRead = 0;
            var device = _devices.FirstOrDefault(x => x.PortName == sp.PortName);
            if (device != null)
            {
                try
                {
                    bytesRead = sp.Read(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                device.DataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Logging(device.DataReceived);
                if (device.DataReceived.Contains("CPIN: READY"))
                {
                    device.SimCardRealy = true;
                }
            }
        }

        /// <summary>
        /// Sms
        /// </summary>
        /// <param name="device"></param>
        /// <param name="numnerPhone"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task Sms(GsmDevice device, Otp smsOtp)
        {
            string numnerPhone = smsOtp.NumberPhone;
            string content = smsOtp.SmsContents;
            var sp = device.SerialPort;
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
            sp.Write("AT+CPIN?; \r");
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            if (!device.SimCardRealy)
            {
                return;
            }

            sp.Write("AT+CSCS=\"GSM\"; \r");
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            sp.Write("AT+CMGF=1; \r");
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            sp.Write("AT+CSMP=17,173,0,0; \r");
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            numnerPhone = "\"" + numnerPhone + "\"";
            sp.Write("AT+CMGS=" + numnerPhone + "\r");
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            sp.Write(content + "\u001a");
            await Task.Delay(TimeSpan.FromMilliseconds(6000));

            int checkSms = 0;
            while (true)
            {
                if (device.DataReceived.Contains("CMS ERROR"))
                {
                    device.IsError = false;
                    smsOtp.Status = false;
                    break;
                }
                if (device.DataReceived.Contains("CMGS:"))
                {
                    break;
                }
                if (device.DataReceived.Contains("CME ERROR"))
                {
                    device.IsError = false;
                    smsOtp.Status = false;
                    break;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                if (checkSms < 2)
                {
                    checkSms++;
                    continue;
                }
                break;
            }
        }
    }
}