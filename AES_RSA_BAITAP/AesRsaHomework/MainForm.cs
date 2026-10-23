using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AesRsaHomework.Crypto;

namespace AesRsaHomework;

public sealed class MainForm : Form
{
    byte[]? aesKey; string? originalFile, decodedFile; RSA? pub, priv; string? pubPem, privPem; byte[]? lastPlain;
    readonly TextBox aesIn = Box(), aesOut = Box(), filePath = Box(false), rsaIn = Box(), rsaOut = Box(), rsaPlain = Box(), keyInfo = Box(), pem = Box(), hashIn = Box(), hashOut = Box(), signIn = Box(), sig = Box();
    readonly Label aesStatus = new() { AutoSize = true }, rsaStatus = new() { AutoSize = true }, hashStatus = new() { AutoSize = true }, signStatus = new() { AutoSize = true };

    public MainForm()
    {
        Text = "Bài tập An toàn và Bảo mật thông tin — AES / RSA";
        Width = 1100; Height = 850; StartPosition = FormStartPosition.CenterScreen;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(AesTab()); tabs.TabPages.Add(RsaTab()); tabs.TabPages.Add(HashTab()); tabs.TabPages.Add(SignTab());
        Controls.Add(tabs);
        FormClosed += (_, _) => { pub?.Dispose(); priv?.Dispose(); };
    }

    TabPage AesTab()
    {
        var p = Page("AES-256");
        Add(p, Group("Khóa AES-256", Buttons(
            Btn("Sinh khóa", (_, _) => Run(() => { aesKey = AesFileCrypto.GenerateKey(); aesStatus.Text = "Đã sinh khóa 32 byte."; })),
            Btn("Lưu khóa...", (_, _) => Run(() => { EnsureAes(); using var d = new SaveFileDialog { Filter = "Key (*.key)|*.key" }; if (d.ShowDialog(this) == DialogResult.OK) AesFileCrypto.SaveKey(d.FileName, aesKey!); })),
            Btn("Nạp khóa...", (_, _) => Run(() => { using var d = new OpenFileDialog { Filter = "Key (*.key)|*.key|Text (*.txt)|*.txt" }; if (d.ShowDialog(this) == DialogResult.OK) { aesKey = AesFileCrypto.LoadKey(d.FileName); aesStatus.Text = "Đã nạp khóa AES-256."; } })), aesStatus)));
        Add(p, Group("Mã hóa / giải mã văn bản", Stack(Label("Văn bản gốc UTF-8:"), aesIn, Label("Bản mã Base64:"), aesOut,
            Buttons(Btn("Mã hóa", (_, _) => Run(() => { EnsureAes(); aesOut.Text = AesFileCrypto.EncryptText(aesIn.Text, aesKey!); })),
                    Btn("Giải mã", (_, _) => Run(() => { EnsureAes(); aesIn.Text = AesFileCrypto.DecryptText(aesOut.Text, aesKey!); }))))));
        var fileButtons = Buttons(filePath,
            Btn("Chọn nguồn...", (_, _) => Run(() => { using var d = new OpenFileDialog(); if (d.ShowDialog(this) == DialogResult.OK) { filePath.Text = d.FileName; originalFile = d.FileName; } })),
            Btn("Mã hóa tệp...", async (_, _) => await EncryptFileAsync()),
            Btn("Giải mã tệp...", async (_, _) => await DecryptFileAsync()),
            Btn("So sánh byte + SHA-256", (_, _) => Run(CompareFiles)), aesStatus);
        filePath.Width = 600;
        Add(p, Group("Tệp lớn: AES-GCM theo từng khối 1 MiB", Stack(fileButtons,
            Label("Không nạp toàn bộ tệp vào RAM. Bấm so sánh sau giải mã để so sánh byte và mã băm SHA-256."),
            Label("Khóa lưu dạng Base64 không có mật khẩu. Không đưa khóa riêng tư lên GitHub."))));
        return p;
    }

