#!/bin/bash

# GitHub Branch Protection Setup Script
# Run this after pushing your code to GitHub

# Make sure you're authenticated with GitHub CLI first: gh auth login

echo "Setting up branch protection for master branch..."

gh api repos/:owner/:repo/branches/master/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":["Frontend Tests","Backend Tests","Integration Check"]}' \
  --field enforce_admins=true \
  --field required_pull_request_reviews='{"required_approving_review_count":1,"dismiss_stale_reviews":true}' \
  --field restrictions=null \
  --field allow_force_pushes=false \
  --field allow_deletions=false

echo "✅ Branch protection rule created successfully!"
echo ""
echo "Master branch is now protected with:"
echo "- No direct pushes allowed"
echo "- Pull requests required"
echo "- All tests must pass before merge"
echo "- At least 1 approval required"
echo "- Stale reviews dismissed on new commits"