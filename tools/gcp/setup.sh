#!/bin/bash
set -e

# GCP Project Bootstrap Script
# This script sets up a new GCP project with the required services for SeeYouThereAPI

# Configuration variables
PROJECT_ID="seeyouthere-api"
PROJECT_NAME="SeeYouThereAPI"
BILLING_ACCOUNT=""
REGION=""
BUDGET_AMOUNT=0
BUDGET_ALERT_EMAIL=""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}Starting GCP Project Bootstrap for ${PROJECT_NAME}${NC}"

# Check if gcloud CLI is installed
if ! command -v gcloud &> /dev/null; then
    echo -e "${RED}Error: gcloud CLI is not installed. Please install it first.${NC}"
    exit 1
fi

echo -e "${YELLOW}Checking authentication...${NC}"
gcloud auth list --filter=status:ACTIVE --format="value(account)" || {
    echo -e "${YELLOW}You need to authenticate with GCP first. Running gcloud auth login...${NC}"
    gcloud auth login
}

# Create new project
echo -e "${YELLOW}Creating new project: ${PROJECT_ID}...${NC}"
gcloud projects create ${PROJECT_ID} --name="${PROJECT_NAME}" || {
    echo -e "${YELLOW}Project ${PROJECT_ID} may already exist. Continuing...${NC}"
}

# Set the project as the default
echo -e "${YELLOW}Setting ${PROJECT_ID} as the default project...${NC}"
gcloud config set project ${PROJECT_ID}

# Link billing account if provided
if [ -n "$BILLING_ACCOUNT" ]; then
    echo -e "${YELLOW}Linking billing account...${NC}"
    gcloud billing projects link ${PROJECT_ID} --billing-account=${BILLING_ACCOUNT}
else
    echo -e "${RED}No billing account specified. You must link a billing account manually.${NC}"
    echo -e "${RED}Services will not be enabled without a billing account.${NC}"
    exit 1
fi

# Enable required APIs
echo -e "${YELLOW}Enabling required GCP services...${NC}"
CORE_SERVICES=(
    "cloudbuild.googleapis.com"
    "containerregistry.googleapis.com"
    "artifactregistry.googleapis.com"
    "run.googleapis.com"
    "secretmanager.googleapis.com"
    "logging.googleapis.com"
)

# First enable core services that don't typically need special permissions
for SERVICE in "${CORE_SERVICES[@]}"; do
    echo -e "Enabling ${SERVICE}..."
    gcloud services enable ${SERVICE} || {
        echo -e "${RED}Failed to enable ${SERVICE}. You may need additional permissions.${NC}"
        echo -e "${YELLOW}Please try enabling it manually in the GCP Console.${NC}"
    }
done

# Try to enable monitoring services (may require additional permissions)
MONITORING_SERVICES=(
    "cloudmonitoring.googleapis.com"
    "cloudbilling.googleapis.com"
)

echo -e "${YELLOW}Attempting to enable monitoring services (optional)...${NC}"
for SERVICE in "${MONITORING_SERVICES[@]}"; do
    echo -e "Enabling ${SERVICE}..."
    gcloud services enable ${SERVICE} || {
        echo -e "${YELLOW}Could not enable ${SERVICE} automatically.${NC}"
        echo -e "${YELLOW}This is often due to permission restrictions and is not critical.${NC}"
        echo -e "${YELLOW}You can manually enable this service in the GCP Console if needed.${NC}"
    }
done

# Create Artifact Registry repository
echo -e "${YELLOW}Creating Artifact Registry Docker repository...${NC}"
gcloud artifacts repositories create seeyouthere-docker \
    --repository-format=docker \
    --location=${REGION} \
    --description="Docker repository for SeeYouThereAPI" || {
        echo -e "${RED}Failed to create Artifact Registry repository.${NC}"
        echo -e "${YELLOW}This may be due to permission issues or the repository already exists.${NC}"
    }

