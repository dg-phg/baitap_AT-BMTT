using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RSA_SHA256_WinForms;

public sealed class MainForm : Form
{
    private RSA? publicRsa;
    private RSA? privateRsa;
    private string? publicPem;
    private string? privatePem;
    private byte[]? originalBytes;
    private string? lastCiphertext;
    private byte[]? lastSignature;
    private byte[]? lastSignedData;

    private readonly Label keyStatus = new() { AutoSize = true };
    private readonly ComboBox pemType = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly TextBox pemView = new();
    private readonly TextBox pemInfo = new();
    private readonly TextBox parametersView = new();

    private readonly ComboBox dataMode = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly TextBox inputData = new();
    private readonly TextBox cipherData = new();
    private readonly TextBox decryptedData = new();
    private readonly TextBox benchmarkResult = new();
    private readonly Label cryptoStatus = new() { AutoSize = true };

    private readonly TextBox hashInput = new();
    private readonly TextBox hashOutput = new();
    private readonly TextBox avalancheOutput = new();

    private readonly TextBox signInput = new();
    private readonly TextBox signatureView = new();
    private readonly Label signatureStatus = new() { AutoSize = true };

    public MainForm()
    {
        Text = "Bài 2 - RSA, SHA-256 và chữ ký số";
        Width = 1100;
        Height = 850;
        MinimumSize = new Size(850, 650);
        StartPosition = FormStartPosition.CenterScreen;

        foreach (TextBox box in new[]
        {
            pemView, pemInfo, parametersView,
            inputData, cipherData, decryptedData,
            benchmarkResult, hashInput, hashOutput,
            avalancheOutput, signInput, signatureView
        })
        {
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Both;
            box.WordWrap = false;
            box.Dock = DockStyle.Fill;
            box.Font = new Font("Consolas", 10);
        }

        pemView.ReadOnly = true;
        pemInfo.ReadOnly = true;
        parametersView.ReadOnly = true;
        cipherData.ReadOnly = true;
        decryptedData.ReadOnly = true;
        benchmarkResult.ReadOnly = true;
        hashOutput.ReadOnly = true;
        avalancheOutput.ReadOnly = true;
        signatureView.ReadOnly = true;

        pemType.Items.AddRange(new object[]
        {
            "Public key",
            "Private key"
        });
        pemType.SelectedIndex = 0;

        dataMode.Items.AddRange(new object[]
        {
            "Mảng byte Base64",
            "Văn bản UTF-8"
        });
        dataMode.SelectedIndex = 0;

        byte[] sample = Enumerable.Range(0, 32)
            .Select(i => (byte)i).ToArray();
        inputData.Text = Convert.ToBase64String(sample);

        hashInput.Text = "An toàn và bảo mật thông tin";
        signInput.Text = "Kiểm thử chữ ký số RSA-PSS.";

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildKeysTab());
        tabs.TabPages.Add(BuildEncryptTab());
        tabs.TabPages.Add(BuildHashTab());
        tabs.TabPages.Add(BuildSignatureTab());

        Controls.Add(tabs);

