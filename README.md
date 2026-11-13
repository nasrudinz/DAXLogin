# DAXLogin

เปิด Microsoft Dynamics AX 2012 ด้วย Windows user ที่บันทึกไว้ในคลิกเดียว แทนการกด Shift + คลิกขวา หรือพิมพ์รหัสผ่านในหน้าจอ `runas`

One-click launcher that starts the Dynamics AX 2012 client (`Ax32.exe`) as a saved Windows user.

## ความสามารถ

- บันทึกได้หลายโปรไฟล์ (domain, user, password, ไฟล์ `.axc`) และตั้งโปรไฟล์เริ่มต้น
- เปิดแอปแล้วเข้า AX ด้วยโปรไฟล์เริ่มต้นทันที (กด Shift ค้างตอนเปิดเพื่อข้าม)
- เมนูที่ system tray สำหรับสลับ user
- ส่งออกและนำเข้าโปรไฟล์เป็นไฟล์ `.daxprofile` ที่เข้ารหัสด้วยรหัสป้องกันไฟล์
- ภาษาไทยและอังกฤษ
- อัปเดตตัวเองจาก GitHub Releases

## การเก็บรหัสผ่าน

- รหัสผ่านเข้ารหัสด้วย Windows DPAPI ผูกกับบัญชี Windows ของผู้ใช้ เก็บที่ `%AppData%\DAXLogin\profiles.json`
- ไฟล์นี้เปิดด้วยบัญชี Windows อื่นหรือเครื่องอื่นไม่ได้
- ใครที่ใช้เครื่องขณะที่คุณ login Windows ค้างไว้ จะเปิด AX ด้วย user ที่บันทึกไว้ได้ ให้ล็อกจอเมื่อลุกจากโต๊ะ
- ไฟล์ `.daxprofile` ใช้ PBKDF2-SHA256 กับ AES-256 และ HMAC ส่งรหัสป้องกันไฟล์ให้ผู้รับคนละช่องทางกับไฟล์

## Build

ต้องมี .NET SDK (แอปเป็น .NET Framework 4.8 ซึ่งมีมากับ Windows 10/11)

```
dotnet build src\DAXLogin -c Release
dotnet test tests\DAXLogin.Tests
```

## ออกเวอร์ชันใหม่

```
dotnet tool install -g vpk --version 1.2.161
.\build\pack.ps1 -Version 1.1.0 -Notes "สิ่งที่เปลี่ยน"
```

ได้ไฟล์ใน `Releases\` จากนั้นอัปโหลดขึ้น GitHub Releases:

```
vpk upload github --repoUrl https://github.com/nasrudinz/DAXLogin --publish --releaseName "DAXLogin 1.1.0" --tag v1.1.0 --token <token> --outputDir Releases
```

แอปที่ติดตั้งแล้วจะพบเวอร์ชันใหม่เองเมื่อเปิดครั้งถัดไป repository ต้องเป็น public เพราะแอปไม่ฝัง token
