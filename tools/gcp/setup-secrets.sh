#!/bin/bash
# Script to set up secrets in Google Cloud Secret Manager for SeeYouThereAPI

# Set your project ID - this should match the one used in setup.sh
PROJECT_ID="seeyouthere-api"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Setting up secrets for SeeYouThereAPI in GCP Secret Manager${NC}"

# Check if gcloud CLI is installed
if ! command -v gcloud &> /dev/null; then
    echo -e "${RED}Error: gcloud CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Make sure we're using the right project
echo -e "${YELLOW}Setting project to ${PROJECT_ID}...${NC}"
gcloud config set project ${PROJECT_ID}

# Function to create a secret
create_secret() {
    local name=$1
    local value=$2

    echo -e "${YELLOW}Creating secret '${name}'...${NC}"

    # Check if secret already exists
    if gcloud secrets describe ${name} &>/dev/null; then
        echo -e "${YELLOW}Secret '${name}' already exists. Updating...${NC}"
        echo -n "${value}" | gcloud secrets versions add ${name} --data-file=-
    else
        echo -e "${YELLOW}Creating new secret '${name}'...${NC}"
        echo -n "${value}" | gcloud secrets create ${name} --replication-policy="automatic" --data-file=-
    fi

    # Set permissions for the Cloud Run service account
    echo -e "${YELLOW}Setting permissions for Cloud Run service account...${NC}"

    # Get the service account (create if it doesn't exist)
    PROJECT_NUMBER=$(gcloud projects describe ${PROJECT_ID} --format='value(projectNumber)')
    SERVICE_ACCOUNT="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"

    # Grant access to the service account
    gcloud secrets add-iam-policy-binding ${name} \
        --member="serviceAccount:${SERVICE_ACCOUNT}" \
        --role="roles/secretmanager.secretAccessor"
}

# Prompt for secret values
echo -e "${YELLOW}Please enter your Amadeus API Key:${NC}"
read -r AMADEUS_API_KEY

echo -e "${YELLOW}Please enter your Amadeus API Secret:${NC}"
read -r AMADEUS_API_SECRET

echo -e "${YELLOW}Please enter your Kiwi API Key:${NC}"
read -r KIWI_API_KEY

# Create the secrets
create_secret "amadeus-api-key" "${AMADEUS_API_KEY}"
create_secret "amadeus-api-secret" "${AMADEUS_API_SECRET}"
create_secret "kiwi-api-key" "${KIWI_API_KEY}"

echo -e "${GREEN}Secrets setup complete!${NC}"
echo -e "${YELLOW}The following secrets are now available in Secret Manager:${NC}"
echo -e "  - amadeus-api-key"
echo -e "  - amadeus-api-secret"
echo -e "  - kiwi-api-key"

echo -e "${GREEN}Your Cloud Run deployment will automatically use these secrets as configured in cloudbuild.yaml${NC}"
