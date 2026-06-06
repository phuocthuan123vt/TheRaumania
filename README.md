# 🎮 The Raumania

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3_LTS-blue.svg?style=for-the-badge&logo=unity)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg?style=for-the-badge&logo=windows)
![Stage](https://img.shields.io/badge/Release-Beta_v1.1.1-blue.svg?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)

*Một game nhập vai mô phỏng quản lý nhà hàng & nấu ăn đầy thú vị được xây dựng trên nền tảng Unity 2D.*

[🇻🇳 Bản Tiếng Việt](#tiếng-viet) | [🇺🇸 English Version](#english)

</div>

---

<a name="tiếng-viet"></a>

# 🇻🇳 HƯỚNG DẪN TẢI VỀ & CHẠY GAME

Chào mừng bạn đến với tài liệu hướng dẫn trải nghiệm game **The Raumania**. Để kiểm thử và chơi game, vui lòng thực hiện tải bản Build hoàn chỉnh (file ZIP), giải nén và chạy trực tiếp file `.exe` trên máy tính Windows.

---

## 📌 Mục lục
1. [PHẦN 1: Hướng dẫn tải bản Build đóng gói sẵn (GitHub Releases)](#vi-part1)
2. [PHẦN 2: Hướng dẫn khởi chạy & Các phím điều khiển](#vi-part2)
3. [PHẦN 3: Bảng điều khiển Cheat (Cheat Console) & Các lệnh hỗ trợ](#vi-part3)
4. [PHẦN 4: Danh sách các Scene chính trong game](#vi-part4)
5. [PHẦN 5: Quản lý dữ liệu lưu trữ (Save Data)](#vi-part5)
6. [PHẦN 6: Khắc phục một số lỗi thường gặp](#vi-part6)

---

<a name="vi-part1"></a>
## 📥 PHẦN 1: Hướng dẫn tải bản Build đóng gói sẵn

Để tải về game phiên bản chạy ngay trên máy tính Windows:

1. Truy cập vào trang GitHub Repository của dự án.
2. Tìm đến mục **Releases** ở thanh bên phải màn hình.
3. Chọn bản phát hành có tên: **`The Raumania - Beta Version 1.1.1`**.
4. Trong phần Assets của Release, tải về tệp tin:
   * **`The-Raumania-v1.1.1-Beta.zip`** (Dung lượng khoảng ~60MB).
5. Nhấp chuột phải vào tệp ZIP tải về, chọn **Extract All...** (hoặc sử dụng phần mềm 7-Zip, WinRAR) để giải nén toàn bộ nội dung ra một thư mục trước khi mở game.

---

<a name="vi-part2"></a>
## 🚀 PHẦN 2: Hướng dẫn khởi chạy & Các phím điều khiển

### 🖥️ 1. Khởi chạy game
* **Bước 1**: Mở thư mục đã giải nén ở [PHẦN 1](#vi-part1).
* **Bước 2**: Click đúp vào tệp tin chạy game: **`TheRaumania.exe`**.
* **Bước 3**: Game sẽ khởi động trực tiếp để bạn trải nghiệm.

### 🎮 2. Bảng điều khiển phím trong game (Gameplay Controls)
| Phím tắt | Hành động | Chức năng chi tiết |
| :---: | :--- | :--- |
| **`W`, `A`, `S`, `D`** / **Arrow Keys** | Di chuyển | Di chuyển nhân vật đi các hướng trên bản đồ 2D. |
| **`E`** | Tương tác | Nói chuyện với khách hàng, bắt đầu nấu ăn, thanh toán tiền, đi ngủ, mua sắm. |
| **`1` - `9`, `0`** | Chọn Hotbar | Chọn các ô chứa đồ từ 1 đến 10 trên thanh công cụ nhanh. |
| **`Q`** | Vứt vật phẩm | Loại bỏ vật phẩm đang cầm trên tay khỏi Hotbar. |
| **`~`** (BackQuote) / **`F1`** | Mở Cheat Console | Bật/tắt bảng nhập lệnh Cheat hỗ trợ game. |

---

<a name="vi-part3"></a>
## ⌨️ PHẦN 3: Bảng điều khiển Cheat (Cheat Console) & Các lệnh hỗ trợ

Trong khi chơi game, bạn có thể nhấn phím **BackQuote** (phím `~` hoặc `` ` `` ở góc trên bên trái bàn phím) hoặc phím **F1** (phím mở dự phòng) để mở bảng điều khiển nhập lệnh cheat ở cạnh dưới màn hình. Nhập các lệnh sau và nhấn **Enter** để thực thi:

| Cú pháp lệnh | Tác dụng | Ví dụ thực tế |
| :--- | :--- | :--- |
| **`give [tên_vật_phẩm] [số_lượng]`** | Nhận ngay vật phẩm có độ tươi ngon 100% vào túi đồ/hotbar. | `give Carrot 5` |
| **`money [số_tiền]`** / `addmoney` | Thêm tiền RC (credits) trực tiếp vào ví. | `money 5000` |
| **`time [số_giờ]`** / `addtime` | Tua nhanh thời gian thêm số giờ chỉ định (tự động qua ngày). | `time 6` |
| **`skipday`** / `nextday` | Bỏ qua ngày hiện tại, chuyển nhanh sang sáng hôm sau. | `skipday` |
| **`hygiene [0..10]`** / `sethygiene` | Thiết lập điểm đánh giá vệ sinh của nhà hàng. | `hygiene 9.0` |
| **`speed [tốc_độ]`** / `setspeed` | Thay đổi tốc độ di chuyển của nhân vật người chơi. | `speed 8` |
| **`food [0..10]`** / `setfood` | Thiết lập điểm chất lượng món ăn của nhà hàng. | `food 9.5` |
| **`decor [0..10]`** / `setdecor` | Thiết lập điểm trang trí và đồng bộ mức độ nâng cấp tương ứng. | `decor 8.0` |
| **`service [0..10]`** / `setservice` | Thiết lập điểm thái độ phục vụ khách hàng. | `service 9.0` |

---

<a name="vi-part4"></a>
## 🎭 PHẦN 4: Danh sách các Scene chính trong game

Game bao gồm các phân cảnh chính sau đây:
* **`scn_MainMenu`**: Giao diện màn hình chính của game (Bắt đầu chơi, Cài đặt âm lượng, Thoát).
* **`scn_Home`**: Nhà riêng của nhân vật chính. Người chơi có thể tương tác với giường ngủ để lưu game và chuyển qua ngày mới.
* **`scn_Village`**: Bản đồ làng Raumania, kết nối các khu vực như Nhà riêng, Nhà hàng và các Cửa hàng.
* **`scn_Restaurant_lv1`, `lv2`, `lv3`**: Các cấp độ nhà hàng để người chơi quản lý, phục vụ khách hàng và nấu ăn.
* **`scn_Store`**: Cửa hàng bán nguyên liệu tươi ngon (rau củ, thịt, gia vị).
* **`scn_UpgradeStore`**: Cửa hàng giúp người chơi nâng cấp trang thiết bị (nâng cấp bếp lò, bàn ghế).

---

<a name="vi-part5"></a>
## 💾 PHẦN 5: Quản lý dữ liệu lưu trữ (Save Data)

Dự án sử dụng tệp tin định dạng JSON để tự động lưu lại tiến trình chơi của bạn (tiền tệ, kho đồ, trang bị đã nâng cấp, cấp độ nhà hàng) mỗi khi bạn đi ngủ ở `scn_Home`.
* **Đường dẫn tệp Save Game trên Windows**:
  ```text
  C:\Users\<Tên_User_Windows>\AppData\LocalLow\DefaultCompany\TheRaumania\GameSaveData.json
  ```
* *Mẹo*: Nếu muốn đặt lại game để chơi lại từ đầu (New Game), bạn chỉ cần xóa tệp tin `GameSaveData.json` này.

---

<a name="vi-part6"></a>
## 🔍 PHẦN 6: Khắc phục một số lỗi thường gặp

1. **Lỗi: Không chạy được file `TheRaumania.exe` hoặc báo thiếu file DLL / Crash.**
   * *Nguyên nhân*: Bạn chạy file exe trực tiếp trong tệp tin nén `.zip` mà chưa giải nén, hoặc giải nén bị thiếu file.
   * *Khắc phục*: Đảm bảo bạn đã giải nén toàn bộ thư mục ZIP. Các thư mục `TheRaumania_Data` và `MonoBleedingEdge` bắt buộc phải nằm cùng cấp thư mục với file `TheRaumania.exe`.
2. **Lỗi: Nhân vật bị khóa di chuyển hoặc nhấn phím tương tác `E` không có tác dụng.**
   * *Nguyên nhân*: Bạn mở nhầm bảng Cheat Console hoặc cửa sổ game bị mất focus chuột.
   * *Khắc phục*: Nhấp chuột trái vào giữa màn hình game. Kiểm tra xem ô nhập lệnh có hiện ở cạnh dưới màn hình không, nhấn phím `~` hoặc `F1` lần nữa để tắt console.

---

<br/>
<br/>

---

<a name="english"></a>

# 🇺🇸 DOWNLOAD & RUNNING GUIDE

Welcome to **The Raumania** experience guide. To test and play the game, please download the packaged build (ZIP file), extract it, and run the `.exe` file directly on Windows.

---

## 📌 Table of Contents
1. [PART 1: Downloading the Pre-Built Package (GitHub Releases)](#en-part1)
2. [PART 2: How to Play & Controls](#en-part2)
3. [PART 3: Cheat Console Panel & Commands](#en-part3)
4. [PART 4: Main Scenes List](#en-part4)
5. [PART 5: Save Game Data Management](#en-part5)
6. [PART 6: Troubleshooting Common Errors](#en-part6)

---

<a name="en-part1"></a>
## 📥 PART 1: Downloading the Pre-Built Package

To download the ready-to-play Windows version:

1. Visit the GitHub Repository of the project.
2. Go to the **Releases** tab on the right side of the screen.
3. Select the release named: **`The Raumania - Beta Version 1.1.1`**.
4. Download the following asset file:
   * **`The-Raumania-v1.1.1-Beta.zip`** (approx. ~60MB).
5. Once downloaded, right-click and select **Extract All...** (or use 7-Zip / WinRAR) to unpack the zip into its own folder before launching the game.

---

<a name="en-part2"></a>
## 🚀 PART 2: How to Play & Controls

### 🖥️ 1. How to run
* **Step 1**: Open the extracted directory from [PART 1](#en-part1).
* **Step 2**: Double-click the game executable: **`TheRaumania.exe`**.
* **Step 3**: The game launches immediately for you to play.

### 🎮 2. Gameplay Controls
| Key Bindings | Action | Description |
| :---: | :--- | :--- |
| **`W`, `A`, `S`, `D`** / **Arrow Keys** | Movement | Move the character around the 2D map. |
| **`E`** | Interact | Talk to NPCs, start cooking minigames, cash out, sleep, and shop. |
| **`1` - `9`, `0`** | Select Hotbar | Switch between the 10 quick slots on your Hotbar. |
| **`Q`** | Discard Item | Drop the currently selected item from your Hotbar. |
| **`~`** (BackQuote) / **`F1`** | Toggle Console | Open or close the Cheat Command Console. |

---

<a name="en-part3"></a>
## ⌨️ PART 3: Cheat Console Panel & Commands

While playing, you can press the **BackQuote** key (`~` or `` ` ``) or **F1** key (fallback key) to toggle the Cheat Console panel at the bottom of the screen. Type a command and press **Enter** to submit:

| Command Syntax | Effect / Description | Example |
| :--- | :--- | :--- |
| **`give [item_name] [qty]`** | Receive the specified item at 100% freshness in your inventory. | `give Carrot 5` |
| **`money [amount]`** / `addmoney` | Adds RC (credits) to the player's wallet balance. | `money 5000` |
| **`time [hours]`** / `addtime` | Advances game time forward by a number of hours (automatically shifts days). | `time 6` |
| **`skipday`** / `nextday` | Skips current day straight to the next morning. | `skipday` |
| **`hygiene [0..10]`** / `sethygiene` | Sets the restaurant's hygiene rating score. | `hygiene 9.0` |
| **`speed [value]`** / `setspeed` | Modifies the player character's movement speed. | `speed 8` |
| **`food [0..10]`** / `setfood` | Sets the restaurant's food quality rating score. | `food 9.5` |
| **`decor [0..10]`** / `setdecor` | Sets decoration score and auto-unlocks corresponding tier. | `decor 8.0` |
| **`service [0..10]`** / `setservice` | Sets the customer service satisfaction score. | `service 9.0` |

---

<a name="en-part4"></a>
## 🎭 PART 4: Main Scenes List

The game contains the following primary scenes:
* **`scn_MainMenu`**: The starting screen (Play Game, Volume Settings, Credits, Quit).
* **`scn_Home`**: The main character's house. Interact with the bed to sleep, save progress, and advance to the next day.
* **`scn_Village`**: The main village map connecting Home, Restaurant, and Shops.
* **`scn_Restaurant_lv1`, `lv2`, `lv3`**: Cooking and management levels to serve customers.
* **`scn_Store`**: Purchase fresh cooking ingredients (vegetables, meats, spices).
* **`scn_UpgradeStore`**: Purchase upgrades for kitchenware, stoves, and restaurant decorations.

---

<a name="en-part5"></a>
## 💾 PART 5: Save Game Data Management

Your game progress (coins, item inventory, bought upgrades, calendar days) is automatically saved to a JSON file whenever you use the bed in `scn_Home` to end the day.
* **Windows Save File Location**:
  ```text
  C:\Users\<Windows_Username>\AppData\LocalLow\DefaultCompany\TheRaumania\GameSaveData.json
  ```
* *Tip*: Deleting `GameSaveData.json` will reset all game data, allowing you to start a fresh New Game.

---

<a name="en-part6"></a>
## 🔍 PART 6: Troubleshooting Common Errors

1. **Error: The game executable does not launch or shows missing DLL / Crashes.**
   * *Reason*: You tried to launch `TheRaumania.exe` directly from inside the ZIP file without extracting it, or files were corrupted during download.
   * *Fix*: Extract the entire ZIP. Make sure folders like `TheRaumania_Data` and `MonoBleedingEdge` remain in the same folder level as `TheRaumania.exe`.
2. **Error: Character is frozen or pressing E does not interact.**
   * *Reason*: The Cheat Console panel is open in the background, or the game window lost focus.
   * *Fix*: Click inside the game window. Check if the console text box is visible at the bottom of the screen; if so, press `~` or `F1` to close it.

---
