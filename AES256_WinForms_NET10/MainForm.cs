using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace AES256_WinForms;

public sealed class MainForm : Form
{
    private byte[]? _aesKey;
    private byte[]? _hmacKey;

    private readonly Label _keyStatus = new() { AutoSize = true, Text = "Chưa có khóa." };
    private readonly TextBox _plainText = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 75, Dock = DockStyle.Fill };
    private readonly TextBox _cipherText = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 75, Dock = DockStyle.Fill };
    private readonly TextBox _decryptText = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 75, Dock = DockStyle.Fill };
    private readonly TextBox _sourcePath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _encryptedPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _decryptedPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button _encryptFileButton = new() { Text = "Mã hóa tệp", AutoSize = true };
    private readonly Button _decryptFileButton = new() { Text = "Giải mã tệp", AutoSize = true };
    private const int BufferSize = 1024 * 1024;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("AES256F1"); // 8 bytes
    private const int HeaderLength = 8 + 16;
    private const int TagLength = 32;

    public MainForm()
    {
        Text = "Bài 1 - Mã hóa AES-256";
        Width = 900;
        Height = 820;
        MinimumSize = new Size(760, 650);
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildKeyTab());
        tabs.TabPages.Add(BuildTextTab());
        tabs.TabPages.Add(BuildFileTab());
        tabs.TabPages.Add(BuildAboutTab());
        Controls.Add(tabs);

        _encryptFileButton.Click += async (_, _) => await RunFileOperationAsync(true);
        _decryptFileButton.Click += async (_, _) => await RunFileOperationAsync(false);
        UpdateFileButtons();
    }

    private TabPage BuildKeyTab()
    {
        var page = new TabPage("Khóa AES");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(15) };
        var generate = new Button { Text = "1. Sinh khóa mới", AutoSize = true };
        var save = new Button { Text = "2. Lưu khóa ra tệp...", AutoSize = true };
        var import = new Button { Text = "3. Import khóa từ tệp...", AutoSize = true };
        generate.Click += (_, _) => GenerateKeys();
        save.Click += (_, _) => SaveKeys();
        import.Click += (_, _) => LoadKeys();
        panel.Controls.Add(generate);
        panel.Controls.Add(save);
        panel.Controls.Add(import);
        panel.Controls.Add(_keyStatus);
        panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(800, 0), Text = "Khóa được lưu trong tệp JSON gồm khóa AES-256 và khóa HMAC-SHA256. Hãy giữ tệp khóa bí mật; người có tệp khóa có thể giải mã dữ liệu." });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildTextTab()
    {
        var page = new TabPage("Văn bản");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        var encrypt = new Button { Text = "Mã hóa văn bản →", AutoSize = true };
        var decrypt = new Button { Text = "← Giải mã văn bản", AutoSize = true };
        layout.Controls.Add(new Label { Text = "Văn bản gốc:", AutoSize = true }, 0, 0);
        layout.Controls.Add(_plainText, 0, 1);
        layout.Controls.Add(encrypt, 0, 2);
        layout.Controls.Add(_cipherText, 0, 3);
        layout.Controls.Add(decrypt, 0, 4);
        layout.Controls.Add(_decryptText, 0, 5);
        encrypt.Click += (_, _) =>
        {
            try { RequireKeys(); _cipherText.Text = EncryptText(_plainText.Text); }
            catch (Exception ex) { ShowError(ex); }
        };
        decrypt.Click += (_, _) =>
        {
            try { RequireKeys(); _decryptText.Text = DecryptText(_cipherText.Text); }
            catch (Exception ex) { ShowError(ex); }
        };
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildFileTab()
    {
        var page = new TabPage("Tệp lớn");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 7, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Tệp nguồn:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_sourcePath, 1, 0);
        var browseSource = new Button { Text = "Chọn...", AutoSize = true };
        browseSource.Click += (_, _) => ChooseSource();
        layout.Controls.Add(browseSource, 2, 0);

        layout.Controls.Add(new Label { Text = "Tệp mã hóa (.aes):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_encryptedPath, 1, 1);
        var browseEncrypted = new Button { Text = "Lưu thành...", AutoSize = true };
        browseEncrypted.Click += (_, _) => ChooseDestination(_encryptedPath, "Tệp AES|*.aes|Tất cả tệp|*.*");
        layout.Controls.Add(browseEncrypted, 2, 1);

        layout.Controls.Add(new Label { Text = "Tệp giải mã:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(_decryptedPath, 1, 2);
        var browseDecrypted = new Button { Text = "Lưu thành...", AutoSize = true };
        browseDecrypted.Click += (_, _) => ChooseDestination(_decryptedPath, "Tất cả tệp|*.*");
        layout.Controls.Add(browseDecrypted, 2, 2);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(_encryptFileButton);
        buttons.Controls.Add(_decryptFileButton);
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 3);

        var compare = new Button { Text = "So sánh byte tệp nguồn và tệp giải mã", AutoSize = true };
        compare.Click += (_, _) => CompareFiles();
        layout.Controls.Add(compare, 0, 4);
        layout.SetColumnSpan(compare, 3);

        layout.Controls.Add(new Label { Text = "Nhật ký / số liệu:", AutoSize = true }, 0, 5);
        layout.SetColumnSpan(_log, 3);
        layout.Controls.Add(_log, 0, 6);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildAboutTab()
    {
        var page = new TabPage("Hướng dẫn");
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            AutoSize = false,
            Text =
                "Định dạng tệp: header (magic + IV) || ciphertext AES-CBC || HMAC-SHA256.\r\n" +
                "HMAC được kiểm tra trước khi giải mã; nếu tệp bị sửa hoặc sai khóa, thao tác bị từ chối.\r\n\r\n" +
                "Thử nghiệm tệp GB: tạo/chọn tệp lớn, bấm Mã hóa tệp, sau đó Giải mã tệp và so sánh byte.\r\n" +
                "Ứng dụng xử lý theo luồng với bộ đệm 1 MiB, không nạp toàn bộ tệp vào RAM. " +
                "Hãy đảm bảo ổ đĩa còn đủ dung lượng cho tệp mã hóa và tệp tạm.\r\n\r\n" +
                "Lưu ý: đây là bài tập minh họa. Với hệ thống mới trong thực tế, nên cân nhắc AES-GCM hoặc định dạng mã hóa đã được kiểm chứng."
        });
        return page;
    }

    private void GenerateKeys()
    {
        _aesKey = RandomNumberGenerator.GetBytes(32);
        _hmacKey = RandomNumberGenerator.GetBytes(32);
        _keyStatus.Text = "Đã sinh khóa AES-256 và khóa HMAC-SHA256.";
        UpdateFileButtons();
        Log("Đã sinh bộ khóa mới.");
    }

    private void SaveKeys()
    {
        try
        {
            RequireKeys();
            using var dialog = new SaveFileDialog { Filter = "Tệp khóa JSON|*.json", FileName = "aes-key.json" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var data = new KeyFile(Convert.ToBase64String(_aesKey!), Convert.ToBase64String(_hmacKey!));
            File.WriteAllText(dialog.FileName, System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Log("Đã lưu khóa: " + dialog.FileName);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void LoadKeys()
    {
        try
        {
            using var dialog = new OpenFileDialog { Filter = "Tệp khóa JSON|*.json|Tất cả tệp|*.*" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var data = System.Text.Json.JsonSerializer.Deserialize<KeyFile>(File.ReadAllText(dialog.FileName))
                       ?? throw new InvalidDataException("Tệp khóa không hợp lệ.");
            byte[] aes = Convert.FromBase64String(data.AesKey);
            byte[] hmac = Convert.FromBase64String(data.HmacKey);
            if (aes.Length != 32 || hmac.Length != 32) throw new InvalidDataException("Khóa phải có độ dài 32 byte.");
            _aesKey = aes; _hmacKey = hmac;
            _keyStatus.Text = "Đã import khóa từ: " + Path.GetFileName(dialog.FileName);
            UpdateFileButtons();
            Log("Đã import khóa: " + dialog.FileName);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private string EncryptText(string text)
    {
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        using var aes = Aes.Create();
        aes.Key = _aesKey!; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        byte[] plain = Encoding.UTF8.GetBytes(text);
        using var ms = new MemoryStream();
        ms.Write(iv);
        using (var crypto = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            crypto.Write(plain); crypto.FlushFinalBlock();
        }
        byte[] body = ms.ToArray();
        byte[] tag = HMACSHA256.HashData(_hmacKey!, body);
        ms.Write(tag);
        return Convert.ToBase64String(ms.ToArray());
    }

    private string DecryptText(string encoded)
    {
        byte[] all = Convert.FromBase64String(encoded);
        if (all.Length < 16 + 16 + TagLength) throw new InvalidDataException("Dữ liệu mã hóa quá ngắn.");
        int bodyLength = all.Length - TagLength;
        byte[] expected = HMACSHA256.HashData(_hmacKey!, all.AsSpan(0, bodyLength));
        if (!CryptographicOperations.FixedTimeEquals(expected, all.AsSpan(bodyLength, TagLength)))
            throw new CryptographicException("Sai khóa hoặc dữ liệu đã bị thay đổi (HMAC không khớp).");
        byte[] iv = all[..16];
        using var aes = Aes.Create();
        aes.Key = _aesKey!; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        using var input = new MemoryStream(all, 16, bodyLength - 16);
        using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var output = new MemoryStream();
        crypto.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private async Task RunFileOperationAsync(bool encrypt)
    {
        try
        {
            RequireKeys();
            string source = encrypt ? _sourcePath.Text.Trim() : _encryptedPath.Text.Trim();
            string destination = encrypt ? _encryptedPath.Text.Trim() : _decryptedPath.Text.Trim();
            if (!File.Exists(source)) throw new FileNotFoundException("Không tìm thấy tệp nguồn.", source);
            if (string.IsNullOrWhiteSpace(destination)) throw new InvalidOperationException("Hãy chọn đường dẫn tệp đích.");
            if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tệp nguồn và tệp đích phải khác nhau.");
            _encryptFileButton.Enabled = _decryptFileButton.Enabled = false;
            var sw = Stopwatch.StartNew();
            long sourceBytes = new FileInfo(source).Length;
            await Task.Run(() =>
            {
                if (encrypt)
                {
                    EncryptFile(source, destination);
                }
                else
                {
                    DecryptFile(source, destination);
                }
            });
            sw.Stop();
            long outputBytes = new FileInfo(destination).Length;
            double mib = sourceBytes / 1024d / 1024d;
            double speed = sw.Elapsed.TotalSeconds > 0 ? mib / sw.Elapsed.TotalSeconds : 0;
            Log($"{(encrypt ? "Mã hóa" : "Giải mã")} thành công.");
            Log($"Tệp nguồn: {sourceBytes:N0} byte ({mib:N2} MiB)");
            Log($"Tệp kết quả: {outputBytes:N0} byte");
            Log($"Thời gian: {sw.Elapsed.TotalSeconds:N3} giây; tốc độ xấp xỉ: {speed:N2} MiB/s");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { _encryptFileButton.Enabled = _decryptFileButton.Enabled = true; }
    }

    private void EncryptFile(string source, string destination)
    {
        string temp = destination + ".tmp";
        try
        {
            byte[] iv = RandomNumberGenerator.GetBytes(16);
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            using (var output = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, BufferSize, FileOptions.SequentialScan))
            {
                output.Write(Magic); output.Write(iv);
                using var aes = Aes.Create();
                aes.Key = _aesKey!; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                    input.CopyTo(crypto, BufferSize);
                output.Flush();
                output.Position = 0;
                using var hmac = new HMACSHA256(_hmacKey!);
                byte[] buffer = new byte[BufferSize];
                int read;
                while ((read = output.Read(buffer, 0, buffer.Length)) > 0) hmac.TransformBlock(buffer, 0, read, buffer, 0);
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                output.Position = output.Length;
                output.Write(hmac.Hash!);
                output.Flush(true);
            }
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private void DecryptFile(string source, string destination)
    {
        string cipherTemp = destination + ".cipher.tmp";
        string plainTemp = destination + ".plain.tmp";
        try
        {
            long length = new FileInfo(source).Length;
            if (length < HeaderLength + 16 + TagLength) throw new InvalidDataException("Tệp mã hóa không hợp lệ hoặc quá ngắn.");
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            using (var hmac = new HMACSHA256(_hmacKey!))
            {
                long authenticatedLength = length - TagLength;
                byte[] buffer = new byte[BufferSize];
                long remaining = authenticatedLength;
                while (remaining > 0)
                {
                    int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read == 0) throw new EndOfStreamException();
                    hmac.TransformBlock(buffer, 0, read, buffer, 0);
                    remaining -= read;
                }
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                byte[] actualTag = new byte[TagLength];
                input.ReadExactly(actualTag);
                if (!CryptographicOperations.FixedTimeEquals(hmac.Hash!, actualTag))
                    throw new CryptographicException("Sai khóa hoặc tệp đã bị sửa (HMAC không khớp).");
            }

            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                byte[] header = new byte[HeaderLength];
                input.ReadExactly(header);
                if (!header.AsSpan(0, Magic.Length).SequenceEqual(Magic)) throw new InvalidDataException("Định dạng tệp không được hỗ trợ.");
                byte[] iv = header.AsSpan(Magic.Length, 16).ToArray();
                long cipherLength = length - HeaderLength - TagLength;
                using var cipherOut = new FileStream(cipherTemp, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan);
                CopyExactly(input, cipherOut, cipherLength);
            }

            using (var cipherIn = new FileStream(cipherTemp, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            using (var plainOut = new FileStream(plainTemp, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan))
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey!;
                using (var headerIn = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    headerIn.Position = Magic.Length;
                    byte[] iv = new byte[16]; headerIn.ReadExactly(iv); aes.IV = iv;
                }
                aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using var crypto = new CryptoStream(cipherIn, aes.CreateDecryptor(), CryptoStreamMode.Read);
                crypto.CopyTo(plainOut, BufferSize);
                plainOut.Flush(true);
            }
            File.Move(plainTemp, destination, true);
        }
        finally
        {
            if (File.Exists(cipherTemp)) File.Delete(cipherTemp);
            if (File.Exists(plainTemp)) File.Delete(plainTemp);
        }
    }

    private static void CopyExactly(Stream input, Stream output, long count)
    {
        byte[] buffer = new byte[BufferSize];
        while (count > 0)
        {
            int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
            if (read == 0) throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            count -= read;
        }
    }

    private void CompareFiles()
    {
        try
        {
            string original = _sourcePath.Text.Trim();
            string restored = _decryptedPath.Text.Trim();
            if (!File.Exists(original) || !File.Exists(restored)) throw new FileNotFoundException("Hãy chọn đúng tệp nguồn và tệp giải mã.");
            var sw = Stopwatch.StartNew();
            var aInfo = new FileInfo(original);
            var bInfo = new FileInfo(restored);
            if (aInfo.Length != bInfo.Length)
            {
                Log($"SO SÁNH: KHÔNG KHỚP. Nguồn={aInfo.Length:N0} byte, giải mã={bInfo.Length:N0} byte.");
                return;
            }
            byte[] a = new byte[BufferSize], b = new byte[BufferSize];
            using var fa = File.OpenRead(original);
            using var fb = File.OpenRead(restored);
            long compared = 0;
            while (true)
            {
                int ra = fa.Read(a, 0, a.Length);
                int rb = fb.Read(b, 0, b.Length);
                if (ra != rb || !a.AsSpan(0, ra).SequenceEqual(b.AsSpan(0, rb)))
                {
                    Log($"SO SÁNH: KHÔNG KHỚP tại vùng byte bắt đầu khoảng {compared:N0}.");
                    return;
                }
                if (ra == 0) break;
                compared += ra;
            }
            sw.Stop();
            Log($"SO SÁNH: KHỚP 100% — đã so sánh {compared:N0} byte.");
            Log($"Thời gian so sánh: {sw.Elapsed.TotalSeconds:N3} giây.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ChooseSource()
    {
        using var dialog = new OpenFileDialog { Title = "Chọn tệp nguồn", CheckFileExists = true };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _sourcePath.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_encryptedPath.Text)) _encryptedPath.Text = dialog.FileName + ".aes";
            if (string.IsNullOrWhiteSpace(_decryptedPath.Text)) _decryptedPath.Text = dialog.FileName + ".restored";
        }
    }

    private void ChooseDestination(TextBox target, string filter)
    {
        using var dialog = new SaveFileDialog { Filter = filter };
        if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.FileName;
    }

    private void RequireKeys()
    {
        if (_aesKey is not { Length: 32 } || _hmacKey is not { Length: 32 })
            throw new InvalidOperationException("Hãy sinh khóa mới hoặc import khóa trước.");
    }

    private void UpdateFileButtons()
    {
        bool hasKeys = _aesKey is { Length: 32 } && _hmacKey is { Length: 32 };
        _encryptFileButton.Enabled = hasKeys;
        _decryptFileButton.Enabled = hasKeys;
    }

    private void Log(string message)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => Log(message))); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void ShowError(Exception ex)
    {
        Log("LỖI: " + ex.Message);
        MessageBox.Show(this, ex.Message, "Có lỗi xảy ra", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed record KeyFile(string AesKey, string HmacKey);
}
