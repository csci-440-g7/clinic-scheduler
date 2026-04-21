#!/bin/bash
# Runs the .NET app natively on EC2 (avoids Docker blazor.web.js publish issue).
# PostgreSQL stays in Docker. The app is published and run directly.
set -euo pipefail

REPO_DIR="/home/ec2-user/clinic-scheduler"
APP_DIR="/home/ec2-user/app"
SERVICE_NAME="clinic-scheduler"

echo "=== ClinicScheduler — Native Deploy ==="

# Verify .env exists and has been filled in
if [ ! -f "$REPO_DIR/.env" ]; then
  echo "ERROR: .env not found. Run bootstrap.sh first, then edit .env."
  exit 1
fi

if grep -q "changeme" "$REPO_DIR/.env"; then
  echo "ERROR: .env still contains placeholder 'changeme' values."
  echo "       Edit $REPO_DIR/.env with real passwords before starting."
  exit 1
fi

# Source .env for variable access
set -a
source "$REPO_DIR/.env"
set +a

# Pull latest code
echo "[1/6] Pulling latest code from MVP..."
git -C "$REPO_DIR" fetch origin MVP
git -C "$REPO_DIR" reset --hard origin/MVP

# Ensure PostgreSQL is running in Docker
echo "[2/6] Starting PostgreSQL container..."
docker-compose -f "$REPO_DIR/docker-compose.yml" --env-file "$REPO_DIR/.env" up -d db

# Wait for PostgreSQL to be healthy
echo "       Waiting for PostgreSQL..."
until docker-compose -f "$REPO_DIR/docker-compose.yml" exec -T db pg_isready -U postgres > /dev/null 2>&1; do
  sleep 1
done
echo "       PostgreSQL is ready."

# Stop any existing app process
echo "[3/6] Stopping existing app process..."
sudo systemctl stop "$SERVICE_NAME" 2>/dev/null || true

# Clean up failed Docker build artifacts (app only — leave db running)
echo "[4/6] Cleaning up Docker build artifacts..."
docker compose -f "$REPO_DIR/docker-compose.yml" rm -sf app 2>/dev/null || true
docker rmi $(docker images --filter "reference=*clinic*scheduler*" -q) 2>/dev/null || true
docker image prune -f 2>/dev/null || true

# Publish the app
echo "[5/6] Publishing .NET app..."
dotnet publish "$REPO_DIR/ClinicScheduler/ClinicScheduler.Web/ClinicScheduler.Web.csproj" \
  -c Release -o "$APP_DIR" --nologo

# Create/update systemd service
echo "[6/6] Starting app via systemd..."
sudo tee /etc/systemd/system/${SERVICE_NAME}.service > /dev/null <<EOF
[Unit]
Description=ClinicScheduler Web App
After=network.target docker.service

[Service]
Type=exec
User=ec2-user
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/ClinicScheduler.Web.dll
Restart=always
RestartSec=5

Environment=ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
Environment=ASPNETCORE_URLS=http://+:8081
Environment=ConnectionStrings__DefaultConnection=Host=localhost;Database=clinic_scheduler;Username=postgres;Password=${POSTGRES_PASSWORD}
Environment=SeedAdmin__Password=${SEED_ADMIN_PASSWORD}

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl start "$SERVICE_NAME"

PUBLIC_IP=$(curl -sf http://169.254.169.254/latest/meta-data/public-ipv4 || echo "<public-ip>")
echo ""
echo "=== App is running! ==="
echo "  URL:    http://${PUBLIC_IP}:8081"
echo "  Logs:   journalctl -u $SERVICE_NAME -f"
echo "  Stop:   sudo systemctl stop $SERVICE_NAME"
echo "  DB:     docker-compose -f $REPO_DIR/docker-compose.yml logs -f db"
