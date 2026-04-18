#!/bin/bash
# Start or update the ClinicScheduler app on EC2.
# Run after bootstrap.sh and after editing .env.
# Safe to re-run — pulls latest code and rebuilds containers.
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

# Pull latest code
echo "[1/2] Pulling latest code from main..."
git -C "$REPO_DIR" pull origin main

# Rebuild and restart containers
echo "[2/2] Building and starting containers..."
docker-compose -f "$REPO_DIR/docker-compose.yml" --env-file "$REPO_DIR/.env" up --build -d

PUBLIC_IP=$(curl -sf http://169.254.169.254/latest/meta-data/public-ipv4 || echo "<public-ip>")
echo ""
echo "=== App is running! ==="
echo "  URL:  http://${PUBLIC_IP}:8080"
echo "  Logs: docker-compose -f $REPO_DIR/docker-compose.yml logs -f app"
echo "  Stop: docker-compose -f $REPO_DIR/docker-compose.yml down"
