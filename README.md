# Bài tập An toàn và Bảo mật thông tin — AES, RSA, SHA-256

Ứng dụng C#/.NET Windows Forms thực hiện các yêu cầu chính của bài tập. Ví dụ Python ở thư mục gốc được giữ nguyên.

## Chức năng

- AES-256: sinh/lưu/nạp khóa, mã hóa và giải mã văn bản.
- AES-256-GCM cho tệp lớn: streaming theo khối 1 MiB, đo thời gian/tốc độ, so sánh từng byte và SHA-256 sau giải mã.
- RSA-2048: sinh cặp khóa; xuất/nhập PEM; xem PEM và tham số n, e, d, p, q, dP, dQ, qInv.
- RSA-OAEP-SHA256: mã hóa/giải mã byte nhỏ; bản mã Base64; RSA-2048 với OAEP-SHA256 giới hạn bản rõ 190 byte.
- Benchmark RSA: thời gian trung bình mã hóa/giải mã qua 100 lần.
- SHA-256 tự cập nhật khi dữ liệu đầu vào thay đổi.
- Chữ ký RSA-PSS/SHA-256: ký, nạp, xác minh; salt ngẫu nhiên giúp các lần ký cùng dữ liệu thường tạo chữ ký khác nhau.

## Yêu cầu

- Windows 10/11.
- .NET 10 SDK.
- Không cần thư viện NuGet ngoài.

Kiểm tra SDK bằng PowerShell:

    dotnet --info

## Chạy từ thư mục gốc repository

    dotnet restore .\AES_RSA_BAITAP\AesRsaHomework\AesRsaHomework.csproj
    dotnet build .\AES_RSA_BAITAP\AesRsaHomework\AesRsaHomework.csproj
    dotnet run --project .\AES_RSA_BAITAP\AesRsaHomework\AesRsaHomework.csproj

Hoặc mở dự án trong Visual Studio rồi nhấn F5.

## Gợi ý thử nghiệm

1. Sinh AES-256, mã hóa rồi giải mã một câu tiếng Việt.
2. Thử tệp rỗng, tệp nhỏ hơn/lớn hơn 1 MiB, 100 MiB, 1 GiB hoặc hơn nếu ổ đĩa đủ dung lượng.
3. Ghi thời gian, dung lượng chính xác và kết quả so sánh byte/SHA-256.
4. Sinh RSA-2048, xuất/nhập public và private PEM.
5. Thử dữ liệu 32 byte, 190 byte và 191 byte (191 byte cần bị từ chối).
6. Sửa một ký tự ở tab SHA-256 để quan sát hiệu ứng avalanche.
7. Ký cùng nội dung hai lần; xác minh chữ ký, sau đó sửa nội dung để xác minh thất bại.

Tài liệu PEM/DER: AES_RSA_BAITAP/docs/GIAI_THICH_PEM_DER.md
Mẫu báo cáo thực nghiệm: AES_RSA_BAITAP/docs/BAO_CAO_THUC_NGHIEM.md

## Lưu ý bảo mật

- Khóa AES lưu Base64 không có nghĩa là được mã hóa hay bảo vệ bằng mật khẩu.
- Không commit private PEM, khóa thử nghiệm nhạy cảm, bản mã hoặc chữ ký riêng tư lên GitHub.
- Đây là mã nguồn bài tập; chỉ dùng dữ liệu thử nghiệm, không dùng thay hệ thống bảo mật sản xuất.
- Định dạng tệp AES-SAFE là định dạng riêng của bài làm, không phải định dạng chuẩn liên thông với ứng dụng khác.
