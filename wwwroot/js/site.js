async function refreshCaptchaField(root) {
    if (!root) {
        return;
    }

    const response = await fetch('/Home/RefreshCaptcha');
    if (!response.ok) {
        return;
    }

    const data = await response.json();
    const question = root.querySelector('[data-captcha-question]');
    const token = root.querySelector('[data-captcha-token]');
    const answer = root.querySelector('[data-captcha-answer]');

    if (question) {
        question.textContent = data.question;
    }

    if (token) {
        token.value = data.token;
    }

    if (answer) {
        answer.value = '';
    }
}

function showPhoneError(input, message) {
    const field = input.closest('.phone-field');
    if (!field) {
        return;
    }

    const error = field.querySelector('.phone-error');
    input.classList.add('is-invalid');

    if (error) {
        error.textContent = message;
        error.classList.remove('d-none');
    }
}

function clearPhoneError(input) {
    const field = input.closest('.phone-field');
    if (!field) {
        return;
    }

    const error = field.querySelector('.phone-error');
    input.classList.remove('is-invalid');

    if (error) {
        error.textContent = '';
        error.classList.add('d-none');
    }
}

function getExpectedNationalDigits(iso2) {
    if (typeof intlTelInputUtils === 'undefined') {
        return null;
    }

    const example = intlTelInputUtils.getExampleNumber(
        iso2,
        true,
        intlTelInputUtils.numberType.MOBILE
    );

    if (!example) {
        return null;
    }

    return example.replace(/\D/g, '').length;
}

function sanitizePhoneInput(input) {
    const digitsOnly = input.value.replace(/\D/g, '');
    const maxLen = parseInt(input.getAttribute('maxlength') || '0', 10);
    input.value = maxLen > 0 ? digitsOnly.slice(0, maxLen) : digitsOnly;
}

function updatePhoneInputRules(input, iti) {
    const country = iti.getSelectedCountryData();
    const expectedDigits = getExpectedNationalDigits(country.iso2);

    if (expectedDigits) {
        input.setAttribute('maxlength', expectedDigits);
        input.dataset.expectedDigits = expectedDigits;
    } else {
        input.setAttribute('maxlength', '15');
        delete input.dataset.expectedDigits;
    }

    sanitizePhoneInput(input);
}

function getPhoneValidationMessage(iti) {
    const country = iti.getSelectedCountryData();
    const expectedDigits = parseInt(
        iti.getInputElement().dataset.expectedDigits || '0',
        10
    );

    if (typeof intlTelInputUtils === 'undefined') {
        if (expectedDigits) {
            return 'Enter exactly ' + expectedDigits + ' digits for ' + country.name + '.';
        }

        return 'Please enter a valid phone number for the selected country.';
    }

    const error = iti.getValidationError();

    switch (error) {
        case intlTelInputUtils.validationError.TOO_SHORT:
            return expectedDigits
                ? 'Enter exactly ' + expectedDigits + ' digits for ' + country.name + '.'
                : 'Phone number is too short for ' + country.name + '.';
        case intlTelInputUtils.validationError.TOO_LONG:
            return expectedDigits
                ? 'Enter only ' + expectedDigits + ' digits for ' + country.name + '.'
                : 'Phone number is too long for ' + country.name + '.';
        case intlTelInputUtils.validationError.INVALID_LENGTH:
            return expectedDigits
                ? 'Enter exactly ' + expectedDigits + ' digits for ' + country.name + '.'
                : 'Invalid phone number length for ' + country.name + '.';
        default:
            return expectedDigits
                ? 'Enter exactly ' + expectedDigits + ' digits for ' + country.name + '.'
                : 'Please enter a valid phone number for the selected country.';
    }
}

function initPhoneInputs() {
    if (typeof intlTelInput !== 'function') {
        return;
    }

    document.querySelectorAll('.phone-input').forEach(function (input) {
        if (input.dataset.itiInitialized === 'true') {
            return;
        }

        const iti = intlTelInput(input, {
            initialCountry: 'in',
            separateDialCode: true,
            nationalMode: true,
            strictMode: true,
            autoPlaceholder: 'aggressive',
            preferredCountries: ['in', 'us', 'gb', 'ae', 'ca', 'au'],
            utilsScript: 'https://cdn.jsdelivr.net/npm/intl-tel-input@24.6.0/build/js/utils.js'
        });

        input.dataset.itiInitialized = 'true';
        updatePhoneInputRules(input, iti);

        input.addEventListener('countrychange', function () {
            clearPhoneError(input);
            input.value = '';
            updatePhoneInputRules(input, iti);
        });

        input.addEventListener('input', function () {
            sanitizePhoneInput(input);
            clearPhoneError(input);
        });

        input.addEventListener('paste', function (event) {
            event.preventDefault();
            const pastedText = (event.clipboardData || window.clipboardData).getData('text') || '';
            input.value = pastedText.replace(/\D/g, '');
            sanitizePhoneInput(input);
            clearPhoneError(input);
        });
    });
}

