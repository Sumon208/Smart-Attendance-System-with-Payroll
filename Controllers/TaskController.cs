using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Services.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "Employee")]
    public class TaskController : Controller
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ApplicationDbContext _context;

        public TaskController(
            ITaskRepository taskRepository,
            IAdminRepository adminRepository,
            IEmployeeRepository employeeRepo,
            ApplicationDbContext context)
        {
            _taskRepository = taskRepository;
            _adminRepository = adminRepository;
            _employeeRepo = employeeRepo;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Fetch all tasks and eagerly load related data
            var tasks = await _taskRepository.GetAllTasksAsync();

            // Populate Statuses dropdown from EnumValue table
            ViewBag.Statuses = _context.Enum
                                       .Where(e => e.EnumType == "Status")
                                       .OrderBy(e => e.Name)
                                       .Select(e => e.Name)
                                       .ToList();

            // Populate Projects dropdown from EnumValue table
            ViewBag.Projects = _context.Enum
                                       .Where(e => e.EnumType == "Project")
                                       .OrderBy(e => e.Name)
                                       .Select(e => e.Name)
                                       .ToList();

            // Populate Shifts dropdown if needed
            ViewBag.Shifts = _context.Enum
                                     .Where(e => e.EnumType == "Shift")
                                     .OrderBy(e => e.Name)
                                     .Select(e => e.Name)
                                     .ToList();

            // Ensure tasks is not null to avoid view errors
            return View(tasks ?? new List<EmployeeTask>());
        }


        // GET: Task/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var empIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

            var model = new EmployeeTaskViewModel
            {
                ShiftList = await GetSelectList("Shift"),
                ProjectList = await GetSelectList("Project"),
                StatusList = await GetSelectList("Status"),
                EmployeeId = string.IsNullOrEmpty(empIdClaim) ? 0 : Convert.ToInt32(empIdClaim)
            };

            ViewBag.LoggedInEmployeeName = User.Identity?.Name;
            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> Create(EmployeeTaskViewModel model)
        {
            try
            {
                // ✅ Claim থেকে EmployeeId পড়ছি (NameIdentifier ব্যবহার করে)
                var nameIdentifier = User.Claims.FirstOrDefault(c =>
                    c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(nameIdentifier))
                {
                    model.EmployeeId = int.Parse(nameIdentifier);
                }

                // ✅ final চেক
                if (model.EmployeeId == 0)
                {
                    throw new Exception("EmployeeId পাওয়া যায়নি। অনুগ্রহ করে আবার লগইন করুন।");
                }

                // Save data
                await _taskRepository.AddTaskAsync(model);

                TempData["SuccessMessage"] = "Employee Task created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the task: {ex.Message}";
            }

            // repopulate dropdowns
            model.ShiftList = await GetSelectList("Shift");
            model.ProjectList = await GetSelectList("Project");
            model.StatusList = await GetSelectList("Status");

            return View(model);
        }






        private async Task<List<SelectListItem>> GetSelectList(string enumType)
        {
            return await _context.Enum
                                 .Where(e => e.EnumType == enumType)
                                 .Select(e => new SelectListItem
                                 {
                                     Value = e.Id.ToString(),  // Dropdown value হবে EnumValue.Id
                                     Text = e.Name             // Dropdown text হবে EnumValue.Name
                                 })
                                 .ToListAsync();
        }


        // GET: Task/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            await PopulateDropdownsAsync();
            return View(task);
        }

        // POST: Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeTask task)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(task);
            }

            await _taskRepository.UpdateTaskAsync(task);
            return RedirectToAction(nameof(Index));
        }

        // POST: Task/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _taskRepository.DeleteTaskAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Helper method to populate dropdowns dynamically from EnumValue table
        private async Task PopulateDropdownsAsync()
        {
            // ... (existing employee dropdown code)

            var allEnums = await _context.Enum
                                         .Where(e => new[] { "Shift", "Project", "Status" }.Contains(e.EnumType))
                                         .ToListAsync();

            List<SelectListItem> GetSelectList(string type) =>
                allEnums
                    .Where(e => e.EnumType == type)
                    .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Name })
                    .ToList();

            ViewBag.Shifts = GetSelectList("Shift");
            ViewBag.Projects = GetSelectList("Project");
            ViewBag.Statuses = GetSelectList("Status");
        }

    }
}
