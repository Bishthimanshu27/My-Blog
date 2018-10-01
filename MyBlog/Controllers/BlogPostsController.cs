using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MyBlog.Helpers;
using MyBlog.Models;
using PagedList;
using PagedList.Mvc;

namespace MyBlog.Controllers
{
    public class BlogPostsController    : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: BlogPosts
        public ActionResult Index(int? page , string searchString)
        {
            int pageSize = 3; // display three blog posts at a time on this page
            int pageNumber = (page ?? 1);

            var postQuery = db.Posts.OrderBy(p => p.Created).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                postQuery = postQuery
                    .Where(p => p.Title.Contains(searchString) ||
                                p.Body.Contains(searchString) ||
                                p.Slug.Contains(searchString) ||
                                p.Comments.Any(t => t.Body.Contains(searchString))
                           ).AsQueryable();
            }

            var postList = postQuery.ToPagedList(pageNumber, pageSize);

            ViewBag.SearchString = searchString;
            return View(postList);
        }

        public ActionResult Details(int ? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        public ActionResult DetailsSlug(string slug)
        {
            if (slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts
                .Include(p => p.Comments.Select(t => t.Author))
                .Where(p => p.Slug == slug)
                .OrderBy(p => p.Id)
                .FirstOrDefault();
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View("Details", blogPost);
        }

        [HttpPost]
        public ActionResult DetailsSlug(string slug,string body )
        {
            if (slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var blogPost = db.Posts
                .Where(p => p.Slug == slug)
                .FirstOrDefault();
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                ViewBag.ErrorMessage = "Comment is required";
                return View("Details", blogPost);
            }


            var comment = new Comments();
            comment.AuthorId = User.Identity.GetUserId();
            comment.BlogPostId = blogPost.Id;
            comment.Created = DateTime.Now;
            comment.Body = body;

            db.Comments.Add(comment);
            db.SaveChanges();

            return RedirectToAction("DetailsSlug", new { slug = slug });
        }

        // GET: BlogPosts/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

       

        // GET: BlogPosts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Created,Updated,Title,Slug,Body,MediaURL,Published")] BlogPost blogPost, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var Slug = StringUtilites.URLFriendly(blogPost.Title);
                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var fileName = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                    blogPost.MediaUrl = "/Uploads/" + fileName;
                }
                if (String.IsNullOrWhiteSpace(Slug))
                {
                    ModelState.AddModelError("Title", "Invalid title");
                    return View(blogPost);
                }
                if (db.Posts.Any(p => p.Slug == Slug))
                {
                    ModelState.AddModelError("Title", "The title must be unique");
                    return View(blogPost);
                }


                blogPost.Slug = Slug;
                blogPost.Created = DateTimeOffset.Now;
                db.Posts.Add(blogPost);
                db.Entry(blogPost).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create([Bind(Include = "Id,Title,Body,MediaURL,Published")] BlogPost blogPost, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var Slug = StringUtilites.URLFriendly(blogPost.Title);
                if (String.IsNullOrWhiteSpace(Slug))
                {
                    ModelState.AddModelError("Title", "Invalid title");
                    return View(blogPost);
                }
                if (db.Posts.Any(p => p.Slug == Slug))
                {
                    ModelState.AddModelError("Title", "The title must be unique");
                    return View(blogPost);
                }
                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var fileName = Path.GetFileName(image.FileName);
                    image.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), fileName));
                    blogPost.MediaUrl = "/Uploads/" + fileName;
                }



                blogPost.Slug = Slug;
                blogPost.Created = DateTimeOffset.Now;
                db.Posts.Add(blogPost);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(blogPost);
        }


        // GET: BlogPosts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BlogPost blogPost = db.Posts.Find(id);
            if (blogPost == null)
            {
                return HttpNotFound();
            }
            return View(blogPost);
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            BlogPost blogPost = db.Posts.Find(id);
            db.Posts.Remove(blogPost);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public ActionResult CreateComment(string slug, string body)
        {
            if (slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var blogPost = db.Posts
                .Where(p => p.Slug == slug)
                .FirstOrDefault();

            if (blogPost == null)
            {
                return HttpNotFound();
            }

            var comment = new Comments();
            comment.AuthorId = User.Identity.GetUserId();
            comment.BlogPostId = blogPost.Id;
            comment.Created = DateTime.Now;
            comment.Body = body;

            db.Comments.Add(comment);
            db.SaveChanges();

            return RedirectToAction("DetailsSlug", new { slug = slug });
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