    TabPage RsaTab()
    {
        var p = Page("RSA / PEM");
        Add(p, Group("Khóa RSA và PEM", Stack(Buttons(
            Btn("Sinh cặp RSA-2048", (_, _) => Run(GenerateRsa)),
            Btn("Xuất public PEM...", (_, _) => Run(() => SavePem(false))),
            Btn("Xuất private PEM...", (_, _) => Run(() => SavePem(true))),
            Btn("Nạp public PEM...", (_, _) => Run(() => LoadPem(false))),
            Btn("Nạp private PEM...", (_, _) => Run(() => LoadPem(true))),
            Btn("Hiện public PEM", (_, _) => pem.Text = pubPem ?? ""),
            Btn("Hiện private PEM", (_, _) => pem.Text = privPem ?? ""), rsaStatus),
            Label("Tham số RSA n, e, d, p, q, dP, dQ, qInv:"), keyInfo, Label("Nội dung PEM:"), pem)));
        Add(p, Group("RSA-OAEP-SHA256: mã hóa dữ liệu nhỏ", Stack(Label("Đầu vào UTF-8:"), rsaIn,
            Buttons(Btn("Mã hóa và lưu Base64...", (_, _) => Run(EncryptRsa)),
                    Btn("Mã hóa khóa AES 32 byte...", (_, _) => Run(EncryptAesKey)),
                    Btn("Nạp bản mã Base64...", (_, _) => Run(LoadRsaCipher)),
                    Btn("Giải mã", (_, _) => Run(DecryptRsa)),
                    Btn("Benchmark 100 lần", async (_, _) => await BenchmarkRsaAsync())),
            Label("Bản mã Base64:"), rsaOut, Label("Kết quả giải mã / HEX:"), rsaPlain,
            Label("RSA-2048/OAEP-SHA256 hỗ trợ tối đa 190 byte. Không dùng RSA mã hóa tệp lớn."))));
        return p;
    }

    TabPage HashTab()
    {
        var p = Page("SHA-256");
        hashIn.TextChanged += (_, _) => UpdateHash();
        Add(p, Group("SHA-256 tự cập nhật", Stack(Label("Đầu vào:"), hashIn, Label("Kết quả HEX (64 ký tự):"), hashOut, hashStatus,
            Label("Cùng dữ liệu cho cùng mã băm. Thay đổi nhỏ thường làm kết quả thay đổi rất lớn (hiệu ứng avalanche). SHA-256 không thể giải mã ngược."))));
        UpdateHash(); return p;
    }

    TabPage SignTab()
    {
        var p = Page("Chữ ký số RSA-PSS");
        Add(p, Group("Ký và xác minh", Stack(Label("Dữ liệu cần ký/xác minh:"), signIn,
            Buttons(Btn("Ký và lưu...", (_, _) => Run(SignData)), Btn("Nạp chữ ký...", (_, _) => Run(LoadSignature)), Btn("Xác minh", (_, _) => Run(VerifySignature))),
            Label("Chữ ký Base64:"), sig, signStatus,
            Label("RSA-PSS/SHA-256 sử dụng salt ngẫu nhiên; cùng dữ liệu và private key thường cho chữ ký khác nhau."))));
        return p;
    }

    async Task EncryptFileAsync()
    {
        try {
            EnsureAes(); if (!File.Exists(filePath.Text)) throw new FileNotFoundException("Chọn nguồn trước.");
            using var d = new SaveFileDialog { Filter = "AES file (*.aes)|*.aes", FileName = Path.GetFileName(filePath.Text) + ".aes" };
            if (d.ShowDialog(this) != DialogResult.OK) return;
            var source = filePath.Text; var dest = d.FileName; long bytes = new FileInfo(source).Length; var key = aesKey!.ToArray(); var sw = Stopwatch.StartNew();
            await Task.Run(() => AesFileCrypto.EncryptFile(source, dest, key)); sw.Stop(); originalFile = source;
            aesStatus.Text = $"Mã hóa {bytes:N0} byte trong {sw.Elapsed.TotalSeconds:N3} giây (~{bytes / 1048576d / Math.Max(sw.Elapsed.TotalSeconds, .000001):N2} MiB/s).";
        } catch (Exception ex) { Error(ex); }
    }

    async Task DecryptFileAsync()
    {
        try {
            EnsureAes(); using var s = new OpenFileDialog { Filter = "AES file (*.aes)|*.aes|All files (*.*)|*.*" }; if (s.ShowDialog(this) != DialogResult.OK) return;
            using var d = new SaveFileDialog { FileName = Path.GetFileNameWithoutExtension(s.FileName) }; if (d.ShowDialog(this) != DialogResult.OK) return;
            var source = s.FileName; var dest = d.FileName; var key = aesKey!.ToArray(); var sw = Stopwatch.StartNew();
            await Task.Run(() => AesFileCrypto.DecryptFile(source, dest, key)); sw.Stop(); decodedFile = dest;
            aesStatus.Text = $"Giải mã {new FileInfo(dest).Length:N0} byte trong {sw.Elapsed.TotalSeconds:N3} giây.";
        } catch (Exception ex) { Error(ex); }
    }

