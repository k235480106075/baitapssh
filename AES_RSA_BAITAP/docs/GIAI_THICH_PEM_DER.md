# Giải thích cấu trúc PEM và DER

PEM là định dạng văn bản bao ngoài dữ liệu nhị phân DER bằng Base64 và các dòng BEGIN/END. DER dùng ASN.1 theo dạng tag-length-value; không có offset cố định cho mọi khóa vì độ dài INTEGER có thể thay đổi.

## Public key dạng SubjectPublicKeyInfo

- Nhãn BEGIN PUBLIC KEY thường biểu thị SubjectPublicKeyInfo.
- Cấu trúc ngoài là SEQUENCE, gồm AlgorithmIdentifier và BIT STRING.
- BIT STRING chứa cấu trúc RSAPublicKey gồm hai INTEGER là n và e.

## Private key dạng PKCS#8

- Nhãn BEGIN PRIVATE KEY thường biểu thị PrivateKeyInfo/PKCS#8.
- Cấu trúc gồm version, AlgorithmIdentifier và OCTET STRING chứa dữ liệu private key.
- Nhãn BEGIN RSA PRIVATE KEY thường dùng cho khóa riêng RSA theo PKCS#1.

## Ý nghĩa tham số RSA

- n (modulus): tích của hai số nguyên tố p và q.
- e (public exponent): số mũ công khai, thường là 65537.
- d (private exponent): số mũ bí mật.
- p và q: hai số nguyên tố tạo nên modulus.
- dP và dQ: số mũ rút gọn phục vụ tối ưu CRT.
- qInv: nghịch đảo modular của q theo modulo p.

Ứng dụng hiển thị nguyên văn PEM và các tham số mà .NET giải mã được. Nếu yêu cầu bảng offset của từng byte DER, cần bổ sung bộ phân tích ASN.1 TLV dựa trên chính tệp PEM thực tế; không gán offset mẫu cố định cho mọi khóa.

Lưu ý: không commit private PEM thật hoặc khóa thử nghiệm nhạy cảm lên GitHub.
