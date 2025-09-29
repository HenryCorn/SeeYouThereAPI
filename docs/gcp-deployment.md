# Google Cloud Platform Deployment Guide

This document describes how to deploy SeeYouThereAPI to Google Cloud Platform (GCP) using Cloud Run, Cloud Build, Artifact Registry, and other GCP services.

## Prerequisites

- Google Cloud Platform account
- Google Cloud SDK (gcloud CLI) installed
- Docker installed for local testing
- Billing account configured on GCP

## Initial Setup

### Bootstrap GCP Project

1. Edit the `tools/gcp/setup.sh` script to set your own:
   - PROJECT_ID (must be globally unique)
   - BILLING_ACCOUNT (your GCP billing account ID)
   - BUDGET_ALERT_EMAIL (email to receive budget notifications)
   - BUDGET_AMOUNT (budget cap in USD)

2. Make the script executable and run it:
   ```bash
   chmod +x tools/gcp/setup.sh
   ./tools/gcp/setup.sh
   ```

This script will:
- Create a new GCP project
- Enable required APIs (Cloud Run, Cloud Build, Artifact Registry, Container Registry, Secret Manager)
- Set up an Artifact Registry repository for Docker images
- Configure budget alerts for cost management

### Store API Keys in Secret Manager

Store your API keys in GCP Secret Manager:

```bash
# Store Amadeus API credentials
gcloud secrets create amadeus-api-key --data-file=/path/to/amadeus-key.txt
gcloud secrets create amadeus-api-secret --data-file=/path/to/amadeus-secret.txt

# Store Kiwi API credentials
gcloud secrets create kiwi-api-key --data-file=/path/to/kiwi-key.txt
```

## Manual Deployment

You can deploy the application manually using the following steps:

1. Build the Docker image:
   ```bash
   docker build -t seeyouthere-api:latest .
   ```

2. Tag the image for Artifact Registry:
   ```bash
   docker tag seeyouthere-api:latest [REGION]-docker.pkg.dev/[PROJECT_ID]/seeyouthere-docker/seeyouthere-api:latest
   ```

3. Push to Artifact Registry:
   ```bash
   docker push [REGION]-docker.pkg.dev/[PROJECT_ID]/seeyouthere-docker/seeyouthere-api:latest
   ```

4. Deploy to Cloud Run:
   ```bash
   gcloud run deploy seeyouthere-api \
     --image=[REGION]-docker.pkg.dev/[PROJECT_ID]/seeyouthere-docker/seeyouthere-api:latest \
     --region=[REGION] \
     --platform=managed \
     --allow-unauthenticated \
     --set-env-vars=ASPNETCORE_ENVIRONMENT=Production \
     --set-secrets=AMADEUS_API_KEY=amadeus-api-key:latest,AMADEUS_API_SECRET=amadeus-api-secret:latest,KIWI_API_KEY=kiwi-api-key:latest
   ```

## Automated CI/CD with Cloud Build

The repository includes a `cloudbuild.yaml` configuration file for setting up continuous deployment with Cloud Build.

### Setting up Cloud Build

1. Connect your repository to Cloud Build:
   - Go to Cloud Build in GCP Console
   - Connect your repository (GitHub, Bitbucket, or Cloud Source Repositories)
   - Select the repository containing SeeYouThereAPI

2. Create a build trigger:
   - Name: "SeeYouThereAPI-Deploy"
   - Event: Push to branch
   - Source: Branch (e.g., ^main$)
   - Configuration: Cloud Build configuration file (cloudbuild.yaml)
   - Substitution variables (if needed):
     - _REGION: Your preferred region (e.g., us-central1)
     - _REPOSITORY: Your Artifact Registry repository name
     - _ENVIRONMENT: Deployment environment (Production, Staging, etc.)

### Manual Trigger

You can manually trigger a build from the command line:

```bash
gcloud builds submit --config=cloudbuild.yaml \
  --substitutions=_REGION="us-central1",_ENVIRONMENT="Production"
```

## Monitoring and Logging

- **Cloud Monitoring**: Access metrics for your Cloud Run service
- **Cloud Logging**: View application logs
- **Budget Alerts**: Configured during setup to notify at 50%, 75%, 90%, and 100% of budget

## Cost Management

Budget alerts have been configured as part of the setup script. You can modify or view your budget at:
https://console.cloud.google.com/billing/

## Security Considerations

- The deployment uses Secret Manager for sensitive credentials
- Cloud Run services are configured with least privilege
- Consider implementing Identity and Access Management (IAM) for production deployments
- Review and adjust the `--allow-unauthenticated` flag for public-facing APIs

## Troubleshooting

- **Deployment issues**: Check Cloud Build logs
- **Runtime issues**: Check Cloud Run service logs
- **API key issues**: Verify secrets in Secret Manager and proper mounting in cloudbuild.yaml