    void CompareFiles()
    {
        if (originalFile is null || !File.Exists(originalFile)) throw new InvalidOperationException("Chọn/mã hóa tệp nguồn trước.");
        if (decodedFile is null || !File.Exists(decodedFile)) throw new InvalidOperationException("Giải mã tệp trước.");
        bool same = AesFileCrypto.CompareFilesByteByByte(originalFile, decodedFile);
        aesStatus.Text = $"Byte: {(same ? "TRÙNG KHỚP" : "KHÁC")}; SHA-256 nguồn={AesFileCrypto.GetSha256Hex(originalFile)}; giải mã={AesFileCrypto.GetSha256Hex(decodedFile)}";
    }

    void GenerateRsa()
    {
        using RSA r = RsaCrypto.GenerateKeyPair(2048);
        pubPem = r.ExportSubjectPublicKeyInfoPem(); privPem = r.ExportPkcs8PrivateKeyPem();
        ImportPublic(pubPem); ImportPrivate(privPem); RefreshRsa(); pem.Text = pubPem;
    }

    void SavePem(bool privateKey)
    {
        string? content = privateKey ? privPem : pubPem; if (content is null) throw new InvalidOperationException("Sinh/nạp khóa trước.");
        using var d = new SaveFileDialog { Filter = "PEM (*.pem)|*.pem", FileName = privateKey ? "rsa-private.pem" : "rsa-public.pem" };
        if (d.ShowDialog(this) == DialogResult.OK) { File.WriteAllText(d.FileName, content); if (privateKey) MessageBox.Show("Không commit private PEM lên GitHub."); }
    }

    void LoadPem(bool privateKey)
    {
        using var d = new OpenFileDialog { Filter = "PEM (*.pem)|*.pem" }; if (d.ShowDialog(this) != DialogResult.OK) return;
        string content = File.ReadAllText(d.FileName);
        if (privateKey) { ImportPrivate(content); privPem = content; pubPem = priv!.ExportSubjectPublicKeyInfoPem(); ImportPublic(pubPem); }
        else { ImportPublic(content); pubPem = content; }
        pem.Text = content; RefreshRsa();
    }

    void ImportPublic(string content) { RSA r = RSA.Create(); try { r.ImportFromPem(content); } catch { r.Dispose(); throw; } pub?.Dispose(); pub = r; }
    void ImportPrivate(string content) { RSA r = RSA.Create(); try { r.ImportFromPem(content); if (!RsaCrypto.HasPrivateKey(r)) throw new CryptographicException("PEM không có private key."); } catch { r.Dispose(); throw; } priv?.Dispose(); priv = r; }

    void RefreshRsa()
    {
        var lines = new List<string>();
        if (pub is not null) { lines.Add("PUBLIC KEY"); lines.Add(RsaCrypto.DescribeKey(pub)); }
        if (priv is not null) { lines.Add("PRIVATE KEY — KHÔNG CHIA SẺ"); lines.Add(RsaCrypto.DescribeKey(priv)); }
        keyInfo.Text = string.Join(Environment.NewLine + Environment.NewLine, lines);
        rsaStatus.Text = $"Public: {(pub is null ? "chưa nạp" : "đã nạp")}; private: {(priv is null ? "chưa nạp" : "đã nạp")}.";
    }

    void EncryptRsa()
    {
        EnsurePublic(); byte[] plain = Encoding.UTF8.GetBytes(rsaIn.Text); EncryptBytes(plain);
        using var d = new SaveFileDialog { Filter = "Base64 (*.b64;*.txt)|*.b64;*.txt", FileName = "rsa-ciphertext.b64" };
        if (d.ShowDialog(this) == DialogResult.OK) File.WriteAllText(d.FileName, rsaOut.Text);
    }
    void EncryptAesKey()
    {
        EnsureAes(); EnsurePublic(); EncryptBytes(aesKey!.ToArray());
        using var d = new SaveFileDialog { Filter = "Base64 (*.b64;*.txt)|*.b64;*.txt", FileName = "aes-key-rsa.b64" };
        if (d.ShowDialog(this) == DialogResult.OK) File.WriteAllText(d.FileName, rsaOut.Text);
    }
    void EncryptBytes(byte[] bytes) { EnsurePublic(); rsaOut.Text = Convert.ToBase64String(RsaCrypto.EncryptBytes(bytes, pub!)); lastPlain = bytes.ToArray(); rsaStatus.Text = $"Đã mã hóa {bytes.Length} byte; giới hạn {RsaCrypto.GetOaepSha256MaximumPlaintext(pub!)} byte."; }
    void LoadRsaCipher() { using var d = new OpenFileDialog { Filter = "Base64 (*.b64;*.txt)|*.b64;*.txt" }; if (d.ShowDialog(this) == DialogResult.OK) rsaOut.Text = File.ReadAllText(d.FileName).Trim(); }
    void DecryptRsa()
    {
        EnsurePrivate(); byte[] data = RsaCrypto.DecryptBytes(Convert.FromBase64String(rsaOut.Text.Trim()), priv!);
        rsaPlain.Text = $"Byte={data.Length}; Base64={Convert.ToBase64String(data)}; HEX={Convert.ToHexString(data)}; UTF-8={Encoding.UTF8.GetString(data)}; khớp nguồn={(lastPlain is not null && CryptographicOperations.FixedTimeEquals(data, lastPlain) ? "CÓ" : "chưa có/không khớp")}";
    }

