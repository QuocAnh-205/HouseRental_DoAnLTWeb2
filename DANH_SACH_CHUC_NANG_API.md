# Danh sách chức năng API — House Rental API

Tài liệu liệt kê **toàn bộ 90 API nghiệp vụ** đang hiển thị trên Swagger
(`http://localhost:5224/swagger/index.html`), chia theo **14 nhóm chức năng lớn**
(không tính 6 API demo lỗi `/api/errors/*`, chỉ chạy ở môi trường Development).
Mỗi API ghi rõ: phương thức, đường dẫn, ai được dùng và **mục đích dùng để làm gì**.

**Quy ước cột "Quyền":**

| Ký hiệu | Ý nghĩa |
|---|---|
| Công khai | Không cần đăng nhập |
| Đăng nhập | Mọi tài khoản đã đăng nhập (gửi kèm header `Authorization: Bearer <access token>`) |
| Tenant | Người thuê nhà |
| Landlord | Chủ nhà |
| Admin | Quản trị viên |

Mọi response đều có cùng định dạng `{ "success": true/false, "message": "...", "data": ..., "errors": [...] }`.

## Mục lục

| # | Nhóm chức năng | Số API | Tóm tắt |
|---|---|---|---|
| 1 | [Auth — Tài khoản & đăng nhập](#1-auth--tài-khoản--đăng-nhập) | 9 | Đăng ký, đăng nhập, token, quên / đổi mật khẩu |
| 2 | [Users — Người dùng](#2-users--người-dùng) | 8 | Hồ sơ cá nhân, hồ sơ công khai, admin quản lý tài khoản |
| 3 | [Properties — Tin đăng cho thuê](#3-properties--tin-đăng-cho-thuê) | 14 | Tìm kiếm, đăng tin, duyệt tin, quản lý ảnh |
| 4 | [Uploads — Tải ảnh lên](#4-uploads--tải-ảnh-lên) | 4 | Upload, xem, liệt kê, xóa ảnh nhà / ảnh đại diện |
| 5 | [Favorites — Nhà yêu thích](#5-favorites--nhà-yêu-thích) | 3 | Lưu / bỏ lưu nhà |
| 6 | [Appointments — Lịch hẹn xem nhà](#6-appointments--lịch-hẹn-xem-nhà) | 8 | Đặt lịch, xác nhận, từ chối, hủy, hoàn tất |
| 7 | [Bookings — Yêu cầu thuê & hợp đồng](#7-bookings--yêu-cầu-thuê--hợp-đồng) | 9 | Gửi yêu cầu thuê, duyệt, hủy, kết thúc hợp đồng |
| 8 | [Invoices — Hóa đơn hàng tháng](#8-invoices--hóa-đơn-hàng-tháng) | 6 | Lập hóa đơn tiền thuê + điện + nước, xem, hủy |
| 9 | [Payments — Thanh toán](#9-payments--thanh-toán) | 5 | Trả tiền cọc / tiền thuê, chủ nhà xác nhận, hoàn tiền |
| 10 | [Reviews — Đánh giá](#10-reviews--đánh-giá) | 6 | Đánh giá nhà đã thuê, chủ nhà phản hồi |
| 11 | [Conversations — Tin nhắn](#11-conversations--tin-nhắn) | 6 | Chat người thuê ↔ chủ nhà ↔ quản trị viên |
| 12 | [Notifications — Thông báo](#12-notifications--thông-báo) | 5 | Xem, đánh dấu đã đọc, xóa thông báo |
| 13 | [Reports — Báo cáo vi phạm](#13-reports--báo-cáo-vi-phạm) | 4 | Báo cáo tin xấu, admin xử lý |
| 14 | [Statistics — Thống kê](#14-statistics--thống-kê) | 3 | Dashboard cho admin, chủ nhà, người thuê |
| | **Tổng** | **90** | |

---

## 1. Auth — Tài khoản & đăng nhập

Quản lý vòng đời đăng nhập: tạo tài khoản, cấp và làm mới token JWT, đăng xuất, khôi phục và đổi mật khẩu.
Các API đăng ký / đăng nhập / làm mới token / quên và đặt lại mật khẩu bị **giới hạn 10 lần mỗi phút cho mỗi IP** để chống dò mật khẩu.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/auth/register` | Công khai | Tạo tài khoản mới với vai trò **người thuê** hoặc **chủ nhà** (không cho tự đăng ký Admin). Kiểm tra email trùng, băm mật khẩu bằng BCrypt, gửi thông báo chào mừng và trả về token để đăng nhập luôn. |
| POST | `/api/auth/login` | Công khai | Đăng nhập bằng email + mật khẩu. Trả về **access token** (dùng gọi API) và **refresh token** (dùng lấy access token mới). Tài khoản bị khóa thì không đăng nhập được. |
| POST | `/api/auth/refresh` | Công khai | Khi access token hết hạn, gửi refresh token để nhận cặp token mới mà không phải nhập lại mật khẩu. Refresh token cũ bị thu hồi ngay (mỗi token chỉ dùng được một lần). |
| POST | `/api/auth/logout` | Công khai | Đăng xuất trên thiết bị hiện tại: thu hồi refresh token đang dùng. |
| POST | `/api/auth/logout-all` | Đăng nhập | Đăng xuất khỏi **tất cả thiết bị**: thu hồi mọi refresh token của tài khoản (dùng khi nghi bị lộ tài khoản). |
| POST | `/api/auth/forgot-password` | Công khai | Bước 1 của quên mật khẩu: tạo **mã đặt lại** (hiệu lực 30 phút) và gửi qua email. Môi trường Development trả luôn mã trong response để test. Luôn trả cùng thông báo dù email có tồn tại hay không, để người ngoài không dò được email. |
| POST | `/api/auth/reset-password` | Công khai | Bước 2 của quên mật khẩu: nhập email + mã + mật khẩu mới. Đặt lại xong thì mọi phiên đăng nhập cũ bị đăng xuất và người dùng nhận thông báo. |
| GET | `/api/auth/me` | Đăng nhập | Lấy thông tin tài khoản đang đăng nhập (tên, email, vai trò, ảnh đại diện...). Frontend gọi khi mở web để biết ai đang đăng nhập. |
| PUT | `/api/auth/change-password` | Đăng nhập | Đổi mật khẩu khi biết mật khẩu hiện tại. Các thiết bị khác bị đăng xuất. |

## 2. Users — Người dùng

Hồ sơ cá nhân, trang công khai của chủ nhà và các công cụ quản trị tài khoản của Admin.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/users` | Admin | Danh sách toàn bộ người dùng, có **tìm kiếm** (tên, email, số điện thoại), **lọc** theo vai trò / trạng thái, **sắp xếp** (`SortBy`: mới nhất, cũ nhất, tên A-Z / Z-A, email, đăng nhập gần nhất) và phân trang — dùng cho trang "Người dùng" của Admin. |
| GET | `/api/users/{id}` | Đăng nhập (Admin hoặc chính mình) | Xem thông tin đầy đủ của một tài khoản. Người thường chỉ xem được chính mình. |
| GET | `/api/users/{id}/profile` | Công khai | **Hồ sơ công khai** của một người (thường là chủ nhà): tên, ảnh, ngày tham gia, số tin đang cho thuê, điểm đánh giá trung bình — giúp người thuê đánh giá độ tin cậy của chủ nhà. |
| GET | `/api/users/{id}/properties` | Công khai | Danh sách các tin **đang cho thuê** của một chủ nhà, hiển thị trên trang hồ sơ công khai. |
| GET | `/api/users/{id}/posts` | Công khai | **Bản tin trên trang cá nhân**: các tin người đó đã đăng, dạng bài viết (đoạn mô tả, tối đa 4 ảnh, tiện ích), mới nhất trước. Người khác chỉ thấy tin đã duyệt; **chính chủ và Admin** thấy thêm tin chờ duyệt, bị từ chối (kèm lý do) và đã ẩn. |
| PUT | `/api/users/me` | Đăng nhập | Cập nhật hồ sơ của chính mình: họ tên, số điện thoại, ảnh đại diện. |
| PATCH | `/api/users/{id}/status` | Admin | **Khóa / mở khóa** tài khoản vi phạm. Khóa thì tài khoản bị đăng xuất khỏi mọi thiết bị và không đăng nhập lại được. Admin không thể tự khóa mình. |
| PATCH | `/api/users/{id}/role` | Admin | **Đổi vai trò** người dùng (Tenant / Landlord / Admin). Người bị đổi phải đăng nhập lại; không đổi được chủ nhà đang có tin đăng. |

## 3. Properties — Tin đăng cho thuê

Nhóm chức năng trung tâm: người thuê tìm nhà, chủ nhà đăng và quản lý tin, Admin kiểm duyệt.
Vòng đời tin đăng: **Chờ duyệt → Đang cho thuê ⇄ Đã ẩn / Đã cho thuê**, hoặc **Chờ duyệt → Bị từ chối → (chủ nhà sửa) → Chờ duyệt**.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/properties` | Công khai | **Tìm kiếm nhà cho thuê** (chỉ tin đã duyệt). Lọc theo từ khóa, thành phố, quận, phường, loại nhà, khoảng giá, diện tích, số phòng ngủ / tắm, tiện ích; **tìm quanh một vị trí** (vĩ độ, kinh độ, bán kính km); sắp xếp mới nhất / giá / diện tích / đánh giá / xem nhiều / **gần nhất**. Dùng cho trang tìm kiếm, trang chủ và bản đồ "Nhà cho thuê gần bạn". |
| POST | `/api/properties` | Landlord | **Đăng tin cho thuê mới** (tiêu đề, mô tả, địa chỉ, toạ độ, giá, tiền cọc, tiện ích, ảnh). Tin ở trạng thái **Chờ duyệt** và Admin nhận thông báo. |
| GET | `/api/properties/stats` | Công khai | Đếm số tin đang cho thuê **theo loại nhà** và **theo quận/huyện** — dùng cho các ô "Khám phá danh mục" và "Thuê theo quận" ở trang chủ. |
| GET | `/api/properties/feed` | Công khai | **Bản tin chung** (trang `/ban-tin`): mọi tin đang cho thuê của tất cả chủ nhà dạng bài viết, sắp theo **thời điểm được Admin duyệt** — tin vừa duyệt nằm đầu để mọi người cùng thấy. Lọc tùy chọn theo loại nhà. |
| GET | `/api/properties/{id}` | Công khai | **Chi tiết một căn nhà**: ảnh, mô tả, tiện ích, vị trí, chủ nhà, điểm đánh giá, đã lưu yêu thích chưa. Mỗi lượt xem của khách được cộng vào lượt xem. Tin chưa duyệt / đã ẩn chỉ chủ nhà và Admin xem được. |
| PUT | `/api/properties/{id}` | Landlord / Admin | **Sửa thông tin tin đăng**. Tin bị từ chối sau khi chủ nhà sửa sẽ tự động được **gửi duyệt lại**. |
| DELETE | `/api/properties/{id}` | Landlord / Admin | **Xóa tin đăng** — chỉ được xóa khi nhà chưa từng có ai thuê; nếu đã có lịch sử thuê thì phải chuyển sang Ẩn để giữ dữ liệu hợp đồng. |
| GET | `/api/properties/{id}/similar` | Công khai | Gợi ý **nhà tương tự** (cùng thành phố, cùng loại, giá chênh ≤ 30%, ưu tiên cùng quận) — hiển thị ở cuối trang chi tiết. |
| GET | `/api/properties/mine` | Landlord | Danh sách **tin của tôi** (mọi trạng thái) để chủ nhà quản lý. |
| GET | `/api/properties/manage` | Admin | Danh sách **toàn bộ tin đăng** để Admin kiểm duyệt, lọc theo trạng thái (ví dụ `Status=PendingApproval` để xem tin chờ duyệt). |
| PATCH | `/api/properties/{id}/status` | Landlord / Admin | **Đổi trạng thái tin**. Admin: **duyệt** (Available) — tin hiện công khai cho mọi người và lên đầu **Bản tin**; **từ chối** (Rejected, kèm lý do); hoặc ẩn. Chủ nhà: chỉ **ẩn / hiện lại** tin đã duyệt. Chủ nhà nhận thông báo khi Admin đổi trạng thái. |
| POST | `/api/properties/{id}/images` | Landlord / Admin | **Thêm ảnh** cho tin (tối đa 20 ảnh), có thể đặt làm ảnh đại diện. |
| DELETE | `/api/properties/{id}/images/{imageId}` | Landlord / Admin | **Xóa một ảnh**; nếu xóa ảnh đại diện thì ảnh kế tiếp tự trở thành ảnh đại diện. |
| PATCH | `/api/properties/{id}/images/{imageId}/thumbnail` | Landlord / Admin | **Chọn ảnh đại diện** — ảnh hiển thị trên thẻ tin ở danh sách và kết quả tìm kiếm. |

## 4. Uploads — Tải ảnh lên

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/uploads/images` | Đăng nhập | **Upload một file ảnh** (jpg, png, webp, gif, tối đa 5 MB) và nhận lại URL. Dùng URL đó cho ảnh tin đăng hoặc ảnh đại diện. Server kiểm tra **chữ ký file thật** (magic bytes), không tin phần mở rộng do người dùng gửi, để chặn file độc hại giả làm ảnh. URL trả về là **đường dẫn tương đối** `/uploads/yyyy/MM/<guid>.jpg` (đi qua Vite proxy / cùng domain khi deploy). Giới hạn request 15 MB và **tối đa 40 ảnh / phút / người dùng** (rate limit). |
| GET | `/api/uploads/images` | Đăng nhập | **Danh sách ảnh tôi đã upload** (Admin: tất cả ảnh), mới nhất trước — dùng để chọn lại ảnh cũ. |
| GET | `/api/uploads/images/{id}` | Đăng nhập (người upload / Admin) | **Xem thông tin một ảnh** (URL, tên file gốc, dung lượng). Người khác gọi sẽ nhận 404 → không dò được ảnh của người khác (chống IDOR). |
| DELETE | `/api/uploads/images/{id}` | Đăng nhập (người upload / Admin) | **Xóa ảnh** khỏi CSDL và ổ đĩa. Ảnh đang dùng cho tin đăng / avatar sẽ bị chặn xóa (409). |

## 5. Favorites — Nhà yêu thích

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/favorites` | Đăng nhập | Danh sách **nhà đã lưu** (nút trái tim), có phân trang — trang "Nhà đã lưu". |
| POST | `/api/favorites/{propertyId}` | Đăng nhập | **Lưu một căn nhà** vào danh sách yêu thích để xem lại sau. |
| DELETE | `/api/favorites/{propertyId}` | Đăng nhập | **Bỏ lưu** một căn nhà. |

## 6. Appointments — Lịch hẹn xem nhà

Người thuê hẹn đến xem nhà trước khi quyết định thuê.
Vòng đời: **Chờ xác nhận → Đã xác nhận → Đã xem nhà**, hoặc **Bị từ chối / Đã hủy**.
Hệ thống tự nhắc lịch cho cả hai bên trước giờ hẹn 24 tiếng.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/appointments` | Tenant | **Đặt lịch hẹn xem nhà** (chọn nhà, ngày giờ, ghi chú). Chủ nhà nhận thông báo để xác nhận. |
| GET | `/api/appointments/{id}` | Đăng nhập (người thuê, chủ nhà hoặc Admin) | Xem **chi tiết một lịch hẹn**. |
| GET | `/api/appointments/mine` | Tenant | Danh sách **lịch hẹn tôi đã đặt**, lọc theo trạng thái. |
| GET | `/api/appointments/received` | Landlord | Danh sách **lịch hẹn khách đặt xem các nhà của tôi**. |
| PATCH | `/api/appointments/{id}/confirm` | Landlord / Admin | **Xác nhận** lịch hẹn (có thể kèm ghi chú như "gọi tôi khi tới"); người thuê nhận thông báo. |
| PATCH | `/api/appointments/{id}/reject` | Landlord / Admin | **Từ chối** lịch hẹn kèm lý do / đề xuất giờ khác. |
| PATCH | `/api/appointments/{id}/cancel` | Đăng nhập (người thuê, chủ nhà hoặc Admin) | **Hủy** lịch hẹn khi có việc bận; bên còn lại nhận thông báo. |
| PATCH | `/api/appointments/{id}/complete` | Landlord / Admin | Đánh dấu **đã dẫn khách xem nhà xong**. |

## 7. Bookings — Yêu cầu thuê & hợp đồng

Một yêu cầu thuê sau khi chủ nhà duyệt sẽ trở thành **hợp đồng thuê**.
Vòng đời: **Chờ duyệt → Đang thuê → Đã kết thúc**, hoặc **Bị từ chối / Đã hủy**.
Hợp đồng hết hạn được hệ thống tự chuyển sang Đã kết thúc và mở lại nhà cho khách mới.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/bookings` | Tenant | **Gửi yêu cầu thuê nhà**: chọn ngày bắt đầu và số tháng thuê (1–60). Hệ thống **chốt giá thuê và tiền cọc tại thời điểm gửi**, tính tổng tiền, kiểm tra nhà còn trống, không trùng lịch với hợp đồng khác, không tự thuê nhà của mình, ngày bắt đầu không ở quá khứ. Chủ nhà nhận thông báo. |
| GET | `/api/bookings` | Admin | Xem **toàn bộ yêu cầu thuê / hợp đồng** trong hệ thống: **lọc** theo trạng thái, **tìm kiếm** theo tên nhà / địa chỉ / tên, email, SĐT người thuê, **sắp xếp** (`SortBy`: mới nhất, cũ nhất, ngày bắt đầu, giá) và phân trang — trang "Hợp đồng" của Admin. |
| GET | `/api/bookings/{id}` | Đăng nhập (người thuê, chủ nhà hoặc Admin) | **Chi tiết một hợp đồng**: nhà, người thuê, thời hạn, giá, tiền cọc, trạng thái, lý do từ chối. |
| GET | `/api/bookings/mine` | Tenant | Danh sách **yêu cầu thuê / hợp đồng của tôi**, lọc theo trạng thái. |
| GET | `/api/bookings/received` | Landlord | Danh sách **yêu cầu thuê gửi đến các nhà của tôi** để chủ nhà xử lý. |
| PATCH | `/api/bookings/{id}/approve` | Landlord / Admin | **Duyệt** yêu cầu → thành hợp đồng, nhà chuyển sang **Đã cho thuê**. Các yêu cầu khác đang chờ cho cùng căn nhà **tự động bị từ chối** và người gửi được báo. Người thuê được nhắc thanh toán tiền cọc. |
| PATCH | `/api/bookings/{id}/reject` | Landlord / Admin | **Từ chối** yêu cầu thuê, bắt buộc ghi lý do gửi cho người thuê. |
| PATCH | `/api/bookings/{id}/cancel` | Tenant / Admin | Người thuê **hủy** yêu cầu đang chờ, hoặc hủy hợp đồng đã duyệt nhưng **chưa tới ngày bắt đầu** (nhà được mở lại cho thuê). Hợp đồng đã bắt đầu thì phải liên hệ chủ nhà. |
| PATCH | `/api/bookings/{id}/complete` | Landlord / Admin | **Kết thúc hợp đồng** (trả nhà); nhà mở lại cho thuê và người thuê được mời để lại đánh giá. |

## 8. Invoices — Hóa đơn hàng tháng

Chủ nhà lập hóa đơn mỗi tháng cho hợp đồng đang hiệu lực.
Trạng thái hóa đơn tự tính: **Chưa thanh toán → Trả một phần → Đã thanh toán**, hoặc **Quá hạn** nếu qua hạn mà chưa trả đủ.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/bookings/{bookingId}/invoices` | Landlord / Admin | **Lập hóa đơn tháng** = tiền thuê + tiền điện (chỉ số mới − cũ × đơn giá) + tiền nước + phí dịch vụ + phí khác. Để trống chỉ số cũ thì hệ thống lấy chỉ số mới của hóa đơn tháng trước. Mỗi tháng chỉ một hóa đơn, kỳ hóa đơn phải nằm trong thời hạn hợp đồng. Người thuê nhận thông báo kèm số tiền và hạn trả. |
| GET | `/api/bookings/{bookingId}/invoices` | Đăng nhập (người thuê, chủ nhà hoặc Admin) | Xem **tất cả hóa đơn của một hợp đồng**. |
| GET | `/api/invoices/{id}` | Đăng nhập (người thuê, chủ nhà hoặc Admin) | **Chi tiết một hóa đơn**: từng khoản tiền, chỉ số điện nước, số đã trả, còn nợ, hạn thanh toán. |
| GET | `/api/invoices/mine` | Tenant | Danh sách **hóa đơn tôi phải trả**, lọc theo trạng thái / tháng / năm. |
| GET | `/api/invoices/received` | Landlord | Danh sách **hóa đơn tôi đã lập** cho các nhà của mình, để theo dõi công nợ. |
| PATCH | `/api/invoices/{id}/cancel` | Landlord / Admin | **Hủy hóa đơn lập sai** — chỉ được hủy khi hóa đơn chưa nhận khoản thanh toán nào. |

## 9. Payments — Thanh toán

Người thuê tạo phiếu thanh toán (chuyển khoản, Momo, tiền mặt...), chủ nhà **xác nhận đã nhận tiền**.
Trạng thái: **Chờ xác nhận → Thành công / Thất bại**; khoản thành công có thể **Hoàn tiền** (ví dụ trả lại tiền cọc).

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/bookings/{bookingId}/payments` | Tenant | **Thanh toán tiền cọc hoặc tiền thuê / hóa đơn** cho hợp đồng đã duyệt. Hệ thống tạo mã giao dịch, chặn trả cọc hai lần, chặn trả vượt số tiền còn nợ của hóa đơn; chủ nhà nhận thông báo để xác nhận. |
| GET | `/api/bookings/{bookingId}/payments` | Đăng nhập (người thuê, chủ nhà hoặc Admin) | **Lịch sử thanh toán của một hợp đồng**. |
| GET | `/api/payments/mine` | Tenant | **Lịch sử thanh toán của tôi** trên mọi hợp đồng. |
| GET | `/api/payments/received` | Landlord | Các **khoản tiền chủ nhà nhận được**; lọc `Status=Pending` để xem những khoản đang chờ mình xác nhận. |
| PATCH | `/api/payments/{id}/status` | Landlord / Admin | **Xác nhận thanh toán**: Thành công (cộng tiền vào hóa đơn, hóa đơn tự chuyển sang Đã thanh toán khi đủ), Thất bại (chưa nhận được tiền), hoặc **Hoàn tiền** cho khoản đã thành công. Không cho xác nhận vượt số tiền còn nợ. Người thuê nhận thông báo kết quả. |

## 10. Reviews — Đánh giá

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/properties/{propertyId}/reviews` | Công khai | Danh sách **đánh giá của một căn nhà** (số sao, nhận xét, phản hồi của chủ nhà), có phân trang. |
| GET | `/api/properties/{propertyId}/reviews/eligibility` | Tenant | **Tôi có được đánh giá căn này không?** Trả `canReview` (đã từng thuê và chưa đánh giá), `hasRented`, `myReview`. Frontend chỉ hiện form viết đánh giá cho người đủ điều kiện. |
| POST | `/api/properties/{propertyId}/reviews` | Tenant | **Đánh giá nhà đã thuê** (1–5 sao + nhận xét). Chỉ người **đã từng thuê** căn nhà đó mới được đánh giá, mỗi người một lần. Điểm trung bình của nhà được tính lại tự động. |
| PUT | `/api/reviews/{id}` | Tenant | **Sửa đánh giá** của chính mình. |
| DELETE | `/api/reviews/{id}` | Đăng nhập (người viết hoặc Admin) | **Xóa đánh giá** — người viết tự xóa, hoặc Admin gỡ đánh giá không phù hợp. |
| PUT | `/api/reviews/{id}/reply` | Landlord / Admin | Chủ nhà **phản hồi đánh giá** (cảm ơn, giải thích); người đánh giá nhận thông báo. |

## 11. Conversations — Tin nhắn

Chat trực tiếp trong hệ thống, không cần trao đổi số điện thoại.
Người thuê ↔ chủ nhà (thường gắn với một căn nhà cụ thể), và người thuê / chủ nhà ↔ **quản trị viên (hỗ trợ)**.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/conversations` | Đăng nhập | **Bắt đầu trò chuyện** với một người (ví dụ hỏi chủ nhà "phòng còn trống không?" từ trang chi tiết nhà). Nếu đã có cuộc trò chuyện với người đó về cùng căn nhà thì tin nhắn được nối tiếp vào. |
| GET | `/api/conversations` | Đăng nhập | **Danh sách cuộc trò chuyện** của tôi, mới nhất trước, kèm tin nhắn cuối, số tin chưa đọc, vai trò người kia (để hiện nhãn "Hỗ trợ" với Admin). |
| POST | `/api/conversations/support` | Đăng nhập (người thuê, chủ nhà) | **Nhắn tin cho quản trị viên** để được hỗ trợ (thanh toán, duyệt tin, báo lỗi...). Không cần biết Admin là ai: hệ thống tự chọn Admin, và nếu đã từng chat với một Admin thì tiếp tục cuộc trò chuyện đó. |
| GET | `/api/conversations/unread-count` | Đăng nhập | **Tổng số tin nhắn chưa đọc** — hiển thị chấm đỏ trên biểu tượng tin nhắn. |
| GET | `/api/conversations/{id}/messages` | Đăng nhập (người trong cuộc) | **Đọc tin nhắn** trong một cuộc trò chuyện (phân trang, mới nhất trước) và tự đánh dấu "đã xem". Người ngoài cuộc không đọc được. |
| POST | `/api/conversations/{id}/messages` | Đăng nhập (người trong cuộc) | **Gửi tin nhắn** trong cuộc trò chuyện. |

## 12. Notifications — Thông báo

Hệ thống tự tạo thông báo khi có sự kiện: tin được duyệt / từ chối, có yêu cầu thuê, hóa đơn mới, thanh toán được xác nhận, nhắc lịch hẹn, hóa đơn quá hạn...

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/notifications` | Đăng nhập | Danh sách **thông báo của tôi**, lọc theo đã đọc / chưa đọc và theo loại (tin đăng, hợp đồng, hóa đơn, thanh toán...). |
| GET | `/api/notifications/unread-count` | Đăng nhập | **Số thông báo chưa đọc** — hiển thị trên biểu tượng chuông. |
| PATCH | `/api/notifications/{id}/read` | Đăng nhập | **Đánh dấu một thông báo đã đọc** (khi bấm vào xem). |
| PATCH | `/api/notifications/read-all` | Đăng nhập | **Đánh dấu tất cả đã đọc**. |
| DELETE | `/api/notifications/{id}` | Đăng nhập | **Xóa** một thông báo không cần nữa. |

## 13. Reports — Báo cáo vi phạm

Cộng đồng giúp Admin phát hiện tin lừa đảo, ảnh sai sự thật, thông tin sai.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| POST | `/api/properties/{propertyId}/reports` | Đăng nhập | **Báo cáo tin đăng vi phạm** với lý do: thông tin sai sự thật, sai giá, nhà đã cho thuê, lừa đảo, tin trùng lặp, nội dung không phù hợp hoặc lý do khác, kèm mô tả. Admin nhận thông báo. |
| GET | `/api/reports/mine` | Đăng nhập | Xem lại **các báo cáo tôi đã gửi** và kết quả xử lý. |
| GET | `/api/reports` | Admin | Danh sách **báo cáo vi phạm** cần xử lý: **lọc** theo trạng thái / lý do / tin đăng, **tìm kiếm** theo tiêu đề tin, mô tả, tên / email người báo cáo, **sắp xếp** (mới nhất / cũ nhất) và phân trang. |
| PATCH | `/api/reports/{id}` | Admin | **Xử lý báo cáo**: chấp nhận (Resolved) — có thể **ẩn luôn tin vi phạm** (chủ nhà được thông báo) — hoặc bác bỏ (Dismissed), kèm ghi chú của Admin; người báo cáo nhận thông báo kết quả. |

## 14. Statistics — Thống kê

Số liệu tổng hợp cho trang **Tổng quan (Dashboard)** của từng vai trò.

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/api/statistics/admin` | Admin | **Tổng quan toàn hệ thống**: số người dùng (theo vai trò, số tài khoản bị khóa), số tin đăng theo từng trạng thái (gồm tin chờ duyệt), số hợp đồng theo trạng thái, báo cáo vi phạm chờ xử lý, tổng giá trị giao dịch và **biểu đồ doanh thu 12 tháng**. |
| GET | `/api/statistics/landlord` | Landlord | **Tổng quan của chủ nhà**: số tin theo trạng thái, tổng lượt xem, điểm đánh giá trung bình và số đánh giá, yêu cầu thuê chờ duyệt, hợp đồng đang hiệu lực, lịch hẹn chờ xác nhận, số hóa đơn chưa thu và **tổng công nợ**, tổng doanh thu và doanh thu theo tháng. |
| GET | `/api/statistics/tenant` | Tenant | **Tổng quan của người thuê**: hợp đồng đang thuê, yêu cầu đang chờ duyệt, lịch hẹn sắp tới, số nhà đã lưu, **hóa đơn chưa trả** và tổng tiền còn nợ, tổng số tiền đã thanh toán. |

---

## Phụ lục: endpoint ngoài Swagger

| Phương thức | Đường dẫn | Quyền | Mục đích |
|---|---|---|---|
| GET | `/health` | Công khai | **Kiểm tra sức khỏe hệ thống**: API còn chạy và kết nối được cơ sở dữ liệu hay không (HTTP 200 = bình thường, 503 = lỗi). Dùng cho giám sát / triển khai. |
| GET | `/openapi/v1.json` | Công khai (Development) | Tài liệu OpenAPI dạng JSON — nguồn dữ liệu cho Swagger UI và Scalar. |
| GET | `/swagger`, `/scalar` | Công khai (Development) | Giao diện tài liệu và thử API trực tiếp trên trình duyệt. |

## Cách thử API trên Swagger

1. Mở `http://localhost:5224/swagger`.
2. Gọi `POST /api/auth/login` với tài khoản mẫu (ví dụ `admin@houserental.vn` / `Admin@123`), sao chép `accessToken` trong response.
3. Bấm nút **Authorize** ở góc trên, dán token, bấm Authorize.
4. Các API cần đăng nhập giờ gọi được với quyền của tài khoản đó.

Tài khoản mẫu: xem mục "Tài khoản mẫu" trong `README.md`.
