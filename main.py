from Crypto.Cipher import AES
from Crypto.Random import get_random_bytes

# Tạo khóa ngẫu nhiên 16 bytes (128-bit) cho AES
key = get_random_bytes(16)
cipher = AES.new(key, AES.MODE_EAX)

# Dữ liệu cần mã hóa
data = b"Bai tap mon An toan va bao mat thong tin"

# Quy trình mã hóa
ciphertext, tag = cipher.encrypt_and_digest(data)
print("Ban ma hoa (Ciphertext):", ciphertext)

# Quy trình giải mã
cipher_dec = AES.new(key, AES.MODE_EAX, cipher.nonce)
plaintext = cipher_dec.decrypt_and_verify(ciphertext, tag)
print("Ban giai ma (Plaintext):", plaintext.decode('utf-8'))