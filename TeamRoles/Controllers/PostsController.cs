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

namespace TeamRoles.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PostsController()
        {
            db = new ApplicationDbContext();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        // GET: Posts
        public ActionResult Index()
        {
            return View(db.Posts.ToList());
        }
        

        // GET: Posts/Create
        public ActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string PostText, string UserID)
        {
            if (!ModelState.IsValid) return View();

            Post x = new Post {PostText = PostText, PostDate = DateTime.Now}; 
            var user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            if (user != null)
            {
                x.ApplicationUser = db.Users.Find(User.Identity.GetUserId());
            }
            db.Posts.Add(x);
            db.SaveChanges();
            return RedirectToAction("Index", "Home");
        }

        // GET: Posts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = db.Posts.Find(id);
            if (post == null)
            {
                return HttpNotFound();
            }
            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PostId,PostText,PostDate")] Post post)
        {
            if (post.PostId != 0)
            {
                Post posttobeupdated = db.Posts.Find(post.PostId);
                if (post.PostText != null)
                {
                    posttobeupdated.PostText = post.PostText;
                }
                if (post.PostDate != null)
                {
                    posttobeupdated.PostDate = post.PostDate;
                }
                db.Entry(posttobeupdated).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }




            
        }

        //// GET: Posts/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Post post = db.Posts.Find(id);
        //    if (post == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(post);
        //}

        //POST: Posts/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = db.Posts.Find(id);
            if (post == null)
            {
                return HttpNotFound();
            }            
            db.Posts.Remove(post);
            db.SaveChanges();
            return RedirectToAction("Index","Home");
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
