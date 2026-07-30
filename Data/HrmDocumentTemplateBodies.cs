namespace SoftflipSolutions.Data;

public static class HrmDocumentTemplateBodies
{
    public const string Appointment =
@"APPOINTMENT LETTER

Date: {{Date}}

To,
{{EmployeeName}}
{{Address}}

Subject: Appointment as {{Designation}}

Dear {{EmployeeName}},

We are pleased to appoint you as {{Designation}} in the {{Department}} department of {{CompanyName}} with effect from {{JoiningDate}}.

1. Designation & Department
You are appointed as {{Designation}} in {{Department}}.

2. Compensation
Your stipend/salary will be ₹{{Amount}} per month, subject to applicable statutory deductions.

3. Working Hours
{{WorkingHours}}
Working Days: {{WorkingDays}}

4. Probation
You will be on probation for {{ProbationMonths}} months from the date of joining.

5. Place of Work
{{CompanyName}}
{{CompanyAddress}}

6. Notice Period
Either party may terminate this appointment by giving {{NoticeDays}} days' written notice, or payment in lieu thereof, as per company policy.

Please report at {{ReportingTime}} on {{JoiningDate}}.

We welcome you to {{CompanyName}} and wish you a successful association.

For {{CompanyName}}
{{SignatoryName}}
{{SignatoryTitle}}";

    public const string Experience =
@"EXPERIENCE CERTIFICATE

Date: {{Date}}

TO WHOMSOEVER IT MAY CONCERN

This is to certify that {{EmployeeName}} (Employee Code: {{EmployeeCode}}) worked with {{CompanyName}} as {{Designation}} in the {{Department}} department.

Period of employment: {{FromDate}} to {{ToDate}}

During the tenure, {{EmployeeName}} performed duties related to the role of {{Designation}} and maintained professional conduct.

We wish {{EmployeeName}} success in future endeavors.

For {{CompanyName}}
{{SignatoryName}}
{{SignatoryTitle}}
{{CompanyAddress}}
{{CompanyPhone}} | {{CompanyEmail}}";

    public const string Relieving =
@"RELIEVING LETTER

Date: {{Date}}

To,
{{EmployeeName}}
{{Address}}

Subject: Relieving Letter

Dear {{EmployeeName}},

This is to confirm that you have been relieved from your duties as {{Designation}} at {{CompanyName}} with effect from {{LastWorkingDate}}.

Your last working day with the organization was {{LastWorkingDate}}.

All company property, credentials, and documents entrusted to you must be returned (if not already done). Full and final settlement will be processed as per company policy.

We thank you for your association with {{CompanyName}} and wish you the best for the future.

For {{CompanyName}}
{{SignatoryName}}
{{SignatoryTitle}}";

    public const string Warning =
@"WARNING LETTER

Date: {{Date}}

To,
{{EmployeeName}}
{{Designation}} · {{Department}}
Employee Code: {{EmployeeCode}}

Subject: Warning Letter

Dear {{EmployeeName}},

This letter serves as a formal warning regarding the following matter:

{{Reason}}

You are advised to improve and adhere to company policies, discipline, and performance expectations. Any further occurrence of a similar nature may lead to stricter disciplinary action, including termination, as per company policy.

Please treat this matter with seriousness.

For {{CompanyName}}
{{SignatoryName}}
{{SignatoryTitle}}

Acknowledgement by Employee
I have read and understood this warning letter.
Name: ________________ Signature: ________________ Date: ________________";
}
