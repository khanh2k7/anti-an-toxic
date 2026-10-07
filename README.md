# 🇻🇳 Anti Ăn Toxic (AUT) - Bộ Gõ Tiếng Việt Toàn Diện & Chống Toxic Chuẩn UniKey

Phần mềm tích hợp đầy đủ các hạng mục của **UniKey / EVKey** kết hợp cùng công nghệ **Anti-Toxic tự động**:

---

## ⌨️ 1. Đa dạng Kiểu gõ (Input Methods)
Bạn có thể tùy ý chọn kiểu gõ quen thuộc ngay trên bảng điều khiển hoặc menu chuột phải ở khay hệ thống:
* **⚡ Telex (Mặc định):**
  * Mũ & móc: `aa` ➔ `â`, `aw` ➔ `ă`, `ee` ➔ `ê`, `oo` ➔ `ô`, `ow` ➔ `ơ`, `uw` / `w` ➔ `ư`, `dd` ➔ `đ`.
  * Dấu thanh: `s` (sắc), `f` (huyền), `r` (hỏi), `x` (ngã), `j` (nặng), `z` (xóa dấu).
* **🔢 VNI (Gõ số):**
  * Dấu thanh: `1` (sắc), `2` (huyền), `3` (hỏi), `4` (ngã), `5` (nặng), `0` (xóa dấu).
  * Mũ & móc: `6` (â, ê, ô), `7` (ơ, ư, ươ), `8` (ă), `9` (đ).
  * *Ví dụ:* `a1` ➔ `á`, `d9` ➔ `đ`, `u7o7ng2` ➔ `đường`. Gõ lại số để xóa/hủy dấu.
