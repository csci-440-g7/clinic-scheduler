#!/bin/bash
set -e

REPO_DIR="/home/ec2-user/clinic-scheduler"

echo "=== ClinicScheduler — Start / Update ==="

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

# Pull latest code from MVP branch
echo "[1/4] Pulling latest code from MVP..."
git -C "$REPO_DIR" pull origin MVP

# Stop existing containers (if any)
echo "[2/4] Stopping existing containers and pruning old images..."
docker-compose -f "$REPO_DIR/docker-compose.yml" down || true
docker image prune -f || true

# Rebuild without cache to ensure fresh image
echo "[3/4] Building image (no cache)..."
docker-compose -f "$REPO_DIR/docker-compose.yml" --env-file "$REPO_DIR/.env" build --no-cache

# Start containers
echo "[4/4] Starting containers..."
docker-compose -f "$REPO_DIR/docker-compose.yml" --env-file "$REPO_DIR/.env" up -d

PUBLIC_IP=$(curl -sf http://169.254.169.254/latest/meta-data/public-ipv4 || echo "<public-ip>")
echo ""
echo "=== App is running! ==="
echo "  URL:  http://${PUBLIC_IP}:8081"
echo "  Logs: docker-compose -f $REPO_DIR/docker-compose.yml logs -f app"
echo "  Stop: docker-compose -f $REPO_DIR/docker-compose.yml down"
