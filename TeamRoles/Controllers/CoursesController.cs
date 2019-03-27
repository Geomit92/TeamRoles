﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TeamRoles.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TeamRoles.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> _userManager;

        public CoursesController()
        {
            db = new ApplicationDbContext();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }
        // GET: Courses
        public ActionResult Index()
        {
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<Course> Courses = new List<Course>();
            Courses = user.Courses.ToList();
            return View(Courses);
        }

        public ActionResult Index_StudentId(string id)
        {
            ApplicationUser student = db.Users.Find(id);
            return View(student.Courses.ToList());
        }


       [Authorize(Roles = "Admin")]
        public ActionResult Admin_Index()
        {
            return View(db.Courses.ToList());
        }

        public ActionResult Index_Selected()
        {
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<ApplicationUser> teachers = new List<ApplicationUser>();
            CourseViewModel model = new CourseViewModel();
            List<Course> Courses = new List<Course>();
            List<Course> ViewCourses = new List<Course>();
            Courses = user.Courses.ToList();
            

            foreach (var course in Courses)
            {
                List<ApplicationUser> alluser = course.ApplicationUsers.ToList();
                course.ApplicationUsers.Clear();
                foreach (var us in alluser)
                {
                    var isInRole = _userManager.IsInRole(us.Id, "Teacher");
                    if (isInRole)
                    {
                        course.ApplicationUsers.Add(us);
                        model.Courses.Add(course);
                    }
                }
            }
            return View(model);
        }

        public ActionResult Index_ToSelect(string id)
        {
            ApplicationUser teacher = db.Users.Find(id);
            ApplicationUser student = db.Users.Find(User.Identity.GetUserId());
            List<Course> TeacherCourses = teacher.Courses.ToList();
            List<Course> StudentCourses = student.Courses.ToList();
            List<Course> Courses = new List<Course>();
            bool found = false;
            foreach (var tcourse in TeacherCourses)
            {
                foreach (var scourse in StudentCourses)
                {
                    if (tcourse.CourseId == scourse.CourseId)
                    {
                        found = true;
                    }
                }
                if (found==false)
                {
                    Courses.Add(tcourse);
                }
                found = false;
            }
            return View(Courses);
        }

        // GET: Courses/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // GET: Courses/Create
        [Authorize(Roles = "Teacher")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public ActionResult Create(Course course)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser teacher = db.Users.Find(User.Identity.GetUserId());
                List<Course> courses = teacher.Courses.ToList();

                foreach (var c in courses)
                {
                    if(c.CourseName == course.CourseName)
                    {
                        return RedirectToAction("Error");
                    }
                }
                course.ApplicationUsers.Add(db.Users.Find(User.Identity.GetUserId()));
                course.TeacherId = User.Identity.GetUserId();
                course.TeacherName = db.Users.Find(User.Identity.GetUserId()).UserName.ToString();
                course.CoursePic = Path.GetFileName(course.ImageFile.FileName);
                try
                {
                    db.Courses.Add(course);
                    db.SaveChanges();
                    var path = teacher.Path + "\\" + course.CourseName;
                    DirectoryInfo di = Directory.CreateDirectory(path.ToString());
                    path = teacher.Path + "\\" + course.CourseName + "\\Submits\\";
                    di = Directory.CreateDirectory(path.ToString());
                    path = teacher.Path + "\\" + course.CourseName + "\\Lectures\\";
                    di = Directory.CreateDirectory(path.ToString());
                }
                catch(Exception e)
                {
                    throw e;
                }

                
                string fileName = Path.Combine(Server.MapPath("~/Users/" + teacher.UserName + "/" + course.CourseName + "/"), course.CoursePic);
                course.ImageFile.SaveAs(fileName);

                return RedirectToAction("Index");
            }

            return View(course);
        }

        public ActionResult Join(int id)
        {
            Course course = db.Courses.Find(id);
            ApplicationUser teacher = FindTeacher(course);
            ApplicationUser student = db.Users.Find(User.Identity.GetUserId());

            GenericRequest req = new GenericRequest();
            req.User1id = teacher.Id;
            req.User2id = student.Id;
            req.Courseid = course.CourseId;
            req.Type = "JoinCourse";
            req.ApplicationUser = teacher;
            teacher.Requests.Add(req);
            db.Requests.Add(req);
            db.SaveChanges();

            return RedirectToAction("RequestSent", "Courses");
        }

        // GET: Courses/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CourseId,CourseName,CourseDescription,ImageFile")] Course course, HttpPostedFileBase ImageFile)
        {
            Course coursetoupdate = db.Courses.Find(course.CourseId);

            if (course.ImageFile != null)
            {
                course.CoursePic = Path.GetFileName(course.ImageFile.FileName);
                ApplicationUser teacher = FindTeacher(course);
                string fileName = Path.Combine(Server.MapPath("~/Users/" + teacher.UserName + "/" + course.CourseName + "/"), course.CoursePic);
                course.ImageFile.SaveAs(fileName);

                coursetoupdate.CoursePic = course.CoursePic;
            }
            
            coursetoupdate.CourseName = course.CourseName;
            coursetoupdate.CourseDescription = course.CourseDescription;

            if (ModelState.IsValid)
            {
                db.Entry(coursetoupdate).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(course);
        }

        // GET: Courses/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Course course = db.Courses.Find(id);
            TeacherController t = new TeacherController();
            ApplicationUser teacher = t.FindTeacher(id);

            try
            {
                var path = teacher.Path + "\\" + course.CourseName;
                Directory.Delete(path.ToString(), true);
                RemoveAssignments(course);
                db.Courses.Remove(course);
                db.SaveChanges();
            }
            catch(Exception e)
            {
                throw e;
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Error()
        {
            return View();
        }

        public ActionResult CourseHome(int? id)
        {
            Course course = db.Courses.Find(id);
            CourseViewModel model = new CourseViewModel();

            if(ModelState.IsValid && course!=null)
            {
                model.CourseName = course.CourseName;
                model.CoursePic = course.CoursePic;
                model.CourseId = course.CourseId;
                model.CourseDescription = course.CourseDescription;
                List<ApplicationUser> alluser = course.ApplicationUsers.ToList();
                List<Enrollment> enrollments = course.Enrollments.ToList();

                foreach (var us in alluser)
                {
                    Course list_course = new Course();
                    var isInStudentRole = _userManager.IsInRole(us.Id, "Student");
                    if (isInStudentRole)
                    {
                        list_course.ApplicationUsers.Add(us);
                        model.Courses.Add(list_course);
                       // model.Grades.Add(us.Enrollments.Where(p => p.CourseId == course.CourseId).FirstOrDefault().Grade);
                    }
                    else
                    {
                        model.Teacher = us;
                    }
                }
                return View(model);
            }
            return RedirectToAction("Index");

        }

        public ApplicationUser FindTeacher(Course course)
        {
            List<ApplicationUser> alluser = db.Courses.Where(c => c.CourseId == course.CourseId).FirstOrDefault().ApplicationUsers.ToList();
            foreach (var teacher in alluser)
            {
                var isInRole = _userManager.IsInRole(teacher.Id, "Teacher");
                if (isInRole)
                {
                    return teacher;
                }
            }
            return null;
        }

        public void RemoveAssignments(Course course)
        {
            List<Assignment> listofassignments = new List<Assignment>();
            listofassignments = db.Assignments.Where(c => c.Course.CourseId == course.CourseId).ToList();
            foreach(var assignment in listofassignments)
            {
                using (var db = new ApplicationDbContext())
                {
                    try
                    {
                        db.Assignments.Remove(assignment);
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }     
            }
        }

        public ActionResult StudentRemoveCourse(int? id)
        {
            ApplicationUser student = db.Users.Find(User.Identity.GetUserId());
            Course course = db.Courses.Find(id);
            student.Courses.Remove(course);
            try
            {
                db.Entry(student).State = EntityState.Modified;
                db.SaveChanges();
                var path = student.Path + "\\" + course.CourseName;
                Directory.Delete(path.ToString(),true);
            }
            catch(Exception e)
            {
                throw e;
            }
            return RedirectToAction("Index_Selected");
        }

        public ActionResult RequestSent()
        {
            return View();
        }

        public ActionResult CourseGrades(string coursename, string teacherid)
        {
            if(coursename!=null & teacherid!=null)
            {
                List<Course> courselist = db.Courses.Where(t => t.TeacherId == teacherid).ToList();
                Course course = courselist.Where(c => c.CourseName == coursename).SingleOrDefault();
                return View(course.Enrollments.ToList());
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
    }
}
