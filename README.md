# clinic-scheduler

East Texas A&amp;M CSCI-440 Group 7 capstone. Pain Management Clinic Scheduler.

# Getting started with development

## Prerequisites

- Install Python (3.12 or higher)
- Install Node.js (v20 or higher)
- Install uv: `curl -LsSf https://astral.sh/uv/install.sh | sh` (macOS/Linux) or `powershell -c "irm https://astral.sh/uv/install.ps1 | iex"` (Windows)

## Initial Installation

1. Clone the repository:
   git clone https://github.com/csci-440-g7/clinic-scheduler.git
   cd pain-management-system

   Set git user.name and user.email:
   git config --local user.name "{YOUR NAME}"
   git config --local user.email "{YOUR LEOMAIL USERNAME}@leomail.tamuc.edu"

2. Install Project Manager:
   In the root directory, run:
   npm install

3. Run Global Setup:
   This command will install all Python dependencies in the backend and Node modules in the frontend:
   npm run setup

## Recommended Extensions (VS Code)

When you open this project in VS Code, you should see a notification to install "Recommended Extensions." Please install them. They include:

- Ruff (Python Linter/Formatter)
- Prettier (Frontend Formatter)
- Python (Microsoft)
- ESLint

## Local Development

To run both the Django backend and the Vite/React frontend simultaneously:
npm run dev

The servers will be available at:

- Backend API: http://localhost:8000
- API Documentation (Swagger): http://localhost:8000/api/docs
- Frontend: http://localhost:5173

## Backend Manual Commands

If you need to run backend commands specifically (ensure you are in the /backend folder):

- Sync environment: `uv sync`
- Create migrations: `uv run python manage.py makemigrations`
- Apply migrations: `uv run python manage.py migrate`
- Create superuser: `uv run python manage.py createsuperuser`

## Workflow Guidelines

- Code Formatting: Formatting is handled automatically on save via Ruff (Python) and Prettier (JS/TS).
- Git: Never commit the `backend/.venv` or `frontend/node_modules` folders. Ensure your local `uv.lock` and `package-lock.json` files are included in your pull requests.
