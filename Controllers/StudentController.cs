using ASPNETCore_DB.Interfaces;
using ASPNETCore_DB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASPNETCore_DB.Controllers
{
    [TypeFilter(typeof(CustomExceptionFilter))]
    public class StudentController : Controller
    {
        private readonly IStudent _studentRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public StudentController(IStudent studentRepo, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            try
            {
                _studentRepo = studentRepo;
                _httpContextAccessor = httpContextAccessor;
                _webHostEnvironment = webHostEnvironment;
            }
            catch (Exception ex)
            {
                throw new Exception("Constructor not initialized - IStudent studentRepo");
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            pageNumber = pageNumber ?? 1;
            int pageSize = 3;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["StudentNumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            ViewResult viewResult =  View();

            try
            {
                viewResult = View(PaginatedList<Student>.Create(_studentRepo.GetStudents(searchString, sortOrder).AsNoTracking(), pageNumber ?? 1, pageSize));
            }
            catch (Exception ex)
            {
                throw new Exception("No student records detected");
            }

            return viewResult;
        }

        [Authorize(Roles = "Admin,User")]
        public IActionResult Details(string id)
        {
            ViewResult viewDetail = View();
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    string userEmail = _httpContextAccessor.HttpContext.User.Identity.Name;
                    viewDetail = View(_studentRepo.ByEmail(userEmail));
                }
                else
                {
                    viewDetail = View(_studentRepo.Details(id));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Student detail not found");
            }

            return viewDetail;
        }

        [HttpGet]
        [Authorize(Roles = "User")]
        public IActionResult Create()
        {
            string userEmail = _httpContextAccessor.HttpContext.User.Identity.Name;
            Student existStudent = _studentRepo.ByEmail(userEmail);
            if (existStudent != null)
            {
                return RedirectToAction("Details", new { id = existStudent.StudentNumber });
            }
            Student student = new Student();
            string fileName = "DefaultPic.png";
            student.Photo = fileName;
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public IActionResult Create(Student student)
        {
            var files = HttpContext.Request.Form.Files;
            string webRootPath = _webHostEnvironment.WebRootPath;
            string upload = webRootPath + WebConstants.ImagePath;
            string fileName = Guid.NewGuid().ToString();
            string extension = Path.GetExtension(files[0].FileName);
            using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension),
            FileMode.Create))
            {
                files[0].CopyTo(fileStream);
            }
            student.Photo = fileName + extension;
            try
            {
                if (ModelState.IsValid)
                {
                    _studentRepo.Create(student);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Student record not saved.");
            }
            var studentExist = _studentRepo.ByEmail(this.User.Identity.Name.ToString());
            if (studentExist != null)
            {
                return RedirectToAction("Details", "Student", new { id = studentExist.StudentNumber });
            }
            else
            {
                return RedirectToAction("Create");
            }
        }

        [HttpGet]
        [Authorize(Roles = "User")]
        public IActionResult Edit(string id)
        {
            ViewResult viewDetail = View();
            try
            {
                viewDetail = View(_studentRepo.Details(id));
            }
            catch (Exception ex)
            {
                throw new Exception("Student detail not found");
            }
            return viewDetail;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public IActionResult Edit(string photoName, Student student)
        {
            if (HttpContext.Request.Form.Files.Count > 0)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;
                string upload = webRootPath + WebConstants.ImagePath;
                string fileName = Guid.NewGuid().ToString();
                string extension = Path.GetExtension(files[0].FileName);

                var oldFile = Path.Combine(upload, photoName);
                if (System.IO.File.Exists(oldFile))
                {
                    System.IO.File.Delete(oldFile);
                }

                using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                student.Photo = fileName + extension;
            }
            else
            {
                student.Photo = photoName;
            }
            try
            {
                _studentRepo.Edit(student);
            }
            catch (Exception ex)
            {
                throw new Exception("Student record not saved.");
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            ViewResult viewDetail = View();
            try
            {
                viewDetail = View(_studentRepo.Details(id));
            }
            catch (Exception ex)
            {
                throw new Exception("Student detail not found");
            }
            return viewDetail;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete([Bind("StudentNumber, FirstName, Surname, EnrollmentDate")] Student student)
        {
            try
            {
                _studentRepo.Delete(student);
            }
            catch (Exception ex)
            {
                throw new Exception("Student could not be deleted");
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
