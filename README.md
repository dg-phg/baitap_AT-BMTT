
<div align="center">

# **TRƯỜNG ĐẠI HỌC KỸ THUẬT CÔNG NGHIỆP**  
 **Sinh viên:** Dương Thị Anh Phương · **Lớp:** K59KMT  
 **Môn học:** An toàn và bảo mật thông tin

</div>
---- 

## Bài tập 1_ FILE: `AES256_WinFoms_NET10` — Mã hóa AES-256

### 1. YÊU CẦU 
Mã hoá AES : Mã hoá đối xứng (mã hoá, giải mã cùng dùng 1 khoá)
- tạo app (not web) : console, form
  1. sinh khoá AES
  2. lưu khoá ra file
  3. import khoá từ file
  4. mã hoá 1 đoạn văn bản -> kq mã hoá ra màn hình
  5. mã hoá 1 file -> lưu vào 1 file.

  y/c: thử nghiệm với file có kích thước lớn (hàng GB)
       tính thời gian mã hoá, thời gian giải mã
       compare theo bytes: file nguồn và file giải mã
   => Kết luận dựa theo số liệu thu được

### 2. Công nghệ và định dạng

- C# · .NET 10 · Windows Forms · Visual Studio 2026.
- AES-256-CBC với IV ngẫu nhiên.
- HMAC-SHA256 dùng kiểm tra tính toàn vẹn trước khi giải mã.
- Xử lý tệp theo luồng với bộ đệm 1 MiB.

Định dạng tệp mã hóa của chương trình:

```text
Magic header (8 byte) || IV (16 byte) || AES-CBC ciphertext || HMAC-SHA256 (32 byte)
```

Khóa AES và khóa HMAC được sinh riêng. Tệp JSON chứa khóa phải được giữ bí mật và không được commit lên repository công khai.

### 3. Giao diện

#### Quản lý khóa AES

<img width="1920" height="1080" alt="sinhkhoa" src="https://github.com/user-attachments/assets/6e928362-2e1d-44d6-a870-54e0bddd3a89" />

#### Mã hóa và giải mã văn bản

<img width="1920" height="1080" alt="vban" src="https://github.com/user-attachments/assets/0e4b1063-2e14-42bd-aef6-15d9e86d01e0" />

#### Giao diện thao tác với tệp

<img width="1920" height="1080" alt="teplon" src="https://github.com/user-attachments/assets/2af3a277-c63b-43d2-830c-ba356a42c7c0" />

### 4. Cách chạy

Yêu cầu Windows 10/11, Visual Studio 2026 với workload **.NET desktop development**, và .NET 10 SDK.

Mở terminal tại thư mục dự án rồi chạy:

```powershell
dotnet --list-sdks
dotnet restore .\AES256_WinForms.csproj
dotnet build .\AES256_WinForms.csproj
dotnet run --project .\AES256_WinForms.csproj
```

### 5. Quy trình kiểm thử

1. Vào tab **Khóa AES**, bấm **Sinh khóa mới**, sau đó **Lưu khóa ra tệp...**.
2. Đóng và mở lại ứng dụng, bấm **Import khóa từ tệp...** để nạp khóa đã lưu.
3. Vào tab **Văn bản**, nhập một đoạn văn bản, mã hóa rồi giải mã. Nội dung sau giải mã phải trùng văn bản gốc.
4. Vào tab **Tệp lớn**, chọn tệp nguồn. Kiểm tra tệp mã hóa và tệp giải mã có đường dẫn khác tệp nguồn.
5. Bấm **Mã hóa tệp**, sau đó **Giải mã tệp**.
6. Bấm **So sánh byte tệp nguồn và tệp giải mã**. Kết quả mong đợi là `KHỚP 100%`.
7. Thử với các tệp 100 MiB, 500 MiB và 1 GiB; ghi lại thời gian thực tế từ nhật ký.
  * H1: TEST_100MB: 
