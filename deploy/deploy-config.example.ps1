# Copy to deploy/deploy-config.ps1 (git-ignored) and adjust.
# Read by deploy.ps1 and provision.ps1 when -AppName / -ResourceGroup / -Subscription are not passed.
# No secrets here: Google, Jira and Xray values are App Service settings (docs/deployment-azure.md).

$DeployAppName = '<app-name>'
$DeployResourceGroup = '<resource-group>'
# Optional: subscription name or id; leave empty to use the current `az account show`.
$DeploySubscription = ''
