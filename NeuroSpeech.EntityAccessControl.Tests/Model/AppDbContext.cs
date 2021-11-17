using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    public class AppDbContext: BaseDbContext<AppDbContext>
    {
        public long UserID;

        public AppDbContext(DbContextOptions<AppDbContext> options, DbContextEvents<AppDbContext> events)
            : base(options, events)
        {

        }

        public DbSet<Post> Posts { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PostContentTag>().HasKey(x => new
            {
                x.PostContentID,
                x.Name
            });
            modelBuilder.Entity<PostTag>().HasKey(x => new { 
                x.PostID,
                x.Name
            });
        }
    }

    [Table("Accounts")]
    public class Account
    {
        public long AccountID { get; set; }
    }

    [Table("Posts")]
    public class Post
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostID { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }


        public long AuthorID { get; set; }

        [ForeignKey(nameof(AuthorID))]
        public Account Author { get; set; }

        public ICollection<PostTag> Tags { get; set; }

        public ICollection<PostContent> Contents { get; set; }

    }

    [Table("PostTags")]
    public class PostTag
    {
        public long PostID { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }

        [ForeignKey(nameof(Name))]
        public Tag Tag { get; set; }
    }


    [Table("PostContents")]
    public class PostContent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostContentID { get; set; }

        public long PostID { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public ICollection<PostContentTag> Tags { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }
    }

    [Table("PostContentTags")]
    public class PostContentTag
    {
        public long PostContentID { get; set; }        

        [MaxLength(100)]
        public string Name { get; set; }

        [ForeignKey(nameof(PostContentID))]
        public PostContent PostContent { get; set; }

        [ForeignKey(nameof(Name))]
        public Tag Tag { get; set; }
    }

    [Table("Tags")]
    public class Tag
    {
        [Key, MaxLength(100)]
        public string Name { get; set; }
    }

}