<img width="1920" height="1080" alt="Tạo tệp kiểm thử 100 MB" src="https://github.com/user-attachments/assets/2b26abdb-46cd-4852-89e8-5d7472ad99f5" />

  * H2: TEST_500MB:
<img width="1920" height="1080" alt="test_500mb" src="https://github.com/user-attachments/assets/4675bd58-4c51-46c8-8f53-21a4ec9ca185" />

  * H3: TEST_1GB:
<img width="1920" height="1080" alt="test_1gb" src="https://github.com/user-attachments/assets/3dcdf603-549f-4bf1-9333-d4df8fa0c741" />

### 6. Số liệu thực nghiệm

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

### 7. Nhận xét và kết luận

Chương trình đã thực hiện các chức năng sinh, lưu và import khóa; mã hóa/giải mã văn bản; mã hóa/giải mã tệp; đo thời gian và so sánh từng byte. Các thử nghiệm 100 MiB, 500 MiB và 1 GiB đều khôi phục dữ liệu chính xác. Với tệp 1 GiB, thời gian mã hóa đo được là 5,563 giây và thời gian giải mã là 6,349 giây.

Kết quả minh họa rằng chương trình có thể xử lý tệp lớn bằng streaming mà không cần nạp toàn bộ tệp vào RAM. Hiệu năng thay đổi theo CPU, thiết bị lưu trữ và điều kiện chạy.

### 8. Lưu ý bảo mật

- Không commit `aes-key.json` hoặc bất kỳ tệp nào chứa khóa AES/HMAC.
- Không commit tệp mã hóa hay dữ liệu kiểm thử lớn nếu không cần thiết.
- Chỉ sử dụng dự án như bài thực hành; với ứng dụng thực tế cần đánh giá thiết kế mật mã và quản lý khóa kỹ lưỡng.

-----

## Bài tập 2_ FILE: `RSA_SHA256_WinFoms` — RSA, SHA-256 và chữ ký số
</div>

### 1. YÊU CẦU 
Mã hoá RSA : mã hoá bất đối xứng (mã hoá 1 chìa, giải mã bằng chìa còn lại)
  tạo app: (.NET)
 1. sinh cặp khoá
 2. xuất cặp khoá ra file (pem)
 3. view pem: cùng các ý nghĩa các byte trong format pem
 4. nhập 1 file pem : public key được n và e
    nhập 1 file pem : priavte key được n và e, d, ...
5. mã hoá dữ liệu sử dụng key vừa import:
    dữ liệu là 1 mảng byte ko quá 190 byte
   (khoá AES 256 bit <=> 32 byte (base64))
   kq mã hoá đc mảng byte (lưu ở dạng base64) => lưu file
6. giải mã: đầu vào là pem, file chứa data đã mã hoá (base64)
   so sánh kq giải mã với nguồn trước khi mã hoá.
7. viết đoạn code đánh giá tốc độ mã hoá, giải mã
   xem dùng khoá nào (pulic key: n, e bé), private key(n, d to đùng)
   thì tốc độ ra sao?
8. tạo giao diện sinh chuỗi băm SHA256 bít
   khi thay đổi đầu vào => băm mới: đánh giá sự thay đổi của kq băm  khi dữ liệu đầu vào thay đổi (rất ít)
9. CHỮ KÝ SỐ: thực ra là dùng RSA với khoá bí mật cho kq băm của dữ liệu => được gọi là chữ ký số của dữ liệu đó.
=> dữ liệu khác nhau : có chữ ký số khác nhau (của cùng người ký)
MONG MUỐN: 
  + cùng 1 dữ liệu, 
  + Cùng 1 người ký (cùng private key) 
  =>thì chữ ký số cũng khác nhau với các lần ký khác nhau
HÃY ĐỀ XUẤT PHƯƠNG PHÁP ĐỂ ĐẠT ĐƯỢC ĐIỀU NÀY.


### 2. Chức năng

