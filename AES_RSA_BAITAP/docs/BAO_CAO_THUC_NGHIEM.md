# Mẫu báo cáo thực nghiệm AES / RSA

Ghi kết quả đo thực tế; không điền số ước tính như thể đó là số liệu đã thử nghiệm.

## 1. Môi trường

- Ngày thử nghiệm:
- Windows:
- CPU:
- RAM:
- .NET SDK:
- Ổ lưu trữ:
- Commit đang thử nghiệm:

## 2. Thử nghiệm AES

| Tệp nguồn | Kích thước chính xác (byte) | Mã hóa (giây) | Giải mã (giây) | Mã hóa (MiB/s) | Giải mã (MiB/s) | Byte trùng? | SHA-256 trùng? |
|---|---:|---:|---:|---:|---:|---|---|
| Tệp rỗng | | | | | | | |
| Nhỏ hơn 1 MiB | | | | | | | |
| Lớn hơn 1 MiB | | | | | | | |
| 100 MiB | | | | | | | |
| 1 GiB | | | | | | | |
| 2 GiB trở lên | | | | | | | |

Nhận xét tốc độ, overhead kích thước tệp mã hóa, khả năng xử lý tệp lớn và kết quả so sánh dữ liệu.

## 3. Thử nghiệm RSA

- Kích thước khóa:
- Padding mã hóa: OAEP-SHA256
- Số lần benchmark:
- Kích thước dữ liệu (byte):
- Mã hóa trung bình (ms/lần):
- Giải mã trung bình (ms/lần):
- Dữ liệu khôi phục khớp nguồn?

Thử các dữ liệu 32, 190 và 191 byte. Với RSA-2048 + OAEP-SHA256, 191 byte phải bị từ chối.

## 4. SHA-256 / chữ ký số

| Thử nghiệm | Kết quả quan sát |
|---|---|
| Cùng đầu vào SHA-256 hai lần | |
| Đổi một ký tự | |
| Ký cùng dữ liệu hai lần | |
| Xác minh chữ ký đúng | |
| Sửa dữ liệu rồi xác minh | |
| Dùng public key không tương ứng | |

## 5. Kết luận

Kết luận dựa trên số liệu thực tế về tốc độ, tính chính xác sau giải mã, giới hạn RSA, hiệu ứng avalanche và tính hợp lệ của chữ ký RSA-PSS.