    async Task BenchmarkRsaAsync()
    {
        try {
            EnsurePublic(); EnsurePrivate(); byte[] sample = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(rsaIn.Text) ? "RSA bench" : rsaIn.Text);
            int max = RsaCrypto.GetOaepSha256MaximumPlaintext(pub!); if (sample.Length > max) sample = sample[..max];
            RSA p = pub!, q = priv!; var result = await Task.Run(() => RsaCrypto.Benchmark(p, q, sample, 100));
            rsaStatus.Text = $"100 lần: mã hóa {result.EncryptMsPerOperation:N4} ms/lần; giải mã {result.DecryptMsPerOperation:N4} ms/lần.";
        } catch (Exception ex) { Error(ex); }
    }

    void UpdateHash() { byte[] data = Encoding.UTF8.GetBytes(hashIn.Text); hashOut.Text = RsaCrypto.Sha256Hex(data); hashStatus.Text = $"Đầu vào {hashIn.Text.Length} ký tự / {data.Length} byte; đầu ra 256 bit."; }
    void SignData()
    {
        EnsurePrivate(); byte[] data = Encoding.UTF8.GetBytes(signIn.Text); byte[] signature = RsaCrypto.SignData(data, priv!); sig.Text = Convert.ToBase64String(signature);
        bool different = !CryptographicOperations.FixedTimeEquals(signature, RsaCrypto.SignData(data, priv!));
        using var d = new SaveFileDialog { Filter = "Chữ ký (*.sig;*.txt)|*.sig;*.txt", FileName = "signature.sig" };
        if (d.ShowDialog(this) == DialogResult.OK) File.WriteAllText(d.FileName, sig.Text);
        signStatus.Text = $"RSA-PSS/SHA-256: hai lần ký {(different ? "cho chữ ký khác nhau." : "chưa khác nhau.")}";
    }
    void LoadSignature() { using var d = new OpenFileDialog { Filter = "Signature (*.sig;*.txt)|*.sig;*.txt" }; if (d.ShowDialog(this) == DialogResult.OK) sig.Text = File.ReadAllText(d.FileName).Trim(); }
    void VerifySignature() { EnsurePublic(); bool ok = RsaCrypto.VerifyData(Encoding.UTF8.GetBytes(signIn.Text), Convert.FromBase64String(sig.Text.Trim()), pub!); signStatus.Text = ok ? "CHỮ KÝ HỢP LỆ." : "CHỮ KÝ KHÔNG HỢP LỆ."; }

    void EnsureAes() { if (aesKey is null || aesKey.Length != 32) throw new InvalidOperationException("Sinh/nạp khóa AES-256 trước."); }
    void EnsurePublic() { if (pub is null) throw new InvalidOperationException("Sinh/nạp public key trước."); }
    void EnsurePrivate() { if (priv is null || !RsaCrypto.HasPrivateKey(priv)) throw new InvalidOperationException("Thao tác cần private key."); }
    void Run(Action a) { try { a(); } catch (Exception ex) { Error(ex); } }
    void Error(Exception ex) => MessageBox.Show(this, ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

    static TabPage Page(string title) { var p = new TabPage(title); var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(8) }; p.Controls.Add(f); p.Tag = f; return p; }
    static void Add(TabPage p, Control c) => ((FlowLayoutPanel)p.Tag!).Controls.Add(c);
    static Control Group(string title, Control content) { var g = new GroupBox { Text = title, Width = 1020, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8), Margin = new Padding(4, 4, 4, 10) }; content.Dock = DockStyle.Top; g.Controls.Add(content); return g; }
    static FlowLayoutPanel Buttons(params Control[] items) { var p = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(3) }; p.Controls.AddRange(items); return p; }
    static FlowLayoutPanel Stack(params Control[] items) { var p = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(3) }; foreach (var i in items) { i.Margin = new Padding(3, 4, 3, 4); p.Controls.Add(i); } return p; }
    static Label Label(string text) => new() { Text = text, AutoSize = true, MaximumSize = new Size(1000, 0) };
    static TextBox Box(bool multiline = true) => new() { Multiline = multiline, Width = 980, Height = multiline ? 75 : 28, ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None, Font = new Font("Consolas", 9F) };
    static Button Btn(string text, EventHandler action) { var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(120, 32), Margin = new Padding(4) }; b.Click += action; return b; }
}