- Sinh cặp khóa RSA-2048.
- Xuất `public.pem` và `private.pem`, import PEM từ tệp.
- Hiển thị PEM, mô tả cấu trúc DER và hiển thị tham số RSA.
- Mã hóa dữ liệu bằng public key, lưu ciphertext dạng Base64.
- Giải mã bằng private key và so sánh dữ liệu với nguồn.
- Benchmark mã hóa, giải mã, ký và xác minh.
- Tính SHA-256 tự động khi đầu vào thay đổi.
- Ký và xác minh chữ ký RSA-PSS.

### 3. Công nghệ

- C# · .NET 10 · Windows Forms · Visual Studio 2026.
- RSA-2048 với OAEP-SHA256 cho mã hóa dữ liệu.
- SHA-256 cho băm dữ liệu.
- RSA-PSS với SHA-256 cho chữ ký số.
- PEM cho biểu diễn khóa bằng văn bản và DER cho dữ liệu ASN.1 nhị phân.

### 4. Giới hạn RSA-OAEP-SHA256

Với RSA-2048, modulus dài 256 byte. OAEP-SHA256 cho phép mã hóa tối đa:

```text
256 - 2 × 32 - 2 = 190 byte
```

Khóa AES-256 dài 32 byte, do đó phù hợp với giới hạn này. Khi biểu diễn 32 byte bằng Base64, chuỗi thường dài 44 ký tự; cần giải mã Base64 về mảng byte trước khi thực hiện RSA.

### 5. PEM và DER

PEM bọc dữ liệu nhị phân DER bằng Base64 và có các dòng BEGIN/END. Public key theo SubjectPublicKeyInfo chứa tham số công khai `n` và `e`. Private key PKCS#8 chứa cấu trúc khóa riêng, bao gồm `n`, `e`, `d`, `p`, `q` và các tham số CRT.

Ý nghĩa từng byte DER phụ thuộc vào thẻ ASN.1, trường độ dài và cấu trúc lồng nhau; không nên gán một ý nghĩa cố định cho mọi vị trí byte của tất cả tệp PEM.

### 6. Ảnh giao diện

#### Khóa RSA và PEM

<img width="1920" height="1080" alt="2 khóa rsa pem" src="https://github.com/user-attachments/assets/dfc02600-bed1-427a-be1c-9acfcd0d7cac" />

#### Mã hóa, giải mã và benchmark

<img width="1920" height="1080" alt="3 mã hóa giải mã" src="https://github.com/user-attachments/assets/2f2e23d1-ae45-4fdf-b24c-a3628e81ffd0" />


#### SHA-256 

<img width="1920" height="1080" alt="4 sha 256" src="https://github.com/user-attachments/assets/f1d23df0-217a-4e62-88d6-c6f56a1447ee" />

#### Chữ ký số 

* Hợp lệ: 
<img width="1920" height="1080" alt="5 cky so" src="https://github.com/user-attachments/assets/3ecc51fc-7eb2-4843-ab00-878d4c5af272" />

* Không hợp lệ:
<img width="1920" height="1080" alt="5 1 cky so khople" src="https://github.com/user-attachments/assets/0f80987c-1e8b-494a-a095-8299261c8f5a" />

### 7. Hướng dẫn chạy

Yêu cầu Windows 10/11, Visual Studio 2026 với workload **.NET desktop development**, và .NET 10 SDK.

Tại thư mục dự án, chạy:

```powershell
dotnet --list-sdks
dotnet restore .\RSA_SHA256_WinForms.csproj
dotnet build .\RSA_SHA256_WinForms.csproj
dotnet run --project .\RSA_SHA256_WinForms.csproj
```

### 8. Quy trình kiểm thử

