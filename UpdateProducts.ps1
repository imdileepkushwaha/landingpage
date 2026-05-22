$products = @(
    "Binary MLM Software", "Matrix MLM Software", "Unilevel MLM Software", "Generation MLM Software",
    "Trading Based MLM Software", "ROI Based MLM Software", "Centralized MLM Software", "De-Centralized MLM Software",
    "Token Based MLM Software", "Product Based MLM Software", "Ecommerce + MLM Software", "School Management Software",
    "Hospital Management Software", "OPD + IPD Billing Software", "CRM Software", "Billing Software",
    "Website Development", "Library Management Software", "Microfinance Management Software", "Multi Recharge Software",
    "Credit Cooperative Software", "Real Estate Software"
)

$images = @(
    "~/images/products/icon_binary_mlm_1779466652202.png",
    "~/images/products/icon_matrix_mlm_1779466667929.png",
    "~/images/products/icon_unilevel_mlm_1779466686284.png",
    "~/images/products/icon_generation_mlm_1779466701996.png",
    "~/images/products/icon_trading_mlm_1779466719764.png",
    "~/images/products/icon_roi_mlm_1779466745351.png",
    "~/images/products/icon_centralized_mlm_1779466762518.png",
    "~/images/products/icon_decentralized_mlm_1779466781243.png",
    "~/images/products/icon_token_mlm_1779466795911.png",
    "~/images/products/icon_product_mlm_1779466812039.png",
    "~/images/products/icon_ecommerce_mlm_1779466835747.png",
    "~/images/products/icon_school_mgmt_1779466851271.png",
    "~/images/products/icon_hospital_mgmt_1779466867609.png",
    "~/images/products/icon_opd_billing_1779466887002.png",
    "~/images/products/icon_crm_software_1779466902348.png",
    "~/images/products/icon_billing_software_1779466924528.png",
    "~/images/products/icon_website_dev_1779466950386.png",
    "~/images/industries/ind_education_1779436447854.png",
    "~/images/industries/ind_ecommerce_1779436415918.png",
    "~/images/industries/ind_telecom_1779436464801.png",
    "~/images/industries/ind_crypto_1779436432224.png",
    "~/images/products/prod_real_estate_1779438791205.png"
)

$html = ""
for ($i = 0; $i -lt $products.Length; $i++) {
    $title = $products[$i]
    $img = $images[$i]
    $html += @"
            <!-- $($i + 1). $title -->
            <div class="col-xl-3 col-lg-4 col-md-6" data-aos="fade-up">
                <div class="product-card-new">
                    <img src="$img" alt="$title">
                    <h5>$title</h5>
                    <button class="btn btn-outline-primary w-100 rounded-pill mt-auto" data-bs-toggle="modal" data-bs-target="#demoRequestModal" data-product="$title">Book a Demo</button>
                </div>
            </div>
"@ + "`n"
}

$file = "Views\Home\Index.cshtml"
$content = Get-Content $file -Raw

# Replace the block
$pattern = '(?s)<div class="row g-4 justify-content-center">.*?<!-- Our Services Section -->'
$replacement = "<div class=`"row g-4 justify-content-center`">`n$html        </div>`n    </div>`n</section>`n`n<!-- Our Services Section -->"

$newContent = [regex]::Replace($content, $pattern, $replacement)
Set-Content -Path $file -Value $newContent
Write-Host "Updated Index.cshtml successfully"
