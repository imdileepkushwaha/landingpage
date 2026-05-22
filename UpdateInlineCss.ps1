$file = "Views\Home\Index.cshtml"
$content = Get-Content $file -Raw

# Replacements
$replacements = @{
    'style="position: fixed; z-index: 9999; top: 100px; right: 20px; max-width: 400px; background: rgba(16, 185, 129, 0.9); backdrop-filter: blur(10px); color: white;"' = ''
    'class="alert alert-success alert-dismissible fade show m-4 shadow-lg border-0 rounded-4"' = 'class="alert alert-success alert-dismissible fade show m-4 shadow-lg border-0 rounded-4 alert-floating-success"'

    'style="position: fixed; z-index: 9999; top: 100px; right: 20px; max-width: 400px; background: rgba(239, 68, 68, 0.9); backdrop-filter: blur(10px); color: white;"' = ''
    'class="alert alert-danger alert-dismissible fade show m-4 shadow-lg border-0 rounded-4"' = 'class="alert alert-danger alert-dismissible fade show m-4 shadow-lg border-0 rounded-4 alert-floating-danger"'

    'style="background: rgba(0, 242, 254, 0.1); border-color: rgba(0,242,254,0.3) !important;"' = ''
    'class="d-inline-flex align-items-center py-1 px-3 rounded-pill border border-secondary mb-4 floating-element"' = 'class="d-inline-flex align-items-center py-1 px-3 rounded-pill border border-secondary mb-4 floating-element new-badge-container"'

    'style="background: linear-gradient(135deg, #00f2fe, #4facfe) !important; color:#fff;"' = ''
    'class="badge bg-primary rounded-pill me-2 glow-pulse"' = 'class="badge bg-primary rounded-pill me-2 glow-pulse new-badge-gradient"'

    'style="font-size: 1.2rem; line-height: 1.8;"' = ''
    'class="lead text-muted mb-5 pe-lg-5"' = 'class="lead text-muted mb-5 pe-lg-5 lead-text-large"'

    'style="margin-right: -10px; z-index: 3;"' = 'class="rounded-circle border border-2 border-dark avatar-stack-1"'
    'style="margin-right: -10px; z-index: 2;"' = 'class="rounded-circle border border-2 border-dark avatar-stack-2"'
    'style="z-index: 1;"' = 'class="rounded-circle border border-2 border-dark avatar-stack-3"'
    'class="rounded-circle border border-2 border-dark"' = '' # we merged class and style above

    'style="background: #fff; padding: 60px 0;"' = ''
    'id="services"' = 'id="services" class="section-services"'

    'style="color: var(--primary-color)"' = 'class="text-primary-custom"'

    'style="max-width: 800px;"' = ''
    'class="text-muted lead mx-auto"' = 'class="text-muted lead mx-auto max-w-800"'

    'style="border-color: rgba(0,242,254,0.3); box-shadow: 0 10px 20px rgba(0,242,254,0.05);"' = ''
    'class="service-card"' = 'class="service-card service-card-highlight"'

    'style="background: linear-gradient(135deg, var(--secondary-color), var(--primary-color)); color: white;"' = ''
    'class="service-icon-box"' = 'class="service-icon-box service-icon-gradient"'

    'style="background: var(--bg-darker); padding: 100px 0;"' = ''
    'id="products"' = 'id="products" class="section-products"'
    'id="industries"' = 'id="industries" class="section-industries"'

    'style="background: var(--bg-dark); padding: 60px 0;"' = ''
    'id="demo"' = 'id="demo" class="section-demo"'

    'style="background: rgba(255,255,255,0.9); border: 1px solid rgba(0,0,0,0.05); box-shadow: 0 20px 40px rgba(0,0,0,0.05);"' = ''
    'class="glass-form-container"' = 'class="glass-form-container glass-form-custom"'

    'style="width: 40px; height: 40px; font-size: 18px; margin-bottom: 0; margin-right: 15px;"' = ''
    'class="icon-wrapper"' = 'class="icon-wrapper icon-wrapper-small"'
}

foreach ($key in $replacements.Keys) {
    # Replace literal strings carefully. In powershell, String.Replace is safest for HTML snippets.
    $val = $replacements[$key]
    $content = $content.Replace($key, $val)
}

# Cleanup extra spaces
$content = $content.Replace('  >', ' >')
$content = $content.Replace(' >', '>')
$content = $content.Replace(' class=""', '')

Set-Content -Path $file -Value $content
Write-Host "Updated Index.cshtml"

$fileLayout = "Views\Shared\_Layout.cshtml"
$contentLayout = Get-Content $fileLayout -Raw

$contentLayout = $contentLayout.Replace('style="background: var(--bg-darker);"', '')
$contentLayout = $contentLayout.Replace('class="modal-content border-0 shadow-lg rounded-4"', 'class="modal-content border-0 shadow-lg rounded-4 modal-content-dark"')

$contentLayout = $contentLayout.Replace('  >', ' >')
$contentLayout = $contentLayout.Replace(' >', '>')

Set-Content -Path $fileLayout -Value $contentLayout
Write-Host "Updated _Layout.cshtml"
