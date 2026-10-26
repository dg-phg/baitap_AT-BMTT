# Bài tập 1 — Mã hóa AES-256

<div align="center">

**TRƯỜNG ĐẠI HỌC KỸ THUẬT CÔNG NGHIỆP**  
**Sinh viên:** Dương Thị Anh Phương · **Lớp:** K59KMT  
**Môn học:** An toàn và bảo mật thông tin

</div>

## 1. Mục tiêu

Xây dựng ứng dụng desktop C# Windows Forms trên .NET 10 để thực hành mã hóa đối xứng AES-256, quản lý khóa và xử lý tệp lớn. Chương trình có các chức năng sinh khóa, lưu khóa JSON, import khóa, mã hóa/giải mã văn bản, mã hóa/giải mã tệp, đo thời gian và so sánh dữ liệu theo từng byte.

## 2. Công nghệ và định dạng

- C# · .NET 10 · Windows Forms · Visual Studio 2026.
- AES-256-CBC với IV ngẫu nhiên.
- HMAC-SHA256 dùng kiểm tra tính toàn vẹn trước khi giải mã.
- Xử lý tệp theo luồng với bộ đệm 1 MiB.

Định dạng tệp mã hóa của chương trình:

```text
Magic header (8 byte) || IV (16 byte) || AES-CBC ciphertext || HMAC-SHA256 (32 byte)
```

Khóa AES và khóa HMAC được sinh riêng. Tệp JSON chứa khóa phải được giữ bí mật và không được commit lên repository công khai.

## 3. Giao diện

### Quản lý khóa AES

![Giao diện sinh, lưu và import khóa AES](images/aes-key-management.jpg)

### Mã hóa và giải mã văn bản

![Giao diện mã hóa và giải mã văn bản AES](images/aes-text-encryption.jpg)

### Giao diện thao tác với tệp

![Giao diện chọn tệp, mã hóa, giải mã và so sánh byte](images/aes-file-test.jpg)

## 4. Cách chạy

Yêu cầu Windows 10/11, Visual Studio 2026 với workload **.NET desktop development**, và .NET 10 SDK.

Mở terminal tại thư mục dự án rồi chạy:

```powershell
dotnet --list-sdks
dotnet restore .\AES256_WinForms.csproj
dotnet build .\AES256_WinForms.csproj
dotnet run --project .\AES256_WinForms.csproj
```

## 5. Quy trình kiểm thử

1. Vào tab **Khóa AES**, bấm **Sinh khóa mới**, sau đó **Lưu khóa ra tệp...**.
2. Đóng và mở lại ứng dụng, bấm **Import khóa từ tệp...** để nạp khóa đã lưu.
3. Vào tab **Văn bản**, nhập một đoạn văn bản, mã hóa rồi giải mã. Nội dung sau giải mã phải trùng văn bản gốc.
4. Vào tab **Tệp lớn**, chọn tệp nguồn. Kiểm tra tệp mã hóa và tệp giải mã có đường dẫn khác tệp nguồn.
5. Bấm **Mã hóa tệp**, sau đó **Giải mã tệp**.
6. Bấm **So sánh byte tệp nguồn và tệp giải mã**. Kết quả mong đợi là `KHỚP 100%`.
7. Thử với các tệp 100 MiB, 500 MiB và 1 GiB; ghi lại thời gian thực tế từ nhật ký.

## 6. Số liệu thực nghiệm

Số liệu dưới đây được ghi nhận từ nhật ký chạy chương trình trên máy của sinh viên.

| Kích thước tệp nguồn | Thời gian mã hóa | Tốc độ mã hóa | Thời gian giải mã | Tốc độ giải mã | Kết quả so sánh |
|---|---:|---:|---:|---:|---|
| 31 byte | 0,016 giây | 0,00 MiB/s | 0,016 giây | 0,01 MiB/s | Khớp 100% |
| 100 MiB | 0,650 giây | 153,75 MiB/s | 0,624 giây | 160,15 MiB/s | Khớp 100% |
| 500 MiB | 2,992 giây | 167,11 MiB/s | 2,958 giây | 169,05 MiB/s | Khớp 100% |
| 1 GiB (1.024 MiB) | 5,563 giây | 184,07 MiB/s | 6,349 giây | 161,29 MiB/s | Khớp 100% |

Số byte sau giải mã:

| Kích thước | Byte nguồn | Byte sau giải mã | Kết quả |
|---|---:|---:|---|
| Tệp nhỏ | 31 | 31 | Khớp |
| 100 MiB | 104.857.600 | 104.857.600 | Khớp |
| 500 MiB | 524.288.000 | 524.288.000 | Khớp |
| 1 GiB | 1.073.741.824 | 1.073.741.824 | Khớp |

Với tệp 1 GiB, chương trình đã so sánh thành công 1.073.741.824 byte. Dữ liệu sau giải mã khớp 100% với tệp nguồn trong lần thử đã thực hiện.

## 7. Nhận xét và kết luận

Chương trình đã thực hiện các chức năng sinh, lưu và import khóa; mã hóa/giải mã văn bản; mã hóa/giải mã tệp; đo thời gian và so sánh từng byte. Các thử nghiệm 100 MiB, 500 MiB và 1 GiB đều khôi phục dữ liệu chính xác. Với tệp 1 GiB, thời gian mã hóa đo được là 5,563 giây và thời gian giải mã là 6,349 giây.

Kết quả minh họa rằng chương trình có thể xử lý tệp lớn bằng streaming mà không cần nạp toàn bộ tệp vào RAM. Hiệu năng thay đổi theo CPU, thiết bị lưu trữ và điều kiện chạy.

## 8. Lưu ý bảo mật

- Không commit `aes-key.json` hoặc bất kỳ tệp nào chứa khóa AES/HMAC.
- Không commit tệp mã hóa hay dữ liệu kiểm thử lớn nếu không cần thiết.
- Chỉ sử dụng dự án như bài thực hành; với ứng dụng thực tế cần đánh giá thiết kế mật mã và quản lý khóa kỹ lưỡng.