* **🔤 Simple Telex:** Telex cơ bản, phím `w` không tự thành `ư` độc lập mà cần gõ `uw`.
* **⌨️ VIQR:** Gõ dấu bằng ký tự đặc biệt (`'` sắc, `` ` `` huyền, `?` hỏi, `~` ngã, `.` nặng, `^` mũ, `+` móc, `(` trăng, `d-` đ).

---

## 📑 2. Đa dạng Bảng mã (Character Sets)
* **Unicode (Dựng sẵn - Mặc định):** Chuẩn quốc tế dùng cho Web, Discord, Facebook, Game.
* **Unicode (Tổ hợp)**
* **TCVN3 (ABC)**
* **VNI Windows**

---

## 💡 3. Các tính năng thông minh chuẩn UniKey 3.6.2 (Phạm Kim Long)
* **Chuyển đổi ngôn ngữ [V] ⮂ [E] tức thì như UniKey:**
  * **Nhấp chuột trái vào icon [V]/[E] ở khay hệ thống (System Tray):** Tự động chuyển đổi mượt mà giữa Tiếng Việt và Tiếng Anh kèm âm thanh báo hiệu vui tai.
  * **Nhấp vào icon [V]/[E] ngay trên đầu bảng điều khiển:** Tiện lợi, trực quan.
  * **Phím tắt nhanh:** Tùy chọn **`Ctrl + Shift`** hoặc **`Alt + Z`**.
* **Thuật toán chính thức từ UniKey Core (`Uk362`):**
  * Tự động bỏ dấu chính xác 100%: `vãi` (không bị `vaĩ`), `quá` (không bị `qúa`), `hoặc` (không bị `hợac`/`hơạc`), `dấu` / `cấu` (không bị `dâú`/`câú`).
  * Gõ chữ `đ` muộn: `did` ➔ `đi`, `dad` ➔ `đa`, `ddid` ➔ `đid`.
  * Biến đổi `ươ`: `thuowng` / `thuongw` ➔ `thương`, `nuowsc` ➔ `nước`.
* **Tự động khôi phục từ tiếng Anh (Spell Check):** Khi gõ từ tiếng Anh như `pass`, `fast`, `test`, `first`, phần mềm nhận diện đuôi phụ âm tiếng Anh và tự động khôi phục không bị nhảy dấu sai!
* **Đặt dấu chuẩn mới:** Tùy chọn đặt dấu kiểu mới (`hoà`, `thuỳ` / `hòa`, `thúy`).
* **Bỏ dấu tự do:** Gõ xong cả từ mới gõ phím dấu vẫn nhận diện chính xác (`toans` ➔ `toán`).
* **Xử lý phím siêu tốc đồng bộ (Zero-lag Atomic SendInput):** Không còn tình trạng nuốt phím hay nhảy loạn dấu khi gõ nhanh trong game/chat.
* **Nút `[⚙️ Mặc định]`:** Khôi phục cài đặt gốc chỉ với 1 click.

1. **Phát hiện & Hóa giải tức thì:**
   * Gõ `dm` + `Space` ➔ tự động biến thành `bạn ơi `
   * Gõ `vcl` + `Enter` ➔ tự động biến thành `quá đỉnh` + gửi tin nhắn
   * Gõ `suc vat` + `Space` ➔ tự động biến thành `người anh em `
   * Gõ `me may` + `Enter` ➔ tự động biến thành `chúc mẹ bạn mạnh khỏe`
   * Tự động lọc từ kéo dài xả giận: `vlllll` ➔ `vl`, `nguuuu` ➔ `ngu`, `d.m` ➔ `dm`.

2. **3 Chế độ bảo vệ:**
   * 🌿 **Văn Minh & Lịch Sự (Mặc định):** Thay bằng từ khen, xưng hô tôn trọng, hòa nhã.
   * 🌸 **Che Giấu (Censor):** Biến tất cả thành `***`.
   * 🧘 **Triết Lý & Giác Ngộ:** Thay bằng các câu châm ngôn, lời Phật dạy cực thấm (`vạn sự tùy duyên`, `tâm bất biến giữa dòng đời vạn biến`).

3. **Phím tắt nhanh:**
   * **`Ctrl + Shift + Z`**: Bật / Tắt bảo vệ.
   * **`F11`**: Phím đơn tiện bật/tắt nhanh trong lúc đang combat game.
   * **`Ctrl + Shift + X`**: Chuyển đổi qua lại giữa 3 chế độ.
   * **`Ctrl + Shift + S`**: Bật / Tắt âm thanh thông báo.

---

## 🚀 Cách sử dụng

1. Nhấp đúp vào [`run.bat`](file:///d:/code/LOL%20Key/run.bat) hoặc mở file `build/Anti Ăn Toxic (AUT).exe`.
2. Bảng điều khiển sẽ hiện lên. Bạn có thể chọn chế độ, chỉnh phím tắt theo ý thích.
3. Bấm **[Đóng (Về Tray)]** để phần mềm chạy ngầm bảo vệ bạn trên mọi ứng dụng và game!
4. Muốn thêm/bớt từ ngữ, bấm nút **[📂 Mở Từ Điển...]** ngay trên giao diện để sửa file [`toxic_dict.json`](file:///d:/code/LOL%20Key/build/toxic_dict.json).

---

## ⚙️ Tùy chỉnh danh sách từ nóng (`toxic_dict.json`)

Mở file [`toxic_dict.json`](file:///d:/code/LOL%20Key/build/toxic_dict.json) bằng Notepad hoặc VS Code để thêm bớt từ theo ý bạn:

```json
{
  "Wholesome": {
    "dm": "bạn ơi",
    "vcl": "quá đỉnh",
    "ngu": "chưa tập trung"
  },
  "Philosophy": {
    "dm": "tâm bất biến giữa dòng đời vạn biến",
    "ngu": "thất bại là mẹ thành công"
  }
}
```
Sau khi sửa file json, khởi động lại app là có hiệu lực ngay!

---

## 📜 Nguồn gốc & Bản quyền (Credits & Attribution)

* **Lõi thuật toán bộ gõ tiếng Việt:** Được kế thừa và chuyển mã trực tiếp từ mã nguồn mở **UniKey 3.6.2** (`Uk362`).
* **Tác giả UniKey:** **Phạm Kim Long** (Copyright © 1998–2002 Pham Kim Long).
* **Giấy phép bản quyền:** Phát hành theo giấy phép **GNU General Public License (GPL) version 2.0**.
* Trân trọng cảm ơn tác giả Phạm Kim Long vì công trình UniKey kinh điển đã phục vụ hàng triệu người Việt trong suốt nhiều thập kỷ qua!

