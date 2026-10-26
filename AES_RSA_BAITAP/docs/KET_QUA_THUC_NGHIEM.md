# Báo cáo kết quả thực nghiệm AES, RSA và SHA-256

## 1. Mục tiêu

Kiểm thử các chức năng mã hóa và giải mã AES-256-GCM, RSA-2048/OAEP-SHA256, tính giá trị băm SHA-256 và ký/xác minh chữ ký số RSA-PSS/SHA-256 trong ứng dụng Windows Forms.

## 2. Môi trường thực nghiệm

- Hệ điều hành: Windows.
- Nền tảng: C# Windows Forms, .NET SDK 10.0.402.
- Ứng dụng: `AES_RSA_BAITAP/AesRsaHomework`.

## 3. Kiểm thử AES-256-GCM với tệp lớn

Ứng dụng xử lý tệp theo từng khối 1 MiB. Sau khi giải mã, chương trình so sánh dữ liệu byte và giá trị SHA-256 của tệp nguồn với tệp đã giải mã.

| Kích thước tệp | Thời gian mã hóa | Thời gian giải mã | Kết quả |
|---|---:|---:|---|
| 100 MB | 0,389 giây | 0,288 giây | Byte và SHA-256 trùng khớp |
| 500 MiB | 1,098 giây | 1,028 giây | Byte và SHA-256 trùng khớp |
| 1 GiB | 1,605 giây | 2,090 giây | Byte và SHA-256 trùng khớp |
| 2 GiB | 22,914 giây | 4,501 giây | Byte và SHA-256 trùng khớp |

**Nhận xét:** Các lần thử đã ghi nhận kết quả so sánh byte và SHA-256 trùng khớp sau giải mã với tệp có kích thước đến 2 GiB. Điều này cho thấy dữ liệu giải mã khôi phục đúng dữ liệu ban đầu trong các lần thử trên.

## 4. Kiểm thử RSA-2048 và định dạng PEM

Đã thực hiện các thao tác sinh cặp khóa RSA-2048, xuất khóa public/private dưới định dạng PEM và nạp lại các khóa. Trạng thái ứng dụng xác nhận khóa công khai và khóa riêng đã được nạp; kết quả kiểm tra nguồn khóa hiển thị **CÓ**.

### 4.1. Mã hóa và giải mã RSA-OAEP-SHA256

- Dữ liệu thử nghiệm: `tuan123`.
- Kết quả giải mã khớp với dữ liệu đầu vào.
- Đã mã hóa khóa AES 32 byte bằng RSA và giải mã thành công; kết quả hiển thị `Byte=32` và `khớp nguồn=CÓ`.
- Kết quả benchmark 100 lần:
  - Thời gian mã hóa trung bình: **0,0144 ms/lần**.
  - Thời gian giải mã trung bình: **0,4377 ms/lần**.

### 4.2. Kiểm tra giới hạn dữ liệu

Với khóa RSA-2048 và OAEP-SHA256, độ dài bản rõ tối đa là 190 byte.

| Độ dài dữ liệu đầu vào | Kết quả |
|---|---|
| 190 byte | Mã hóa thành công |
| 191 byte | Chương trình từ chối mã hóa và thông báo dữ liệu vượt giới hạn 190 byte |

**Nhận xét:** Chương trình chấp nhận dữ liệu ở giới hạn 190 byte và từ chối dữ liệu 191 byte, phù hợp với giới hạn của cấu hình RSA-OAEP-SHA256 đang sử dụng.

## 5. Kiểm thử SHA-256

- Giá trị băm đầu ra có độ dài 256 bit, được biểu diễn bằng 64 ký tự hệ hexadecimal.
- Khi thay đổi dữ liệu đầu vào từ `tuan123` sang `tuan124`, giá trị băm thu được thay đổi.
- SHA-256 là hàm băm một chiều, không phải thuật toán mã hóa có thể giải mã ngược.

**Nhận xét:** Kết quả thử nghiệm minh họa rằng thay đổi dữ liệu đầu vào tạo ra giá trị băm khác nhau.

## 6. Kiểm thử chữ ký số RSA-PSS/SHA-256

- Đã ký dữ liệu và nạp chữ ký để xác minh.
- Khi xác minh đúng dữ liệu đã ký, chương trình hiển thị **CHỮ KÝ HỢP LỆ**.
- Khi thay đổi dữ liệu đầu vào xác minh, chương trình hiển thị **CHỮ KÝ KHÔNG HỢP LỆ**.

**Nhận xét:** Kết quả cho thấy bước xác minh phân biệt được dữ liệu khớp với dữ liệu đã ký và dữ liệu đã bị thay đổi.

## 7. Kết luận

Các chức năng AES-256-GCM, RSA-2048/OAEP-SHA256, SHA-256 và chữ ký số RSA-PSS/SHA-256 đã được kiểm thử. Với các lần thử đã ghi nhận, dữ liệu tệp sau giải mã AES trùng khớp với dữ liệu nguồn đến kích thước 2 GiB; RSA mã hóa/giải mã dữ liệu nhỏ thành công trong giới hạn cho phép; SHA-256 tạo giá trị băm 256 bit; và kiểm tra chữ ký cho kết quả hợp lệ hoặc không hợp lệ tương ứng với dữ liệu xác minh.

**Lưu ý bảo mật:** Không đưa khóa riêng tư `rsa-private.pem` hoặc khóa bí mật dùng trong thử nghiệm lên repository công khai.