# Set up budget and alerts (if billing email is provided)
if [ -n "$BUDGET_ALERT_EMAIL" ]; then
    echo -e "${YELLOW}Attempting to set up budget alerts...${NC}"

    if gcloud billing budgets --help &>/dev/null; then
        echo -e "${YELLOW}Creating budget alert configuration...${NC}"

        # Create a budget JSON file
        cat > budget.json << EOF
{
  "displayName": "SeeYouThereAPI Budget",
  "budgetFilter": {
    "projects": ["projects/${PROJECT_ID}"]
  },
  "amount": {
    "specifiedAmount": {
      "currencyCode": "USD",
      "units": "${BUDGET_AMOUNT}"
    }
  },
  "thresholdRules": [
    {
      "thresholdPercent": 0.5,
      "spendBasis": "CURRENT_SPEND"
    },
    {
      "thresholdPercent": 0.75,
      "spendBasis": "CURRENT_SPEND"
    },
    {
      "thresholdPercent": 0.9,
      "spendBasis": "CURRENT_SPEND"
    },
    {
      "thresholdPercent": 1.0,
      "spendBasis": "CURRENT_SPEND"
    }
  ],
  "notificationsRule": {
    "monitoringNotificationChannels": [],
    "pubsubTopic": "",
    "schemaVersion": "1.0",
    "disableDefaultIamRecipients": false
  },
  "allUpdatesRule": {
    "pubsubTopic": "",
    "schemaVersion": "1.0",
    "monitoringNotificationChannels": [],
    "disableDefaultIamRecipients": false
  }
}
EOF

        # Try to create a notification channel for email alerts
        echo -e "${YELLOW}Attempting to create notification channel...${NC}"
        CHANNEL_ID=$(gcloud monitoring channels create \
            --display-name="SeeYouThereAPI Budget Alerts" \
            --description="Email notifications for budget alerts" \
            --type=email \
            --channel-labels=email_address=${BUDGET_ALERT_EMAIL} \
            --format="value(name)" 2>/dev/null) || {
                echo -e "${YELLOW}Could not create monitoring channel automatically.${NC}"
                echo -e "${YELLOW}This is likely due to permission restrictions.${NC}"
            }

        if [ -n "$CHANNEL_ID" ]; then
            # Update budget.json with the notification channel
            CHANNEL_NAME=$(echo $CHANNEL_ID | sed 's/.*\///')
            sed -i '' "s/\"monitoringNotificationChannels\": \[\]/\"monitoringNotificationChannels\": \[\"${CHANNEL_NAME}\"\]/" budget.json

            # Try to create the budget using the Cloud Billing Budget API
            echo -e "${YELLOW}Creating budget with alert thresholds at 50%, 75%, 90%, and 100%...${NC}"
            gcloud billing budgets create \
                --billing-account=${BILLING_ACCOUNT} \
                --budget-file=budget.json || {
                    echo -e "${YELLOW}Could not create budget automatically.${NC}"
                    echo -e "${YELLOW}Manual configuration instructions are provided below.${NC}"
                }
        fi

        # Clean up temporary file
        rm -f budget.json
    else
        echo -e "${YELLOW}The gcloud billing budgets command is not available.${NC}"
    fi

    # Provide manual instructions regardless of whether automatic setup worked
    echo -e "${YELLOW}===== MANUAL BUDGET SETUP INSTRUCTIONS =====${NC}"
    echo -e "${YELLOW}If budget alerts were not set up automatically, follow these steps:${NC}"
    echo -e "${YELLOW}1. Go to: https://console.cloud.google.com/billing/${NC}"
    echo -e "${YELLOW}2. Select your billing account${NC}"
    echo -e "${YELLOW}3. Click on 'Budgets & alerts' in the left menu${NC}"
    echo -e "${YELLOW}4. Click 'CREATE BUDGET'${NC}"
    echo -e "${YELLOW}5. Select your project: ${PROJECT_ID}${NC}"
    echo -e "${YELLOW}6. Set budget amount: \$${BUDGET_AMOUNT}${NC}"
    echo -e "${YELLOW}7. Set threshold rules for 50%, 75%, 90%, and 100%${NC}"
    echo -e "${YELLOW}8. Add your email: ${BUDGET_ALERT_EMAIL}${NC}"
    echo -e "${YELLOW}9. Click 'Save'${NC}"
    echo -e "${YELLOW}=========================================${NC}"
else
    echo -e "${RED}No budget alert email specified. Skipping budget creation.${NC}"
fi

echo -e "${GREEN}GCP project bootstrap complete!${NC}"
echo -e "${YELLOW}Project ID: ${PROJECT_ID}${NC}"
echo -e "${YELLOW}Region: ${REGION}${NC}"
echo -e "${YELLOW}Enabled core services: Cloud Run, Cloud Build, Container Registry, Artifact Registry, Secret Manager${NC}"
echo -e "${YELLOW}Note: Some services may need to be enabled manually due to permission restrictions.${NC}"

echo -e "${GREEN}Next steps:${NC}"
echo -e "${YELLOW}1. Update the cloudbuild.yaml with your project-specific information${NC}"
echo -e "${YELLOW}2. Set up secrets in Secret Manager for your API keys${NC}"
echo -e "${YELLOW}3. Use Cloud Build to deploy your application${NC}"
echo -e "${YELLOW}4. If not already done, configure budget alerts manually following the instructions above${NC}"

# Provide instructions for the cloudbuild.yaml modifications
echo -e "${YELLOW}===== CLOUDBUILD.YAML UPDATE INSTRUCTIONS =====${NC}"
echo -e "${YELLOW}Update the following in your cloudbuild.yaml file:${NC}"
echo -e "${YELLOW}  _REGION: '${REGION}'${NC}"
echo -e "${YELLOW}  _REPOSITORY: 'seeyouthere-docker'${NC}"
echo -e "${YELLOW}  _ENVIRONMENT: 'Production' (or your preferred environment)${NC}"
echo -e "${YELLOW}=========================================${NC}"