        hashInput.TextChanged += (_, _) => UpdateHash();
        UpdateHash();
    }

    private static Button MakeButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(4)
        };

        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        return button;
    }

    private static TableLayoutPanel MakeTable(int columns, int rows)
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            Padding = new Padding(8)
        };
    }

    private static void AddFullWidth(
        TableLayoutPanel table, Control control, int row)
    {
        table.Controls.Add(control, 0, row);
        table.SetColumnSpan(control, table.ColumnCount);
    }

    // =====================================================
    // TAB 1: SINH KHÓA, PEM VÀ THAM SỐ RSA
    // =====================================================

    private TabPage BuildKeysTab()
    {
        var page = new TabPage("Khóa RSA & PEM");
        var table = MakeTable(1, 4);

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };

        buttons.Controls.Add(MakeButton("Sinh cặp khóa RSA-2048", GenerateKeys));
        buttons.Controls.Add(MakeButton("Xuất cặp khóa PEM", ExportKeys));
        buttons.Controls.Add(MakeButton("Import public.pem", ImportPublic));
        buttons.Controls.Add(MakeButton("Import private.pem", ImportPrivate));

        table.Controls.Add(buttons, 0, 0);
        table.Controls.Add(keyStatus, 0, 1);

        var chooser = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        chooser.Controls.Add(new Label
        {
            Text = "Loại khóa cần xem:",
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0)
        });

        chooser.Controls.Add(pemType);
        chooser.Controls.Add(MakeButton("Xem PEM", ShowPem));
        table.Controls.Add(chooser, 0, 2);

        var subtabs = new TabControl { Dock = DockStyle.Fill };

        var tabPem = new TabPage("PEM nguyên văn");
        tabPem.Controls.Add(pemView);

        var tabInfo = new TabPage("Ý nghĩa PEM / DER");
        tabInfo.Controls.Add(pemInfo);

        var tabParams = new TabPage("Tham số RSA");
        tabParams.Controls.Add(parametersView);

        subtabs.TabPages.Add(tabPem);
        subtabs.TabPages.Add(tabInfo);
        subtabs.TabPages.Add(tabParams);

        table.Controls.Add(subtabs, 0, 3);
        page.Controls.Add(table);

        return page;
    }

    private void GenerateKeys()
    {
        using RSA rsa = RSA.Create(2048);

        publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        privatePem = rsa.ExportPkcs8PrivateKeyPem();

        SetPublicKey(publicPem);
        SetPrivateKey(privatePem);

        lastSignature = null;
        lastSignedData = null;

        pemType.SelectedIndex = 0;
        keyStatus.Text =
            "Đã sinh RSA-2048. Public key dùng mã hóa/xác minh; " +
            "private key dùng giải mã/ký.";

        ShowPem();
    }

    private void ExportKeys()
    {
        if (publicPem is null || privatePem is null)
        {
            throw new InvalidOperationException(
                "Hãy sinh cặp khóa hoặc import đủ khóa trước.");
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "Chọn thư mục lưu hai tệp PEM"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        File.WriteAllText(
            Path.Combine(dialog.SelectedPath, "public.pem"),
            publicPem,
            new UTF8Encoding(false));

        File.WriteAllText(
            Path.Combine(dialog.SelectedPath, "private.pem"),
            privatePem,
            new UTF8Encoding(false));

        MessageBox.Show(
            "Đã xuất public.pem và private.pem.\n" +
            "Không chia sẻ private.pem.",
            "Xuất khóa",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ImportPublic()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn public.pem",
            Filter = "PEM files|*.pem;*.txt|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string pem = File.ReadAllText(dialog.FileName);
        SetPublicKey(pem);
        publicPem = pem;

        pemType.SelectedIndex = 0;
        keyStatus.Text = "Đã import public key: " + dialog.FileName;
        ShowPem();
    }

    private void ImportPrivate()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn private.pem",
            Filter = "PEM files|*.pem;*.txt|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string pem = File.ReadAllText(dialog.FileName);
        SetPrivateKey(pem);
        privatePem = pem;

        publicPem = privateRsa!.ExportSubjectPublicKeyInfoPem();
        SetPublicKey(publicPem);

        pemType.SelectedIndex = 1;
        keyStatus.Text = "Đã import private key: " + dialog.FileName;
        ShowPem();
    }

    private void SetPublicKey(string pem)
    {
        RSA rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem);
            RSAParameters p = rsa.ExportParameters(false);

            if (p.Modulus is null || p.Exponent is null)
                throw new CryptographicException(
                    "Không tìm thấy tham số n và e.");

            publicRsa?.Dispose();
            publicRsa = rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private void SetPrivateKey(string pem)
    {
        RSA rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem);
            RSAParameters p = rsa.ExportParameters(true);

            if (p.Modulus is null || p.Exponent is null || p.D is null)
                throw new CryptographicException(
                    "Tệp không chứa private key hợp lệ.");

            privateRsa?.Dispose();
            privateRsa = rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private void ShowPem()
    {
        string? pem = pemType.SelectedIndex == 1
            ? privatePem
            : publicPem;

        if (string.IsNullOrWhiteSpace(pem))
        {
            pemView.Clear();
            pemInfo.Text = "Chưa có PEM.";
            parametersView.Clear();
            return;
        }

        bool isPrivate = pemType.SelectedIndex == 1;
        pemView.Text = pem;
        pemInfo.Text = AnalyzePem(pem, isPrivate);

        RSA? rsa = isPrivate ? privateRsa : publicRsa;
        parametersView.Text = rsa is null
            ? "Chưa có tham số RSA."
            : FormatParameters(rsa, isPrivate);
    }

    private static string AnalyzePem(string pem, bool isPrivate)
    {
        string[] lines = pem.Split(
            new[] { "\r\n", "\n", "\r" },
            StringSplitOptions.RemoveEmptyEntries);

        string begin = lines.FirstOrDefault(
            x => x.StartsWith("-----BEGIN ", StringComparison.Ordinal))
            ?? "(không thấy BEGIN)";

        string end = lines.LastOrDefault(
            x => x.StartsWith("-----END ", StringComparison.Ordinal))
            ?? "(không thấy END)";

        string base64 = string.Concat(
            lines.Where(x => !x.StartsWith("-----"))
                 .Select(x => x.Trim()));

        byte[] der = Convert.FromBase64String(base64);

        var sb = new StringBuilder();
        sb.AppendLine("PEM là dữ liệu DER được biểu diễn bằng Base64.");
        sb.AppendLine("Dòng bắt đầu: " + begin);
        sb.AppendLine("Dòng kết thúc: " + end);
        sb.AppendLine($"Độ dài Base64: {base64.Length} ký tự");
        sb.AppendLine($"Độ dài DER: {der.Length} byte");
        sb.AppendLine();

        if (isPrivate)
        {
            sb.AppendLine("PRIVATE KEY thường dùng cấu trúc PKCS#8.");
            sb.AppendLine("Chứa cấu trúc khóa RSA và các tham số riêng:");
            sb.AppendLine("n, e, d, p, q, dP, dQ, qInv.");
            sb.AppendLine("Các tham số riêng phải được bảo vệ.");
        }
        else
        {
            sb.AppendLine("PUBLIC KEY thường dùng SubjectPublicKeyInfo.");
            sb.AppendLine("Gồm AlgorithmIdentifier và BIT STRING.");
            sb.AppendLine("Khóa công khai RSA có n (modulus) và e (exponent).");
            sb.AppendLine("Public key không chứa d.");
        }

        sb.AppendLine();
        sb.AppendLine("Các byte DER đầu tiên ở dạng hex:");

        for (int i = 0; i < Math.Min(der.Length, 96); i += 16)
        {
            int count = Math.Min(16, der.Length - i);

            sb.AppendLine(
                $"{i:X4}: {Convert.ToHexString(der.AsSpan(i, count))}");
        }

        sb.AppendLine();
        sb.AppendLine(
            "0x30 thường là tag ASN.1 SEQUENCE. Ý nghĩa các vị trí byte " +
            "phụ thuộc vào cấu trúc DER và độ dài từng trường.");

        return sb.ToString();
    }

    private static string FormatParameters(RSA rsa, bool includePrivate)
    {
        RSAParameters p = rsa.ExportParameters(includePrivate);

        var sb = new StringBuilder();
        sb.AppendLine("Thuật toán: RSA");
        sb.AppendLine($"Kích thước n: {rsa.KeySize} bit");
        sb.AppendLine("n = " + Hex(p.Modulus));
        sb.AppendLine("e = " + Hex(p.Exponent));

        if (p.Exponent is not null)
        {
            var e = new BigInteger(
                p.Exponent, isUnsigned: true, isBigEndian: true);

            sb.AppendLine("e (thập phân) = " + e);
        }

        if (includePrivate)
        {
            sb.AppendLine("d = " + Hex(p.D));
            sb.AppendLine("p = " + Hex(p.P));
            sb.AppendLine("q = " + Hex(p.Q));
            sb.AppendLine("DP = " + Hex(p.DP));
            sb.AppendLine("DQ = " + Hex(p.DQ));
            sb.AppendLine("InverseQ = " + Hex(p.InverseQ));
            sb.AppendLine("Cảnh báo: không chia sẻ các tham số private.");
        }

        return sb.ToString();
    }

    private static string Hex(byte[]? data) =>
        data is null ? "(không có)" : Convert.ToHexString(data);

    // =====================================================
    // TAB 2: MÃ HÓA, GIẢI MÃ VÀ ĐO TỐC ĐỘ
    // =====================================================

    private TabPage BuildEncryptTab()
    {
        var page = new TabPage("Mã hóa / giải mã RSA");
        var table = MakeTable(2, 7);

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };

        toolbar.Controls.Add(dataMode);
        toolbar.Controls.Add(MakeButton(
            "Tạo khóa AES-256 (32 byte)", GenerateAesKey));

        toolbar.Controls.Add(MakeButton(
            "Lưu dữ liệu nguồn (.bin)", SaveSource));

        AddFullWidth(table, toolbar, 0);

        table.Controls.Add(new Label
        {
            Text = "Dữ liệu nguồn",
            AutoSize = true
        }, 0, 1);

        table.Controls.Add(new Label
        {
            Text = "Ciphertext Base64",
            AutoSize = true
        }, 1, 1);

        table.Controls.Add(inputData, 0, 2);
        table.Controls.Add(cipherData, 1, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };

        buttons.Controls.Add(MakeButton(
            "Mã hóa bằng public key", EncryptAndSave));

        buttons.Controls.Add(MakeButton(
            "Giải mã từ tệp Base64", DecryptFromFile));

        buttons.Controls.Add(MakeButton(
            "Đo tốc độ 100 lần", RunBenchmark));

        AddFullWidth(table, buttons, 3);

        AddFullWidth(table, new Label
        {
            Text = "Dữ liệu sau giải mã (Base64)",
            AutoSize = true
        }, 4);

        AddFullWidth(table, decryptedData, 5);
        AddFullWidth(table, benchmarkResult, 6);

        page.Controls.Add(table);
        return page;
    }

    private byte[] GetInputBytes()
    {
        if (dataMode.SelectedIndex == 0)
        {
            try
            {
                return Convert.FromBase64String(inputData.Text.Trim());
            }
            catch (FormatException)
            {
                throw new InvalidDataException(
                    "Dữ liệu đầu vào phải là Base64 hợp lệ.");
            }
        }

        return Encoding.UTF8.GetBytes(inputData.Text);
    }

    private void GenerateAesKey()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);

        dataMode.SelectedIndex = 0;
        inputData.Text = Convert.ToBase64String(key);
        originalBytes = key.ToArray();

        cryptoStatus.Text = "Đã tạo mảng byte khóa AES-256 dài 32 byte.";
    }

    private void SaveSource()
    {
        byte[] data = GetInputBytes();

        using var dialog = new SaveFileDialog
        {
            Filter = "Binary file|*.bin|All files|*.*",
            FileName = "rsa_source.bin"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        File.WriteAllBytes(dialog.FileName, data);
        originalBytes = data.ToArray();

        MessageBox.Show($"Đã lưu {data.Length} byte dữ liệu nguồn.");
    }

    private void EncryptAndSave()
    {
        if (publicRsa is null)
            throw new InvalidOperationException(
                "Hãy sinh cặp khóa hoặc import public.pem.");

        byte[] data = GetInputBytes();

        if (data.Length == 0 || data.Length > 190)
        {
            throw new InvalidDataException(
                $"RSA-2048 + OAEP-SHA256 chỉ mã hóa tối đa 190 byte. " +
                $"Dữ liệu hiện tại: {data.Length} byte.");
        }

        byte[] encrypted = publicRsa.Encrypt(
            data, RSAEncryptionPadding.OaepSHA256);

        string base64 = Convert.ToBase64String(encrypted);

        cipherData.Text = base64;
        originalBytes = data.ToArray();
        lastCiphertext = base64;

        using var dialog = new SaveFileDialog
        {
            Filter = "Base64 file|*.b64;*.txt|All files|*.*",
            FileName = "rsa_ciphertext.b64"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(
                dialog.FileName, base64, new UTF8Encoding(false));

            cryptoStatus.Text =
                $"Đã mã hóa {data.Length} byte; " +
                $"ciphertext {encrypted.Length} byte. Đã lưu tệp.";
        }
        else
        {
            cryptoStatus.Text =
                $"Mã hóa thành công {data.Length} byte; chưa lưu tệp.";
        }
    }

    private void DecryptFromFile()
    {
        if (privateRsa is null)
            throw new InvalidOperationException(
                "Hãy sinh cặp khóa hoặc import private.pem.");

        using var open = new OpenFileDialog
        {
            Filter = "Base64 file|*.b64;*.txt|All files|*.*",
            Title = "Chọn tệp ciphertext Base64"
        };

        if (open.ShowDialog(this) != DialogResult.OK)
            return;

        string base64 = File.ReadAllText(open.FileName).Trim();
        byte[] encrypted = Convert.FromBase64String(base64);

        byte[] plain = privateRsa.Decrypt(
            encrypted, RSAEncryptionPadding.OaepSHA256);

        decryptedData.Text = Convert.ToBase64String(plain);

        byte[]? expected = null;

        if (lastCiphertext == base64 && originalBytes is not null)
        {
            expected = originalBytes;
        }
        else
        {
            using var source = new OpenFileDialog
            {
                Title = "Chọn tệp dữ liệu nguồn để đối chiếu",
                Filter = "Binary file|*.bin|All files|*.*"
            };

            if (source.ShowDialog(this) == DialogResult.OK)
                expected = File.ReadAllBytes(source.FileName);
        }

        bool matches = expected is not null &&
            expected.AsSpan().SequenceEqual(plain);

        string result = expected is null
            ? "Chưa chọn dữ liệu nguồn nên chưa thể so sánh."
            : matches
                ? $"KHỚP 100%: dữ liệu giống nhau ({plain.Length} byte)."
                : $"KHÔNG KHỚP: nguồn {expected.Length} byte, " +
                  $"giải mã {plain.Length} byte.";

        using var save = new SaveFileDialog
        {
            Filter = "Binary file|*.bin|All files|*.*",
            FileName = "rsa_decrypted.bin"
        };

        if (save.ShowDialog(this) == DialogResult.OK)
            File.WriteAllBytes(save.FileName, plain);

        cryptoStatus.Text = result;

        MessageBox.Show(
            result, "Kết quả giải mã",
            MessageBoxButtons.OK,
            matches ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void RunBenchmark()
    {
        if (publicRsa is null || privateRsa is null)
            throw new InvalidOperationException(
                "Cần có cả public key và private key.");

        byte[] data = GetInputBytes();

        if (data.Length == 0 || data.Length > 190)
            throw new InvalidDataException(
                "Dữ liệu benchmark phải từ 1 đến 190 byte.");

        const int count = 100;
        var ciphertexts = new byte[count][];

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            ciphertexts[i] = publicRsa.Encrypt(
                data, RSAEncryptionPadding.OaepSHA256);
        }

        sw.Stop();
        double encryptMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();

        for (int i = 0; i < count; i++)
        {
            byte[] plain = privateRsa.Decrypt(
                ciphertexts[i], RSAEncryptionPadding.OaepSHA256);

            if (!plain.AsSpan().SequenceEqual(data))
                throw new CryptographicException(
                    "Benchmark giải mã không khớp dữ liệu.");
        }

        sw.Stop();
        double decryptMs = sw.Elapsed.TotalMilliseconds;

        var signatures = new byte[count][];

        sw.Restart();

        for (int i = 0; i < count; i++)
        {
            signatures[i] = privateRsa.SignData(
                data, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);
        }

        sw.Stop();
        double signMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();

        for (int i = 0; i < count; i++)
        {
            if (!publicRsa.VerifyData(
                data, signatures[i], HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss))
            {
                throw new CryptographicException(
                    "Benchmark xác minh chữ ký thất bại.");
            }
        }

        sw.Stop();
        double verifyMs = sw.Elapsed.TotalMilliseconds;

        benchmarkResult.Text =
            $"RSA-2048, dữ liệu {data.Length} byte, {count} lần\r\n\r\n" +
            $"Mã hóa public key: tổng {encryptMs:N3} ms; " +
            $"trung bình {encryptMs / count:N4} ms/lần.\r\n" +
            $"Giải mã private key: tổng {decryptMs:N3} ms; " +
            $"trung bình {decryptMs / count:N4} ms/lần.\r\n" +
            $"Ký bằng RSA-PSS: tổng {signMs:N3} ms; " +
            $"trung bình {signMs / count:N4} ms/lần.\r\n" +
            $"Xác minh chữ ký: tổng {verifyMs:N3} ms; " +
            $"trung bình {verifyMs / count:N4} ms/lần.\r\n\r\n" +
            "e thường là 65537 nên thao tác public key thường nhanh hơn. " +
            "Hãy dùng số đo thực tế để kết luận.";
    }

    // =====================================================
    // TAB 3: SHA-256
    // =====================================================

    private TabPage BuildHashTab()
    {
        var page = new TabPage("SHA-256");
        var table = MakeTable(2, 4);

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

        AddFullWidth(table, new Label
        {
            Text = "Dữ liệu đầu vào - hash tự cập nhật khi thay đổi",
            AutoSize = true
        }, 0);

        AddFullWidth(table, hashInput, 1);

        table.Controls.Add(new Label
        {
            Text = "SHA-256",
            AutoSize = true
        }, 0, 2);

        table.Controls.Add(MakeButton(
            "Đổi một ký tự và so sánh", ShowAvalanche), 1, 2);

        table.Controls.Add(hashOutput, 0, 3);
        table.Controls.Add(avalancheOutput, 1, 3);

        page.Controls.Add(table);
        return page;
    }

    private void UpdateHash()
    {
        byte[] input = Encoding.UTF8.GetBytes(hashInput.Text);
        byte[] hash = SHA256.HashData(input);

        hashOutput.Text =
            $"Đầu vào: {input.Length} byte\r\n" +
            $"Hash: {hash.Length * 8} bit\r\n\r\n" +
            Convert.ToHexString(hash);
    }

    private void ShowAvalanche()
    {
        string original = hashInput.Text;

        if (original.Length == 0)
            original = "a";

        string changed =
            (original[0] == 'A' ? "B" : "A") + original[1..];

        byte[] h1 = SHA256.HashData(Encoding.UTF8.GetBytes(original));
        byte[] h2 = SHA256.HashData(Encoding.UTF8.GetBytes(changed));

        int differentBits = 0;

        for (int i = 0; i < h1.Length; i++)
        {
            differentBits += BitOperations.PopCount(
                (uint)(h1[i] ^ h2[i]));
        }

        avalancheOutput.Text =
            $"Dữ liệu gốc:\r\n{original}\r\n" +
            $"SHA-256:\r\n{Convert.ToHexString(h1)}\r\n\r\n" +
            $"Dữ liệu đổi một ký tự:\r\n{changed}\r\n" +
            $"SHA-256:\r\n{Convert.ToHexString(h2)}\r\n\r\n" +
            $"Số bit khác nhau: {differentBits}/256 " +
            $"({differentBits / 256d * 100:N2}%).";
    }

    // =====================================================
    // TAB 4: CHỮ KÝ SỐ RSA-PSS
    // =====================================================

    private TabPage BuildSignatureTab()
    {
        var page = new TabPage("Chữ ký số RSA-PSS");
        var table = MakeTable(1, 5);

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        table.Controls.Add(new Label
        {
            Text = "Dữ liệu cần ký (UTF-8):",
            AutoSize = true
        }, 0, 0);

        table.Controls.Add(signInput, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        buttons.Controls.Add(MakeButton(
            "Ký SHA-256 bằng private key", SignData));

        buttons.Controls.Add(MakeButton(
            "Xác minh bằng public key", VerifySignature));

        table.Controls.Add(buttons, 0, 2);
        table.Controls.Add(signatureView, 0, 3);
        table.Controls.Add(signatureStatus, 0, 4);

        page.Controls.Add(table);
        return page;
    }

    private void SignData()
    {
        if (privateRsa is null)
            throw new InvalidOperationException(
                "Hãy sinh khóa hoặc import private.pem.");

        byte[] data = Encoding.UTF8.GetBytes(signInput.Text);

        byte[] signature = privateRsa.SignData(
            data, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        bool sameInput = lastSignedData is not null &&
            lastSignedData.AsSpan().SequenceEqual(data);

        bool differentSignature = lastSignature is not null &&
            !lastSignature.AsSpan().SequenceEqual(signature);

        signatureView.Text = Convert.ToBase64String(signature);

        lastSignedData = data.ToArray();
        lastSignature = signature.ToArray();

        signatureStatus.Text = sameInput && differentSignature
            ? "Chữ ký mới khác chữ ký trước dù dữ liệu không đổi. " +
              "RSA-PSS sử dụng salt ngẫu nhiên."
            : "Đã ký dữ liệu. Bấm ký lần nữa để kiểm tra RSA-PSS.";
    }

    private void VerifySignature()
    {
        if (publicRsa is null)
            throw new InvalidOperationException(
                "Hãy sinh khóa hoặc import public.pem.");

        if (string.IsNullOrWhiteSpace(signatureView.Text))
            throw new InvalidOperationException(
                "Chưa có chữ ký để xác minh.");

        byte[] data = Encoding.UTF8.GetBytes(signInput.Text);
        byte[] signature =
            Convert.FromBase64String(signatureView.Text.Trim());

        bool valid = publicRsa.VerifyData(
            data, signature, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        signatureStatus.Text = valid
            ? "CHỮ KÝ HỢP LỆ."
            : "CHỮ KÝ KHÔNG HỢP LỆ: dữ liệu hoặc chữ ký đã thay đổi.";
    }
}