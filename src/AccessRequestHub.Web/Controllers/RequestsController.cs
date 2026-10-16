using AccessRequestHub.Web.Data;
using AccessRequestHub.Web.Domain;
using AccessRequestHub.Web.Models;
using AccessRequestHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using static AccessRequestHub.Web.Domain.Enums;
using static AccessRequestHub.Web.Services.AppExceptions;

namespace AccessRequestHub.Web.Controllers
{
    public class RequestsController : Controller
    {
        private readonly AccessRequestService _service;
        private readonly AppDbContext _db;

        public RequestsController(AccessRequestService service, AppDbContext db)
        {
            _service = service;
            _db = db;
        }

        // ---------- My Requests ----------
        public async Task<IActionResult> Index() =>
            View(await _service.GetMyRequestsAsync(User.GetUserId()));

        // ---------- Inbox ----------
        public async Task<IActionResult> Inbox() =>
            View(await _service.GetInboxAsync(User.GetUserId()));

        // ---------- Create ----------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadApplicationsAsync();
            return View(new CreateRequestViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadApplicationsAsync();
                return View(vm);
            }

            try
            {
                var (request, created) = await _service.CreateAsync(User.GetUserId(),
                    new CreateAccessRequestInput(vm.ClientRequestId, vm.ApplicationId!.Value,
                        vm.Environment, vm.AccessLevel, vm.Justification));

                TempData["Success"] = created
                    ? "Request berhasil dikirim."
                    : "Request ini sudah pernah dikirim; menampilkan request yang sama.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }
            catch (RequestValidationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadApplicationsAsync();
                return View(vm);
            }
            catch (NotFoundException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadApplicationsAsync();
                return View(vm);
            }
        }

        // ---------- Details ----------
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var r = await _service.GetDetailAsync(User.GetUserId(), id);
                return View(new RequestDetailViewModel { Request = r, CanAct = CanAct(r) });
            }
            catch (NotFoundException) { return NotFound(); }
            catch (ForbiddenException) { return Forbid(); }
        }

        // ---------- Approve / Reject ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string rowVersion)
        {
            return await ActAsync(id, () =>
                _service.ApproveAsync(User.GetUserId(), id, Convert.FromBase64String(rowVersion)),
                "Request disetujui.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rowVersion, string? reason)
        {
            return await ActAsync(id, () =>
                _service.RejectAsync(User.GetUserId(), id, Convert.FromBase64String(rowVersion), reason),
                "Request ditolak.");
        }

        // ---------- helpers ----------
        private async Task<IActionResult> ActAsync(Guid id, Func<Task> action, string successMessage)
        {
            try
            {
                await action();
                TempData["Success"] = successMessage;
            }
            catch (RequestValidationException ex) { TempData["Error"] = ex.Message; }
            catch (ConflictException ex) { TempData["Conflict"] = ex.Message; }
            catch (ForbiddenException) { return Forbid(); }
            catch (NotFoundException) { return NotFound(); }
            catch (FormatException) { TempData["Error"] = "Data version tidak valid."; }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Hanya menentukan tampilan tombol. Keamanan sebenarnya tetap di service.
        private bool CanAct(AccessRequest r)
        {
            var me = User.GetUserId();
            if (r.RequesterId == me) return false;
            return (r.Status == RequestStatus.PendingManagerApproval && r.Requester.ManagerId == me)
                || (r.Status == RequestStatus.PendingSystemOwnerApproval && r.Application.SystemOwnerId == me);
        }

        private async Task LoadApplicationsAsync() =>
            ViewBag.Applications = new SelectList(
                await _db.Applications.AsNoTracking().OrderBy(a => a.Name).ToListAsync(), "Id", "Name");
    }
}
