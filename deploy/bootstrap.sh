#!/bin/bash
# Run once on a fresh Amazon Linux 2023 EC2 instance.
# Usage: bash bootstrap.sh
set -euo pipefail

echo "=== ClinicScheduler — EC2 Bootstrap ==="

# ── .NET 10 SDK ───────────────────────────────────────────────────────────────
echo "[1/5] Installing .NET 10 SDK..."
sudo rpm -Uvh https://packages.microsoft.com/config/amazonlinux/2023/packages-microsoft-prod.rpm 2>/dev/null || true
sudo dnf install -y dotnet-sdk-10.0

# ── Docker (for PostgreSQL) ───────────────────────────────────────────────────
echo "[2/5] Installing Docker..."
sudo dnf install -y docker git
sudo systemctl enable --now docker
sudo usermod -aG docker ec2-user

# ── Docker Compose (standalone binary) ────────────────────────────────────────
echo "[3/5] Installing Docker Compose..."
COMPOSE_VERSION=$(curl -fsSL https://api.github.com/repos/docker/compose/releases/latest \
  | grep '"tag_name"' | cut -d'"' -f4)
sudo curl -fsSL \
  "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-linux-x86_64" \
  -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# ── Clone repo ────────────────────────────────────────────────────────────────
echo "[4/5] Cloning repository..."
REPO_DIR="/home/ec2-user/clinic-scheduler"
if [ -d "$REPO_DIR" ]; then
  echo "  Repo already exists at $REPO_DIR — pulling latest..."
  git -C "$REPO_DIR" pull origin MVP
else
  git clone -b MVP https://github.com/csci-440-g7/clinic-scheduler.git "$REPO_DIR"
fi
sudo chown -R ec2-user:ec2-user "$REPO_DIR"

# ── .env setup ────────────────────────────────────────────────────────────────
echo "[5/5] Setting up .env..."
ENV_FILE="$REPO_DIR/.env"
if [ ! -f "$ENV_FILE" ]; then
  cp "$REPO_DIR/.env.example" "$ENV_FILE"
  echo ""
  echo "  !! .env created from .env.example — you MUST edit it with real passwords !!"
  echo "     nano $ENV_FILE"
else
  echo "  .env already exists — skipping copy."
fi

# ── Done ──────────────────────────────────────────────────────────────────────
PUBLIC_IP=$(curl -sf http://169.254.169.254/latest/meta-data/public-ipv4 || echo "<public-ip>")
echo ""
echo "=== Bootstrap complete! ==="
echo ""
echo "Next steps:"
echo "  1. Log out and back in so the docker group takes effect:"
echo "       exit"
echo "       ssh -i ~/clinic-capstone-key.pem ec2-user@${PUBLIC_IP}"
echo ""
echo "  2. Edit .env with real passwords:"
echo "       nano $ENV_FILE"
echo "     POSTGRES_PASSWORD  — any strong password"
echo "     SEED_ADMIN_PASSWORD — min 10 chars, uppercase, digit, special char"
echo "     ASPNETCORE_ENVIRONMENT — leave as Production"
echo ""
echo "  3. Start the app (native — recommended):"
echo "       bash $REPO_DIR/deploy/start-native.sh"
echo ""
echo "     Or via Docker (has known blazor.web.js issue):"
echo "       bash $REPO_DIR/deploy/start.sh"
echo ""
echo "  App will be available at: http://${PUBLIC_IP}:8081"
