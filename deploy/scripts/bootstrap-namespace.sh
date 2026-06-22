#!/usr/bin/env bash
# bootstrap-namespace.sh — one-time setup of the numbatwallet-test namespace on the
# shared nonprod AKS cluster (aks-shared-nonprod-aue), following the
# credentry-infrastructure conventions.
#
# Creates:
#   - namespace numbatwallet-test (labels project/environment per the shared pattern)
#   - K8s Secret numbatwallet-secrets, sourced from Key Vault kv-numbatwallet-test-aue:
#       ConnectionStrings--numbatwallet  -> connection-string
#       jwt-secret-key                   -> jwt-secret-key
#       field-encryption-key             -> field-encryption-key
#       admin-api-key                    -> admin-api-key
#
# Prerequisites:
#   - az login (with access to subscription "Credenxia AU LAB" for the product KV)
#   - kubectl context pointing at aks-shared-nonprod-aue (az aks get-credentials + kubelogin)
#
# Idempotent: safe to re-run; updates the secret in place.

set -euo pipefail

NAMESPACE="numbatwallet-test"
KV_NAME="kv-numbatwallet-test-aue"
KV_SUBSCRIPTION="${KV_SUBSCRIPTION:-Credenxia AU LAB}"
SECRET_NAME="numbatwallet-secrets"

echo ">>> Ensuring namespace ${NAMESPACE}"
kubectl apply -f - <<EOF
apiVersion: v1
kind: Namespace
metadata:
  name: ${NAMESPACE}
  labels:
    project: numbatwallet
    environment: test
    managed-by: credentry-infrastructure
    app.kubernetes.io/managed-by: credentry-infrastructure
EOF

echo ">>> Reading secrets from Key Vault ${KV_NAME}"
get_kv() {
  az keyvault secret show --subscription "${KV_SUBSCRIPTION}" \
    --vault-name "${KV_NAME}" --name "$1" --query value -o tsv
}

CONNECTION_STRING="$(get_kv 'ConnectionStrings--numbatwallet')"
JWT_SECRET_KEY="$(get_kv 'jwt-secret-key')"
FIELD_ENCRYPTION_KEY="$(get_kv 'field-encryption-key')"
ADMIN_API_KEY="$(get_kv 'admin-api-key')"

echo ">>> Applying K8s secret ${SECRET_NAME} in ${NAMESPACE}"
kubectl create secret generic "${SECRET_NAME}" \
  --namespace "${NAMESPACE}" \
  --from-literal=connection-string="${CONNECTION_STRING}" \
  --from-literal=jwt-secret-key="${JWT_SECRET_KEY}" \
  --from-literal=field-encryption-key="${FIELD_ENCRYPTION_KEY}" \
  --from-literal=admin-api-key="${ADMIN_API_KEY}" \
  --dry-run=client -o yaml | kubectl apply -f -

echo ">>> Done. Namespace contents:"
kubectl get all,secrets -n "${NAMESPACE}"
