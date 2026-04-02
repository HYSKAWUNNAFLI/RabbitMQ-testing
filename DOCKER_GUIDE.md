# Hướng dẫn Chạy và Đẩy lên Docker Hub

## 1. Chạy ứng dụng tại máy cục bộ (Local)

Để khởi động toàn bộ 3 services (RabbitMQ, Backend, Frontend) bạn chỉ cần chạy lệnh sau tại thư mục gốc:
```bash
docker-compose up --build -d
```
Sau khi chạy thành công:
- **Frontend (UI Quản lý):** http://localhost:3000
- **RabbitMQ Management UI:** http://localhost:15672 (user/pass mặc định là `guest`/`guest`)
- **Backend API:** http://localhost:4000

## 2. Kiểm tra log
Để xem backend đang gọi các terminal như thế nào:
```bash
docker-compose logs -f backend
```

## 3. Đóng gói Images và đẩy lên Docker Hub

Nếu bạn muốn public ứng dụng này lên Docker Hub của bạn, hãy làm theo các bước sau:

**Bước 1: Đăng nhập vào Docker Hub**
```bash
docker login
```
*Nhập Username và Password của bạn.*

**Bước 2: Build các image với thẻ (tag) phù hợp tài khoản của bạn**
Thay `your_dockerhub_username` bằng tên thật của bạn.
```bash
docker build -t your_dockerhub_username/rabbitmq-tester-backend:latest -f BackendManager/Dockerfile .
docker build -t your_dockerhub_username/rabbitmq-tester-frontend:latest -f frontend/Dockerfile ./frontend
```

**Bước 3: Push lên Docker Hub**
```bash
docker push your_dockerhub_username/rabbitmq-tester-backend:latest
docker push your_dockerhub_username/rabbitmq-tester-frontend:latest
```

**Bước 4: Chạy ở máy tính khác với file docker-compose-prod.yml**
Tại server bất kỳ, bạn chỉ cần một file `docker-compose.yml` tải các image đó về:
```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
  backend:
    image: your_dockerhub_username/rabbitmq-tester-backend:latest
    environment:
      - RABBITMQ_HOST=rabbitmq
    ports:
      - "4000:4000"
  frontend:
    image: your_dockerhub_username/rabbitmq-tester-frontend:latest
    ports:
      - "3000:80"
```
Rồi chạy `docker-compose up -d`.
