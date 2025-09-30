#!/bin/bash

# Fix all the brace warnings and naming issues in security files

# Fix SecurityHeaders.cs brace warnings
sed -i '' '184s/if (string.IsNullOrEmpty(input))/if (string.IsNullOrEmpty(input)) {/' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs
sed -i '' '185a\        }' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

sed -i '' '198s/if (string.IsNullOrEmpty(input))/if (string.IsNullOrEmpty(input)) {/' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs
sed -i '' '199a\        }' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

sed -i '' '214s/if (string.IsNullOrEmpty(fileName))/if (string.IsNullOrEmpty(fileName)) {/' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs
sed -i '' '215a\        }' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

sed -i '' '241s/if (string.IsNullOrWhiteSpace(email))/if (string.IsNullOrWhiteSpace(email)) {/' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs
sed -i '' '242a\        }' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

sed -i '' '257s/if (string.IsNullOrWhiteSpace(url))/if (string.IsNullOrWhiteSpace(url)) {/' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs
sed -i '' '258a\        }' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

sed -i '' '266s/if (string.IsNullOrWhiteSpace(phoneNumber))/if (string.IsNullOrWhiteSpace(phoneNumber)) {/' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs
sed -i '' '267a\        }' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

# Fix SecurityAuditService.cs issues
sed -i '' '144s/if (from.HasValue)/if (from.HasValue) {/' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs
sed -i '' '145a\        }' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs

sed -i '' '147s/if (to.HasValue)/if (to.HasValue) {/' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs
sed -i '' '148a\        }' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs

sed -i '' '167s/if (from.HasValue)/if (from.HasValue) {/' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs
sed -i '' '168a\        }' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs

# Fix the 'to' parameter name issue
sed -i '' 's/DateTime? to/DateTime? endDate/g' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs
sed -i '' 's/to.HasValue/endDate.HasValue/g' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs
sed -i '' 's/to.Value/endDate.Value/g' src/NumbatWallet.Web.Api/Security/SecurityAuditService.cs

# Fix substring issue with AsSpan
sed -i '' 's/nameWithoutExtension.Substring(0, 255 - extension.Length)/nameWithoutExtension.AsSpan(0, 255 - extension.Length).ToString()/g' src/NumbatWallet.Web.Api/Security/SecurityHeaders.cs

echo "Fixed all security file warnings"