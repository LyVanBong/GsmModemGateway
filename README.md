# GsmModemGateway 📟
> Dịch vụ API điều khiển GSM Modem để tự động gửi/nhận SMS (Worker Service).

![GitHub last commit](https://img.shields.io/github/last-commit/LyVanBong/GsmModemGateway)

## 📝 Giới Thiệu
**GsmModemGateway** là một .NET Worker Service được thiết kế để kết nối và điều khiển các thiết bị GSM Modem/Dongle qua cổng COM/Serial. Dịch vụ cung cấp HTTP API để các ứng dụng khác có thể gửi lệnh SMS, USSD một cách dễ dàng.

## 🚀 Tính Năng
-   **REST API**: Gửi tin nhắn, kiểm tra tài khoản (USSD) qua HTTP.
-   **Queue Management**: Quản lý hàng đợi tin nhắn thông minh.
-   **Docker Support**: Dễ dàng triển khai trên Linux/Raspberry Pi.

## 🛠 Công Nghệ
-   **Framework**: .NET Core Worker Service.
-   **Library**: System.IO.Ports / GSMComm.

## 📞 Liên Hệ
-   **GitHub**: [LyVanBong](https://github.com/LyVanBong)
