# Kết quả thực nghiệm AES / RSA / SHA-256

> Tài liệu này ghi lại kết quả người dùng đã báo cáo hoặc ảnh chụp giao diện đã thể hiện. Những số đo chưa được ghi lại được để trống, không tự ước lượng.

## 1. Môi trường thử nghiệm

- Hệ điều hành: Windows (theo môi trường chạy ứng dụng).
- Ứng dụng: C# Windows Forms, dự án `AES_RSA_BAITAP/AesRsaHomework`.
- SDK .NET: .NET SDK 10.0.402 đã được xác nhận bằng `dotnet --info`.
- Ngày kiểm thử chính xác, CPU, RAM và ổ lưu trữ: chưa ghi lại.

## 2. AES-256-GCM

Người dùng báo đã chạy thử các kích thước tệp sau:

| Tệp thử nghiệm | Kết quả được báo cáo | Thời gian mã hóa | Thời gian giải mã | Byte/SHA-256 sau giải mã |
|---|---|---:|---:|---|
| 100 MB | Hoàn thành | 0,389s(~257,03MiB/s) |0,288s |  ` TRÙNG KHỚP` và SHA-256 nguồn/giải mã giống nhau |
| 500 MiB | Hoàn thành | 1,098s(~455,53MiB/s) | 1,028s |  ` TRÙNG KHỚP` và SHA-256 nguồn/giải mã giống nhau |
| 1 GiB | Hoàn thành | 1,605s?(~638MiB/s) | 2,090s | ` TRÙNG KHỚP` và SHA-256 nguồn/giải mã giống nhau |
| 2 GiB | Hoàn thành | 22,914s(~88,38MiB/s) | 4,501 | ` TRÙNG KHỚP` và SHA-256 nguồn/giải mã giống nhau |

Chương trình mô tả cơ chế AES-GCM xử lý tệp theo từng khối 1 MiB. Cần ghi thêm thời gian, tốc độ và kết quả so sánh byte/SHA-256 riêng cho từng kích thước nếu báo cáo yêu cầu bảng benchmark đầy đủ.

## 3. RSA-2048, PEM và OAEP-SHA256

- Sinh cặp khóa RSA-2048: đã thực hiện.
- Xuất tệp public PEM và private PEM: đã thực hiện.
- Nạp lại public PEM và private PEM: đã thực hiện.
- Mã hóa rồi giải mã chuỗi `tuan123`: giao diện hiển thị `khớp nguồn=CÓ`.
- Mã hóa khóa AES 32 byte bằng RSA: ảnh chụp hiển thị `Byte=32` và `khớp nguồn=CÓ`.
- Benchmark 100 lần: giao diện hiển thị:
  - Mã hóa trung bình: **0,0144 ms/lần**.
  - Giải mã trung bình: **0,4377 ms/lần**.
- Giới hạn RSA-2048/OAEP-SHA256: người dùng báo đã thử đầu vào 190 byte và 191 byte. Kết quả chi tiết của phép thử 191 byte chưa được ghi lại ở đây. Theo giới hạn OAEP-SHA256 của khóa RSA-2048, bản rõ tối đa là 190 byte; 191 byte phải bị từ chối.

**Lưu ý bảo mật:** không đưa `rsa-private.pem`, khóa thử nghiệm hoặc dữ liệu bí mật lên repository công khai.

## 4. SHA-256

- Đã nhập dữ liệu và quan sát mã băm SHA-256 dài 64 ký tự hexadecimal (256 bit).
- Người dùng báo đã thay đổi đầu vào để quan sát kết quả băm thay đổi.
- Ảnh chụp hiển thị đầu vào `tuan124` nhưng bộ đếm giao diện ghi 13 ký tự / 13 byte. Chuỗi ASCII `tuan124` thực tế có 7 ký tự / 7 byte UTF-8; cần chạy lại phép thử và chụp ảnh có bộ đếm đúng trước khi dùng làm minh chứng cuối cùng.
- SHA-256 là hàm băm một chiều, không phải phép mã hóa có thể giải mã ngược.

## 5. Chữ ký số RSA-PSS/SHA-256

- Đã thực hiện thao tác ký và xác minh chữ ký trong ứng dụng.
- Khi dữ liệu trên giao diện là `tuan124`, ứng dụng hiển thị **`CHỮ KÝ KHÔNG HỢP LỆ`** trong một lần xác minh. Kết quả này phù hợp với trường hợp dữ liệu đã bị thay đổi so với dữ liệu được ký.
- Người dùng sau đó xác nhận đã hoàn thành việc xác minh lại với dữ liệu gốc và các bước tiếp theo.
- Cần lưu ảnh minh chứng riêng cho cả hai trường hợp: dữ liệu gốc cho kết quả chữ ký hợp lệ; dữ liệu bị sửa cho kết quả chữ ký không hợp lệ.
- Với RSA-PSS dùng salt ngẫu nhiên, hai chữ ký của cùng dữ liệu và cùng khóa thường khác nhau nhưng vẫn có thể xác minh hợp lệ.

## 6. Tóm tắt

Các chức năng chính đã được người dùng chạy thử: AES-256-GCM với nhiều kích thước tệp, RSA-2048 và PEM, mã hóa/giải mã OAEP-SHA256, benchmark RSA, SHA-256 và chữ ký RSA-PSS. Bảng này chỉ ghi số liệu thực sự đã quan sát được; không có thời gian AES hoặc kết quả chi tiết nào được tự suy đoán.

### Những thông tin nên bổ sung nếu cần báo cáo định lượng

1. Thời gian mã hóa/giải mã AES và tốc độ MiB/s cho từng kích thước.
2. Kết quả byte/SHA-256 riêng cho các tệp 500 MiB, 1 GiB và 2 GiB.
3. Kết quả cụ thể của đầu vào RSA 191 byte.
4. Ảnh chụp SHA-256 sau khi bộ đếm ký tự/byte được kiểm tra lại.
5. Thông số CPU, RAM và ngày chạy thử.
