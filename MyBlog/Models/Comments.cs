using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyBlog.Models
{
    public class Comments
    {
        public int Id { get; set; }

        public int postId { get; set; }

        public int AuthorId { get; set; }

        public virtual ApplicationUser Author { get; set; }

        public string Body { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Updated { get; set; }

        public string UpdateReason { get; set; }


    }

}