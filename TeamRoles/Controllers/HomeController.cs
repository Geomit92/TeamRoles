﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using TeamRoles.Models;
using TeamRoles.Models.ViewModels;
using TeamRoles.Repositories;

namespace TeamRoles.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> _userManager;

        public HomeController()
        {
            db = new ApplicationDbContext();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        [AllowAnonymous]
        public ActionResult Home(bool? validated)
        {
//            ViewBag.Validated = validated;
            return View();
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            return View();
        }

        public ActionResult Index()
        {
            HomeIndexViewModel model = new HomeIndexViewModel();
            UserRepository repository = new UserRepository();
            model.User = db.Users.Find(User.Identity.GetUserId());
            model.Posts = db.Posts.AsEnumerable().Reverse().ToList();
            model.TotalLessons = model.User.Courses.Count();
            model.TotalStudents = repository.GetTotalStudents(model.User);
            return View(model);
        }

        public ActionResult AdminNavbar()
        {
            ViewBag.messages = db.Users.Where(m => m.Validated == false).Count();
            return PartialView("_AdminNavbar");
        }

        public ActionResult TeacherNavbar()
        {
            var userId = User.Identity.GetUserId();
            ViewBag.messages = db.Requests.Where(m => m.User1id == userId).Count();
            return PartialView("_TeacherNavbar");
        }

        public ActionResult StudentNavbar()
        {
            var userId = User.Identity.GetUserId();
            ViewBag.messages = db.Requests.Where(m => m.User2id == userId && m.Type =="ParentStudent").Count();
            return PartialView("_StudentNavbar");
        }

        public ActionResult JoinRequests()
        {
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<GenericRequest> requests = user.Requests.ToList();
            List<RequestViewModel> viewmodelrequests = Convert(requests);
            return View(viewmodelrequests);
        }

        public ActionResult AcceptParentRequests()
        {
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<GenericRequest> requests = user.Requests.ToList();
            List<RequestViewModel> viewmodelrequests = Convert(requests);
            return View(viewmodelrequests);
        }

        public ActionResult AdminRoleRequests()
        {
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            List<GenericRequest> requests = user.Requests.ToList();
            List<RequestViewModel> viewmodelrequests = Convert(requests);
            return View(viewmodelrequests);
        }

        public ActionResult Requests()
        {
            if(User.IsInRole("Teacher"))
            {
                return RedirectToAction("JoinRequests");
            }
            else if(User.IsInRole("Student"))
            {
                return RedirectToAction("AcceptParentRequests");
            }
            else if(User.IsInRole("Admin"))
            {
                return RedirectToAction("AdminRoleRequests");
            }
            return View();
        }

        public ActionResult AcceptRequest(int? id)
        {
            GenericRequest req = db.Requests.Find(id);
            UserRepository repository = new UserRepository();
            if (req.Type == "JoinCourse")
            {
                repository.AcceptJoinRequest(req);
                return RedirectToAction("JoinRequests", "Home");
            }
            else if(req.Type ==  "ParentStudent")
            {
                repository.AcceptParentRequest(req);
                return RedirectToAction("AcceptParentRequests", "Home");
            }
            else
            {
                ApplicationUser user = db.Users.Find(req.User1id);
                user.Validated = true;
                try
                {
                    db.Entry(user).State = EntityState.Modified;
                    db.Requests.Remove(req);
                    db.SaveChanges();
                    UserRepository.BuildEmailTemplate("Your Account was successfully validated!You are allowed to login!", user.Email);
                }
                catch(Exception e)
                {
                    throw e;
                }
                return RedirectToAction("AdminRoleRequests", "Home");
            }
        }

        public ActionResult DeclineRequest(int? id)
        {
            GenericRequest req = db.Requests.Find(id);
            if (req.Type == "JoinCourse")
            {
                db.Requests.Remove(req);
                db.SaveChanges();
                return RedirectToAction("JoinRequests", "Home");
            }
            else if (req.Type == "ParentStudent")
            {
                db.Requests.Remove(req);
                db.SaveChanges();
                return RedirectToAction("AcceptParentRequests", "Home");
            }
            else
            {
                ApplicationUser deleted = db.Users.Find(req.User1id);
                try
                {
                    db.Users.Remove(deleted);
                    db.Requests.Remove(req);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    throw e;
                }
                return RedirectToAction("AdminRoleRequests", "Home");
            }
        }

        public List<RequestViewModel> Convert(List<GenericRequest> requests)
        {
            List<RequestViewModel> requestlist = new List<RequestViewModel>();
            foreach(var req in requests)
            {
                RequestViewModel temp = new RequestViewModel(req.ReqId,req.User1id,req.User2id,req.Courseid,req.Type,req.Role);
                requestlist.Add(temp);
            }
            return requestlist;
        }

        public ActionResult Chat()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}