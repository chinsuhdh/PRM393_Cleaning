Cấu trúc database bạn thiết kế rất chặt chẽ, tối ưu cho việc ánh xạ sang Entity Framework Core và phân chia các module cho backend. Khung thư mục Flutter cũng rất rõ ràng, tách biệt được các luồng UI cho Client, Worker và Admin.

Dưới đây là nội dung markdown hoàn chỉnh cho file `README.md`. Bạn có thể copy toàn bộ đoạn dưới đây và dán thẳng vào trình soạn thảo trên GitHub.

---

# Hệ Thống Đặt Lịch Dịch Vụ Vệ Sinh Tích Hợp AI (AI-Powered Cleaning Service Platform)

## Giới thiệu dự án

Dự án là một nền tảng kết nối khách hàng và thợ cung cấp dịch vụ vệ sinh, được thiết kế theo kiến trúc mở rộng cao (scalability). Hệ thống tích hợp Trí tuệ nhân tạo (AI) để hỗ trợ tìm kiếm thợ phù hợp (Matchmaking), tư vấn tự động qua Chatbot (RAG Knowledge Base) và quản lý vận hành theo thời gian thực. Hệ thống đề cao chất lượng mã nguồn, tính bảo mật và logic chặt chẽ của các luồng nghiệp vụ.

## Công nghệ sử dụng

* **Backend:** ASP.NET Core (C#), Entity Framework Core, SignalR (Real-time Hub).
* **Frontend (Mobile App):** Flutter (Dart).
* **Database:** PostgreSQL.
* **Authentication:** JWT (JSON Web Token), OAuth2 (Google, Facebook, Apple).
* **AI & DevOps:** Tích hợp mô hình LLM mã nguồn mở, RAG, GitHub Actions (CI/CD).

## Cấu trúc Mobile App (Flutter)

Dự án frontend được tổ chức theo cấu trúc module hóa, hỗ trợ 3 vai trò người dùng chính:

* **Client (Khách hàng):** Đặt lịch, theo dõi trạng thái đơn, thanh toán và đánh giá.
* **Worker (Thợ):** Nhận việc, xem bản đồ và tọa độ GPS, quản lý thu nhập qua ví.
* **Admin (Quản trị viên):** Quản lý danh mục dịch vụ, giám sát hệ thống và duyệt hồ sơ thợ.

## Kiến trúc Backend (ASP.NET Core Modules)

Hệ thống backend được chia thành 7 phân hệ độc lập để dễ dàng bảo trì và mở rộng:

1. **Identity & Access Management (IAM):** Quản lý định danh, bảo mật, vòng đời JWT token và xác thực OTP.
2. **User Management:** Quản lý hồ sơ khách hàng, định vị địa chỉ và quy trình xác minh năng lực thợ.
3. **Catalog & Pricing:** Quản lý danh mục dịch vụ, đơn giá cơ bản và cấu hình tính phí linh hoạt.
4. **Booking & Workflow:** Động cơ cốt lõi xử lý vòng đời đơn đặt lịch và ghi log chuyển đổi trạng thái đơn hàng.
5. **Billing & Feedback:** Xử lý thanh toán qua các cổng điện tử (MoMo, VNPay) và hệ thống đánh giá hai chiều.
6. **AI Services:** Tích hợp Chatbot tư vấn ngữ nghĩa dựa trên tài liệu (RAG) và thuật toán AI chấm điểm, gợi ý thợ (Worker Recommendation).
7. **Core Infrastructure & DevOps:** Hệ thống push thông báo realtime (SignalR), xử lý các tác vụ nền (Background Jobs) và ghi log tập trung.

## Cấu trúc Cơ sở dữ liệu (PostgreSQL)

Database bao gồm 26 bảng được chuẩn hóa, chia làm các nhóm nghiệp vụ cốt lõi:

* **Nhóm định danh:** Accounts, Tokens, Liên kết mạng xã hội, Lịch sử truy cập.
* **Nhóm nghiệp vụ lõi:** Profiles, Services, Bookings, Trạng thái đơn hàng.
* **Nhóm tài chính:** Payments, Reviews.
* **Nhóm AI:** Models, Conversations, Recommendations, RAG Embeddings.
* **Nhóm giám sát:** Notifications, Deployment Logs, System Logs.

## Hướng dẫn cài đặt

1. **Clone repository:** Tải mã nguồn về máy cục bộ của bạn.
2. **Thiết lập Database:** Chạy file script SQL được cung cấp để khởi tạo cấu trúc các bảng và extension trên PostgreSQL.
3. **Cấu hình Backend:** Cập nhật chuỗi kết nối cơ sở dữ liệu và các API Key cần thiết trong file `appsettings.json`.
4. **Chạy Backend:** Thực thi lệnh `dotnet run` để khởi động API server.
5. **Chạy Mobile App:** Di chuyển vào thư mục ứng dụng, cài đặt các thư viện phụ thuộc bằng `flutter pub get` và khởi chạy bằng lệnh `flutter run`.
