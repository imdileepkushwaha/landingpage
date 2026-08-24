using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;
using SoftflipSolutions.ViewModels;

namespace SoftflipSolutions.Controllers;

public partial class AdminController
{
    // --- Lead edit ---

    public async Task<IActionResult> EditEnquiry(int id)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry == null) return NotFound();
        return View(enquiry);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEnquiry(int id, Enquiry model)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry == null) return NotFound();

        ModelState.Remove(nameof(Enquiry.Notes));
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.CreatedAt = enquiry.CreatedAt;
            return View(model);
        }

        enquiry.Name = model.Name.Trim();
        enquiry.Email = model.Email.Trim();
        enquiry.Phone = model.Phone.Trim();
        enquiry.Requirement = model.Requirement.Trim();
        enquiry.Message = model.Message?.Trim() ?? "";
        enquiry.Status = model.Status;

        await _context.SaveChangesAsync();
        await _audit.LogAsync("EditEnquiry", LeadPipeline.LeadEnquiry, id);
        TempData["SuccessMessage"] = "Enquiry updated.";
        return RedirectToAction(nameof(EnquiryDetails), new { id });
    }

    public async Task<IActionResult> EditDemoRequest(int id)
    {
        var request = await _context.DemoRequests.FindAsync(id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDemoRequest(int id, DemoRequest model)
    {
        var request = await _context.DemoRequests.FindAsync(id);
        if (request == null) return NotFound();

        ModelState.Remove(nameof(DemoRequest.Notes));
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.CreatedAt = request.CreatedAt;
            return View(model);
        }

        request.Name = model.Name.Trim();
        request.Email = model.Email.Trim();
        request.Phone = model.Phone.Trim();
        request.CompanyName = model.CompanyName.Trim();
        request.Requirement = model.Requirement.Trim();
        request.Message = model.Message?.Trim() ?? "";
        request.Status = model.Status;

        await _context.SaveChangesAsync();
        await _audit.LogAsync("EditDemoRequest", LeadPipeline.LeadDemo, id);
        TempData["SuccessMessage"] = "Demo request updated.";
        return RedirectToAction(nameof(DemoRequestDetails), new { id });
    }

    public async Task<IActionResult> EditClientLead(int id)
    {
        var lead = await _context.ClientLeads.FindAsync(id);
        if (lead == null) return NotFound();
        ViewBag.LeadSources = await GetLeadSourcesAsync();
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClientLead(int id, ClientLead model)
    {
        var lead = await _context.ClientLeads.FindAsync(id);
        if (lead == null) return NotFound();

        ModelState.Remove(nameof(ClientLead.Notes));
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.CreatedAt = lead.CreatedAt;
            ViewBag.LeadSources = await GetLeadSourcesAsync();
            return View(model);
        }

        lead.Name = model.Name.Trim();
        lead.Mobile = model.Mobile.Trim();
        lead.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        lead.Source = model.Source.Trim();
        lead.Requirement = model.Requirement.Trim();
        lead.Budget = string.IsNullOrWhiteSpace(model.Budget) ? null : model.Budget.Trim();
        lead.Status = model.Status;

        await _context.SaveChangesAsync();
        await _audit.LogAsync("EditClientLead", LeadPipeline.LeadClient, id);
        TempData["SuccessMessage"] = "Client lead updated.";
        return RedirectToAction(nameof(ClientLeadDetails), new { id });
    }

    // --- Lead merge ---

    public async Task<IActionResult> MergeLeads()
    {
        ViewBag.Groups = await BuildMergeGroupsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeLeads(int keepId, int mergeId)
    {
        if (keepId == mergeId)
        {
            TempData["ErrorMessage"] = "Cannot merge a lead with itself.";
            return RedirectToAction(nameof(MergeLeads));
        }

        var keep = await _context.ClientLeads.Include(c => c.Notes).FirstOrDefaultAsync(c => c.Id == keepId);
        var merge = await _context.ClientLeads.Include(c => c.Notes).FirstOrDefaultAsync(c => c.Id == mergeId);
        if (keep == null || merge == null)
        {
            TempData["ErrorMessage"] = "One or both client leads not found.";
            return RedirectToAction(nameof(MergeLeads));
        }

        foreach (var note in merge.Notes)
        {
            note.ClientLeadId = keepId;
            note.NoteText = $"[Merged from #{mergeId}] {note.NoteText}";
        }

        _context.ClientLeads.Remove(merge);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("MergeClientLeads", LeadPipeline.LeadClient, keepId, $"Merged #{mergeId} into #{keepId}");
        TempData["SuccessMessage"] = $"Merged lead #{mergeId} into #{keepId}. Notes moved.";
        return RedirectToAction(nameof(ClientLeadDetails), new { id = keepId });
    }

    // --- Pipeline board ---

    public async Task<IActionResult> PipelineBoard()
    {
        var columns = new Dictionary<string, List<PipelineCardVm>>
        {
            [LeadPipeline.Pending] = new(),
            [LeadPipeline.Confirmed] = new(),
            [LeadPipeline.ProposalSent] = new(),
            [LeadPipeline.Invoiced] = new(),
            [LeadPipeline.Paid] = new(),
            [LeadPipeline.Rejected] = new()
        };

        static string? PipelineSubtitle(string? phone, string? email, string? requirement)
        {
            if (!string.IsNullOrWhiteSpace(phone)) return phone.Trim();
            if (!string.IsNullOrWhiteSpace(email)) return email.Trim();
            if (string.IsNullOrWhiteSpace(requirement)) return null;
            var r = requirement.Trim();
            return r.Length <= 42 ? r : r[..39] + "…";
        }

        foreach (var e in await _context.Enquiries.AsNoTracking().ToListAsync())
        {
            var status = NormalizePipelineStatus(e.Status);
            columns[status].Add(new PipelineCardVm
            {
                LeadType = LeadPipeline.LeadEnquiry,
                Id = e.Id,
                Name = e.Name,
                Status = status,
                DetailUrl = Url.Action(nameof(EnquiryDetails), new { id = e.Id })!,
                Subtitle = PipelineSubtitle(e.Phone, e.Email, e.Requirement)
            });
        }

        foreach (var d in await _context.DemoRequests.AsNoTracking().ToListAsync())
        {
            var status = NormalizePipelineStatus(d.Status);
            columns[status].Add(new PipelineCardVm
            {
                LeadType = LeadPipeline.LeadDemo,
                Id = d.Id,
                Name = d.Name,
                Status = status,
                DetailUrl = Url.Action(nameof(DemoRequestDetails), new { id = d.Id })!,
                Subtitle = PipelineSubtitle(d.Phone, d.Email, d.Requirement)
            });
        }

        foreach (var c in await _context.ClientLeads.AsNoTracking().ToListAsync())
        {
            var status = NormalizePipelineStatus(c.Status);
            columns[status].Add(new PipelineCardVm
            {
                LeadType = LeadPipeline.LeadClient,
                Id = c.Id,
                Name = c.Name,
                Status = status,
                DetailUrl = Url.Action(nameof(ClientLeadDetails), new { id = c.Id })!,
                Subtitle = PipelineSubtitle(c.Mobile, c.Email, c.Requirement)
            });
        }

        ViewBag.Columns = columns;
        return View();
    }

    // --- Lead tasks ---

    public async Task<IActionResult> LeadTasks(bool showDone = false)
    {
        var query = _context.LeadTasks.AsNoTracking().AsQueryable();
        if (!showDone)
            query = query.Where(t => !t.IsDone);

        var tasks = await query.OrderBy(t => t.DueAt).ThenByDescending(t => t.CreatedAt).ToListAsync();
        ViewBag.ShowDone = showDone;
        ViewBag.LeadNames = await ResolveLeadNamesAsync(tasks.Select(t => (t.LeadType, t.LeadId)).Distinct().ToList());
        return View(tasks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLeadTask(string leadType, int leadId, string title, string? assignedTo, DateTime? dueAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Task title is required.";
            return RedirectToAction(nameof(LeadTasks));
        }

        _context.LeadTasks.Add(new LeadTask
        {
            LeadType = leadType,
            LeadId = leadId,
            Title = title.Trim(),
            AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo.Trim(),
            DueAt = dueAt
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Task added.";
        return RedirectToAction(nameof(LeadTasks));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLeadTask(int id)
    {
        var task = await _context.LeadTasks.FindAsync(id);
        if (task == null) return NotFound();

        task.IsDone = true;
        task.CompletedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Task marked complete.";
        return RedirectToAction(nameof(LeadTasks));
    }

    // --- Proposal revision ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProposalRevision(int id)
    {
        var source = await _context.Proposals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (source == null) return NotFound();

        var rootId = source.ParentProposalId ?? source.Id;
        var maxVersion = await _context.Proposals
            .Where(p => p.Id == rootId || p.ParentProposalId == rootId)
            .MaxAsync(p => (int?)p.Version) ?? source.Version;

        var revision = new Proposal
        {
            LeadType = source.LeadType,
            LeadId = source.LeadId,
            Title = source.Title,
            Scope = source.Scope,
            TemplateKey = source.TemplateKey,
            ServiceCatalogId = source.ServiceCatalogId,
            SelectedModulesJson = source.SelectedModulesJson,
            Amount = source.Amount,
            ValidUntil = source.ValidUntil.AddDays(30),
            Version = maxVersion + 1,
            ParentProposalId = rootId
        };

        _context.Proposals.Add(revision);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("CreateProposalRevision", "Proposal", revision.Id, $"From #{id} v{revision.Version}");
        TempData["SuccessMessage"] = $"Proposal revision v{revision.Version} created.";
        return RedirectToAction(nameof(Proposals));
    }

    // --- HRM: leave ---

    public async Task<IActionResult> LeaveRequests()
    {
        var leaves = await _context.LeaveRequests
            .Include(l => l.Employee)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        return View(leaves);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LeaveRequests(int employeeId, string leaveType, DateTime fromDate, DateTime toDate, string? reason)
    {
        if (fromDate > toDate)
        {
            TempData["ErrorMessage"] = "From date cannot be after to date.";
            return RedirectToAction(nameof(LeaveRequests));
        }

        _context.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveType = leaveType,
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            Reason = reason?.Trim()
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Leave request submitted.";
        return RedirectToAction(nameof(LeaveRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveLeave(int id)
    {
        var leave = await _context.LeaveRequests.FindAsync(id);
        if (leave == null) return NotFound();
        leave.Status = "Approved";
        leave.ReviewedBy = User.Identity?.Name;
        leave.ReviewedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Leave approved.";
        return RedirectToAction(nameof(LeaveRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLeave(int id)
    {
        var leave = await _context.LeaveRequests.FindAsync(id);
        if (leave == null) return NotFound();
        leave.Status = "Rejected";
        leave.ReviewedBy = User.Identity?.Name;
        leave.ReviewedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Leave rejected.";
        return RedirectToAction(nameof(LeaveRequests));
    }

    // --- HRM: holidays ---

    public async Task<IActionResult> Holidays()
    {
        var holidays = await _context.CompanyHolidays.OrderBy(h => h.Date).ToListAsync();
        return View(holidays);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHoliday(string name, DateTime date, string type = "Public")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Holiday name is required.";
            return RedirectToAction(nameof(Holidays));
        }

        _context.CompanyHolidays.Add(new CompanyHoliday
        {
            Name = name.Trim(),
            Date = date.Date,
            Type = type
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Holiday added.";
        return RedirectToAction(nameof(Holidays));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var holiday = await _context.CompanyHolidays.FindAsync(id);
        if (holiday != null)
        {
            _context.CompanyHolidays.Remove(holiday);
            await _context.SaveChangesAsync();
        }
        TempData["SuccessMessage"] = "Holiday removed.";
        return RedirectToAction(nameof(Holidays));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHoliday(int id)
    {
        var holiday = await _context.CompanyHolidays.FindAsync(id);
        if (holiday == null) return NotFound();
        holiday.IsActive = !holiday.IsActive;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Holidays));
    }

    // --- HRM: attendance ---

    public async Task<IActionResult> AttendanceReport(int? month, int? year, int? employeeId)
    {
        var m = month ?? DateTime.Today.Month;
        var y = year ?? DateTime.Today.Year;
        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);

        var query = _context.AttendancePunches
            .Include(p => p.Employee)
            .Where(p => p.PunchedAt >= start && p.PunchedAt < end);

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        ViewBag.Month = m;
        ViewBag.Year = y;
        ViewBag.EmployeeId = employeeId;
        ViewBag.Employees = await _context.Employees.OrderBy(e => e.FullName).ToListAsync();
        return View(await query.OrderBy(p => p.PunchedAt).ToListAsync());
    }

    public async Task<IActionResult> AttendanceReportExport(int? month, int? year, int? employeeId)
    {
        var m = month ?? DateTime.Today.Month;
        var y = year ?? DateTime.Today.Year;
        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);

        var query = _context.AttendancePunches
            .Include(p => p.Employee)
            .Where(p => p.PunchedAt >= start && p.PunchedAt < end);

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        var punches = await query.OrderBy(p => p.PunchedAt).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance");
        ws.Cell(1, 1).Value = "Employee Code";
        ws.Cell(1, 2).Value = "Employee Name";
        ws.Cell(1, 3).Value = "Punch Type";
        ws.Cell(1, 4).Value = "Punched At";
        ws.Cell(1, 5).Value = "Notes";

        var row = 2;
        foreach (var p in punches)
        {
            ws.Cell(row, 1).Value = p.Employee?.EmployeeCode ?? "";
            ws.Cell(row, 2).Value = p.Employee?.FullName ?? "";
            ws.Cell(row, 3).Value = p.PunchType;
            ws.Cell(row, 4).Value = p.PunchedAt;
            ws.Cell(row, 5).Value = p.Notes ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Attendance_{y}_{m:D2}.xlsx");
    }

    // --- HRM: employee files ---

    public async Task<IActionResult> EmployeeFiles(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound();

        var files = await _context.EmployeeFiles
            .Where(f => f.EmployeeId == employeeId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        ViewBag.Employee = employee;
        return View(files);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmployeeFiles(int employeeId, string category, string title, IFormFile file)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound();

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            return RedirectToAction(nameof(EmployeeFiles), new { employeeId });
        }

        var dir = Path.Combine(_env.WebRootPath, "uploads", "employee-files", employeeId.ToString());
        Directory.CreateDirectory(dir);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(dir, storedName);
        await using (var fs = System.IO.File.Create(fullPath))
            await file.CopyToAsync(fs);

        _context.EmployeeFiles.Add(new EmployeeFile
        {
            EmployeeId = employeeId,
            Category = category,
            Title = string.IsNullOrWhiteSpace(title) ? file.FileName : title.Trim(),
            FilePath = $"/uploads/employee-files/{employeeId}/{storedName}",
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedBy = User.Identity?.Name
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "File uploaded.";
        return RedirectToAction(nameof(EmployeeFiles), new { employeeId });
    }

    public async Task<IActionResult> DownloadEmployeeFile(int id)
    {
        var file = await _context.EmployeeFiles.FindAsync(id);
        if (file == null) return NotFound();

        var relative = file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relative);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        return PhysicalFile(fullPath, file.ContentType ?? "application/octet-stream", Path.GetFileName(fullPath));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployeeFile(int id)
    {
        var file = await _context.EmployeeFiles.FindAsync(id);
        if (file == null) return NotFound();

        var employeeId = file.EmployeeId;
        var relative = file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relative);
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.EmployeeFiles.Remove(file);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "File deleted.";
        return RedirectToAction(nameof(EmployeeFiles), new { employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEmployeeManager(int employeeId, int? managerId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound();

        if (managerId == employeeId)
        {
            TempData["ErrorMessage"] = "Employee cannot be their own manager.";
            return RedirectToAction(nameof(EmployeeDetails), new { id = employeeId });
        }

        employee.ManagerId = managerId;
        employee.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Manager updated.";
        return RedirectToAction(nameof(EmployeeDetails), new { id = employeeId });
    }

    // --- HRM: salary & payslips ---

    public async Task<IActionResult> SalaryStructure(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound();

        var structure = await _context.SalaryStructures.FirstOrDefaultAsync(s => s.EmployeeId == employeeId)
            ?? new SalaryStructure { EmployeeId = employeeId };

        ViewBag.Employee = employee;
        return View(structure);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalaryStructure(int employeeId, decimal basic, decimal hra, decimal allowance, decimal deductions)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound();

        var structure = await _context.SalaryStructures.FirstOrDefaultAsync(s => s.EmployeeId == employeeId);
        if (structure == null)
        {
            structure = new SalaryStructure { EmployeeId = employeeId };
            _context.SalaryStructures.Add(structure);
        }

        structure.Basic = basic;
        structure.Hra = hra;
        structure.Allowance = allowance;
        structure.Deductions = deductions;
        structure.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Salary structure saved.";
        return RedirectToAction(nameof(SalaryStructure), new { employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePayslip(int employeeId, int year, int month)
    {
        var structure = await _context.SalaryStructures.FirstOrDefaultAsync(s => s.EmployeeId == employeeId);
        if (structure == null)
        {
            TempData["ErrorMessage"] = "Set salary structure first.";
            return RedirectToAction(nameof(SalaryStructure), new { employeeId });
        }

        var exists = await _context.Payslips.AnyAsync(p => p.EmployeeId == employeeId && p.Year == year && p.Month == month);
        if (exists)
        {
            TempData["ErrorMessage"] = "Payslip already exists for this month.";
            return RedirectToAction(nameof(Payslips));
        }

        _context.Payslips.Add(new Payslip
        {
            EmployeeId = employeeId,
            Year = year,
            Month = month,
            Basic = structure.Basic,
            Hra = structure.Hra,
            Allowance = structure.Allowance,
            Deductions = structure.Deductions,
            NetPay = structure.Net,
            GeneratedBy = User.Identity?.Name
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Payslip generated.";
        return RedirectToAction(nameof(Payslips));
    }

    public async Task<IActionResult> Payslips(int? employeeId)
    {
        var query = _context.Payslips.Include(p => p.Employee).AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        ViewBag.Employees = await _context.Employees.OrderBy(e => e.FullName).ToListAsync();
        ViewBag.EmployeeId = employeeId;
        return View(await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync());
    }

    // --- Sales: invoice reminders ---

    public async Task<IActionResult> InvoiceReminders()
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status == "Unpaid" || i.Status == "Partial")
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        var items = new List<InvoiceReminderVm>();
        foreach (var inv in invoices)
        {
            var contact = await GetLeadContactAsync(inv.LeadType, inv.LeadId);
            items.Add(new InvoiceReminderVm
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                LeadName = contact?.Name ?? $"Lead #{inv.LeadId}",
                Phone = contact?.Phone,
                Balance = inv.Balance,
                Status = inv.Status,
                LastReminderAt = inv.LastReminderAt
            });
        }

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvoiceReminder(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return NotFound();

        var contact = await GetLeadContactAsync(invoice.LeadType, invoice.LeadId);
        var msg = $"Reminder: Invoice {invoice.InvoiceNumber} — balance ₹{invoice.Balance:N0} pending.";
        await _notifications.NotifyAsync($"Invoice reminder: {invoice.InvoiceNumber}", msg, "Warning",
            Url.Action(nameof(Invoices))!);

        invoice.LastReminderAt = DateTime.Now;
        await _context.SaveChangesAsync();

        var digits = NormalizePhoneDigits(contact?.Phone);
        if (digits.Length >= 10)
            TempData["WhatsAppLink"] = $"https://wa.me/91{digits}?text={Uri.EscapeDataString(msg)}";

        TempData["SuccessMessage"] = "Reminder logged.";
        return RedirectToAction(nameof(InvoiceReminders));
    }

    // --- Sales: follow-up automation ---

    public async Task<IActionResult> FollowUpAutomation()
    {
        var overdue = await _context.FollowUpReminders
            .Where(f => !f.IsDone && f.DueAt < DateTime.Now)
            .OrderBy(f => f.DueAt)
            .ToListAsync();

        ViewBag.LeadNames = await ResolveLeadNamesAsync(overdue.Select(f => (f.LeadType, f.LeadId)).Distinct().ToList());
        return View(overdue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NudgeFollowUp(int id)
    {
        var reminder = await _context.FollowUpReminders.FindAsync(id);
        if (reminder == null) return NotFound();

        var name = (await ResolveLeadNamesAsync([(reminder.LeadType, reminder.LeadId)])).GetValueOrDefault((reminder.LeadType, reminder.LeadId), "Lead");
        await _notifications.NotifyAsync($"Follow-up overdue: {name}", reminder.Note, "Warning",
            LeadDetailsPath(reminder.LeadType, reminder.LeadId));

        TempData["SuccessMessage"] = "Nudge notification created.";
        return RedirectToAction(nameof(FollowUpAutomation));
    }

    // --- Sales: recurring invoices ---

    public async Task<IActionResult> RecurringInvoices()
    {
        ViewBag.Recurring = await _context.RecurringInvoices.OrderByDescending(r => r.IsActive).ThenBy(r => r.NextDueDate).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecurringInvoices(string leadType, int leadId, string title, decimal amount, string frequency, DateTime nextDueDate)
    {
        _context.RecurringInvoices.Add(new RecurringInvoice
        {
            LeadType = leadType,
            LeadId = leadId,
            Title = title.Trim(),
            Amount = amount,
            Frequency = frequency,
            NextDueDate = nextDueDate.Date
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Recurring invoice created.";
        return RedirectToAction(nameof(RecurringInvoices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRecurringInvoice(int id)
    {
        var item = await _context.RecurringInvoices.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(RecurringInvoices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDueRecurring()
    {
        var due = await _context.RecurringInvoices
            .Where(r => r.IsActive && r.NextDueDate <= DateTime.Today)
            .ToListAsync();

        var count = 0;
        foreach (var r in due)
        {
            var invoice = new Invoice
            {
                LeadType = r.LeadType,
                LeadId = r.LeadId,
                InvoiceNumber = await NextInvoiceNumberAsync(),
                Title = r.Title,
                Description = $"Recurring ({r.Frequency})",
                Amount = r.Amount,
                Status = "Unpaid"
            };
            _context.Invoices.Add(invoice);

            r.LastGeneratedAt = DateTime.Now;
            r.NextDueDate = r.Frequency switch
            {
                "Quarterly" => r.NextDueDate.AddMonths(3),
                "Yearly" => r.NextDueDate.AddYears(1),
                _ => r.NextDueDate.AddMonths(1)
            };
            count++;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = count > 0 ? $"Generated {count} invoice(s)." : "No recurring invoices due today.";
        return RedirectToAction(nameof(RecurringInvoices));
    }

    // --- System: admin users ---

    public async Task<IActionResult> AdminUsers(int? id)
    {
        var users = await _context.AdminUsers.OrderBy(u => u.Username).ToListAsync();
        ViewBag.Users = users;

        int? selectedId = id;
        if (!selectedId.HasValue && TempData["AccessAdminId"] is string accessIdRaw && int.TryParse(accessIdRaw, out var accessId))
            selectedId = accessId;
        selectedId ??= users.FirstOrDefault()?.Id;
        if (selectedId.HasValue)
        {
            var selected = users.FirstOrDefault(u => u.Id == selectedId.Value);
            if (selected != null)
            {
                ViewBag.SelectedAdmin = selected;
                await _adminAccess.EnsureDefaultsIfEmptyAsync(selected.Id, selected.Role);
                ViewBag.SelectedMenus = await _adminAccess.GetMenuKeysAsync(selected.Id);
                ViewBag.MenuCatalog = AdminMenuCatalog.All;
            }
        }

        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAdminUser(string username, string password, string role)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "Username and password are required.";
            return RedirectToAction(nameof(AdminUsers));
        }

        if (await _context.AdminUsers.AnyAsync(u => u.Username == username))
        {
            TempData["ErrorMessage"] = "Username already exists.";
            return RedirectToAction(nameof(AdminUsers));
        }

        if (!AdminRoles.All.Contains(role))
            role = AdminRoles.Sales;

        var user = new AdminUser
        {
            Username = username,
            PasswordHash = PasswordHelper.Hash(password),
            Role = role
        };
        _context.AdminUsers.Add(user);
        await _context.SaveChangesAsync();
        await _adminAccess.SetMenusAsync(user.Id,
            string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
                ? AdminMenuCatalog.AllKeys
                : AdminMenuCatalog.DefaultKeys);
        await _audit.LogAsync("AddAdminUser", "AdminUser", user.Id, username);
        TempData["SuccessMessage"] = "Admin user created. Allot menus below.";
        TempData["AccessAdminId"] = user.Id.ToString();
        return RedirectToAction(nameof(AdminUsers), new { id = user.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAdminAccess(int adminUserId, string[]? menus)
    {
        TempData["AccessAdminId"] = adminUserId.ToString();
        var user = await _context.AdminUsers.FindAsync(adminUserId);
        if (user == null) return NotFound();

        var menuList = menus ?? Array.Empty<string>();
        if (string.Equals(user.Role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            await _adminAccess.SetMenusAsync(adminUserId, AdminMenuCatalog.AllKeys);
            TempData["SuccessMessage"] = "SuperAdmin always has all menus.";
        }
        else
        {
            await _adminAccess.SetMenusAsync(adminUserId, menuList.Length > 0 ? menuList : AdminMenuCatalog.DefaultKeys);
            TempData["SuccessMessage"] = "Menu access saved for " + user.Username + ".";
        }

        await _audit.LogAsync("UpdateAdminAccess", "AdminUser", adminUserId, string.Join(",", menuList));
        return RedirectToAction(nameof(AdminUsers), new { id = adminUserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdminUser(int id)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Id == 1)
        {
            TempData["ErrorMessage"] = "Cannot deactivate the primary admin.";
            return RedirectToAction(nameof(AdminUsers));
        }
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(AdminUsers), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAdminPassword(int id, string newPassword)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null) return NotFound();
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["ErrorMessage"] = "Password is required.";
            return RedirectToAction(nameof(AdminUsers), new { id });
        }
        user.PasswordHash = PasswordHelper.Hash(newPassword);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Password reset.";
        return RedirectToAction(nameof(AdminUsers), new { id });
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // --- System: logs & notifications ---

    public async Task<IActionResult> AuditLogs()
    {
        return View(await _context.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(500).ToListAsync());
    }

    public async Task<IActionResult> Notifications()
    {
        return View(await _context.AdminNotifications.OrderByDescending(n => n.CreatedAt).Take(200).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var n = await _context.AdminNotifications.FindAsync(id);
        if (n != null)
        {
            n.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Notifications));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        await _context.AdminNotifications.Where(n => !n.IsRead).ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        TempData["SuccessMessage"] = "All notifications marked read.";
        return RedirectToAction(nameof(Notifications));
    }

    public async Task<IActionResult> EmailLogs()
    {
        return View(await _context.EmailLogs.OrderByDescending(e => e.SentAt).Take(300).ToListAsync());
    }

    // --- System: data export ---

    public IActionResult DataExport() => View();

    public async Task<IActionResult> ExportEmployeesCsv()
    {
        var rows = await _context.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Code,Name,Email,Mobile,Department,Designation,Joined,Active");
        foreach (var e in rows)
            sb.AppendLine($"{Csv(e.EmployeeCode)},{Csv(e.FullName)},{Csv(e.Email)},{Csv(e.Mobile)},{Csv(e.Department)},{Csv(e.Designation)},{e.DateOfJoining:yyyy-MM-dd},{e.IsActive}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "employees.csv");
    }

    public async Task<IActionResult> ExportInvoicesCsv()
    {
        var rows = await _context.Invoices.AsNoTracking().OrderByDescending(i => i.CreatedAt).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Number,Title,Amount,Paid,Status,Created");
        foreach (var i in rows)
            sb.AppendLine($"{Csv(i.InvoiceNumber)},{Csv(i.Title)},{i.Amount},{i.AmountPaid},{Csv(i.Status)},{i.CreatedAt:yyyy-MM-dd}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "invoices.csv");
    }

    public async Task<IActionResult> ExportLeadsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Name,Phone,Email,Requirement,Status,Created");
        foreach (var e in await _context.Enquiries.AsNoTracking().ToListAsync())
            sb.AppendLine($"Enquiry,{Csv(e.Name)},{Csv(e.Phone)},{Csv(e.Email)},{Csv(e.Requirement)},{Csv(e.Status)},{e.CreatedAt:yyyy-MM-dd}");
        foreach (var d in await _context.DemoRequests.AsNoTracking().ToListAsync())
            sb.AppendLine($"Demo,{Csv(d.Name)},{Csv(d.Phone)},{Csv(d.Email)},{Csv(d.Requirement)},{Csv(d.Status)},{d.CreatedAt:yyyy-MM-dd}");
        foreach (var c in await _context.ClientLeads.AsNoTracking().ToListAsync())
            sb.AppendLine($"ClientLead,{Csv(c.Name)},{Csv(c.Mobile)},{Csv(c.Email ?? "")},{Csv(c.Requirement)},{Csv(c.Status)},{c.CreatedAt:yyyy-MM-dd}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "leads.csv");
    }

    // --- System: WhatsApp settings ---

    public async Task<IActionResult> WhatsAppSettings()
    {
        var dict = await _context.AdminSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
        ViewBag.ApiUrl = dict.GetValueOrDefault("WhatsAppApiUrl", "");
        ViewBag.ApiToken = dict.GetValueOrDefault("WhatsAppApiToken", "");
        ViewBag.PhoneNumberId = dict.GetValueOrDefault("WhatsAppPhoneNumberId", "");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWhatsAppSettings(string apiUrl, string apiToken, string phoneNumberId)
    {
        await SaveSettingAsync("WhatsAppApiUrl", apiUrl?.Trim() ?? "");
        await SaveSettingAsync("WhatsAppApiToken", apiToken?.Trim() ?? "");
        await SaveSettingAsync("WhatsAppPhoneNumberId", phoneNumberId?.Trim() ?? "");
        TempData["SuccessMessage"] = "WhatsApp settings saved.";
        return RedirectToAction(nameof(WhatsAppSettings));
    }

    // --- Dashboard charts ---

    public async Task<IActionResult> DashboardChartsJson()
    {
        var labels = new List<string>();
        var totals = new List<decimal>();

        for (var i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            labels.Add(monthStart.ToString("MMM yyyy"));
            var total = await _context.Invoices
                .Where(inv => inv.CreatedAt >= monthStart && inv.CreatedAt < monthEnd)
                .SumAsync(inv => (decimal?)inv.Amount) ?? 0m;
            totals.Add(total);
        }

        return Json(new { labels, totals });
    }

    // --- Helpers ---

    private static string NormalizePipelineStatus(string? status) =>
        status switch
        {
            LeadPipeline.Confirmed => LeadPipeline.Confirmed,
            LeadPipeline.ProposalSent => LeadPipeline.ProposalSent,
            LeadPipeline.Invoiced => LeadPipeline.Invoiced,
            LeadPipeline.Paid => LeadPipeline.Paid,
            LeadPipeline.Rejected => LeadPipeline.Rejected,
            _ => LeadPipeline.Pending
        };

    private async Task<List<string>> GetLeadSourcesAsync()
    {
        var fromDb = await _context.ClientLeads.Select(c => c.Source).Distinct().ToListAsync();
        return DefaultLeadSources.Concat(fromDb).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
    }

    private async Task<List<MergeLeadGroupVm>> BuildMergeGroupsAsync()
    {
        var clientLeads = await _context.ClientLeads.AsNoTracking().ToListAsync();
        var enquiries = await _context.Enquiries.AsNoTracking().ToListAsync();
        var groups = new Dictionary<string, MergeLeadGroupVm>();

        void AddClient(ClientLead c, string key, string matchOn)
        {
            if (!groups.TryGetValue(key, out var g))
            {
                g = new MergeLeadGroupVm { MatchKey = key, MatchOn = matchOn };
                groups[key] = g;
            }
            g.ClientLeads.Add(new MergeLeadItemVm
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Mobile,
                Email = c.Email,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                DetailUrl = Url.Action(nameof(ClientLeadDetails), new { id = c.Id })!
            });
        }

        var byPhone = clientLeads.GroupBy(c => NormalizePhoneDigits(c.Mobile)).Where(g => g.Key.Length >= 8 && g.Count() > 1);
        foreach (var g in byPhone)
        {
            var key = $"phone:{g.Key}";
            foreach (var c in g)
                AddClient(c, key, "Phone");
        }

        var byEmail = clientLeads
            .Where(c => !string.IsNullOrWhiteSpace(c.Email))
            .GroupBy(c => c.Email!.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1);
        foreach (var g in byEmail)
        {
            var key = $"email:{g.Key}";
            foreach (var c in g)
                AddClient(c, key, "Email");
        }

        foreach (var group in groups.Values.Where(g => g.ClientLeads.Count > 1))
        {
            var phones = group.ClientLeads.Select(c => NormalizePhoneDigits(c.Phone)).Where(p => p.Length >= 8).Distinct().ToHashSet();
            var emails = group.ClientLeads.Select(c => c.Email?.Trim().ToLowerInvariant()).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToHashSet();

            foreach (var e in enquiries)
            {
                var match = (phones.Contains(NormalizePhoneDigits(e.Phone)) ||
                             (emails.Count > 0 && emails.Contains(e.Email.Trim().ToLowerInvariant())));
                if (!match) continue;
                group.RelatedEnquiries.Add(new MergeLeadItemVm
                {
                    Id = e.Id,
                    Name = e.Name,
                    Phone = e.Phone,
                    Email = e.Email,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt,
                    DetailUrl = Url.Action(nameof(EnquiryDetails), new { id = e.Id })!
                });
            }
        }

        return groups.Values.Where(g => g.ClientLeads.Count > 1).OrderByDescending(g => g.ClientLeads.Count).ToList();
    }

    private async Task SaveSettingAsync(string key, string value)
    {
        var setting = await _context.AdminSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
            _context.AdminSettings.Add(new AdminSetting { Key = key, Value = value });
        else
            setting.Value = value;
        await _context.SaveChangesAsync();
    }

    private static string Csv(string? value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