1. Sinh cặp khóa RSA-2048 và xuất thành `public.pem` cùng `private.pem`.
2. Đóng ứng dụng, chạy lại và import riêng từng tệp PEM.
3. Xem public key để kiểm tra `n,e`; xem private key để kiểm tra thêm `d,p,q` và tham số CRT.
4. Tạo dữ liệu khóa AES-256 32 byte, mã hóa bằng public key và lưu ciphertext Base64.
5. Giải mã bằng private key, lưu dữ liệu khôi phục và so sánh với dữ liệu nguồn.
6. Chạy benchmark 100 lần, ghi lại tổng thời gian và thời gian trung bình.
7. Thay đổi một ký tự ở đầu vào SHA-256 và so sánh giá trị băm.
8. Ký cùng dữ liệu hai lần bằng RSA-PSS; kiểm tra hai chữ ký khác nhau nhưng đều xác minh hợp lệ.
9. Sửa dữ liệu nhưng giữ chữ ký cũ; việc xác minh phải thất bại.

### 9. Kết quả benchmark

Benchmark được thực hiện với dữ liệu 32 byte và 100 lần lặp.

| Thao tác | Khóa sử dụng | Tổng thời gian | Trung bình mỗi lần |
|---|---|---:|---:|
| Mã hóa OAEP-SHA256 | Public key | 2,880 ms | 0,0288 ms |
| Giải mã OAEP-SHA256 | Private key | 45,881 ms | 0,4588 ms |
| Ký RSA-PSS | Private key | 44,534 ms | 0,4453 ms |
| Xác minh RSA-PSS | Public key | 1,755 ms | 0,0175 ms |

Trong phép đo này, giải mã bằng private key chậm hơn mã hóa public key khoảng 15,9 lần; ký bằng private key chậm hơn xác minh public key khoảng 25,4 lần. Đây là kết quả của môi trường thử nghiệm cụ thể, không phải hằng số cho mọi máy tính.

### 10. Kết quả SHA-256

SHA-256 luôn tạo đầu ra 256 bit (32 byte). Trong thử nghiệm, thay đổi một ký tự cho kết quả có 126/256 bit khác nhau, tương đương 49,22%. Kết quả minh họa hiệu ứng thác đổ của hàm băm.

### 11. Chữ ký số RSA-PSS

Ứng dụng dùng SHA-256 và RSA-PSS để ký dữ liệu bằng private key và xác minh bằng public key. RSA-PSS dùng salt ngẫu nhiên nên có thể tạo ra chữ ký khác nhau cho cùng dữ liệu và cùng private key, đồng thời mỗi chữ ký vẫn có thể xác minh hợp lệ.

Các kết quả kiểm thử mong đợi:

| Trường hợp | Kết quả mong đợi |
|---|---|
| Ký dữ liệu và xác minh bằng public key | Hợp lệ |
| Ký cùng dữ liệu hai lần bằng RSA-PSS | Chữ ký khác nhau |
| Xác minh từng chữ ký với dữ liệu gốc | Hợp lệ |
| Sửa dữ liệu nhưng giữ chữ ký cũ | Không hợp lệ |

### 12. Nhận xét và kết luận

Chương trình minh họa được quy trình sinh và quản lý cặp khóa RSA, mã hóa/giải mã dữ liệu, phân tích PEM/DER, benchmark RSA, hàm băm SHA-256 và chữ ký số RSA-PSS. Các kết quả đo trên dữ liệu 32 byte cho thấy các thao tác public key nhanh hơn các thao tác private key trong cấu hình đã thử nghiệm.

Bài tập giúp củng cố kiến thức về mật mã bất đối xứng, quản lý khóa, tính toàn vẹn dữ liệu, hàm băm và xác minh chữ ký số.

### 13. Lưu ý bảo mật

- Không commit `private.pem`, khóa riêng hoặc dữ liệu bí mật lên GitHub.
- Không đưa khóa riêng vào mã nguồn hoặc ảnh chụp màn hình công khai.
- Dự án phục vụ mục đích học tập; hệ thống thực tế cần quản lý khóa an toàn và đánh giá bảo mật kỹ lưỡng.