function setFormSubmitting(form, isSubmitting) {
    const submitButton = form.querySelector('[type="submit"]');
    if (!submitButton) {
        return;
    }

    if (isSubmitting) {
        if (!submitButton.dataset.originalHtml) {
            submitButton.dataset.originalHtml = submitButton.innerHTML;
        }

        submitButton.disabled = true;
        submitButton.innerHTML =
            '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Sending...';
        form.classList.add('is-submitting');
        return;
    }

    form.classList.remove('is-submitting');
    submitButton.disabled = false;

    if (submitButton.dataset.originalHtml) {
        submitButton.innerHTML = submitButton.dataset.originalHtml;
    }
}

function resetLeadFormSelects(form) {
    form.querySelectorAll('select').forEach(function (select) {
        const placeholder = select.querySelector('option[disabled]');
        select.value = placeholder ? '' : select.options[0].value;
    });
}

function clearLeadForm(form) {
    if (!form) {
        return;
    }

    form.reset();
    resetLeadFormSelects(form);

    form.querySelectorAll('[data-captcha-answer]').forEach(function (input) {
        input.value = '';
    });

    form.querySelectorAll('.phone-input').forEach(function (input) {
        input.value = '';

        if (typeof intlTelInput === 'function') {
            const iti = intlTelInput.getInstance(input);
            if (iti) {
                iti.setCountry('in');
                updatePhoneInputRules(input, iti);
            }
        }

        clearPhoneError(input);
    });

    form.querySelectorAll('[data-captcha-root]').forEach(function (root) {
        refreshCaptchaField(root);
    });

    setFormSubmitting(form, false);
}

function clearLeadForms(formType) {
    if (formType) {
        clearLeadForm(document.querySelector('.lead-form[data-form-type="' + formType + '"]'));
    } else {
        document.querySelectorAll('.lead-form').forEach(clearLeadForm);
    }

    const demoModal = document.getElementById('demoRequestModal');
    if (demoModal && window.bootstrap) {
        const modalInstance = bootstrap.Modal.getInstance(demoModal);
        if (modalInstance) {
            modalInstance.hide();
        }
    }
}

function validateLeadFormPhone(form) {
    const phoneInput = form.querySelector('.phone-input');
    if (!phoneInput || typeof intlTelInput !== 'function') {
        return true;
    }

    const iti = intlTelInput.getInstance(phoneInput);
    if (!iti) {
        return true;
    }

    clearPhoneError(phoneInput);
    sanitizePhoneInput(phoneInput);

    const expectedDigits = parseInt(phoneInput.dataset.expectedDigits || '0', 10);
    const enteredDigits = phoneInput.value.replace(/\D/g, '');

    if (expectedDigits && enteredDigits.length !== expectedDigits) {
        showPhoneError(
            phoneInput,
            'Enter exactly ' + expectedDigits + ' digits for ' + iti.getSelectedCountryData().name + '.'
        );
        phoneInput.focus();
        return false;
    }

    if (!iti.isValidNumber()) {
        showPhoneError(phoneInput, getPhoneValidationMessage(iti));
        phoneInput.focus();
        return false;
    }

    phoneInput.value = iti.getNumber();
    return true;
}

function initLeadFormValidation() {
    document.querySelectorAll('.lead-form').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (form.classList.contains('is-submitting')) {
                event.preventDefault();
                return;
            }

            if (!validateLeadFormPhone(form)) {
                event.preventDefault();
                return;
            }

            setFormSubmitting(form, true);
        });
    });
}

document.addEventListener('click', function (event) {
    const refreshButton = event.target.closest('.captcha-refresh-btn');
    if (!refreshButton) {
        return;
    }

    event.preventDefault();
    refreshCaptchaField(refreshButton.closest('[data-captcha-root]'));
});

document.addEventListener('DOMContentLoaded', function () {
    initPhoneInputs();
    initLeadFormValidation();

    const demoModal = document.getElementById('demoRequestModal');
    if (demoModal) {
        demoModal.addEventListener('show.bs.modal', function () {
            refreshCaptchaField(demoModal.querySelector('[data-captcha-root="demo-modal"]'));
        });
    }

    const successAlert = document.querySelector('[data-form-success="true"]');
    if (successAlert) {
        clearLeadForms(successAlert.getAttribute('data-submitted-form') || '');
    }
});
