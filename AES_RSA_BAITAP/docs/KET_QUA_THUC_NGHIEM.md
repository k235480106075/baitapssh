# Báo cáo kết quả thực nghiệm AES, RSA và SHA-256

## 1. Mục tiêu

Kiểm thử các chức năng mã hóa và giải mã AES-256-GCM, RSA-2048/OAEP-SHA256, tính giá trị băm SHA-256 và ký/xác minh chữ ký số RSA-PSS/SHA-256 trong ứng dụng Windows Forms.

## 2. Môi trường thực nghiệm

- Hệ điều hành: Windows.
- Nền tảng: C# Windows Forms, .NET SDK 10.0.402.
- Ứng dụng: `AES_RSA_BAITAP/AesRsaHomework`.

## 3. Kết quả kiểm thử AES-256-GCM

Ứng dụng xử lý tệp theo từng khối 1 MiB. Các kích thước tệp dưới đây đã được chạy thử; sau giải mã, giao diện hiển thị kết quả so sánh byte và SHA-256 nguồn/giải mã là trùng khớp.

| Kích thước tệp | Thời gian mã hóa | Thời gian giải mã | Kết quả kiểm tra |
|---|---:|---:|---|
| 100 MB | 0,389 giây | 0,288 giây | Byte và SHA-256 trùng khớp |
| 500 MiB | 1,098 giây | 1,028 giây | Byte và SHA-256 trùng khớp |
| 1 GiB | 1,605 giây* | 2,090 giây | Byte và SHA-256 trùng khớp |
| 2 GiB | 22,914 giây | 4,501 giây | Byte và SHA-256 trùng khớp |

* Giá trị thời gian mã hóa 1 GiB được ghi lại từ kết quả thử nghiệm hiện có; cần đối chiếu lại với ảnh chụp gốc nếu sử dụng để so sánh hiệu năng chính thức.

**Nhận xét:** Các phép thử đã hoàn thành với tệp đến 2 GiB. Kết quả byte và SHA-256 trùng khớp cho thấy dữ liệu sau giải mã giống dữ liệu ban đầu trong các lần thử đã ghi nhận.

## 4. Kết quả kiểm thử RSA-2048 và PEM

Các thao tác sinh khóa RSA-2048, xuất khóa public/private PEM và nạp lại hai khóa đã được thực hiện. Sau khi nạp khóa, ứng dụng báo trạng thái public và private key đã được nạp; kiểm tra nguồn khóa hiển thị **CÓ**.

### 4.1. Mã hóa và giải mã bằng RSA-OAEP-SHA256

- Chuỗi thử nghiệm: `tuan123`.
- Kết quả giải mã: khớp với dữ liệu đầu vào.
- Mã hóa khóa AES dài 32 byte bằng RSA: thành công; kết quả giải mã hiển thị `Byte=32` và `khớp nguồn=CÓ`.
- Benchmark 100 lần:
  - Mã hóa trung bình: **0,0144 ms/lần**.
  - Giải mã trung bình: **0,4377 ms/lần**.

### 4.2. Kiểm tra giới hạn độ dài dữ liệu

Với RSA-2048 sử dụng OAEP-SHA256, độ dài bản rõ tối đa là 190 byte.

| Dữ liệu đầu vào | Kết quả |
|---|---|
| 190 byte | Mã hóa thành công |
| 191 byte | Ứng dụng từ chối và thông báo dữ liệu vượt giới hạn 190 byte |

Kết quả cho thấy ứng dụng xử lý đúng giới hạn độ dài của RSA-2048/OAEP-SHA256.

## 5. Kết quả kiểm thử SHA-256

- Ứng dụng tạo giá trị băm SHA-256 có độ dài 256 bit, biểu diễn bằng 64 ký tự hệ hexadecimal.
- Khi thay đổi dữ liệu đầu vào, giá trị băm hiển thị thay đổi rõ rệt.
- SHA-256 là hàm băm một chiều, không phải thuật toán mã hóa có thể giải mã ngược.

## 6. Kết quả kiểm thử chữ ký số RSA-PSS/SHA-256

- Đã thực hiện thao tác ký dữ liệu và nạp chữ ký để xác minh.
- Với dữ liệu gốc không thay đổi, ứng dụng hiển thị **CHỮ KÝ HỢP LỆ**.
- Khi dữ liệu xác minh được thay đổi, ứng dụng hiển thị **CHỮ KÝ KHÔNG HỢP LỆ**.

Kết quả minh họa rằng chữ ký hợp lệ khi dữ liệu xác minh khớp với dữ liệu đã ký và khóa xác minh tương ứng; thay đổi dữ liệu làm cho bước xác minh thất bại.

## 7. Kết luận

Các chức năng chính đã được kiểm thử gồm AES-256-GCM với tệp đến 2 GiB, RSA-2048 với khóa PEM, RSA-OAEP-SHA256, SHA-256 và chữ ký số RSA-PSS/SHA-256. Các lần thử được ghi nhận cho thấy giải mã AES khôi phục dữ liệu trùng khớp; RSA mã hóa/giải mã dữ liệu nhỏ thành công trong giới hạn cho phép; SHA-256 tạo mã băm 256 bit; và cơ chế xác minh chữ ký phân biệt được dữ liệu hợp lệ với dữ liệu đã bị thay đổi.

**Lưu ý bảo mật:** Không đưa khóa riêng tư `rsa-private.pem`, khóa AES hoặc dữ liệu bí mật lên repository công khai.
